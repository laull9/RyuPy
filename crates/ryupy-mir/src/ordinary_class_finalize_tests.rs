//! 普通类终结是effectful void调用；错误ABI形态必须在生成CIL前拒绝。
use crate::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

fn sample(assign: bool, array: bool, args: Vec<Operand>) -> crate::Program {
    let mut p = crate::module_capture_tests::program(Statement::Line(1));
    let f = &mut p.functions[p.entry];
    let dest = f.body.locals.iter().next().unwrap().0;
    let value = if array {
        Rvalue::CallWithArgs {
            callee: RuntimeFn::FinalizeOrdinaryClass,
            prefix: args,
            args: vec![],
        }
    } else {
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::FinalizeOrdinaryClass),
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
fn finalize_ordinary_class_accepts_only_one_dynamic_argument_void_eval() {
    let args = vec![Operand::Const(Constant::Null)];
    assert!(crate::validate::validate(&sample(false, false, args.clone())).is_ok());
    for assign in [false, true] {
        assert!(crate::validate::validate(&sample(assign, true, args.clone())).is_err());
    }
    assert!(crate::validate::validate(&sample(true, false, args.clone())).is_err());
    for args in [
        vec![],
        vec![Operand::Const(Constant::I64(1))],
        vec![args[0].clone(), args[0].clone()],
    ] {
        assert!(crate::validate::validate(&sample(false, false, args)).is_err());
    }
}
#[test]
fn finalize_ordinary_class_is_preserved_by_dead_value_elimination() {
    use crate::passes::MirPass;
    let mut p = sample(false, false, vec![Operand::Const(Constant::Null)]);
    let f = &mut p.functions[p.entry];
    crate::passes::dead_values::DeadValues.run(f);
    assert!(matches!(
        f.body.blocks[f.body.entry].stmts.as_slice(),
        [Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::FinalizeOrdinaryClass),
            ..
        })]
    ));
    assert_eq!(f.body.locals.len(), 1);
    assert_eq!(f.ret, MirType::Unit);
}
