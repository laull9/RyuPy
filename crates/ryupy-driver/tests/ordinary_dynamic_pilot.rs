//! 首包原源CP/真实PE差分：候选普通import与同源公开loader均复用唯一环境。
//! 不覆盖已有case；feature关闭的发行默认保持原路径，测试用显式Session隔离候选。
use ryupy_driver::{BuildOptions, ModuleEnvironmentPolicy, Session};
use std::{
    path::{Path, PathBuf},
    process::Command,
};
struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn compare(directory: &Path, entry: &str, selected: &[&str]) {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = Root(
        std::env::temp_dir().join(format!("ryupy-p1-contract-{}-{stamp}", std::process::id())),
    );
    std::fs::create_dir(&root.0).unwrap();
    let input = directory.join(entry);
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let expected = Command::new(python)
        .arg("-B")
        .arg(&input)
        .current_dir(directory)
        .output()
        .unwrap();
    std::fs::write(root.0.join("cpython.stdout"), &expected.stdout).unwrap();
    std::fs::write(root.0.join("cpython.stderr"), &expected.stderr).unwrap();
    assert!(
        expected.status.success() && expected.stderr.is_empty(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    for explicit in [false, true] {
        let mut session = Session {
            module_environment_policy: if explicit {
                ModuleEnvironmentPolicy::ExplicitOnly
            } else {
                ModuleEnvironmentPolicy::OrdinaryDynamicPilot
            },
            instance_modules: if explicit {
                selected.iter().map(|name| (*name).into()).collect()
            } else {
                vec![]
            },
            ..Default::default()
        };
        let artifact = ryupy_driver::build(
            &mut session,
            &BuildOptions {
                input: input.clone(),
                out_dir: root.0.join(if explicit { "explicit" } else { "automatic" }),
                aot: false,
                search_paths: vec![],
            },
        )
        .unwrap_or_else(|error| panic!("{error}: {}", session.render_diagnostics(false)));
        for target in selected {
            let decision = session
                .module_environment_report
                .iter()
                .find(|decision| decision.module == *target)
                .unwrap();
            assert!(decision.compiled);
            assert_eq!(decision.execution, "InstanceGlobals");
            assert!(if explicit {
                decision.reason == "explicit-instance"
            } else {
                decision.reason.starts_with("ordinary-dynamic-pilot:")
            });
        }
        std::fs::write(
            root.0.join(if explicit {
                "explicit-plan.json"
            } else {
                "automatic-plan.json"
            }),
            session.module_environment_report_json().unwrap(),
        )
        .unwrap();
        let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
        let label = if explicit { "explicit" } else { "automatic" };
        std::fs::write(root.0.join(format!("{label}.stdout")), &actual.stdout).unwrap();
        std::fs::write(root.0.join(format!("{label}.stderr")), &actual.stderr).unwrap();
        assert_eq!(
            actual.status.code(),
            Some(0),
            "{}",
            String::from_utf8_lossy(&actual.stderr)
        );
        assert_eq!(actual.stdout, expected.stdout);
        assert_eq!(actual.stderr, expected.stderr);
    }
}
#[test]
fn ordinary_dynamic_complete_new_contracts() {
    let root =
        ryupy_driver::runtime::workspace_root().join("tests/fixtures/module_environment_pilot");
    for (directory, entry) in [
        ("probe", "entry.py"),
        ("probe", "loader_entry.py"),
        ("ordinary_retry_escape", "entry.py"),
        ("callee_binding_owner", "entry.py"),
    ] {
        compare(&root.join(directory), entry, &["subject"]);
    }
}
#[test]
fn ordinary_dynamic_preserves_complete_original_consumers() {
    let root = ryupy_driver::runtime::workspace_root().join("tests/cases");
    for (directory, target) in [
        ("phase3/m3_7_unbound_exports", "helper"),
        ("module_owners/function_globals", "subject"),
        ("module_owners/builtins_capture", "subject"),
        ("module_owners/generator_owner", "subject"),
        ("module_from_spec/direct_failure", "subject"),
    ] {
        compare(&root.join(directory), "entry.py", &[target]);
    }
}

#[test]
fn ordinary_dynamic_keeps_complete_defaults_reflection_boundary() {
    let input = ryupy_driver::runtime::workspace_root()
        .join("tests/fixtures/module_environment_pilot/callee_defaults_owner/entry.py");
    for automatic in [false, true] {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let root = Root(
            std::env::temp_dir().join(format!("ryupy-p1-defaults-{}-{stamp}", std::process::id())),
        );
        std::fs::create_dir(&root.0).unwrap();
        let mut session = Session {
            module_environment_policy: if automatic {
                ModuleEnvironmentPolicy::OrdinaryDynamicPilot
            } else {
                ModuleEnvironmentPolicy::ExplicitOnly
            },
            instance_modules: if automatic {
                vec![]
            } else {
                vec!["subject".into()]
            },
            ..Default::default()
        };
        let artifact = ryupy_driver::build(
            &mut session,
            &BuildOptions {
                input: input.clone(),
                out_dir: root.0.join("out"),
                aot: false,
                search_paths: vec![],
            },
        )
        .unwrap();
        let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
        std::fs::write(root.0.join("actual.stdout"), &actual.stdout).unwrap();
        std::fs::write(root.0.join("actual.stderr"), &actual.stderr).unwrap();
        assert_eq!(actual.status.code(), Some(1));
        assert_eq!(
            String::from_utf8_lossy(&actual.stdout).trim(),
            "callee captured before argument exactly once"
        );
        assert!(
            String::from_utf8_lossy(&actual.stderr)
                .contains("AttributeError: 'function' object has no attribute '__defaults__'")
        );
    }
}
