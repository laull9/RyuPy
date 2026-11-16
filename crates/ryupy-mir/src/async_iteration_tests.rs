//! 真实异步循环的固定ABI拒绝门：先完整编译，再篡改协议实参，验证错误不会进入CIL。
use crate::*;

fn compile() -> Program {
    let source = "async def f(source):\n    async for value in source:\n        pass\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("async_for.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty());
    let (hir, errors) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty());
    let (program, _, errors) = build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    assert!(validate::validate(&program).is_ok());
    program
}

#[test]
fn async_iteration_protocol_rejects_wrong_fixed_abi_arguments() {
    for wanted in [
        RuntimeFn::AsyncIterStart,
        RuntimeFn::AsyncIterNext,
        RuntimeFn::AsyncIterIsStop,
        RuntimeFn::CoroutineNextAwaitStart,
    ] {
        let mut program = compile();
        let mut changed = false;
        'functions: for (_, function) in program.functions.iter_mut() {
            for (_, block) in function.body.blocks.iter_mut() {
                for statement in &mut block.stmts {
                    if let Statement::Assign {
                        value:
                            Rvalue::Call {
                                callee: Callee::Runtime(runtime),
                                args,
                            },
                        ..
                    } = statement
                        && *runtime == wanted
                    {
                        args[0] = Operand::Const(Constant::I64(1));
                        changed = true;
                        break 'functions;
                    }
                }
            }
        }
        assert!(changed, "真实程序必须经过{wanted:?}");
        assert!(
            validate::validate(&program)
                .unwrap_err()
                .iter()
                .any(|e| e.contains("固定运行时 ABI"))
        );
    }
}
