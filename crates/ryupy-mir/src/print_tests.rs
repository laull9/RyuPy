//! print ABI 验证回归：输出目标缺失或返回值未消费必须在 MIR 阶段阻断。
use crate::{
    BasicBlock, Body, Callee, Constant, Function, MirType, Operand, Program, RuntimeFn, Rvalue,
    Statement, Terminator,
};
use la_arena::Arena;

fn program(statement: Statement) -> Program {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![statement],
        terminator: Terminator::Return(None),
    });
    let mut functions = Arena::default();
    let entry = functions.alloc(Function {
        name: "main".into(),
        codegen_name: "main".into(),
        source_file: "print.py".into(),
        params: vec![],
        ret: MirType::Unit,
        body: Body {
            locals: Arena::default(),
            blocks,
            entry,
            regions: vec![],
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
    });
    Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
        source_name: "print.py".into(),
    }
}

#[test]
fn print_write_requires_target_and_matching_primitive() {
    for args in [
        vec![Operand::Const(Constant::I64(1))],
        vec![
            Operand::Const(Constant::Null),
            Operand::Const(Constant::Bool(true)),
        ],
    ] {
        let input = program(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::WriteI64),
            args,
        }));
        assert!(
            crate::validate::validate(&input)
                .unwrap_err()
                .iter()
                .any(|error| error.contains("输出目标ABI"))
        );
    }
    let input = program(Statement::Eval(Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::WriteI64),
        args: vec![
            Operand::Const(Constant::Null),
            Operand::Const(Constant::I64(1)),
        ],
    }));
    assert!(crate::validate::validate(&input).is_ok());
}

#[test]
fn print_target_result_cannot_remain_on_cil_stack() {
    let input = program(Statement::Eval(Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::PrintTarget),
        args: vec![],
    }));
    assert!(
        crate::validate::validate(&input)
            .unwrap_err()
            .iter()
            .any(|error| error.contains("返回值必须写入临时"))
    );
}
