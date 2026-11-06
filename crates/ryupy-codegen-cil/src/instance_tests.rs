//! 实例模块真实PE审计：显式执行上下文、唯一字典助手及旧模块静态ABI。
use dotnetdll::prelude::*;
use ryupy_mir::{
    Callee, Constant, GlobalDecl, GlobalStorage, MirType, ModuleExecutionKind, Operand, RuntimeFn,
    Rvalue, Statement,
};

#[test]
fn instance_pe_has_explicit_context_and_no_public_static_mirror() {
    let mut program = crate::exact_i64_tests::program();
    program.globals.alloc(GlobalDecl {
        name: "ordinary_name_without_magic_prefix".into(),
        ty: MirType::Dynamic,
        storage: GlobalStorage::InstancePublic { name: "x".into() },
    });
    let module = &mut program.modules[program.entry];
    module.execution = ModuleExecutionKind::InstanceGlobals;
    module.exports.clear();
    let init = &mut program.functions[module.init];
    let state = crate::exact_i64_tests::local(&mut init.body.locals, MirType::Dynamic);
    let value = crate::exact_i64_tests::local(&mut init.body.locals, MirType::Dynamic);
    init.module_state = Some(state);
    init.module_state_source = Some(ryupy_mir::InstanceStateSource::ModuleEntryParameter);
    init.params = vec![state];
    init.body.blocks[init.body.entry].stmts = vec![
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::InstanceImportGuard),
            args: vec![Operand::Local(state)],
        }),
        Statement::Assign {
            dest: value,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceLoadName),
                args: vec![
                    Operand::Local(state),
                    Operand::Const(Constant::Str("x".into())),
                ],
            },
        },
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::InstanceStoreName),
            args: vec![
                Operand::Local(state),
                Operand::Const(Constant::Str("x".into())),
                Operand::Local(value),
            ],
        }),
        Statement::Assign {
            dest: value,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::InstanceExecutionSource),
                args: vec![Operand::Local(state)],
            },
        },
    ];
    let artifact = crate::emit_assembly_with_report(
        &program,
        &crate::EmitOptions {
            assembly_name: "InstanceAudit".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&artifact.bytes, ReadOptions::default()).unwrap();
    assert!(
        res.type_definitions
            .iter()
            .flat_map(|t| &t.fields)
            .all(|f| f.name != "ordinary_name_without_magic_prefix")
    );
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .collect();
    assert!(
        !methods
            .iter()
            .any(|m| m.name.starts_with("__mod_get_") || m.name.starts_with("__mod_set_"))
    );
    let execute = methods.iter().find(|m| m.name == "__main__").unwrap();
    let object_ref = res
        .enumerate_type_references()
        .find(|(_, t)| t.name == "Object")
        .unwrap()
        .0;
    let object: MethodType = BaseType::class(object_ref).into();
    assert_eq!(
        execute.signature,
        MethodSignature::new(false, ReturnType::VOID, vec![Parameter::value(object)])
    );
    let body = execute.body.as_ref().unwrap();
    assert_eq!(body.header.maximum_stack_size, 256);
    assert!(
        body.instructions
            .iter()
            .any(|i| matches!(i, Instruction::LoadArgument(0)))
    );
    assert!(!body.instructions.iter().any(|i| matches!(
        i,
        Instruction::LoadStaticField { .. } | Instruction::StoreStaticField { .. }
    )));
    for (name, signature) in [
        ("LoadName", msig! { static object(object, string) }),
        ("StoreName", msig! { static void(object, string, object) }),
        ("ExecutionSource", msig! { static object(object) }),
        ("ImportGuard", msig! { static void(object) }),
    ] {
        let reference = res
            .method_references
            .iter()
            .find(|r| r.name == name && r.signature == signature);
        assert!(reference.is_some(), "缺少实例ABI {name}");
        assert_eq!(
            body.instructions
                .iter()
                .filter(|i| i.show(&res).contains(&format!("PyModuleInstances.{name}(")))
                .count(),
            1
        );
    }
    let register = methods
        .iter()
        .find(|m| m.name == "__init_functions")
        .unwrap();
    assert_eq!(
        register.body.as_ref().unwrap().header.maximum_stack_size,
        32
    );
    assert!(artifact.report.modules[0].getter.is_none());
    assert!(artifact.report.modules[0].setter.is_none());
    let code = &register.body.as_ref().unwrap().instructions;
    assert_eq!(
        code.iter()
            .filter(|i| i.show(&res).contains("RegisterInstanceSourceLocated"))
            .count(),
        1
    );
    assert!(code.iter().any(
        |i| matches!(i, Instruction::LoadMethodPointer(_)) && i.show(&res).contains("__main__")
    ));
    assert!(
        !code
            .iter()
            .any(|i| matches!(i, Instruction::Call { .. }) && i.show(&res).contains("__main__"))
    );
    let reference = res
        .method_references
        .iter()
        .find(|r| r.name == "RegisterInstanceSourceLocated")
        .unwrap();
    assert_eq!(reference.signature.parameters.len(), 7);
    assert_eq!(
        reference.signature.parameters[3],
        Parameter::value(BaseType::Int64.into())
    );
    assert_eq!(
        reference.signature.parameters[5],
        Parameter::value(BaseType::Boolean.into())
    );
    assert!(
        reference.signature.parameters[6]
            .show(&res)
            .contains("PyModuleExecute")
    );
}

