//! 动态异常匹配ABI的真实PE引用与调用；不加载或执行生成程序集。
use dotnetdll::prelude::*;
use ryupy_mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

#[test]
fn dynamic_exception_match_uses_boolean_two_object_runtime_abi() {
    let mut program = crate::exact_i64_tests::program();
    let init = program.modules[program.entry].init;
    let body = &mut program.functions[init].body;
    let matched = crate::exact_i64_tests::local(&mut body.locals, MirType::Bool);
    body.blocks[body.entry].stmts.push(Statement::Assign {
        dest: matched,
        value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ExceptionMatch),
            args: vec![
                Operand::Const(Constant::None),
                Operand::Const(Constant::None),
            ],
        },
    });
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "dynamic_except_abi".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let reference = res
        .method_references
        .iter()
        .find(|m| m.name == "MatchDynamicException")
        .unwrap();
    assert_eq!(reference.signature, msig! { static bool(object, object) });
    let MethodReferenceParent::Type(parent) = &reference.parent else {
        panic!("匹配入口必须属于真实runtime类型");
    };
    assert_eq!(parent.show(&res), "[RyuPy.Runtime]RyuPy.Runtime.PyErrors");
    let method = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .find(|m| m.name == "__main__")
        .unwrap();
    let instructions = &method.body.as_ref().unwrap().instructions;
    assert!(
        instructions
            .iter()
            .any(|i| i.show(&res).contains("MatchDynamicException"))
    );
}
