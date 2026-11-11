//! 新普通类在定义者CFG内的冷事务。静态ClassId不产生公开模块槽，每次执行均分配新frame/cell。
//! body表达式的arena owner来自HIR明示信息；Finish成功才填原cell，再装饰并一次发布。
use crate::build::FunctionBuilder;
use crate::mir::*;
use ryupy_hir::{ClassBase, ClassId};

impl FunctionBuilder<'_> {
    pub(crate) fn build_lexical_class_def(&mut self, id: ClassId) {
        let class = &self.hir.classes[id];
        let Some(definition) = &class.lexical else {
            return;
        };
        if definition.owner != Some(self.current_hir_fn) {
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(self.hir.file, class.range),
                    "词法类表达式arena owner与定义函数不一致",
                )
                .with_code("E0300"),
            );
            return;
        }
        let Some(target) = definition.target.clone() else {
            return;
        };
        let name = class.name.to_string();
        let qualname = definition.qualname.clone();
        let items = class.body.clone();
        let annotations = class.has_annotations;
        let base = class.base;
        let decorators = self.prepare_decorators(&class.decorators.clone());
        if let Some(state) = self.module_state {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceClassGuard),
                args: vec![Operand::Local(state)],
            }));
        }
        let context = self.expr_builder().capture_definition_context();
        let bases = match base {
            Some(ClassBase::Runtime(value)) => vec![self.freeze_expression(value)],
            None => Vec::new(),
            _ => {
                self.diagnostics.push(
                    ryupy_base::Diagnostic::error(
                        ryupy_base::Span::new(self.hir.file, class.range),
                        "词法类不能复用旧静态基类/专用布局路径",
                    )
                    .with_code("E0300"),
                );
                return;
            }
        };
        let outer_state = self.module_state;
        if let Some(Operand::Local(context)) = context {
            self.module_state = Some(context);
        }
        // 真实定义模块的当前__name__，不是编译canonical名或调用者ambient元数据。
        let module_name = self
            .hir
            .vars
            .iter()
            .find_map(|(id, value)| (value.name.as_str() == "__name__").then_some(id));
        let Some(module_name) = module_name else {
            self.module_state = outer_state;
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(self.hir.file, class.range),
                    "词法类缺少定义模块__name__绑定来源",
                )
                .with_code("E0300"),
            );
            return;
        };
        let module = self.expr_builder().build_module_name(module_name);
        let module_type =
            if self.module_state.is_some() || self.types.is_dynamic(self.current_hir_fn) {
                MirType::Dynamic
            } else {
                self.globals[self.module_vars[&module_name]].ty.clone()
            };
        let module = self.coerce_mir(module, &module_type, &MirType::Dynamic);
        let namespace = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: namespace,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::LexicalClassPrepare),
                args: vec![
                    Operand::Const(Constant::Str(name)),
                    Operand::Const(Constant::Str(qualname)),
                    module,
                    Operand::Array(bases),
                ],
            },
        });
        let original = self.alloc_temp(MirType::Dynamic);
        let cell = self.begin_class_cell(id, original);
        if let Some(cell) = cell {
            self.lexical_cell_stack.push((id, Operand::Local(cell)));
        }
        let outer_namespace = self.class_namespace.replace(Operand::Local(namespace));
        let outer_kind = std::mem::replace(&mut self.lexical_namespace, true);
        let outer_cell = std::mem::replace(
            &mut self.class_cell,
            cell.map(|cell| (id, Operand::Local(cell))),
        );
        if annotations {
            let values =
                self.expr_builder()
                    .runtime_call(RuntimeFn::MakeDict, vec![], MirType::Dynamic);
            self.emit_set_attr(Operand::Local(namespace), "__annotations__", values);
        }
        self.build_class_items(&items, Operand::Local(namespace), false);
        self.class_namespace = outer_namespace;
        self.lexical_namespace = outer_kind;
        self.class_cell = outer_cell;
        if cell.is_some() {
            self.lexical_cell_stack.pop();
        }
        self.module_state = outer_state;
        self.push_stmt(Statement::Assign {
            dest: original,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::LexicalClassFinish),
                args: vec![Operand::Local(namespace)],
            },
        });
        if let Some(cell) = cell {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceClassCellGuard),
                args: vec![Operand::Local(original)],
            }));
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CellSet),
                args: vec![Operand::Local(cell), Operand::Local(original)],
            }));
        }
        let value = self.apply_decorators(Operand::Local(original), decorators, false);
        if let (Some(cell), Operand::Local(published)) = (cell, &value)
            && let Some((_, plan)) = self
                .class_cells
                .iter_mut()
                .find(|(_, plan)| plan.cell == cell)
        {
            plan.completion = self.current_bb;
            plan.published_class = *published;
        }
        self.publish_lexical_class(target, value, namespace, cell);
    }
}
