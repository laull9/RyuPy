//! 通用模块字典profile的图/位置/注册合同；运行时真实D与C API现场由双独立产品门同次CP差分。
use super::*;

fn session(root: &Path) -> Session {
    use crate::cpython_provider::{ContainerTransfer, Plan, Source, version::PythonVersion};
    let mut sess = Session {
        experimental_stock_module_namespaces: true,
        cpython_provider: Some(root.join("frozen.json")),
        ..Default::default()
    };
    sess.cpython_provider_state.plan = Some(Plan {
        version: PythonVersion::new(3,12,13), root: root.into(),
        source: serde_json::from_value::<Source>(serde_json::json!({"project":"fixture", "build_id":"frozen", "source_url":"handwritten-fixture", "licenses":[]})).unwrap(),
        modules: vec!["math".into(), "ssl".into()], stdlib: Default::default(), declared: Default::default(), packages: Default::default(),
        container_transfer: ContainerTransfer::Proxy, payloads: vec![], delegation: Default::default(), bridge: root.into(), shim: root.join("shim"), _shim_build: None, dependency_report: serde_json::Value::Null,
    });
    sess
}
fn tree() -> crate::test_temp::Root {
    let root = crate::test_temp::Root::new("stock-module-namespaces");
    std::fs::create_dir(root.join("harbor")).unwrap();
    for (path, text) in [
        (
            "entry.py",
            "from harbor import state, calls\nprint(calls.read())\n",
        ),
        ("harbor/__init__.py", "marker = 13\n"),
        (
            "harbor/state.py",
            "value = 5\ndef expose():\n    return globals()\ndef read():\n    return value\n",
        ),
        (
            "harbor/calls.py",
            "from . import state\ndef read():\n    try:\n        return state.read()\n    except (ValueError, state.problem):\n        return 0\n",
        ),
    ] {
        std::fs::write(root.join(path), text).unwrap();
    }
    root
}

#[test]
fn whole_graph_uses_existing_instance_abi_and_stock_module_registration() {
    let root = tree();
    for aot in [false, true] {
        let mut sess = session(&root);
        let (linked, _, _) =
            compile_linked_locations(&mut sess, &root.join("entry.py"), &[], false, None, aot)
                .unwrap_or_else(|error| panic!("{error}: {}", sess.render_diagnostics(false)));
        assert_eq!(linked.modules.len(), 4);
        for (_, module) in linked.modules.iter() {
            assert_eq!(
                module.execution,
                ryupy_mir::ModuleExecutionKind::InstanceGlobals
            );
            assert!(module.instance_capabilities.provider_namespace);
            assert!(matches!(
                module.location,
                ryupy_mir::ModuleLocation::BundleRelative(_)
            ));
            assert_eq!(
                module.environment_reason,
                "experimental-stock-module-namespaces"
            );
        }
        assert!(
            linked
                .functions
                .iter()
                .all(|(_, f)| f.core != ryupy_mir::FunctionCore::StockThinStatic)
        );
        let cil = ryupy_codegen_cil::dump_source_cil(
            &linked,
            &ryupy_codegen_cil::EmitOptions {
                assembly_name: "NamespaceContract".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        assert_eq!(
            cil.matches("PyModules.RegisterStockModuleSourceLocated")
                .count(),
            4,
            "{cil}"
        );
        assert!(!cil.contains("PyModules.RegisterStockEntrySourceLocated"));
        assert!(!cil.contains("PyModules.RegisterInstanceSourceLocated"));
        let audit = &sess
            .cpython_provider_state
            .plan
            .as_ref()
            .unwrap()
            .dependency_report["module_namespaces"];
        assert_eq!(audit["modules"].as_array().unwrap().len(), 4);
        assert!(
            sess.module_environment_report
                .iter()
                .all(|row| row.provider_namespace && row.compiled)
        );
    }
}

#[test]
fn profile_rejects_mixed_authority_and_unproven_source_before_hir() {
    let root = tree();
    for mode in 0..6 {
        let mut sess = session(&root);
        match mode {
            0 => sess.cpython_provider_state.plan = None,
            1 => sess.fixed_binding_build = true,
            2 => sess.cpython_entry = true,
            3 => sess.cpython_modules.push("harbor".into()),
            4 => sess.instance_modules.push("harbor.state".into()),
            5 => {
                sess.cpython_provider_state
                    .plan
                    .as_mut()
                    .unwrap()
                    .version
                    .micro += 1
            }
            _ => unreachable!(),
        }
        assert!(compile_linked(&mut sess, &root.join("entry.py"), &[], false, None).is_err());
        assert!(
            sess.render_diagnostics(false)
                .contains("provider模块字典profile要求")
        );
    }
    for source in [
        "def read():\n    return (v for v in [1])\n",
        "try:\n    pass\nexcept make_target():\n    pass\n",
        "type Alias = int\n",
    ] {
        std::fs::write(root.join("entry.py"), source).unwrap();
        let mut sess = session(&root);
        assert!(compile_linked(&mut sess, &root.join("entry.py"), &[], false, None).is_err());
        assert!(sess.module_environment_report.is_empty());
    }
    assert!(!Session::default().experimental_stock_module_namespaces);
}
