//! 新冷读取的真实PE签名与调用指令；保留旧TypeObject引用，不扩Python语义到后端。
use crate::exact_i64_tests::{function, local, program, single_block};
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

#[test]
fn default_builtin_lookup_emits_exact_string_to_object_abi() {
    let mut program = program();
    let mut locals = Arena::default();
    let result = local(&mut locals, MirType::Dynamic);
    program.functions.alloc(function(
        "lookup_probe",
        single_block(
            locals,
            vec![Statement::Assign {
                dest: result,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::LoadDefaultBuiltin),
                    args: vec![Operand::Const(Constant::Str("KeyError".into()))],
                },
            }],
            Some(Operand::Local(result)),
        ),
        vec![],
        MirType::Dynamic,
    ));
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "builtin_lookup_abi".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    for name in ["TypeObject", "LoadDefaultBuiltin"] {
        let entries: Vec<_> = res
            .method_references
            .iter()
            .filter(|m| m.name == name)
            .collect();
        assert_eq!(entries.len(), 1);
        assert_eq!(entries[0].signature, msig! { static object(string) });
        let MethodReferenceParent::Type(parent) = &entries[0].parent else {
            panic!("内建读取ABI必须属于实际运行时类型");
        };
        assert_eq!(parent.show(&res), "[RyuPy.Runtime]RyuPy.Runtime.PyOps");
    }
    let method = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .find(|m| m.name == "lookup_probe")
        .unwrap();
    let ins = &method.body.as_ref().unwrap().instructions;
    assert_eq!(ins.len(), 5);
    assert!(matches!(ins[0], Instruction::LoadString(_)));
    assert!(ins[1].show(&res).contains("PyOps.LoadDefaultBuiltin("));
    assert!(matches!(ins[2], Instruction::StoreLocal(0)));
    assert!(matches!(ins[3], Instruction::LoadLocal(0)));
    assert!(matches!(ins[4], Instruction::Return));
}
