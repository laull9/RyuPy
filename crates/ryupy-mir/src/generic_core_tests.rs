//! 真实HIR复用降级的结构门：typed ABI与单份发布保留，伴生关系必须随link重定位。
use crate::*;

const SOURCE: &str = "def leaf(value: int) -> int:\n    return value\ndef forward(value: int) -> int:\n    return leaf(value)\nprint(forward(3))\n";
fn compile(name: &str, enabled: bool) -> (Program, Vec<ModuleExport>) {
    compile_source(name, enabled, SOURCE)
}
fn compile_source(name: &str, enabled: bool, source: &str) -> (Program, Vec<ModuleExport>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add(format!("{name}.py"), source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: name.into(),
            file: format!("{name}.py"),
            ..Default::default()
        },
        source,
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

#[test]
fn source_runtime_dataclass_construction_stays_dynamic_and_has_no_companion() {
    let source =
        include_str!("../../../tests/e2e/phase3/c2_function_companion_dataclass_helper.py");
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("dataclass_helper.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "dataclass_helper".into(),
            file: "dataclass_helper.py".into(),
            ..Default::default()
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (_, class) = hir
        .classes
        .iter()
        .find(|(_, c)| c.name.as_str() == "Box")
        .unwrap();
    assert!(class.is_dataclass && class.runtime_dataclass);
    let function = hir
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "make_box")
        .unwrap()
        .0;
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert_eq!(types.mode(function), ryupy_ty::FuncMode::Dynamic);
    let (mut program, _, diagnostics) = build::build_program_with_generic(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    let functions: Vec<_> = program
        .functions
        .iter()
        .filter(|(_, f)| f.name == "make_box")
        .collect();
    assert_eq!(functions.len(), 1);
    let (_, function) = functions[0];
    assert_eq!(function.core, FunctionCore::Ordinary);
    assert_eq!(function.param_meta[0].ty, MirType::Dynamic);
    assert!(function.dynamic_origin.is_some());
    assert!(
        !rvalues(function).any(|v| matches!(v, Rvalue::Unbox { .. } | Rvalue::NewObject { .. }))
    );
    assert!(rvalues(function).any(|v| matches!(
        v,
        Rvalue::DynCallValue { .. }
            | Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CallValue),
                ..
            }
    )));
}

#[test]
fn generic_range_uses_object_iteration_and_preserves_original_counted_paths() {
    let source = "def fold(seed: int, n: int) -> int:\n    total = seed\n    for value in range(n):\n        total += value\n    return total\n";
    let (program, _) = compile_source("range_helper", true, source);
    let (_, typed) = program
        .functions
        .iter()
        .find(|(_, f)| f.name == "fold" && f.signature_global.is_some())
        .unwrap();
    let FunctionCore::TypedWithGeneric { generic } = typed.core else {
        panic!("需保typed及通用两个核心")
    };
    assert_eq!(typed.ret, MirType::I64);
    assert_eq!(typed.params.len(), 2);
    assert!(
        typed
            .params
            .iter()
            .all(|id| typed.body.locals[*id].ty == MirType::I64)
    );
    assert!(rvalues(typed).any(|v| matches!(v, Rvalue::Binary { op: BinOp::Add, .. })));
    assert!(!rvalues(typed).any(|v| matches!(
        v,
        Rvalue::Unbox { .. }
            | Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::IterStart),
                ..
            }
    )));
    let generic = &program.functions[generic];
    assert!(!rvalues(generic).any(|v| matches!(
        v,
        Rvalue::Unbox { .. }
            | Rvalue::Call {
                callee: Callee::Fn(_),
                ..
            }
    )));
    for runtime in [
        RuntimeFn::IterStart,
        RuntimeFn::IterNextOk,
        RuntimeFn::IterValue,
    ] {
        assert_eq!(rvalues(generic).filter(|v| matches!(v, Rvalue::Call { callee: Callee::Runtime(actual), .. } if *actual == runtime)).count(), 1);
    }
    assert_eq!(rvalues(generic).filter(|v| matches!(v, Rvalue::Call { callee: Callee::Runtime(RuntimeFn::DynBuiltin), args } if matches!(args.first(), Some(Operand::Const(Constant::Str(name))) if name == "range"))).count(), 1);
    // 普通Dynamic的既有计数优化没有因伴生体修复迁移到迭代对象路径。
    let dynamic_source = source
        .replace("seed: int, n: int", "seed, n")
        .replace(" -> int", "");
    let (dynamic, _) = compile_source("dynamic_range", true, &dynamic_source);
    let function = dynamic
        .functions
        .iter()
        .find(|(_, f)| f.name == "fold")
        .unwrap()
        .1;
    assert_eq!(function.core, FunctionCore::Ordinary);
    assert!(rvalues(function).any(|v| matches!(v, Rvalue::Unbox { .. })));
    assert!(!rvalues(function).any(|v| matches!(
        v,
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::IterStart),
            ..
        }
    )));
}
