//! 真实PE读回普通类终结void签名与调用，不增加返回局部或类型解释器。
use dotnetdll::prelude::*;
use ryupy_mir::{Callee, Constant, Operand, RuntimeFn, Rvalue, Statement};
#[test]
fn ordinary_class_finalize_emits_void_object_call() {
    let mut program = crate::exact_i64_tests::program();
    let f = &mut program.functions[program.modules[program.entry].init];
    f.body.blocks[f.body.entry].stmts = vec![Statement::Eval(Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::FinalizeOrdinaryClass),
        args: vec![Operand::Const(Constant::Null)],
    })];
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "FinalizeOrdinaryProbe".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let method = res
        .method_references
        .iter()
        .find(|m| m.name == "FinalizeOrdinaryClass")
        .unwrap();
    assert_eq!(method.signature, msig! { static void(object) });
    let count = res
        .type_definitions
        .iter()
        .flat_map(|t| t.methods.iter())
        .filter_map(|m| m.body.as_ref())
        .flat_map(|b| b.instructions.iter())
        .filter(|ins| {
            matches!(ins, Instruction::Call { .. })
                && ins
                    .show(&res)
                    .contains("PyOps.FinalizeOrdinaryClass(object)")
        })
        .count();
    assert_eq!(count, 1);
}
