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
        self.build_delegation(expression, RuntimeFn::GeneratorDelegateStart)
    }

    /// await 复用显式暂停CFG，但入口只接受 awaitable 协议。
    pub(crate) fn build_await(
        &mut self,
        expression: ExprId,
        context: ryupy_hir::AwaitContext,
    ) -> Operand {
        let start = match context {
            ryupy_hir::AwaitContext::Expression => RuntimeFn::CoroutineAwaitStart,
            ryupy_hir::AwaitContext::AsyncEnter => RuntimeFn::CoroutineEnterAwaitStart,
            ryupy_hir::AwaitContext::AsyncExit => RuntimeFn::CoroutineExitAwaitStart,
        };
        self.build_delegation(expression, start)
    }

    /// 合成with读取真正异常链，不能通过用户同名属性伪装第三个退出参数。
    pub(crate) fn build_exception_traceback(&mut self, expression: ExprId) -> Operand {
        let raw = self.build_expr(expression);
        let ty = self.ty_of(expression);
        let value = self.coerce(raw, &ty, &MirType::Dynamic);
        self.runtime_call(RuntimeFn::TracebackOf, vec![value], MirType::Dynamic)
    }

    fn build_delegation(&mut self, expression: ExprId, start: RuntimeFn) -> Operand {
        use crate::mir::{Rvalue, Statement};
        let source = self.build_expr(expression);
        let ty = self.ty_of(expression);
        let source = self.coerce(source, &ty, &MirType::Dynamic);
        let delegate = self.runtime_call(start, vec![source], MirType::Dynamic);
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