#[test]
fn legacy_pe_keeps_static_field_and_zero_argument_init() {
    let mut program = crate::exact_i64_tests::program();
    // 名称即使似乎属于实例仍保静态，分类只能来自MIR。
    let global = program.globals.alloc(GlobalDecl {
        name: "instance_public_x".into(),
        ty: MirType::I64,
        storage: GlobalStorage::LegacyStatic,
    });
    let module = &mut program.modules[program.entry];
    module.exports.push(ryupy_mir::ModuleExport {
        name: "x".into(),
        value: global,
        guard: ryupy_mir::ExportGuard::None,
    });
    let init = &mut program.functions[module.init];
    init.body.blocks[init.body.entry]
        .stmts
        .push(Statement::SetGlobal {
            dest: global,
            value: Operand::Const(Constant::I64(7)),
        });
    let artifact = crate::emit_assembly_with_report(
        &program,
        &crate::EmitOptions {
            assembly_name: "LegacyInstanceAudit".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    assert!(artifact.report.modules[0].getter.is_some());
    assert!(artifact.report.modules[0].setter.is_some());
    let res = Resolution::parse(&artifact.bytes, ReadOptions::default()).unwrap();
    assert!(
        !res.type_references
            .iter()
            .any(|t| t.name == "PyModuleInstances" || t.name == "PyModuleExecute")
    );
    let field = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.fields)
        .find(|f| f.name == "instance_public_x")
        .unwrap();
    assert_eq!(field.return_type, BaseType::Int64.into());
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .collect();
    let init = methods.iter().find(|m| m.name == "__main__").unwrap();
    assert_eq!(init.signature, msig! { static void() });
    assert!(
        init.body
            .as_ref()
            .unwrap()
            .instructions
            .iter()
            .any(|i| matches!(i, Instruction::StoreStaticField { .. }))
    );
    let registration = methods
        .iter()
        .find(|m| m.name == "__init_functions")
        .unwrap();
    let code = &registration.body.as_ref().unwrap().instructions;
    assert_eq!(
        code.iter()
            .filter(|i| matches!(i, Instruction::LoadMethodPointer(_)))
            .count(),
        3
    );
    assert!(
        code.iter()
            .any(|i| i.show(&res).contains("PyModules.Register("))
    );
    assert!(
        !code
            .iter()
            .any(|i| i.show(&res).contains("RegisterInstance"))
    );
    for prefix in ["__mod_get_", "__mod_set_"] {
        assert_eq!(
            methods
                .iter()
                .filter(|m| m.name.starts_with(prefix))
                .count(),
            1
        );
    }
}

#[test]
fn mixed_pe_keeps_location_identity_and_accessor_indices() {
    use ryupy_mir::ModuleLocation;
    for (location, kind, value) in [
        (ModuleLocation::LegacyDiagnostic, 0, ""),
        (
            ModuleLocation::SourceAbsolute("/source/pkg.py".into()),
            1,
            "/source/pkg.py",
        ),
        (
            ModuleLocation::ExecutableRelative("resources/pkg.py".into()),
            2,
            "resources/pkg.py",
        ),
    ] {
        let mut program = crate::exact_i64_tests::program();
        let mut instance = ryupy_mir::ModuleDef {
            name: "pkg".into(),
            package: "pkg".into(),
            file: "diagnostic.py".into(),
            is_package: true,
            execution: ModuleExecutionKind::InstanceGlobals,
            location,
            file_backed_by_global: false,
            protected_exports: vec![],
            exports: vec![],
            init: program.modules[program.entry].init,
        };
        let mut function = crate::exact_i64_tests::function(
            "pkg_execute",
            crate::exact_i64_tests::single_block(Default::default(), vec![], None),
            vec![],
            MirType::Unit,
        );
        function.name = "pkg_execute".into();
        function.codegen_name = "pkg_execute".into();
        let state = crate::exact_i64_tests::local(&mut function.body.locals, MirType::Dynamic);
        function.params = vec![state];
        function.module_state = Some(state);
        function.module_state_source = Some(ryupy_mir::InstanceStateSource::ModuleEntryParameter);
        instance.init = program.functions.alloc(function);
        program.modules.alloc(instance);
        let artifact = crate::emit_assembly_with_report(
            &program,
            &crate::EmitOptions {
                assembly_name: "MixedInstanceAudit".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        assert!(artifact.report.modules[0].getter.is_some());
        assert!(artifact.report.modules[1].getter.is_none());
        let res = Resolution::parse(&artifact.bytes, ReadOptions::default()).unwrap();
        let methods: Vec<_> = res
            .type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .collect();
        assert!(methods.iter().any(|m| m.name == "__mod_get_0"));
        assert!(!methods.iter().any(|m| m.name == "__mod_get_1"));
        let registration = methods
            .iter()
            .find(|m| m.name == "__init_functions")
            .unwrap();
        let code = &registration.body.as_ref().unwrap().instructions;
        let call = code
            .iter()
            .position(|i| i.show(&res).contains("RegisterInstanceSourceLocated"))
            .unwrap();
        assert!(matches!(code[call - 4], Instruction::LoadConstantInt32(1)));
        assert!(code[call - 5].show(&res).contains(value));
        assert!(matches!(code[call - 6], Instruction::LoadConstantInt64(k) if k == kind));
        assert!(code[call - 7].show(&res).contains("diagnostic.py"));
        assert!(code[call - 2].show(&res).contains("pkg_execute"));
        assert!(
            code.iter()
                .any(|i| i.show(&res).contains("PyModules.Register("))
        );
    }
}
