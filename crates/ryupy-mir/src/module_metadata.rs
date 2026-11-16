//! 将本次模块元数据初始化降为固定字符串参数的冷运行时调用。
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};
impl ExprBuilder<'_> {
    pub(crate) fn build_module_metadata(
        &mut self,
        module: &ryupy_hir::Name,
        name: &ryupy_hir::Name,
    ) -> Operand {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ModuleMetadata),
                args: [module, name]
                    .map(|name| Operand::Const(Constant::Str(name.as_str().into())))
                    .into(),
            },
        });
        Operand::Local(dest)
    }
}
