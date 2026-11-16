//! 实例源码异常链的实际MIR能力/来源验证；合成with与裸链分别验收。
use crate::*;
fn lower(source: &str) -> (ryupy_hir::Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("handler.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let mut info = ryupy_hir::ModuleInfo {
        instance_globals: true,
        ..Default::default()
    };
    info.instance_capabilities.ordinary_name_handlers = true;
    ryupy_hir::lower_module(&parsed, &info, source)
}
fn compile(source: &str) -> Program {
    let (hir, diagnostics) = lower(source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        validate::validate(&program).is_ok(),
        "{:?}",
        validate::validate(&program)
    );
    program
}
#[test]
fn pure_bare_retains_source_contract_and_actual_catch() {
    let mut p = compile("try:\n    pass\nexcept:\n    pass\n");
    assert_eq!(p.functions[p.entry].body.source_handler_contracts.len(), 1);
    let region = p.functions[p.entry]
        .body
        .regions
        .iter_mut()
        .find(|r| r.source_handler_chain.is_some())
        .unwrap();
    region.source_handler_chain = None;
    assert!(
        validate::validate(&p)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("源码异常链"))
    );
}
#[test]
fn typed_shortcut_and_missing_capability_are_rejected() {
    for typed in [false, true] {
        let mut p = compile("try:\n    pass\nexcept ValueError:\n    pass\n");
        if typed {
            let region = p.functions[p.entry]
                .body
                .regions
                .iter_mut()
                .find(|r| r.source_handler_chain.is_some())
                .unwrap();
            region.kind = RegionKind::Catch {
                type_names: vec!["ValueError".into()],
            };
        } else {
            p.functions[p.entry]
                .instance_capabilities
                .ordinary_name_handlers = false;
        }
        assert!(
            validate::validate(&p)
                .unwrap_err()
                .iter()
                .any(|e| e.contains("源码异常链"))
        );
    }
}
#[test]
fn with_and_generator_without_source_handler_do_not_require_new_contract() {
    for source in [
        "def f(cm):\n    with cm:\n        pass\n",
        "def f(cm):\n    with cm:\n        yield 1\n",
        "try:\n    pass\nfinally:\n    pass\n",
    ] {
        let p = compile(source);
        assert!(
            p.functions
                .iter()
                .all(|(_, f)| f.body.source_handler_contracts.is_empty())
        );
    }
}
#[test]
fn generator_class_and_lexical_explicit_deletions_stay_negative() {
    for source in [
        "def f():\n    try:\n        yield 1\n    except:\n        pass\n",
        "class C:\n    try:\n        pass\n    except:\n        pass\n",
        "def f(x):\n    del x\n",
        "def f(x):\n    def g():\n        return x\n    del x\n",
        "def f(x):\n    def g():\n        nonlocal x\n        del x\n",
        "try:\n    pass\nexcept (ValueError,):\n    pass\n",
    ] {
        assert!(!lower(source).1.is_empty(), "{source}");
    }
}
#[test]
fn generator_frame_cannot_claim_source_handler_but_may_inherit_capability() {
    let mut p = compile("try:\n    pass\nexcept:\n    pass\n");
    let state = p.functions[p.entry].module_state.unwrap();
    p.functions[p.entry].module_state_source = Some(InstanceStateSource::GeneratorFrame {
        frame: state,
        slot: 4,
    });
    assert!(
        validate::validate(&p)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("源码异常链"))
    );
}

#[test]
fn source_chain_ids_and_linked_capability_are_not_advisory() {
    for fault in 0..3 {
        let mut p = compile("try:\n    pass\nexcept:\n    pass\n");
        let body = &mut p.functions[p.entry].body;
        match fault {
            0 => body.source_handler_contracts.clear(),
            1 => {
                let region = body
                    .regions
                    .iter_mut()
                    .find(|r| r.source_handler_chain.is_some())
                    .unwrap();
                region.source_handler_chain = Some(999);
            }
            _ => {
                let region = body
                    .regions
                    .iter()
                    .find(|r| r.source_handler_chain.is_some())
                    .unwrap();
                let duplicate = Region {
                    source_handler_chain: region.source_handler_chain,
                    kind: RegionKind::CatchAll,
                    try_start: region.try_start,
                    try_len: region.try_len,
                    handler: region.handler,
                    exc_local: region.exc_local,
                };
                body.regions.push(duplicate);
            }
        }
        assert!(
            validate::validate(&p)
                .unwrap_err()
                .iter()
                .any(|e| e.contains("源码异常链"))
        );
    }
    let p = compile("try:\n    pass\nexcept:\n    pass\n");
    let linked = link(
        vec![(
            p,
            UnitMeta {
                execution: ModuleExecutionKind::InstanceGlobals,
                instance_capabilities: Default::default(),
                environment_reason: "explicit-instance".into(),
                location: ModuleLocation::SourceAbsolute("/virtual/handler.py".into()),
                file_backed_by_global: true,
                is_package: false,
                protected_exports: vec![],
                name: "handler".into(),
                package: "".into(),
                file: "handler.py".into(),
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

#[test]
fn instance_hir_cannot_drop_source_requirement_or_ignore_target() {
    for semantics in [
        ryupy_hir::HandlerSemantics::SourceStatic,
        ryupy_hir::HandlerSemantics::SourceDynamic,
        ryupy_hir::HandlerSemantics::SynthesizedWith,
    ] {
        let (mut hir, diagnostics) = lower("try:\n    pass\nexcept ValueError:\n    pass\n");
        assert!(diagnostics.is_empty());
        for (_, handler) in hir.functions[hir.main].body.handlers.iter_mut() {
            handler.semantics = semantics;
        }
        let (types, diagnostics) = ryupy_ty::check_module(&hir);
        assert!(diagnostics.is_empty());
        let (_, _, diagnostics) = build_program(&hir, &types);
        assert!(!diagnostics.is_empty(), "{semantics:?}");
    }
}
