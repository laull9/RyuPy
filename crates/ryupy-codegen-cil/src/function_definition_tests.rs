//! 读回真实 PE：来源只在共享签名初始化时登记，ABI 与源常量不能被后端符号替换。
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{DefinitionInfo, GlobalDecl, MirType};

#[test]
fn function_definition_registers_once_in_cold_signature_initialization() {
    let mut program = crate::exact_i64_tests::program();
    for (name, definition) in [
        (
            "backend_symbol",
            Some(DefinitionInfo {
                lexical_qualname: "Container.make.<locals>.child".into(),
                firstlineno: 19,
            }),
        ),
        ("unknown_implicit", None),
    ] {
        let signature = program.globals.alloc(GlobalDecl {
            storage: ryupy_mir::GlobalStorage::LegacyStatic,
            name: format!("{name}_signature"),
            ty: MirType::Dynamic,
        });
        let mut function = crate::exact_i64_tests::function(
            name,
            crate::exact_i64_tests::single_block(Arena::default(), vec![], None),
            vec![],
            MirType::Unit,
        );
        function.signature_global = Some(signature);
        function.definition = definition;
        program.functions.alloc(function);
    }
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "FunctionDefinitionProbe".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let reference = res
        .method_references
        .iter()
        .find(|m| m.name == "FunctionDefinition")
        .unwrap();
    assert_eq!(
        reference.signature,
        msig! { static void(object, string, long) }
    );
    let mut calls = 0;
    for ty in &res.type_definitions {
        for method in &ty.methods {
            let Some(body) = &method.body else {
                continue;
            };
            for (index, instruction) in body.instructions.iter().enumerate() {
                if matches!(instruction, Instruction::Call { .. })
                    && instruction.show(&res).contains("FunctionDefinition")
                {
                    calls += 1;
                    assert_eq!(method.name, "__init_functions");
                    assert!(matches!(
                        body.instructions[index - 1],
                        Instruction::LoadConstantInt64(19)
                    ));
                    assert!(
                        body.instructions[index - 2]
                            .show(&res)
                            .contains("Container.make.<locals>.child")
                    );
                    assert!(matches!(
                        body.instructions[index - 3],
                        Instruction::LoadStaticField { .. }
                    ));
                    assert!(
                        body.instructions[index - 3]
                            .show(&res)
                            .contains("backend_symbol_signature")
                    );
                }
            }
        }
    }
    assert_eq!(calls, 1);
}
