//! 星号解包固定 ABI 的参数、结果与栈消费约束，不扩展旧非星号入口合同。
use crate::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

fn sample(args: Vec<Operand>, result: MirType, eval: bool, array: bool) -> crate::Program {
    let mut program = crate::module_capture_tests::program(Statement::Line(1));
    let function = &mut program.functions[program.entry];
    let dest = function.body.locals.iter().next().unwrap().0;
    function.body.locals[dest].ty = result;
    let value = if array {
        Rvalue::CallWithArgs {
            callee: RuntimeFn::UnpackEx,
            prefix: args,
            args: vec![],
        }
    } else {
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::UnpackEx),
            args,
        }
    };
    function.body.blocks[function.body.entry].stmts = vec![if eval {
        Statement::Eval(value)
    } else {
        Statement::Assign { dest, value }
    }];
    program
}

fn arguments() -> Vec<Operand> {
    vec![
        Operand::Const(Constant::Null),
        Operand::Const(Constant::I64(1)),
        Operand::Const(Constant::I64(2)),
    ]
}

#[test]
fn unpack_ex_accepts_only_fixed_dynamic_i64_i64_to_dynamic() {
    assert!(
        crate::validate::validate(&sample(arguments(), MirType::Dynamic, false, false)).is_ok()
    );
    for eval in [false, true] {
        assert!(
            crate::validate::validate(&sample(arguments(), MirType::Dynamic, eval, true)).is_err()
        );
    }
    assert!(
        crate::validate::validate(&sample(arguments(), MirType::Dynamic, true, false)).is_err()
    );
    assert!(crate::validate::validate(&sample(arguments(), MirType::I64, false, false)).is_err());
    for index in 0..3 {
        let mut args = arguments();
        args[index] = if index == 0 {
            Operand::Const(Constant::I64(0))
        } else {
            Operand::Const(Constant::Null)
        };
        assert!(crate::validate::validate(&sample(args, MirType::Dynamic, false, false)).is_err());
    }
    let mut too_many = arguments();
    too_many.push(Operand::Const(Constant::I64(0)));
    for args in [vec![], arguments()[..2].to_vec(), too_many] {
        assert!(crate::validate::validate(&sample(args, MirType::Dynamic, false, false)).is_err());
    }
}

#[test]
fn unpack_ex_is_not_removed_when_its_result_is_unused() {
    use crate::passes::MirPass;
    let mut program = sample(arguments(), MirType::Dynamic, false, false);
    let function = &mut program.functions[program.entry];
    crate::passes::dead_values::DeadValues.run(function);
    assert!(matches!(
        function.body.blocks[function.body.entry].stmts.as_slice(),
        [Statement::Assign {
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::UnpackEx),
                ..
            },
            ..
        }]
    ));
}
