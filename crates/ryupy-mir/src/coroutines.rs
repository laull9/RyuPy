//! 原生协程工厂的发布边界：先完成共享显式状态机降级，再包装独立协程协议对象。
use crate::mir::*;

/// 包装实际工厂的返回值；恢复入口依旧是编译生成的 CIL，不执行 Python 源码。
pub(crate) fn wrap_factories(program: &mut Program, functions: &[FnId]) {
    for &id in functions {
        let body = &mut program.functions[id].body;
        for (_, block) in body.blocks.iter_mut() {
            if let Terminator::Return(Some(value)) = &mut block.terminator {
                let wrapped = body.locals.alloc(LocalDecl {
                    ty: MirType::Dynamic,
                    name: None,
                    is_cell: false,
                });
                block.stmts.push(Statement::Assign {
                    dest: wrapped,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::CoroutineCreate),
                        args: vec![value.clone()],
                    },
                });
                *value = Operand::Local(wrapped);
            }
        }
    }
}
