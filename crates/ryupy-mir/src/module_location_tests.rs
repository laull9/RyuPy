//! 模块位置的固定字符串ABI：错误参数、返回槽及数组调用均不能发射坏栈。
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
        module_state: None,
        module_state_source: None,
        name: "main".into(),
        codegen_name: "main".into(),
        source_file: "print.py".into(),
        definition: None,
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
        dynamic_origin: None,
    });
    Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
        source_name: "print.py".into(),
    }
}

fn valid_for(
    runtime: RuntimeFn,
    args: Vec<Operand>,
    ret: MirType,
    eval: bool,
    array_call: bool,
) -> bool {
    let mut input = program(Statement::Line(1));
    let func = &mut input.functions[input.entry];
    let dest = func.body.locals.iter().next().unwrap().0;
    func.body.locals[dest].ty = ret;
    let value = if array_call {
        Rvalue::CallWithArgs {
            callee: runtime,
            prefix: args,
            args: vec![],
        }
    } else {
        Rvalue::Call {
            callee: Callee::Runtime(runtime),
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

fn valid(args: Vec<Operand>, ret: MirType, eval: bool, array_call: bool) -> bool {
    valid_for(RuntimeFn::ModuleFile, args, ret, eval, array_call)
}

#[test]
fn module_file_fixed_abi_is_closed_in_all_statement_shapes() {
    let arg = Operand::Const(Constant::Str("pkg.unit".into()));
    assert!(valid(vec![arg.clone()], MirType::Str, false, false));
    for args in [
        vec![],
        vec![arg.clone(), arg.clone()],
        vec![Operand::Const(Constant::Null)],
        vec![Operand::Const(Constant::I64(0))],
    ] {
        assert!(!valid(args, MirType::Str, false, false));
    }
    for ret in [MirType::I64, MirType::Dynamic] {
        assert!(!valid(vec![arg.clone()], ret, false, false));
    }
    assert!(!valid(vec![arg.clone()], MirType::Str, true, false));
    assert!(!valid(vec![arg.clone()], MirType::Str, false, true));
    assert!(!valid(vec![arg], MirType::Str, true, true));
}

#[test]
fn module_metadata_fixed_abi_requires_two_strings_and_dynamic_result() {
    let arg = Operand::Const(Constant::Str("pkg.unit".into()));
    let valid =
        |args, ret, eval, array| valid_for(RuntimeFn::ModuleMetadata, args, ret, eval, array);
    assert!(valid(
        vec![arg.clone(), arg.clone()],
        MirType::Dynamic,
        false,
        false
    ));
    for args in [
        vec![],
        vec![arg.clone()],
        vec![arg.clone(), Operand::Const(Constant::Null)],
    ] {
        assert!(!valid(args, MirType::Dynamic, false, false));
    }
    assert!(!valid(
        vec![arg.clone(), arg.clone()],
        MirType::Str,
        false,
        false
    ));
    assert!(!valid(
        vec![arg.clone(), arg.clone()],
        MirType::Dynamic,
        true,
        false
    ));
    assert!(!valid(
        vec![arg.clone(), arg],
        MirType::Dynamic,
        false,
        true
    ));
}
