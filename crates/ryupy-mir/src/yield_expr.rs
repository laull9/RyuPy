//! yield 表达式的暂停边：表达式结果在恢复时由 send 输入填入。

use crate::expr_builder::ExprBuilder;
use crate::mir::{BasicBlock, MirType, Operand, RuntimeFn, Terminator};
use ryupy_hir::ExprId;

impl ExprBuilder<'_> {
    pub(crate) fn build_yield_expr(&mut self, value: Option<ExprId>) -> Operand {
        let value = match value {
            Some(value) => {
                let ty = self.ty_of(value);
                let operand = self.build_expr(value);
                self.coerce(operand, &ty, &MirType::Dynamic)
            }
            None => self.runtime_call(RuntimeFn::DynNone, Vec::new(), MirType::Dynamic),
        };
        let input = self.alloc_temp(MirType::Dynamic);
        let resume = self.blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        });
        self.blocks[*self.current_bb].terminator = Terminator::Suspend {
            value,
            resume,
            input: Some(input),
        };
        *self.current_bb = resume;
        Operand::Local(input)
    }
}
