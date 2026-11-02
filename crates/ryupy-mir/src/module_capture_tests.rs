//! 模块方法捕获ABI与效果：不能错误丢弃返回值/参数，也不能被死值优化抹掉。
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

fn valid(args: Vec<Operand>, ret: MirType, eval: bool, array_call: bool) -> bool {
    let mut input = program(Statement::Line(1));
    let func = &mut input.functions[input.entry];
    let dest = func.body.locals.iter().next().unwrap().0;
    func.body.locals[dest].ty = ret;
    let value = if array_call {
        Rvalue::CallWithArgs {
            callee: RuntimeFn::CaptureModuleMethod,
            prefix: args,
            args: vec![],
        }
    } else {
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::CaptureModuleMethod),
            args,
        }
    };
    func.body.blocks[func.body.entry].stmts[0] = if eval {
        Statement::Eval(value)
    } else {
        Statement::Assign { dest, value }
    };
    crate::validate::validate(&input).is_ok()
}

#[test]
fn capture_module_method_abi_rejects_bad_arguments_results_and_array_forms() {
    let args = vec![
        Operand::Const(Constant::Null),
        Operand::Const(Constant::Str("method".into())),
    ];
    assert!(valid(args.clone(), MirType::Dynamic, false, false));
    for bad in [
        vec![],
        vec![args[0].clone()],
        vec![args[1].clone(), args[1].clone()],
        vec![args[0].clone(), args[0].clone()],
    ] {
        assert!(!valid(bad, MirType::Dynamic, false, false));
    }
    for ret in [MirType::I64, MirType::Str, MirType::Bool] {
        assert!(!valid(args.clone(), ret, false, false));
    }
    assert!(!valid(args.clone(), MirType::Dynamic, true, false));
    assert!(!valid(args.clone(), MirType::Dynamic, false, true));
    assert!(!valid(args, MirType::Dynamic, true, true));
}

#[test]
fn unused_capture_is_effectful_and_survives_dead_values() {
    use crate::passes::MirPass;
    let mut input = program(Statement::Line(1));
    let function = &mut input.functions[input.entry];
    let dest = function.body.locals.iter().next().unwrap().0;
    function.body.blocks[function.body.entry].stmts = vec![Statement::Assign {
        dest,
        value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::CaptureModuleMethod),
            args: vec![
                Operand::Const(Constant::Null),
                Operand::Const(Constant::Str("missing".into())),
            ],
        },
    }];
    crate::passes::dead_values::DeadValues.run(function);
    assert_eq!(function.body.blocks[function.body.entry].stmts.len(), 1);
}
