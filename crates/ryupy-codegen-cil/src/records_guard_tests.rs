//! 守卫ABI实发射验证：不以MIR节点存在替代托管签名与调用证据。
use dotnetdll::prelude::*;
use ryupy_mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

#[test]
fn records_guard_emits_bool_object_object_runtime_call() {
    let mut program = crate::exact_i64_tests::program();
    let function = &mut program.functions[program.modules[program.entry].init];
    let result = crate::exact_i64_tests::local(&mut function.body.locals, MirType::Bool);
    function.body.blocks[function.body.entry].stmts = vec![Statement::Assign {
        dest: result,
        value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::RecordsTryIncrementMaterialized),
            args: vec![
                Operand::Const(Constant::Null),
                Operand::Const(Constant::Null),
            ],
        },
    }];
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "records_guard".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let reference = res
        .method_references
        .iter()
        .find(|m| m.name == "TryMaterialized")
        .unwrap();
    assert_eq!(reference.signature, msig! {static bool(object,object)});
    let main = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .find(|m| m.name == "__main__")
        .unwrap();
    let calls = main
        .body
        .as_ref()
        .unwrap()
        .instructions
        .iter()
        .filter(|i| i.show(&res).contains("TryMaterialized("))
        .count();
    assert_eq!(calls, 1);
}
