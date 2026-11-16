//! 类构造的透明异常清理：只释放外部 prepared namespace，再原样抛出同一异常。
//! 清理域不是 Python except，不修改 handled exception 或 traceback/frame 状态。
use crate::build::FunctionBuilder;
use crate::mir::{BlockId, Callee, MirType, Operand, Region, RegionKind, RuntimeFn, Rvalue, Statement, Terminator};

impl FunctionBuilder<'_> {
    pub(crate) fn begin_class_cleanup(&mut self) -> BlockId {
        let start = self.new_block();
        self.terminate_current(Terminator::Goto(start));
        self.current_bb = start;
        self.protected_stack.push(self.block_index(start));
        start
    }

    pub(crate) fn end_class_cleanup(&mut self, start: BlockId, target: Operand) {
        let normal = self.if_unterminated_take();
        let end = self.block_index_of_next_block();
        self.protected_stack.pop();
        let exception = self.alloc_temp(MirType::Dynamic);
        let handler = self.new_block();
        self.current_bb = handler;
        self.push_stmt(Statement::Assign { dest: exception, value: Rvalue::CatchException });
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ClassDefinitionAbort), args: vec![target],
        }));
        self.blocks[handler].terminator = Terminator::Raise { value: Some(Operand::Local(exception)), cause: None };
        self.regions.push(Region {
            source_handler_chain: None, kind: RegionKind::CatchAll, try_start: start,
            try_len: end - self.block_index(start), handler, exc_local: Some(exception),
        });
        let after = self.new_block();
        if let Some(normal) = normal { self.blocks[normal].terminator = Terminator::Goto(after); }
        self.current_bb = after;
    }
}
