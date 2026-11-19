//! locals专用ABI必须在MIR验证阶段拒绝错误CIL形态，且同步不能当纯操作删除。
use crate::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

fn sample(rt: RuntimeFn, args: Vec<Operand>, assign: bool, array: bool) -> crate::Program {
    let mut p = crate::module_capture_tests::program(Statement::Line(1));
    let f = &mut p.functions[p.entry];
    let dest = f.body.locals.iter().next().unwrap().0;
    let value = if array {
        Rvalue::CallWithArgs {
            callee: rt,
            prefix: args,
            args: vec![],
        }
    } else {
        Rvalue::Call {
            callee: Callee::Runtime(rt),
            args,
        }
    };
    f.body.blocks[f.body.entry].stmts = vec![if assign {
        Statement::Assign { dest, value }
    } else {
        Statement::Eval(value)
    }];
    p
}

#[test]
fn function_locals_abi_enforces_arguments_and_return_consumption() {
    let null = Operand::Const(Constant::Null);
    for (rt, args, assign) in [
        (
            RuntimeFn::FunctionLocalsValidate,
            vec![Operand::Array(vec![]); 3],
            false,
        ),
        (RuntimeFn::FunctionLocalsEnsure, vec![null.clone()], true),
        (
            RuntimeFn::FunctionLocalsSync,
            vec![
                null.clone(),
                Operand::Const(Constant::Str("x".into())),
                null,
            ],
            false,
        ),
    ] {
        assert!(crate::validate::validate(&sample(rt, args.clone(), assign, false)).is_ok());
        assert!(crate::validate::validate(&sample(rt, args.clone(), !assign, false)).is_err());
        assert!(crate::validate::validate(&sample(rt, args.clone(), assign, true)).is_err());
        assert!(crate::validate::validate(&sample(rt, vec![], assign, false)).is_err());
        let mut p = sample(rt, args, assign, false);
        use crate::passes::MirPass;
        crate::passes::dead_values::DeadValues.run(&mut p.functions[p.entry]);
        assert_eq!(
            p.functions[p.entry]
                .body
                .blocks
                .iter()
                .next()
                .unwrap()
                .1
                .stmts
                .len(),
            1
        );
    }
    let invalid = sample(
        RuntimeFn::FunctionLocalsValidate,
        vec![Operand::Const(Constant::Null); 3],
        false,
        false,
    );
    assert!(crate::validate::validate(&invalid).is_err());
    let mut wrong = sample(
        RuntimeFn::FunctionLocalsEnsure,
        vec![Operand::Const(Constant::Null)],
        true,
        false,
    );
    for (_, local) in wrong.functions[wrong.entry].body.locals.iter_mut() {
        local.ty = MirType::I64;
    }
    assert!(crate::validate::validate(&wrong).is_err());
}
