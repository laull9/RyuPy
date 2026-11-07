//! 实际构建路径的元数据槽、作用域重绑定与真实PE生成，不批准文本快照。
use ryupy_mir::{Callee, MirType, RuntimeFn, Rvalue, Statement};

#[test]
fn synthetic_metadata_preserves_final_ordinary_global_types() {
    let source = "number = 42\nlabel = 'hello'\ndef read() -> int:\n    return number + 1\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("unit.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (hir, errors) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            instance_capabilities: Default::default(),
            instance_globals: false,
            name: "unit".into(),
            package: "".into(),
            file: "unit.py".into(),
            resolve_file_at_runtime: true,
        },
        source,
    );
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    assert_eq!(types.mode(hir.main), ryupy_ty::FuncMode::Static);
    let read = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "read")
        .unwrap()
        .0;
    assert_eq!(types.mode(read), ryupy_ty::FuncMode::Static);
    let (mut program, exports, errors) = ryupy_mir::build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    for (name, expected) in [
        ("number", MirType::I64),
        ("label", MirType::Str),
        ("__spec__", MirType::Dynamic),
        ("__loader__", MirType::Dynamic),
        ("__cached__", MirType::Dynamic),
        ("__path__", MirType::Dynamic),
    ] {
        let slot = exports.iter().find(|e| e.name == name).unwrap().value;
        assert_eq!(program.globals[slot].ty, expected, "{name}");
    }
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    // 真发射保证静态main可承载四个独立object初值，不能只在类型提示层绕过检查。
    let program = ryupy_mir::link(
        vec![(
            program,
            ryupy_mir::UnitMeta {
                instance_capabilities: Default::default(),
                execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
                environment_reason: "existing-contract".into(),
                location: ryupy_mir::ModuleLocation::SourceAbsolute("/source/unit.py".into()),
                file_backed_by_global: true,
                is_package: false,
                protected_exports: vec![],
                name: "unit".into(),
                package: "".into(),
                file: "unit.py".into(),
                exports,
            },
        )],
        0,
    );
    let bytes = ryupy_codegen_cil::emit_assembly(
        &program,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "metadata_static_globals".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    assert!(bytes.starts_with(b"MZ"));
}

#[test]
fn module_metadata_real_slots_survive_lowering_and_pe_emission() {
    let source = "saved = __spec__\ndef read():\n    return __spec__\ndef replace(value):\n    global __spec__\n    __spec__ = value\ndef local():\n    __spec__ = 42\n    return __spec__\ndef add(x: int, y: int) -> int:\n    return x + y\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("unit.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_capabilities: Default::default(),
        instance_globals: false,
        name: "pkg.unit".into(),
        package: "pkg".into(),
        file: "unit.py".into(),
        resolve_file_at_runtime: true,
    };
    let (hir, errors) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    // 用户读取动态元数据不是合成前言，仍须保留原整函数降级。
    assert_eq!(types.mode(hir.main), ryupy_ty::FuncMode::Dynamic);
    let add = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "add")
        .unwrap()
        .0;
    assert_eq!(types.mode(add), ryupy_ty::FuncMode::Static);
    let (mut program, exports, errors) = ryupy_mir::build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    for name in ["__spec__", "__loader__", "__cached__", "__path__"] {
        let export = exports.iter().find(|e| e.name == name).unwrap();
        assert_eq!(program.globals[export.value].ty, MirType::Dynamic);
    }
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    let mut count = 0;
    for (id, function) in program.functions.iter() {
        for (_, block) in function.body.blocks.iter() {
            for stmt in &block.stmts {
                if let Statement::Assign {
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ModuleMetadata),
                            args,
                        },
                    ..
                } = stmt
                {
                    assert_eq!(id, program.entry, "用户函数不得在每次读取时查询元数据旁表");
                    assert_eq!(args.len(), 2);
                    count += 1;
                }
            }
        }
    }
    assert_eq!(count, 4);
    let program = ryupy_mir::link(
        vec![(
            program,
            ryupy_mir::UnitMeta {
                instance_capabilities: Default::default(),
                execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
                environment_reason: "existing-contract".into(),
                location: ryupy_mir::ModuleLocation::SourceAbsolute("/source/pkg/unit.py".into()),
                file_backed_by_global: true,
                is_package: false,
                protected_exports: vec![],
                name: info.name,
                package: info.package,
                file: info.file,
                exports,
            },
        )],
        0,
    );
    let options = ryupy_codegen_cil::EmitOptions {
        assembly_name: "metadata_structure".into(),
        entry_source_path: None,
    };
    let cil = ryupy_codegen_cil::dump_cil(&program, &options).unwrap();
    assert_eq!(cil.matches("PyModules.ModuleMetadata(").count(), 4);
    let bytes = ryupy_codegen_cil::emit_assembly(&program, &options).unwrap();
    assert!(bytes.starts_with(b"MZ"));
}
