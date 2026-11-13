//! 显式普通模块相对导入的真实CP/PE差分与自动选择不扩域合同。
#[path = "../src/test_temp.rs"]
mod test_temp;
use ryupy_driver::{BuildOptions, ModuleEnvironmentPolicy, Session};
use std::process::Command;

#[test]
fn relative_instance_import_preserves_fromlist_and_bindings() {
    compare("entry.py");
    compare("loader_entry.py");
    compare("failure_entry.py");
    compare("scope_entry.py");
    compare("user_exception_entry.py");
}
fn compare(entry: &str) {
    let source =
        ryupy_driver::runtime::workspace_root().join("tests/fixtures/module_relative_import");
    let root = test_temp::Root::new("relative-instance-import");
    let expected = Command::new(std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into()))
        .arg("-B")
        .arg(source.join(entry))
        .output()
        .unwrap();
    assert!(expected.status.success(), "{:?}", expected);
    assert!(expected.stderr.is_empty());
    std::fs::write(root.join("cpython.stdout"), &expected.stdout).unwrap();
    std::fs::write(root.join("cpython.stderr"), &expected.stderr).unwrap();
    for explicit in [false, true] {
        let mut session = Session {
            module_environment_policy: ModuleEnvironmentPolicy::OrdinaryDynamicPilot,
            instance_modules: if explicit {
                if entry == "user_exception_entry.py" {
                    vec![
                        "failpkg.user_matching".into(),
                        "failpkg.class_matching".into(),
                        "failpkg.overlap_matching".into(),
                        "failpkg.wrong_matching".into(),
                    ]
                } else if entry == "scope_entry.py" {
                    vec!["pkg.scoped".into()]
                } else if entry == "pending_error_name_entry.py" {
                    vec![
                        "failpkg.subject".into(),
                        "pkg.failing".into(),
                        "failpkg.matching".into(),
                    ]
                } else if entry == "failure_entry.py" {
                    vec!["failpkg.subject".into(), "pkg.failing".into()]
                } else if entry == "loader_entry.py" {
                    vec!["pkg.subject".into(), "cycle.subject".into()]
                } else {
                    vec!["pkg.subject".into()]
                }
            } else {
                vec![]
            },
            ..Default::default()
        };
        let artifact = ryupy_driver::build(
            &mut session,
            &BuildOptions {
                input: source.join(entry),
                out_dir: root.join(if explicit { "explicit" } else { "automatic" }),
                aot: false,
                search_paths: vec![],
            },
        )
        .unwrap_or_else(|error| panic!("{error}: {}", session.render_diagnostics(false)));
        let decision = session
            .module_environment_report
            .iter()
            .find(|d| {
                d.module
                    == if entry == "user_exception_entry.py" {
                        "failpkg.user_matching"
                    } else if entry == "scope_entry.py" {
                        "pkg.scoped"
                    } else if entry == "failure_entry.py" || entry == "pending_error_name_entry.py"
                    {
                        "failpkg.subject"
                    } else {
                        "pkg.subject"
                    }
            })
            .unwrap();
        assert_eq!(
            decision.execution,
            if explicit {
                "InstanceGlobals"
            } else {
                "LegacySingleton"
            }
        );
        assert_eq!(decision.relative_from_import, explicit);
        std::fs::write(
            root.join(if explicit {
                "explicit-plan.json"
            } else {
                "automatic-plan.json"
            }),
            session.module_environment_report_json().unwrap(),
        )
        .unwrap();
        if explicit {
            let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
            std::fs::write(root.join("actual.stdout"), &actual.stdout).unwrap();
            std::fs::write(root.join("actual.stderr"), &actual.stderr).unwrap();
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

// 完整失败源保留：CP能写异常name，当前旧通用异常属性协议在导入前拒绝。
#[test]
#[ignore = "既有ModuleNotFoundError.name属性写入未支持，不计相对导入strict通过"]
fn relative_import_pending_exception_name_attribute() {
    compare("pending_error_name_entry.py");
}
