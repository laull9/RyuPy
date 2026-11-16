//! 真实普通catch的冷传播接收；生成器由降级后的物理catch接收，避免读取旧激活深度。
use crate::build::FunctionBuilder;
use crate::mir::{Callee, LocalId, Operand, RuntimeFn, Rvalue, Statement};

impl FunctionBuilder<'_> {
    pub(crate) fn receive_native_traceback(&mut self, exception: LocalId) {
        if self.trace_product || cfg!(feature = "trace-adapter-test")
            || self.hir.functions[self.current_hir_fn].is_generator {
            return;
        }
        if let Some(depth) = self.frame_depth {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TracebackReceive),
                args: vec![Operand::Local(exception), Operand::Local(depth)],
            }));
        }
    }
}
