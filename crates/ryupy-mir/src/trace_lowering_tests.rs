//! 审计旁证保持真实 IR，不改变 Legacy 编译或伪造最终资格。
use crate::*;

#[test]
fn audit_build_and_link_preserve_real_generator_and_parameter_origins() {
    let source = include_str!("../../../tests/cases/generator_dynamic/frames/entry.py");
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("other_source.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty());
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty());
    let (ordinary, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty());
    let (program, exports, diagnostics, origins) = trace_lowering::build_for_audit(&hir, &types);
    assert!(diagnostics.is_empty());
    assert_eq!(format!("{ordinary:?}"), format!("{program:?}"));
    assert_eq!(origins.generators.len(), 1);
    let generator = &origins.generators[0];
    assert_ne!(generator.factory, generator.resume);
    assert_eq!(program.functions[generator.resume].params, vec![generator.frame_parameter]);
    for parameter in &program.functions[generator.factory].params {
        let slot = parameter.into_raw().into_u32() as usize + 4;
        assert!(matches!(generator.initial_slots[slot], Operand::Local(local) if local == *parameter));
    }
    let meta = UnitMeta {
        instance_capabilities: Default::default(),
        execution: ModuleExecutionKind::LegacySingleton,
        environment_reason: "trace-audit-only".into(),
        location: Default::default(),
        file_backed_by_global: false,
        is_package: false,
        protected_exports: Vec::new(),
        name: "__main__".into(),
        package: String::new(),
        file: "other_source.py".into(),
        exports,
    };
    let (linked, maps) = trace_lowering::link_for_audit(vec![(program, meta)], 0);
    let audit = trace_final_audit::inspect(&linked, &origins, &maps);
    assert!(audit.mapping_errors.is_empty(), "{:?}", audit.mapping_errors);
    assert!(!audit.operations.is_empty());
    assert!(audit.operations.iter().any(|site| matches!(site.operation, Rvalue::MakeGenerator { .. })));
    // 缺失link来源必须拒绝审计，而不能借单模块下标碰巧相等放行。
    assert!(!trace_final_audit::inspect(&linked, &origins, &[]).mapping_errors.is_empty());
}
