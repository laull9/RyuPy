//! 候选实际单源码图门与固定双版本oracle；临时输入/输出全部在仓库外并自动清理。
use super::*;
fn session(root: &Path) -> Session {
    use crate::cpython_provider::{ContainerTransfer, Plan, Source, version::PythonVersion};
    let mut session = Session {
        experimental_stock_thin_functions: true,
        cpython_provider: Some(root.join("provider.json")),
        ..Default::default()
    };
    session.cpython_provider_state.plan = Some(Plan {
        version: PythonVersion::new(3,12,13), root: root.into(), source: serde_json::from_value::<Source>(serde_json::json!({"project":"python-build-standalone", "build_id":"20260623", "source_url":"https://github.com/astral-sh/python-build-standalone/releases/tag/20260623", "licenses":[]})).unwrap(), modules: vec![], stdlib: Default::default(), declared: Default::default(), packages: Default::default(),
        container_transfer: ContainerTransfer::Proxy, payloads: vec![], delegation: Default::default(),
        bridge: root.into(), shim: root.join("shim"), _shim_build: None, dependency_report: serde_json::Value::Null,
    });
    session
}
#[test]
fn stock_thin_graph_freezes_candidate_and_keeps_namespace() {
    let root = crate::test_temp::Root::new("stock-thin-driver");
    for source in [
        include_str!("../../../tests/cases/stock_thin_compiler/identity.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/global_rebinding.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/builtin_and_function.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/shutdown.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/local_identity.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/local_unbound.py"),
        include_str!("../../../tests/cases/stock_thin_compiler/identity_order.py"),
    ] {
        std::fs::write(root.join("entry.py"), source).unwrap();
        let mut session = session(&root);
        let (linked, _, _) = compile_linked(&mut session, &root.join("entry.py"), &[], false, None)
            .unwrap_or_else(|e| panic!("{e}: {}", session.render_diagnostics(false)));
        assert_eq!(linked.modules.len(), 1);
        let module = &linked.modules[linked.entry];
        assert_eq!(
            module.execution,
            ryupy_mir::ModuleExecutionKind::InstanceGlobals
        );
        assert!(module.instance_capabilities.entry_namespace);
        assert_eq!(
            module.environment_reason,
            "experimental-stock-thin-functions"
        );
        assert!(linked.external_provider.is_some());
        ryupy_mir::source_provenance::require_linked(&linked).unwrap();
        let bytes = ryupy_codegen_cil::emit_assembly(
            &linked,
            &ryupy_codegen_cil::EmitOptions {
                assembly_name: "ThinDriver".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        assert!(!bytes.is_empty());
    }
}
#[test]
fn stock_thin_graph_rejects_provider_fixed_delegated_and_extra_source() {
    let root = crate::test_temp::Root::new("stock-thin-reject");
    std::fs::write(root.join("entry.py"), "def f(x):\n    return x\n").unwrap();
    for case in 0..7 {
        let mut s = session(&root);
        match case {
            0 => s.cpython_provider_state.plan = None,
            1 => s.fixed_binding_build = true,
            2 => s.cpython_entry = true,
            3 => {
                std::fs::write(root.join("extra.py"), "value = 1\n").unwrap();
                s.include_modules.push("extra".into());
            }
            4..=6 => {
                let versions = [(3, 11, 15), (3, 12, 14), (3, 14, 7)];
                let (major, minor, micro) = versions[case - 4];
                s.cpython_provider_state.plan.as_mut().unwrap().version =
                    crate::cpython_provider::version::PythonVersion::new(major, minor, micro);
            }
            _ => unreachable!(),
        }
        assert!(compile_linked(&mut s, &root.join("entry.py"), &[], false, None).is_err());
        assert!(s.render_diagnostics(false).contains("薄函数候选要求"));
    }
    assert!(!Session::default().experimental_stock_thin_functions);
}

#[test]
fn stock_thin_dump_freezes_a_fresh_session_before_qualification() {
    let root = crate::test_temp::Root::new("stock-thin-dump-prepare");
    let input = root.join("entry.py");
    std::fs::write(&input, "def f(x):\n    return x\n").unwrap();
    let mut session = Session {
        experimental_stock_thin_functions: true,
        cpython_provider: Some(root.join("missing-provider.json")),
        ..Default::default()
    };
    let error = dump(&mut session, &input, Stage::Mir).unwrap_err();
    // 回归旧行为会先报E0100，配置声明从未被冻结；现在由共同prepare呈现实际输入错误。
    assert!(format!("{error:#}").contains("CPython provider 声明不存在"));
    assert!(session.diagnostics.is_empty());
}
#[test]
#[ignore = "显式指定固定CPython3.12.13和3.13.3，现场执行手写oracle"]
fn stock_thin_pinned_two_version_oracle() {
    use std::process::Command;
    let files = [
        "identity.py",
        "global_rebinding.py",
        "builtin_and_function.py",
        "shutdown.py",
        "local_identity.py",
        "local_unbound.py",
        "identity_order.py",
    ];
    let root = Path::new(env!("CARGO_MANIFEST_DIR")).join("../../tests/cases/stock_thin_compiler");
    let mut observations = vec![];
    for (variable, version) in [
        ("RYUPY_STOCK_THIN_PYTHON312", "3.12.13"),
        ("RYUPY_STOCK_THIN_PYTHON313", "3.13.3"),
    ] {
        let executable = std::env::var(variable).expect("需要显式固定oracle解释器");
        let output = Command::new(&executable).args(["-I", "-c", "import platform; print(platform.python_version()); print(platform.python_implementation())"]).output().unwrap();
        assert!(output.status.success());
        assert_eq!(
            String::from_utf8(output.stdout).unwrap(),
            format!("{version}\nCPython\n")
        );
        let mut results = vec![];
        for file in files {
            let output = Command::new(&executable)
                .arg("-I")
                .arg(root.join(file))
                .output()
                .unwrap();
            assert!(
                output.status.success(),
                "{}",
                String::from_utf8_lossy(&output.stderr)
            );
            assert!(output.stderr.is_empty());
            results.push(output.stdout);
        }
        observations.push(results);
    }
    assert_eq!(observations[0], observations[1]);
}
