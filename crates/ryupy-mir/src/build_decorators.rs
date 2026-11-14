//! 定义位置的函数创建、元数据和装饰器应用；所有中间值先保存再执行后续用户代码。
use crate::build::FunctionBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_hir::{AssignTarget, Binding, Decorator, ExprId, FuncId};

/// 注解在创建函数前求值；这里只保存已冻结的Python对象，安装时不再运行用户表达式。
struct PreparedFunctionMetadata {
    doc: Operand,
    names: Vec<Operand>,
    values: Vec<Operand>,
}

impl FunctionBuilder<'_> {
    /// 冻结装饰器表达式的当前值，避免后续默认值重绑定模块槽位。
    pub(crate) fn prepare_decorators(
        &mut self,
        decorators: &[Decorator],
    ) -> Vec<(Decorator, Option<Operand>)> {
        decorators
            .iter()
            .map(|d| {
                let value = if let Decorator::Expr(expr) = d {
                    Some(self.freeze_expression(*expr))
                } else {
                    None
                };
                (*d, value)
            })
            .collect()
    }

    pub(crate) fn freeze_expression(&mut self, expr: ExprId) -> Operand {
        let ty = self.expr_builder().ty_of(expr);
        let raw = self.expr_builder().build_expr(expr);
        let boxed = self.expr_builder().coerce(raw, &ty, &MirType::Dynamic);
        let saved = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: saved,
            value: Rvalue::Use(boxed),
        });
        Operand::Local(saved)
    }

    pub(crate) fn apply_decorators(
        &mut self,
        mut value: Operand,
        decorators: Vec<(Decorator, Option<Operand>)>,
        frozen: bool,
    ) -> Operand {
        for (decorator, callable) in decorators.into_iter().rev() {
            value = match decorator {
                Decorator::Expr(_) => self.expr_builder().runtime_call(
                    RuntimeFn::CallValue,
                    vec![
                        callable.unwrap_or(Operand::Const(Constant::Null)),
                        Operand::Array(vec![value]),
                        Operand::Array(vec![]),
                        Operand::Array(vec![]),
                    ],
                    MirType::Dynamic,
                ),
                Decorator::Dataclass => self.expr_builder().runtime_call(
                    RuntimeFn::ApplyDataclass,
                    vec![value, Operand::Const(Constant::Bool(frozen))],
                    MirType::Dynamic,
                ),
                _ => {
                    let kind = match decorator {
                        Decorator::Property => 1,
                        Decorator::StaticMethod => 2,
                        _ => 3,
                    };
                    self.expr_builder().runtime_call(
                        RuntimeFn::Decorate,
                        vec![Operand::Const(Constant::I64(kind)), value],
                        MirType::Dynamic,
                    )
                }
            };
        }
        value
    }

    /// 每次def执行创建新默认值数组和函数对象，闭包环境保存cell引用。
    pub(crate) fn create_function(&mut self, func: FuncId) -> Operand {
        let params: Vec<_> = self.hir.functions[func]
            .params
            .iter()
            .map(|p| p.default)
            .collect();
        let defaults = params
            .iter()
            .map(|param| match param {
                Some(expr) => self.freeze_expression(*expr),
                None => Operand::Const(Constant::Null),
            })
            .collect();
        let metadata = self.prepare_function_metadata(func);
        let env = self.expr_builder().build_closure_env(func);
        let module = self.expr_builder().capture_function_module();
        let instance_context = self.expr_builder().capture_definition_context();
        let local = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: local,
            value: Rvalue::MakeFunction {
                instance_context,
                func: self.fn_map[&func],
                env,
                defaults,
            },
        });
        let value = Operand::Local(local);
        self.expr_builder()
            .install_function_module(value.clone(), module);
        self.install_function_metadata(metadata, value.clone());
        value
    }

    pub(crate) fn build_decorated_def(
        &mut self,
        func: FuncId,
        target: &AssignTarget,
        decorators: &[ExprId],
    ) {
        let decorators: Vec<_> = decorators.iter().map(|e| Decorator::Expr(*e)).collect();
        let prepared = self.prepare_decorators(&decorators);
        let value = self.create_function(func);
        let value = self.apply_decorators(value, prepared, false);
        let binding = match target {
            AssignTarget::Local(id) => Binding::Local(*id),
            AssignTarget::ModuleVar(id) => Binding::ModuleVar(*id),
            AssignTarget::Free(id) => Binding::Free(*id),
            _ => return,
        };
        // 只有整个装饰链成功之后才替换公开绑定。
        self.store_binding(binding, value, &MirType::Dynamic);
    }

    fn prepare_function_metadata(&mut self, func: FuncId) -> Option<PreparedFunctionMetadata> {
        let function = &self.hir.functions[func];
        // 空元数据由新函数对象惰性提供，不为常见无标注函数分配空数组/字典。
        if function.docstring.is_none()
            && function.ret.is_none()
            && function
                .params
                .iter()
                .all(|param| param.annotation.is_none())
        {
            return None;
        }
        let doc = function.docstring.clone();
        let annotations = function.annotation_values.clone();
        let mut names = Vec::new();
        let mut values = Vec::new();
        for (name, annotation) in annotations {
            names.push(Operand::Const(Constant::Str(name.as_str().to_owned())));
            values.push(self.freeze_expression(annotation));
        }
        let doc = doc
            .map(|s| Operand::Const(Constant::Str(s)))
            .unwrap_or(Operand::Const(Constant::Null));
        Some(PreparedFunctionMetadata { doc, names, values })
    }

    fn install_function_metadata(
        &mut self,
        metadata: Option<PreparedFunctionMetadata>,
        value: Operand,
    ) {
        let Some(PreparedFunctionMetadata { doc, names, values }) = metadata else {
            return;
        };
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::FunctionMetadata),
            args: vec![value, doc, Operand::Array(names), Operand::Array(values)],
        }));
    }
}
