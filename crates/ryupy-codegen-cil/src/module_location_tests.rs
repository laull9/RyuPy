//! 模块位置的真实PE机械契约：显式模式/公开槽标志及冷读取，不猜路径或Python名字。
use dotnetdll::prelude::*;
use ryupy_mir::{Callee, Constant, MirType, ModuleLocation, Operand, RuntimeFn, Rvalue, Statement};

#[test]
fn serialized_module_locations_keep_diagnostic_and_runtime_values_separate() {
    for (location, expected_kind, value) in [
        (ModuleLocation::LegacyDiagnostic, 0, ""),
        (
            ModuleLocation::SourceAbsolute("/private/source/pkg/unit.py".into()),
            1,
            "/private/source/pkg/unit.py",
        ),
        (
            ModuleLocation::ExecutableRelative("_ryupy_resources/v1/modules/pkg/unit.py".into()),
            2,
            "_ryupy_resources/v1/modules/pkg/unit.py",
        ),
    ] {
        let mut program = crate::exact_i64_tests::program();
        let module = &mut program.modules[program.entry];
        module.file = "unit.py".into();
        module.location = location;
        module.file_backed_by_global = true;
        let name = module.name.clone();
        let init = &mut program.functions[module.init];
        let dest = crate::exact_i64_tests::local(&mut init.body.locals, MirType::Str);
        init.body.blocks[init.body.entry].stmts.insert(
            0,
            Statement::Assign {
                dest,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::ModuleFile),
                    args: vec![Operand::Const(Constant::Str(name))],
                },
            },
        );
        let bytes = crate::emit_assembly(
            &program,
            &crate::EmitOptions {
                assembly_name: "module_locations".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let methods: Vec<_> = res
            .type_definitions
            .iter()
            .flat_map(|ty| &ty.methods)
            .collect();
        let registration = methods
            .iter()
            .find(|method| method.name == "__init_functions")
            .unwrap();
        let code = &registration.body.as_ref().unwrap().instructions;
        let call = code
            .iter()
            .position(|ins| ins.show(&res).contains("PyModules.RegisterLocated"))
            .unwrap();
        // 三组ldnull/ldftn/newobj共九条指令；显式flag/位置/kind在其之前。
        assert!(matches!(code[call - 10], Instruction::LoadConstantInt32(1)));
        assert!(
            matches!(code[call - 12], Instruction::LoadConstantInt64(kind) if kind == expected_kind)
        );
        assert!(code[call - 11].show(&res).contains(value));
        assert!(code[call - 13].show(&res).contains("unit.py"));
        let located_ref = res
            .method_references
            .iter()
            .find(|method| method.name == "RegisterLocated")
            .unwrap();
        assert_eq!(located_ref.signature.parameters.len(), 9);
        assert_eq!(
            located_ref.signature.parameters[3],
            Parameter::value(BaseType::Int64.into())
        );
        assert_eq!(
            located_ref.signature.parameters[5],
            Parameter::value(BaseType::Boolean.into())
        );
        let init_method = methods
            .iter()
            .find(|method| method.name == "__main__")
            .unwrap();
        let init_code = &init_method.body.as_ref().unwrap().instructions;
        assert_eq!(
            init_code
                .iter()
                .filter(|ins| ins.show(&res).contains("PyModules.ModuleFile"))
                .count(),
            1
        );
        let file_ref = res
            .method_references
            .iter()
            .find(|method| method.name == "ModuleFile")
            .unwrap();
        assert_eq!(file_ref.signature.parameters.len(), 1);
        assert_eq!(
            file_ref.signature.parameters[0],
            Parameter::value(BaseType::String.into())
        );
    }
}

#[test]
fn legacy_dump_and_registration_do_not_invent_runtime_paths() {
    let program = crate::exact_i64_tests::program();
    let dump = crate::dump_cil(
        &program,
        &crate::EmitOptions {
            assembly_name: "module_locations".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    assert!(dump.contains("PyModules.Register("));
    assert!(!dump.contains("PyModules.RegisterLocated("));
    assert!(!dump.contains("PyModules.ModuleFile("));
}

#[test]
fn public_file_dynamic_setter_stores_original_object_without_string_conversion() {
    let mut program = crate::exact_i64_tests::program();
    let file = program.globals.alloc(ryupy_mir::GlobalDecl {
        name: "file_slot".into(),
        ty: MirType::Dynamic,
    });
    let module = &mut program.modules[program.entry];
    module.file_backed_by_global = true;
    module.exports.push(ryupy_mir::ModuleExport {
        name: "__file__".into(),
        value: file,
        guard: ryupy_mir::ExportGuard::ValueSentinel,
    });
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "file_identity".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let setter = res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .find(|method| method.name == "__mod_set_0")
        .unwrap();
    let instructions = &setter.body.as_ref().unwrap().instructions;
    let store = instructions
        .iter()
        .position(|ins| matches!(ins, Instruction::StoreStaticField { .. }))
        .unwrap();
    assert!(matches!(
        instructions[store - 1],
        Instruction::LoadArgument(2)
    ));
    assert!(
        !instructions
            .iter()
            .any(|ins| matches!(ins, Instruction::CastClass { .. }))
    );
    assert!(
        !instructions
            .iter()
            .any(|ins| ins.show(&res).contains("PyCast"))
    );
}
