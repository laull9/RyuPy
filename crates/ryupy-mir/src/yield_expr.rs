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
            delegated: false,
        };
        *self.current_bb = resume;
        Operand::Local(input)
    }
}

impl ExprBuilder<'_> {
    /// yield-from是显式委托循环，终值与每次yield分开；异常转发由帧输入协议完成。
    pub(crate) fn build_yield_from(&mut self, expression: ExprId) -> Operand {
        use crate::mir::{Rvalue, Statement};
        let source = self.build_expr(expression);
        let ty = self.ty_of(expression);
        let source = self.coerce(source, &ty, &MirType::Dynamic);
        let delegate = self.runtime_call(
            RuntimeFn::GeneratorDelegateStart,
            vec![source],
            MirType::Dynamic,
        );
        let step = self.blocks.alloc(BasicBlock {
            stmts: vec![],
            terminator: Terminator::Unreachable,
        });
        let yielded = self.blocks.alloc(BasicBlock {
            stmts: vec![],
            terminator: Terminator::Unreachable,
        });
        let finished = self.blocks.alloc(BasicBlock {
            stmts: vec![],
            terminator: Terminator::Unreachable,
        });
        self.blocks[*self.current_bb].terminator = Terminator::Goto(step);
        *self.current_bb = step;
        let more = self.alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest: more,
            value: Rvalue::GeneratorDelegateStep {
                delegate: delegate.clone(),
            },
        });
        self.blocks[step].terminator = Terminator::Branch {
            cond: Operand::Local(more),
            then_: yielded,
            else_: finished,
        };
        *self.current_bb = yielded;
        let value = self.runtime_call(
            RuntimeFn::GeneratorDelegateCurrent,
            vec![delegate.clone()],
            MirType::Dynamic,
        );
        self.blocks[yielded].terminator = Terminator::Suspend {
            value,
            resume: step,
            input: None,
            delegated: true,
        };
        *self.current_bb = finished;
        self.runtime_call(
            RuntimeFn::GeneratorDelegateResult,
            vec![delegate],
            MirType::Dynamic,
        )
    }
}
