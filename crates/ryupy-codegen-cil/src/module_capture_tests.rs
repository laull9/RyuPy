//! 捕获模块方法的机械PE协议：dyn/string→dyn，不引入后端Python模块判定。
use dotnetdll::prelude::*;
use ryupy_mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

#[test]
fn module_capture_uses_exact_object_string_abi_before_method_call() {
    let mut program = crate::exact_i64_tests::program();
    let function = &mut program.functions[program.modules[program.entry].init];
    let captured = crate::exact_i64_tests::local(&mut function.body.locals, MirType::Dynamic);
    let result = crate::exact_i64_tests::local(&mut function.body.locals, MirType::Dynamic);
    function.body.blocks[function.body.entry].stmts = vec![
        Statement::Assign {
            dest: captured,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CaptureModuleMethod),
                args: vec![
                    Operand::Const(Constant::Null),
                    Operand::Const(Constant::Str("f".into())),
                ],
            },
        },
        Statement::Assign {
            dest: result,
            value: Rvalue::DynCallMethod {
                obj: Operand::Local(captured),
                name: "f".into(),
                args: vec![],
            },
        },
    ];
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "module_capture".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let reference = res
        .method_references
        .iter()
        .find(|method| method.name == "CaptureModuleMethod")
        .unwrap();
    assert_eq!(
        reference.signature,
        msig! { static object (object, string) }
    );
    let main = res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .find(|method| method.name == "__main__")
        .unwrap();
    let code = &main.body.as_ref().unwrap().instructions;
    let capture = code
        .iter()
        .position(|instruction| instruction.show(&res).contains("CaptureModuleMethod"))
        .unwrap();
    let invoke = code
        .iter()
        .position(|instruction| instruction.show(&res).contains("CallMethod0"))
        .unwrap();
    assert!(capture < invoke);
    assert!(matches!(code[capture - 2], Instruction::LoadNull));
    assert!(matches!(code[capture - 1], Instruction::LoadString(_)));
    assert!(matches!(code[capture + 1], Instruction::StoreLocal(_)));
}
