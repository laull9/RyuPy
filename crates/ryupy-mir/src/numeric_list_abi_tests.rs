//! 数值列表边界的类型/栈契约：预检参数保持动态，原生浮点追加不能误装箱。
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
            locals: {
                let mut locals = Arena::default();
                locals.alloc(crate::LocalDecl {
                    ty: MirType::Dynamic,
                    name: None,
                    is_cell: false,
                });
                locals
            },
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

fn verify_call(runtime: RuntimeFn, args: Vec<Operand>, ret: MirType, eval: bool) -> bool {
    let value = Rvalue::Call {
        callee: Callee::Runtime(runtime),
        args,
    };
    let mut input = program(Statement::Eval(Rvalue::Call {
        callee: Callee::Runtime(runtime),
        args: Vec::new(),
    }));
    let func = &mut input.functions[input.entry];
    let dest = func.body.locals.iter().next().unwrap().0;
    func.body.locals[dest].ty = ret;
    func.body.blocks[func.body.entry].stmts[0] = if eval {
        Statement::Eval(value)
    } else {
        Statement::Assign { dest, value }
    };
    crate::validate::validate(&input).is_ok()
}
#[test]
fn numeric_list_calls_validate_arguments_results_and_stack_consumption() {
    let dynamic = Operand::Const(Constant::Null);
    let cases = [
        (RuntimeFn::NumericListCreate, vec![], MirType::Dynamic),
        (
            RuntimeFn::NumericListAppendF64,
            vec![dynamic.clone(), Operand::Const(Constant::F64(1.5))],
            MirType::Dynamic,
        ),
        (
            RuntimeFn::NumericListPeekList,
            vec![dynamic.clone(), dynamic.clone()],
            MirType::Dynamic,
        ),
        (
            RuntimeFn::NumericListCanReadF64,
            vec![dynamic.clone(), dynamic.clone()],
            MirType::Bool,
        ),
        (
            RuntimeFn::NumericListReadF64,
            vec![dynamic.clone(), dynamic.clone()],
            MirType::F64,
        ),
    ];
    for (callee, args, ret) in cases {
        assert!(verify_call(callee, args.clone(), ret.clone(), false));
        assert!(!verify_call(callee, args.clone(), ret.clone(), true));
        assert!(!verify_call(callee, args.clone(), MirType::I64, false));
        let mut extra = args.clone();
        extra.push(dynamic.clone());
        assert!(!verify_call(callee, extra, ret.clone(), false));
        if !args.is_empty() {
            let mut bad = args;
            *bad.last_mut().unwrap() = Operand::Const(Constant::I64(0));
            assert!(!verify_call(callee, bad, ret.clone(), false));
        }
    }
    assert!(!verify_call(
        RuntimeFn::NumericListAppendF64,
        vec![dynamic.clone(), dynamic],
        MirType::Dynamic,
        false
    ));
}
