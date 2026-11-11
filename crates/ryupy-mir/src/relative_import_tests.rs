//! 相对导入能力必须存在于实际MIR，不仅依赖driver选择或运行时参数类型。
use crate::*;
fn compile() -> Program {
    let source = "from .data import value\ndef f():\n    return value\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("relative.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    assert_eq!(
        ryupy_hir::ordinary_dynamic_eligibility(&parsed),
        ryupy_hir::OrdinaryDynamicEligibility::InstanceSyntaxBoundary
    );
    let mut info = ryupy_hir::ModuleInfo {
        instance_globals: true,
        package: "pkg".into(),
        ..Default::default()
    };
    assert!(!ryupy_hir::lower_module(&parsed, &info, source).1.is_empty());
    info.instance_capabilities.relative_from_import = true;
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(hir.functions[hir.main].body.stmts.iter().any(|(_, s)| matches!(&s.kind, ryupy_hir::StmtKind::ImportFrom { relative: Some((1, name)), .. } if name == "data")));
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty());
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty());
    program
}
#[test]
fn relative_import_requires_capability_and_real_context() {
    let mut program = compile();
    assert!(
        validate::validate(&program).is_ok(),
        "{:?}",
        validate::validate(&program)
    );
    assert!(pretty::dump(&program).contains("InstanceRelativeImport"));
    for (_, function) in program.functions.iter_mut() {
        function.instance_capabilities = Default::default();
    }
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("缺少相对导入能力"))
    );
    let mut program = compile();
    let main = &mut program.functions[program.entry];
    for (_, block) in main.body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign {
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceRelativeImport),
                        args,
                    },
                ..
            } = statement
            {
                args[0] = Operand::Const(Constant::Null);
            }
        }
    }
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("显式state"))
    );
}

#[test]
fn relative_import_invalid_entry_is_diagnostic() {
    let mut program = compile();
    program.entry = la_arena::Idx::from_raw(999999.into());
    assert!(validate::validate(&program).is_err());
}

#[test]
fn relative_capability_does_not_admit_class_body_imports_or_handlers() {
    for source in [
        "class C:\n    from .plain import value\n",
        "try:\n    pass\nexcept ValueError:\n    pass\n",
    ] {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("boundary.py", source);
        let (parsed, _) = ryupy_syntax::parse(file, source);
        let info = ryupy_hir::ModuleInfo {
            instance_globals: true,
            instance_capabilities: ryupy_base::InstanceCapabilities {
                direct_globals: false,
                relative_from_import: true,
                ordinary_name_handlers: false,
            },
            ..Default::default()
        };
        let (_, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
        assert!(!diagnostics.is_empty(), "{source}");
    }
}

#[test]
fn relative_import_linked_plan_cannot_drop_capability() {
    let program = compile();
    let linked = link(
        vec![(
            program,
            UnitMeta {
                execution: ModuleExecutionKind::InstanceGlobals,
                instance_capabilities: Default::default(),
                environment_reason: "explicit-instance".into(),
                location: ModuleLocation::SourceAbsolute("/virtual/relative.py".into()),
                file_backed_by_global: true,
                is_package: false,
                protected_exports: vec![],
                name: "pkg.subject".into(),
                package: "pkg".into(),
                file: "relative.py".into(),
                exports: vec![],
            },
        )],
        0,
    );
    assert!(
        validate::validate_linked_exports(&linked)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("计划能力与实际入口不一致"))
    );
}
