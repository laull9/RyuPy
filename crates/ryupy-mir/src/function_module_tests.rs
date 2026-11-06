//! 函数所属模块冷写入与实例默认值读取的固定ABI，非法MIR不能留坏CIL栈。
use crate::{
    BasicBlock, Body, Callee, Constant, Function, LocalDecl, MirType, Operand, Program, RuntimeFn,
    Rvalue, Statement, Terminator,
};
use la_arena::Arena;

fn valid(runtime: RuntimeFn, args: Vec<Operand>, ret: MirType, eval: bool, array: bool) -> bool {
    let mut locals = Arena::default();
    let dest = locals.alloc(LocalDecl {
        ty: ret,
        name: None,
        is_cell: false,
    });
    let value = if array {
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
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![if eval {
            Statement::Eval(value)
        } else {
            Statement::Assign { dest, value }
        }],
        terminator: Terminator::Return(None),
    });
    let mut functions = Arena::default();
    let entry = functions.alloc(Function {
        module_state: None,
        module_state_source: None,
        name: "module_abi".into(),
        codegen_name: "module_abi".into(),
        source_file: "module_abi.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Unit,
        body: Body {
            locals,
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
    crate::validate::validate(&Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
        source_name: "module_abi.py".into(),
    })
    .is_ok()
}

#[test]
fn function_module_requires_two_objects_and_void_eval() {
    let object = Operand::Const(Constant::Null);
    assert!(valid(
        RuntimeFn::FunctionModule,
        vec![object.clone(), object.clone()],
        MirType::Dynamic,
        true,
        false
    ));
    for args in [
        vec![],
        vec![object.clone()],
        vec![object.clone(), Operand::Const(Constant::I64(1))],
    ] {
        assert!(!valid(
            RuntimeFn::FunctionModule,
            args,
            MirType::Dynamic,
            true,
            false
        ));
    }
    assert!(!valid(
        RuntimeFn::FunctionModule,
        vec![object.clone(), object.clone()],
        MirType::Dynamic,
        false,
        false
    ));
    assert!(!valid(
        RuntimeFn::FunctionModule,
        vec![object.clone(), object],
        MirType::Dynamic,
        true,
        true
    ));
}

#[test]
fn function_defaults_requires_one_object_and_dynamic_destination() {
    let object = Operand::Const(Constant::Null);
    assert!(valid(
        RuntimeFn::FunctionDefaults,
        vec![object.clone()],
        MirType::Dynamic,
        false,
        false
    ));
    for args in [
        vec![],
        vec![object.clone(), object.clone()],
        vec![Operand::Const(Constant::I64(1))],
    ] {
        assert!(!valid(
            RuntimeFn::FunctionDefaults,
            args,
            MirType::Dynamic,
            false,
            false
        ));
    }
    assert!(!valid(
        RuntimeFn::FunctionDefaults,
        vec![object.clone()],
        MirType::I64,
        false,
        false
    ));
    assert!(!valid(
        RuntimeFn::FunctionDefaults,
        vec![object.clone()],
        MirType::Dynamic,
        true,
        false
    ));
    assert!(!valid(
        RuntimeFn::FunctionDefaults,
        vec![object],
        MirType::Dynamic,
        false,
        true
    ));
}

#[test]
fn frame_origin_runtime_abi_is_exact() {
    let text = Operand::Const(Constant::Str("frame".into()));
    let origin = Operand::Const(Constant::Null);
    let args = vec![text.clone(), text, origin.clone()];
    assert!(valid(
        RuntimeFn::TracePushWithOrigin,
        args.clone(),
        MirType::I64,
        false,
        false
    ));
    assert!(!valid(
        RuntimeFn::TracePushWithOrigin,
        args.clone(),
        MirType::Dynamic,
        false,
        false
    ));
    assert!(!valid(
        RuntimeFn::TracePushWithOrigin,
        args.clone(),
        MirType::I64,
        true,
        false
    ));
    assert!(!valid(
        RuntimeFn::TracePushWithOrigin,
        args,
        MirType::I64,
        false,
        true
    ));
    assert!(valid(
        RuntimeFn::ModuleExecutionSource,
        vec![origin.clone()],
        MirType::Dynamic,
        false,
        false
    ));
    assert!(!valid(
        RuntimeFn::ModuleExecutionSource,
        vec![origin.clone()],
        MirType::I64,
        false,
        false
    ));
    assert!(!valid(
        RuntimeFn::ModuleExecutionSource,
        vec![origin],
        MirType::Dynamic,
        true,
        false
    ));
}
