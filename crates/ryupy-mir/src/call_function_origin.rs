//! 静态已知Dynamic直调冻结私有实例；无可信私有源时才保留实际callable路径。
use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Constant, MirType, Operand, RuntimeFn};
use ryupy_hir::{CallArg, FuncId};
use ryupy_ty::Ty;

impl ExprBuilder<'_> {
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
