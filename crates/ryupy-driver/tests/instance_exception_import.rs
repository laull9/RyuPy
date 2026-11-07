//! 普通Name/bare实例异常链的完整CP/PE差分，自动选择与显式能力分别审计。
use ryupy_driver::{BuildOptions, ModuleEnvironmentPolicy, Session};
use std::process::Command;
#[test]
fn instance_exception_lexical_owner_and_cleanup_contract() {
    let source = ryupy_driver::runtime::workspace_root().join("tests/fixtures/instance_exception");
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = std::env::temp_dir().join(format!(
        "ryupy-instance-handler-{}-{stamp}",
        std::process::id()
    ));
    std::fs::create_dir_all(&root).unwrap();
    let expected = Command::new(std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into()))
        .arg("-B")
        .arg(source.join("entry.py"))
        .output()
        .unwrap();
    std::fs::write(root.join("cp.stdout"), &expected.stdout).unwrap();
    std::fs::write(root.join("cp.stderr"), &expected.stderr).unwrap();
    assert!(expected.status.success());
    assert!(expected.stderr.is_empty());
    for explicit in [false, true] {
        let mut session = Session {
            module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
            include_modules: vec!["captured".into()],
            instance_modules: if explicit {
                vec!["lookups".into(), "cleanup".into(), "captured".into()]
            } else {
                vec![]
            },
            ..Default::default()
        };
        let mode = if explicit { "explicit" } else { "automatic" };
        let artifact = ryupy_driver::build(
            &mut session,
            &BuildOptions {
                input: source.join("entry.py"),
                out_dir: root.join(mode),
                aot: false,
                search_paths: vec![],
            },
        )
        .unwrap_or_else(|e| panic!("{e}: {}", session.render_diagnostics(false)));
        std::fs::write(
            root.join(format!("{mode}-plan.json")),
            session.module_environment_report_json().unwrap(),
        )
        .unwrap();
        for name in ["lookups", "cleanup", "captured"] {
            let decision = session
                .module_environment_report
                .iter()
                .find(|d| d.module == name)
                .unwrap();
            assert_eq!(decision.ordinary_name_handlers, explicit);
            assert_eq!(
                decision.execution,
                if explicit {
                    "InstanceGlobals"
                } else {
                    "LegacySingleton"
                }
            );
        }
        if explicit {
            let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
            std::fs::write(root.join("jit.stdout"), &actual.stdout).unwrap();
            std::fs::write(root.join("jit.stderr"), &actual.stderr).unwrap();
            assert!(
                actual.status.success(),
                "{}",
                String::from_utf8_lossy(&actual.stderr)
            );
            assert_eq!(actual.stdout, expected.stdout);
            assert_eq!(actual.stderr, expected.stderr);
        }
    }
}
