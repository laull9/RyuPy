//! `class` 语句的 MIR 构建（M2.5 / M2.6）。
//!
//! Python 的 `class` 是**运行时语句**：先求值基类，再按顺序执行类体（属性赋值、
//! 方法定义、表达式副作用），最后得到类对象。这里不把类体提到任何“编译期常量”里，
//! 而是在模块入口的 `class` 位置生成：
//!
//! ```text
//! class_global = MakeClass(name, bases, exception_factory)
//! SetAttr(class_global, "attr", <表达式>)      // 按源码顺序
//! defaults_of(method)                          // def 执行时求值默认值
//! SetAttr(class_global, "method", Decorate(.., <PyFunction>))
//! ```
//!
//! 异常类多一个工厂委托参数：codegen 为每个用户异常类生成一个 CLR 子类，
//! 运行时通过委托创建它，从而保持 CLR 与 Python 继承树同构（`except ValueError`
//! 才能直接映射成类型化 catch）。

use ryupy_hir::{ClassBase, ClassItemKind};

use crate::build::FunctionBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

impl FunctionBuilder<'_> {
    /// 构建一条 `class` 语句。
    pub(crate) fn build_class_def(&mut self, cls_id: ryupy_hir::ClassId) {
        if self.hir.classes[cls_id].lexical.is_some() {
            self.build_lexical_class_def(cls_id);
            return;
        }
        let class_global = self.hir_class_globals.get(&cls_id).copied();
        let cls = &self.hir.classes[cls_id];
        let name = cls.name.as_str().to_owned();
        let base = cls.base;
        let is_exception = cls.is_exception;
        let items = cls.body.clone();
        let mir_cls = self.class_map[&cls_id];
        let decorated_binding = cls.decorated_binding;
        let frozen = cls.frozen;
        let enum_members = cls.enum_members.clone();
        let fields = cls
            .declared_fields
            .iter()
            .map(|name| Operand::Const(Constant::Str(name.as_str().to_owned())))
            .collect();
        let decorators = if decorated_binding.is_some() || cls.runtime_dataclass {
            cls.decorators.clone()
        } else {
            vec![]
        };
        let decorators = self.prepare_decorators(&decorators);
        // LOAD_BUILD_CLASS先于类体MAKE_FUNCTION，二者都先于bases副作用。
        if let Some(state) = self.module_state {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceClassGuard),
                args: vec![Operand::Local(state)],
            }));
        }
        let class_context = self.expr_builder().capture_definition_context();

        // 1. 基类表达式先于类体求值（与 CPython 的 `__build_class__` 顺序一致）。
        let bases = match base {
            Some(ClassBase::User(base_id)) => match self.hir_class_globals.get(&base_id) {
                Some(global) => vec![Operand::Global(*global)],
                None => Vec::new(),
            },
            Some(ClassBase::Builtin(type_name)) => {
                let type_object = self.expr_builder().runtime_call(
                    RuntimeFn::TypeObject,
                    vec![Operand::Const(Constant::Str(type_name.to_owned()))],
                    MirType::Dynamic,
                );
                vec![type_object]
            }
            Some(
                ClassBase::Expr(expr)
                | ClassBase::Native { value: expr, .. }
                | ClassBase::TypedDict { value: expr, .. },
            ) => {
                vec![self.freeze_expression(expr)]
            }
            Some(ClassBase::Runtime(expr)) => {
                let value = self.freeze_expression(expr);
                vec![self.expr_builder().runtime_call(
                    RuntimeFn::ValidateOrdinaryBase,
                    vec![value],
                    MirType::Dynamic,
                )]
            }
            None => Vec::new(),
        };

        // 2. 创建类对象。异常类的 factory 由 `__init_class_factories` 提前写入静态槽位。
        let factory = if is_exception {
            match self.class_factories.get(&mir_cls).copied() {
                Some(global) => Operand::Global(global),
                None => Operand::Const(Constant::Null),
            }
        } else {
            Operand::Const(Constant::Null)
        };
        let dest = self.alloc_temp(MirType::Dynamic);
        let (constructor, args) = if let Some(context) = &class_context {
            (
                RuntimeFn::InstanceMakeClass,
                vec![
                    context.clone(),
                    Operand::Const(Constant::Str(name)),
                    Operand::Array(bases),
                ],
            )
        } else if let Some(ClassBase::TypedDict { total, .. }) = base {
            (
                RuntimeFn::TypedDictBegin,
                vec![
                    Operand::Const(Constant::Str(name)),
                    bases[0].clone(),
                    Operand::Const(Constant::Bool(total)),
                    Operand::Const(Constant::Str(self.hir.info.name.clone())),
                ],
            )
        } else {
            (
                if enum_members.is_some() {
                    RuntimeFn::EnumCreate
                } else {
                    RuntimeFn::MakeClass
                },
                vec![
                    Operand::Const(Constant::Str(name)),
                    Operand::Array(bases),
                    factory,
                ],
            )
        };
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(constructor),
                args,
            },
        });
        if let Some(global) = class_global {
            self.push_stmt(Statement::SetGlobal {
                dest: global,
                value: Operand::Local(dest),
            });
        }
        let class_object = Operand::Local(dest);
        let class_cell = self.begin_class_cell(cls_id, dest);
        if let Some(cell) = class_cell {
            self.lexical_cell_stack.push((cls_id, Operand::Local(cell)));
        }

        let previous_state = self.module_state;
        if let Some(Operand::Local(context)) = class_context {
            self.module_state = Some(context);
        }
        let previous_namespace = self.class_namespace.replace(class_object.clone());
        let previous_cell = std::mem::replace(
            &mut self.class_cell,
            class_cell.map(|cell| (cls_id, Operand::Local(cell))),
        );
        if cls.has_annotations {
            let annotations =
                self.expr_builder()
                    .runtime_call(RuntimeFn::MakeDict, vec![], MirType::Dynamic);
            self.emit_set_attr(class_object.clone(), "__annotations__", annotations);
        }
        // 3. 按源码顺序执行类体。
        let conditional = items
            .iter()
            .any(|item| matches!(item.kind, ClassItemKind::If { .. }));
        self.build_class_items(
            &items,
            class_object.clone(),
            matches!(base, Some(ClassBase::TypedDict { .. })),
        );
        self.class_namespace = previous_namespace;
        self.class_cell = previous_cell;
        if class_cell.is_some() {
            self.lexical_cell_stack.pop();
        }
        self.module_state = previous_state;
        // 类体最终namespace完成后才处理隐式new绑定；类decorator仍在其后。
        // 专用Enum/TypedDict/异常构造协议不走普通类终结；dataclass装饰前仍普通。
        if !is_exception
            && enum_members.is_none()
            && !matches!(base, Some(ClassBase::TypedDict { .. }))
        {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::FinalizeOrdinaryClass),
                args: vec![class_object.clone()],
            }));
        }
        // 类体和普通类终结均成功后才填入原类；任何 class decorator 都在此之后。
        if let Some(cell) = class_cell {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceClassCellGuard),
                args: vec![class_object.clone()],
            }));
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CellSet),
                args: vec![Operand::Local(cell), class_object.clone()],
            }));
        }
        if !conditional
            && !matches!(base, Some(ClassBase::TypedDict { .. }))
            && !self.hir.classes[cls_id].declared_fields.is_empty()
        {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DeclareDataclass),
                args: vec![
                    class_object.clone(),
                    Operand::Array(fields),
                    Operand::Const(Constant::Bool(frozen)),
                ],
            }));
        }
        let class_object = if let Some(names) = enum_members {
            self.expr_builder().runtime_call(
                RuntimeFn::EnumFinalize,
                vec![
                    class_object,
                    Operand::Array(
                        names
                            .into_iter()
                            .map(|name| Operand::Const(Constant::Str(name.as_str().to_owned())))
                            .collect(),
                    ),
                ],
                MirType::Dynamic,
            )
        } else {
            class_object
        };
        let class_object = if matches!(base, Some(ClassBase::TypedDict { .. })) {
            self.expr_builder().runtime_call(
                RuntimeFn::TypedDictFinish,
                vec![class_object],
                MirType::Dynamic,
            )
        } else {
            class_object
        };
        let value = self.apply_decorators(class_object, decorators, frozen);
        // 记录实际发布接点，不用函数Return猜类创建成功；异常路径仍可带空cell逃逸。
        if let (Some(cell), Operand::Local(published)) = (class_cell, &value)
            && let Some((_, plan)) = self
                .class_cells
                .iter_mut()
                .find(|(_, plan)| plan.cell == cell)
        {
            plan.completion = self.current_bb;
            plan.published_class = *published;
        }
        if let Some(binding) = decorated_binding {
            self.store_binding(
                ryupy_hir::Binding::ModuleVar(binding),
                value,
                &MirType::Dynamic,
            );
        }
    }

    /// 类体 if 不创建作用域，只把实际分支接入当前方法的普通 CFG。
    pub(crate) fn build_class_items(
        &mut self,
        items: &[ryupy_hir::ClassItem],
        class_object: Operand,
        typed_dict: bool,
    ) {
        for (index, item) in items.iter().cloned().enumerate() {
            match item.kind {
                ClassItemKind::NestedClass(class) => self.build_class_def(class),
                ClassItemKind::If { test, body, orelse } => {
                    let raw = self.expr_builder().build_expr(test);
                    let ty = self.expr_builder().ty_of(test);
                    let cond = self.expr_builder().to_bool_operand(raw, &ty);
                    let yes = self.new_block();
                    let no = self.new_block();
                    let join = self.new_block();
                    self.terminate_current(crate::mir::Terminator::Branch {
                        cond,
                        then_: yes,
                        else_: no,
                    });
                    self.current_bb = yes;
                    self.build_class_items(&body, class_object.clone(), false);
                    self.terminate_current(crate::mir::Terminator::Goto(join));
                    self.current_bb = no;
                    self.build_class_items(&orelse, class_object.clone(), false);
                    self.terminate_current(crate::mir::Terminator::Goto(join));
                    self.current_bb = join;
                }
                ClassItemKind::Annotation { name, value } => {
                    let value = self.freeze_expression(value);
                    let getter = if self.lexical_namespace {
                        RuntimeFn::LexicalNamespaceGet
                    } else {
                        RuntimeFn::ClassNamespaceGet
                    };
                    let mapping = self.expr_builder().runtime_call(
                        getter,
                        vec![
                            class_object.clone(),
                            Operand::Const(Constant::Str("__annotations__".into())),
                        ],
                        MirType::Dynamic,
                    );
                    self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::SetItem),
                        args: vec![
                            mapping,
                            Operand::Const(Constant::Str(name.as_str().to_owned())),
                            value,
                        ],
                    }));
                }
                ClassItemKind::Attr { name, value } => {
                    let raw = self.expr_builder().build_expr(value);
                    let ty = self.expr_builder().ty_of(value);
                    let value = self.expr_builder().coerce(raw, &ty, &MirType::Dynamic);
                    self.emit_set_attr(class_object.clone(), name.as_str(), value);
                }
                ClassItemKind::Expr(value) => {
                    let value = self.expr_builder().build_expr(value);
                    if index == 0 && typed_dict {
                        self.emit_set_attr(class_object.clone(), "__doc__", value);
                    }
                }
                ClassItemKind::Method { func, decorators } => {
                    // 装饰器先求值，再求默认值/创建原始函数，最后逆序应用。
                    let prepared = self.prepare_decorators(&decorators);
                    let value = self.create_function(func);
                    let value = self.apply_decorators(value, prepared, false);
                    let method_name = self.hir.functions[func].name.as_str().to_owned();
                    self.emit_set_attr(class_object.clone(), &method_name, value);
                }
            }
        }
    }

    /// 写类属性。属性值是动态值（普通类对象 / 方法 / 修饰器结果）。
    pub(crate) fn emit_set_attr(&mut self, obj: Operand, name: &str, value: Operand) {
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(if self.lexical_namespace {
                RuntimeFn::LexicalNamespaceSet
            } else {
                RuntimeFn::SetAttr
            }),
            args: vec![obj, Operand::Const(Constant::Str(name.to_owned())), value],
        }));
    }
}
