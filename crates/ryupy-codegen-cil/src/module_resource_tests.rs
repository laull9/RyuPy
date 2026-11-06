//! 真实PE锁定包身份冷登记：模式来自MIR，非包显式false，旧dump保留旧入口。
use dotnetdll::prelude::*;
use ryupy_mir::ModuleLocation;

fn emitted(location: ModuleLocation, package: bool, located: bool) -> Vec<u8> {
    let mut program = crate::exact_i64_tests::program();
    let module = &mut program.modules[program.entry];
    module.location = location;
    module.is_package = package;
    module.file_backed_by_global = located;
    // 故意同名__init__.py：backend不得据诊断名选择资源注册。
    module.file = "__init__.py".into();
    crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "resource_registration".into(),
            entry_source_path: None,
        },
    )
    .unwrap()
}
#[test]
fn actual_package_registration_preserves_exact_abi_and_location_modes() {
    for (location, expected_kind, expected_value) in [
        (ModuleLocation::LegacyDiagnostic, 0, ""),
        (
            ModuleLocation::SourceAbsolute("/private/pkg/__init__.py".into()),
            1,
            "/private/pkg/__init__.py",
        ),
        (
            ModuleLocation::ExecutableRelative(
                "_internal/resources/v1/modules/pkg/__init__.py".into(),
            ),
            2,
            "_internal/resources/v1/modules/pkg/__init__.py",
        ),
    ] {
        let bytes = emitted(location, true, true);
        let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let method = res
            .type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .find(|m| m.name == "__init_functions")
            .unwrap();
        let code = &method.body.as_ref().unwrap().instructions;
        let calls: Vec<_> = code
            .iter()
            .enumerate()
            .filter(|(_, i)| i.show(&res).contains("PyModules.RegisterSourceLocated("))
            .collect();
        assert_eq!(calls.len(), 1);
        let call = calls[0].0;
        // 三组真实委托构造仍为九条；其前新增package bool，不替换原公开file槽flag。
        assert!(matches!(code[call - 10], Instruction::LoadConstantInt32(1)));
        assert!(matches!(code[call - 11], Instruction::LoadConstantInt32(1)));
        assert!(
            matches!(code[call - 13], Instruction::LoadConstantInt64(value) if value == expected_kind)
        );
        assert!(code[call - 12].show(&res).contains(expected_value));
        assert!(code[call - 14].show(&res).contains("__init__.py"));
        assert_eq!(
            code[call - 9..call]
                .iter()
                .filter(|i| matches!(i, Instruction::LoadMethodPointer(_)))
                .count(),
            3
        );
        let reference = res
            .method_references
            .iter()
            .find(|m| m.name == "RegisterSourceLocated")
            .unwrap();
        assert_eq!(reference.signature.parameters.len(), 10);
        assert_eq!(reference.signature.return_type, ReturnType::VOID);
        for index in [0, 1, 2, 4] {
            assert_eq!(
                reference.signature.parameters[index],
                Parameter::value(BaseType::String.into())
            );
        }
        assert_eq!(
            reference.signature.parameters[3],
            Parameter::value(BaseType::Int64.into())
        );
        for index in [5, 6] {
            assert_eq!(
                reference.signature.parameters[index],
                Parameter::value(BaseType::Boolean.into())
            );
        }
        assert!(
            !code
                .iter()
                .any(|i| i.show(&res).contains("PyModules.Import("))
        );
        assert!(
            !code
                .iter()
                .any(|i| i.show(&res).contains("PyModules.RegisterLocated("))
        );
    }
}
#[test]
fn nonpackage_is_explicit_and_legacy_dump_keeps_existing_registration() {
    for (package, located, expected) in [
        (false, true, "RegisterSourceLocated("),
        (false, false, "Register("),
        (true, false, "Register("),
    ] {
        let bytes = emitted(ModuleLocation::LegacyDiagnostic, package, located);
        let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let method = res
            .type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .find(|m| m.name == "__init_functions")
            .unwrap();
        let text: Vec<_> = method
            .body
            .as_ref()
            .unwrap()
            .instructions
            .iter()
            .map(|i| i.show(&res))
            .collect();
        assert_eq!(
            text.iter()
                .filter(|i| i.contains(&format!("PyModules.{expected}")))
                .count(),
            1
        );
        assert_eq!(
            text.iter().any(|i| i.contains("RegisterSourceLocated(")),
            located
        );
        if located {
            let code = &method.body.as_ref().unwrap().instructions;
            let call = text
                .iter()
                .position(|i| i.contains("RegisterSourceLocated("))
                .unwrap();
            assert!(matches!(code[call - 10], Instruction::LoadConstantInt32(0)));
        }
    }
}
