//! 无关键字 print 的原始数值快路径：实参全部求值后只捕获一次输出目标。
//! 关键字与星号展开交给动态内建绑定，保留 sep/end/file/flush 的完整调用语义。
use crate::build::FunctionBuilder;
use crate::mir::{Callee, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_hir::CallArg;
use ryupy_ty::Ty;
impl FunctionBuilder<'_> {
    /// 不创建实参数组；静态 primitive 直接写入捕获的目标，避免每个元素装箱。
    pub(crate) fn build_print_fast(&mut self, args: &[CallArg]) {
        let dynamic = self.types.is_dynamic(self.current_hir_fn);
        let mut written = Vec::with_capacity(args.len());
        for arg in args {
            // 调用者仅对全位置、无星号参数选择此路径。
            let expression = arg.expr();
            let operand = self.expr_builder().build_expr(expression);
            let ty = self.expr_builder().ty_of(expression);
            let (operand, write) = if dynamic {
                (
                    self.expr_builder().coerce(operand, &ty, &MirType::Dynamic),
                    RuntimeFn::WriteObject,
                )
            } else {
                match ty {
                    Ty::Int => (operand, RuntimeFn::WriteI64),
                    Ty::Float => (operand, RuntimeFn::WriteF64),
                    Ty::Bool => (operand, RuntimeFn::WriteBool),
                    Ty::Str => (operand, RuntimeFn::WriteStr),
                    _ => (
                        self.expr_builder().coerce(operand, &ty, &MirType::Dynamic),
                        RuntimeFn::WriteObject,
                    ),
                }
            };
            written.push((operand, write));
        }
        // 必须在所有实参求值之后、首次write之前取stdout。自定义write重绑定sys.stdout
        // 不影响本次后续分隔符与换行；递归print则独立捕获自己的目标。
        let target =
            self.expr_builder()
                .runtime_call(RuntimeFn::PrintTarget, vec![], MirType::Dynamic);
        for (index, (operand, write)) in written.into_iter().enumerate() {
            if index != 0 {
                self.emit_print_write(RuntimeFn::WriteSep, vec![target.clone()]);
            }
            self.emit_print_write(write, vec![target.clone(), operand]);
        }
        self.emit_print_write(RuntimeFn::WriteNewline, vec![target]);
    }
    fn emit_print_write(&mut self, write: RuntimeFn, args: Vec<Operand>) {
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(write),
            args,
        }));
    }
}
