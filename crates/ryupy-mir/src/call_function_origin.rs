//! 静态已知Dynamic直调冻结私有实例；无可信私有源时才保留实际callable路径。
use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_hir::{CallArg, FuncId};
use ryupy_ty::Ty;

impl ExprBuilder<'_> {
    /// 通用数组入口按源序冻结每个值，后续walrus/回调不能改变已求得的实参。
    pub(crate) fn freeze_generic_argument(&mut self, value: Operand) -> Operand {
        if self.types.generic.is_none() {
            return value;
        }
        let saved = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: saved,
            value: Rvalue::Use(value),
        });
        Operand::Local(saved)
    }

    pub(crate) fn capture_direct_origin(&mut self, function: FuncId) -> Option<Operand> {
        let global = self.fn_sources.get(&function).copied()?;
        let name = self.hir.functions[function].name.as_str().to_owned();
        Some(self.runtime_call(
            RuntimeFn::LoadName,
            vec![Operand::Global(global), Operand::Const(Constant::Str(name))],
            MirType::Dynamic,
        ))
    }

    /// 绑定默认值发生在显式实参之后；同实例读而不是在实参前快照数组。
    pub(crate) fn direct_function_defaults(
        &mut self,
        function: FuncId,
        origin: Option<&Operand>,
    ) -> Option<Operand> {
        match origin {
            Some(origin) => Some(self.runtime_call(
                RuntimeFn::FunctionDefaults,
                vec![origin.clone()],
                MirType::Dynamic,
            )),
            None => self.load_published_function_defaults(function),
        }
    }

    pub(crate) fn call_published_function(
        &mut self,
        function: FuncId,
        args: &[CallArg],
        result_ty: &Ty,
    ) -> Operand {
        let Some(global) = self.fn_globals.get(&function).copied() else {
            let range = self.hir.functions[self.current_hir_fn].range;
            self.diagnostics.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(self.hir.file, range),
                    "静态函数引用缺少可调用实例，请保留实际函数值调用",
                )
                .with_code("E0305"),
            );
            return self.none_value();
        };
        let name = self.hir.functions[function].name.as_str().to_owned();
        let callable = self.runtime_call(
            RuntimeFn::LoadName,
            vec![Operand::Global(global), Operand::Const(Constant::Str(name))],
            MirType::Dynamic,
        );
        let result = self.call_value(callable, args);
        self.coerce(result, &Ty::Dynamic, &ty_to_mir(result_ty))
    }
}

impl ExprBuilder<'_> {
    /// 生成一次 `PyOps.CallValue(callable, args, kwNames, kwValues)`。
    ///
    /// 纯位置、参数不超过 3 个时改用 [`Rvalue::DynCallValue`]：codegen 会走
    /// `PyPic.CallValueN`，运行时复用调用点上的参数数组。
    pub(crate) fn call_value(&mut self, callable: Operand, args: &[CallArg]) -> Operand {
        // 一等全局对象必须先读取，实参回调可能重绑槽；不改变源码Func的静态直调路径。
        let callable = if matches!(callable, Operand::Global(_)) {
            let saved = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: saved,
                value: Rvalue::Use(callable),
            });
            Operand::Local(saved)
        } else {
            callable
        };
        if self.types.generic.is_some() {
            let (positional, names, values) = self.build_call_arrays(args);
            return self.runtime_call(
                RuntimeFn::CallValueGeneric,
                vec![
                    callable,
                    Operand::Array(positional),
                    Operand::Array(names),
                    Operand::Array(values),
                ],
                MirType::Dynamic,
            );
        }
        if args.is_empty() && self.supports_zero_call_frame() {
            return self.call_zero_with_frame(callable);
        }
        if args.len() <= 3 && args.iter().all(|a| matches!(a, CallArg::Positional(_))) {
            let values: Vec<Operand> = args
                .iter()
                .map(|arg| {
                    let expr = arg.expr();
                    let op = self.build_expr(expr);
                    let ty = self.ty_of(expr);
                    self.coerce(op, &ty, &MirType::Dynamic)
                })
                .collect();
            let dest = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::DynCallValue {
                    callee: callable,
                    args: values,
                },
            });
            return Operand::Local(dest);
        }
        let (positional, kw_names, kw_values) = self.build_call_arrays(args);
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CallValue),
                args: vec![
                    callable,
                    Operand::Array(positional),
                    Operand::Array(kw_names),
                    Operand::Array(kw_values),
                ],
            },
        });
        Operand::Local(dest)
    }
}
