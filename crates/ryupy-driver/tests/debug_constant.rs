//! __debug__固定非-O语言常量：显式/自动实例准入、真实PE执行及语法拒绝不产出程序。
use ryupy_driver::{BuildOptions, ModuleEnvironmentPolicy, Session};

#[test]
fn debug_load_works_in_explicit_and_pilot_instances_without_becoming_global_lookup() {
    let root = std::env::temp_dir().join(format!("ryupy-debug-instance-{}", std::process::id()));
    std::fs::create_dir_all(&root).unwrap();
    std::fs::write(
        root.join("subject.py"),
        "def read(value=__debug__):\n    return __debug__, value\n",
    )
    .unwrap();
    std::fs::write(root.join("entry.py"), "import subject\nprint(__debug__, subject.read())\nsetattr(subject, '__debug__', False)\nprint(subject.read(), getattr(subject, '__debug__'))\ntry:\n    assert False, 'active'\nexcept AssertionError as e:\n    print(str(e))\n").unwrap();
    for explicit in [false, true] {
        let mut session = Session {
            module_environment_policy: if explicit {
                ModuleEnvironmentPolicy::ExplicitOnly
            } else {
                ModuleEnvironmentPolicy::OrdinaryDynamicPilot
            },
            instance_modules: if explicit {
                vec!["subject".into()]
            } else {
                vec![]
            },
            ..Default::default()
        };
        let artifact = ryupy_driver::build(
            &mut session,
            &BuildOptions {
                input: root.join("entry.py"),
                out_dir: root.join(if explicit { "explicit" } else { "pilot" }),
                aot: false,
                search_paths: vec![],
            },
        )
        .unwrap_or_else(|error| panic!("{error}: {}", session.render_diagnostics(false)));
        let decision = session
            .module_environment_report
            .iter()
            .find(|m| m.module == "subject")
            .unwrap();
        assert!(decision.compiled);
        assert_eq!(decision.execution, "InstanceGlobals");
        let output = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
        assert!(
            output.status.success(),
            "{}",
            String::from_utf8_lossy(&output.stderr)
        );
        assert!(output.stderr.is_empty());
        assert_eq!(
            String::from_utf8(output.stdout).unwrap(),
            "True (True, True)\n(True, True) False\nactive\n"
        );
    }
    std::fs::remove_dir_all(root).unwrap();
}

#[test]
fn illegal_debug_write_rejects_before_output_artifact_is_created() {
    let root = std::env::temp_dir().join(format!("ryupy-debug-reject-{}", std::process::id()));
    std::fs::create_dir_all(&root).unwrap();
    for (index, source) in [
        "__debug__ = False",
        "del __debug__",
        "obj.__debug__ = 1",
        "f(__debug__=1)",
        "def f(__debug__): pass",
    ]
    .iter()
    .enumerate()
    {
        let input = root.join(format!("case{index}.py"));
        std::fs::write(&input, format!("print('MUST_NOT_EXECUTE')\n{source}\n")).unwrap();
        let mut session = Session::default();
        let output = root.join(format!("out{index}"));
        assert!(
            ryupy_driver::build(
                &mut session,
                &BuildOptions {
                    input,
                    out_dir: output.clone(),
                    aot: false,
                    search_paths: vec![]
                }
            )
            .is_err()
        );
        assert!(session.diagnostics.iter().any(|e| e.code == Some("E0001")));
        assert!(!output.join(format!("case{index}.dll")).exists());
    }
    std::fs::remove_dir_all(root).unwrap();
}
