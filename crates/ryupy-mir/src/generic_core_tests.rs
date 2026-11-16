//! 真实HIR复用降级的结构门：typed ABI与单份发布保留，伴生关系必须随link重定位。
use crate::*;

const SOURCE: &str = "def leaf(value: int) -> int:\n    return value\ndef forward(value: int) -> int:\n    return leaf(value)\nprint(forward(3))\n";
fn compile(name: &str, enabled: bool) -> (Program, Vec<ModuleExport>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add(format!("{name}.py"), SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: name.into(),
            file: format!("{name}.py"),
            ..Default::default()
        },
        SOURCE,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (mut program, exports, diagnostics) = if enabled {
        build::build_program_with_generic(&hir, &types)
    } else {
        build_program(&hir, &types)
    };
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    validate::validate(&program).unwrap();
    (program, exports)
}
fn unit(name: &str) -> (Program, UnitMeta) {
    let (program, exports) = compile(name, true);
    (
        program,
        UnitMeta {
            instance_capabilities: Default::default(),
            execution: ModuleExecutionKind::LegacySingleton,
            environment_reason: "companion-structure".into(),
            location: ModuleLocation::LegacyDiagnostic,
            file_backed_by_global: true,
            is_package: false,
            protected_exports: vec![],
            name: name.into(),
            package: String::new(),
            file: format!("{name}.py"),
            exports,
        },
    )
}
fn rvalues(function: &Function) -> impl Iterator<Item = &Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter_map(|s| match s {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
}
#[test]
fn generic_core_keeps_static_abi_and_only_canonical_publication() {
    let (ordinary, _) = compile("__main__", false);
    assert_eq!(ordinary.functions.len(), 3);
    assert!(
        ordinary
            .functions
            .iter()
            .all(|(_, f)| f.core == FunctionCore::Ordinary)
    );
    let (program, _) = compile("__main__", true);
    assert_eq!(program.functions.len(), 5);
    let published: Vec<_> = rvalues(&program.functions[program.entry])
        .filter_map(|v| match v {
            Rvalue::MakeFunction { func, .. } => Some(*func),
            _ => None,
        })
        .collect();
    assert_eq!(published.len(), 2);
    for id in published {
        let typed = &program.functions[id];
        let FunctionCore::TypedWithGeneric { generic } = typed.core else {
            panic!("typed必须有显式伴生关系")
        };
        assert_eq!(typed.params.len(), 1);
        assert_eq!(typed.body.locals[typed.params[0]].ty, MirType::I64);
        assert_eq!(typed.ret, MirType::I64);
        assert!(typed.dynamic_origin.is_none());
        let generic = &program.functions[generic];
        assert_eq!(
            generic.core,
            FunctionCore::GenericCompanion { canonical: id }
        );
        assert_eq!(generic.ret, MirType::Dynamic);
        assert!(generic.signature_global.is_none());
        assert_eq!(generic.params.len(), 2);
        assert!(
            generic
                .params
                .iter()
                .all(|id| generic.body.locals[*id].ty == MirType::Dynamic)
        );
        assert!(!rvalues(typed).any(|v| matches!(
            v,
            Rvalue::Unbox { .. } | Rvalue::Box { .. } | Rvalue::DynCallValue { .. }
        )));
        if typed.name == "forward" {
            assert!(rvalues(generic).any(|v| matches!(
                v,
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::CallValueGeneric),
                    ..
                }
            )));
        }
    }
}
#[test]
fn generic_core_link_remaps_pairs_and_rejects_cross_unit_counterfeits() {
    let mut linked = link(vec![unit("first"), unit("second")], 0);
    validate::validate_linked_exports(&linked).unwrap();
    let pairs: Vec<_> = linked
        .functions
        .iter()
        .filter_map(|(id, f)| match f.core {
            FunctionCore::TypedWithGeneric { generic } => Some((id, generic)),
            _ => None,
        })
        .collect();
    assert_eq!(pairs.len(), 4);
    for (typed, generic) in &pairs {
        assert_eq!(
            linked.functions[*generic].core,
            FunctionCore::GenericCompanion { canonical: *typed }
        );
        assert_eq!(
            source_provenance::for_function(&linked.globals, *typed),
            source_provenance::for_function(&linked.globals, *generic)
        );
    }
    let ((first, first_generic), (second, second_generic)) = (pairs[0], pairs[2]);
    linked.functions[first].core = FunctionCore::TypedWithGeneric {
        generic: second_generic,
    };
    linked.functions[second_generic].core = FunctionCore::GenericCompanion { canonical: first };
    linked.functions[second].core = FunctionCore::TypedWithGeneric {
        generic: first_generic,
    };
    linked.functions[first_generic].core = FunctionCore::GenericCompanion { canonical: second };
    assert!(
        validate::validate_linked_exports(&linked)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("通用伴生体"))
    );
}
#[test]
fn generic_core_rejects_metadata_duplication_and_independent_publication() {
    let (mut program, _) = compile("__main__", true);
    let (typed, generic) = program
        .functions
        .iter()
        .find_map(|(id, f)| match f.core {
            FunctionCore::TypedWithGeneric { generic } => Some((id, generic)),
            _ => None,
        })
        .unwrap();
    program.functions[generic].signature_global = program.functions[typed].signature_global;
    assert!(validate::validate(&program).is_err());
    program.functions[generic].signature_global = None;
    let main = &mut program.functions[program.entry];
    for (_, block) in main.body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign {
                value: Rvalue::MakeFunction { func, .. },
                ..
            } = statement
                && *func == typed
            {
                *func = generic;
            }
        }
    }
    assert!(
        validate::validate(&program)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("不能独立创建"))
    );
}
