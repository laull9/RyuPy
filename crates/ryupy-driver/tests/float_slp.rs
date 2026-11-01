//! 真packed SLP与相同守卫标量控制的集成验证；ISA关闭仍运行完整Python语义。
use ryupy_driver::{BuildOptions, Session, Stage};
mod support;

fn section<'a>(mir: &'a str, name: &str) -> &'a str {
    mir.split(&format!("fn {name}("))
        .nth(1)
        .unwrap_or_else(|| panic!("缺少函数{name}"))
        .split("\nfn ")
        .next()
        .unwrap()
}

#[test]
fn fresh_independent_pairs_really_vectorize_but_aliases_do_not() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/e2e/phase3/m3_10_float_slp.py");
    let default_mir = support::dump(&mut Session::default(), &input, Stage::Mir).unwrap();
    assert!(!default_mir.contains("vector128_supported()"));
    let mut packed = Session {
        experimental_packed_slp: true,
        ..Session::default()
    };
    let mir = support::dump(&mut packed, &input, Stage::Mir).unwrap();
    for name in ["pair", "asymmetric", "recurrence", "with_tail"] {
        let body = section(&mir, name);
        assert!(
            body.contains("vector128_supported()"),
            "缺ISA守卫{name}：{body}"
        );
        assert!(body.contains("vector_pack_f64"), "未实际打包{name}：{body}");
        assert!(
            body.contains("vector_lane_f64"),
            "未独立提取结果{name}：{body}"
        );
    }
    for name in ["shared", "observed"] {
        assert!(
            !section(&mir, name).contains("vector_pack_f64"),
            "非法配对{name}"
        );
    }
    let mut scalar = Session {
        diagnostic_scalar_slp: true,
        ..Session::default()
    };
    let scalar_mir = support::dump(&mut scalar, &input, Stage::Mir).unwrap();
    assert!(section(&scalar_mir, "pair").contains("vector128_supported()"));
    assert!(!scalar_mir.contains("vector_pack_f64"));
    assert!(!scalar_mir.contains("vector_lane_f64"));
}

#[test]
fn packed_scalar_control_and_disabled_isa_match_pinned_python() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/e2e/phase3/m3_10_float_slp.py");
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = std::process::Command::new(&python)
        .arg("--version")
        .output()
        .unwrap();
    let pinned = std::fs::read_to_string(root.join("tests/pinned-python.txt")).unwrap();
    assert!(String::from_utf8_lossy(&version.stdout).contains(pinned.trim()));
    let expected = std::process::Command::new(python)
        .arg(&input)
        .output()
        .unwrap();
    assert!(expected.status.success());
    for scalar in [false, true] {
        let mut session = Session {
            diagnostic_scalar_slp: scalar,
            experimental_packed_slp: !scalar,
            ..Session::default()
        };
        let artifact = ryupy_driver::build(
            &mut session,
            &BuildOptions {
                input: input.clone(),
                out_dir: root.join(format!("target/test_float_slp_{scalar}")),
                aot: false,
                search_paths: vec![],
            },
        )
        .unwrap();
        for hardware_off in [false, true] {
            let mut command = std::process::Command::new("dotnet");
            command.arg(&artifact.dll);
            if hardware_off {
                command.env("DOTNET_EnableHWIntrinsic", "0");
                command.env("COMPlus_EnableHWIntrinsic", "0");
            }
            let actual = command.output().unwrap();
            assert_eq!(
                actual.status.code(),
                expected.status.code(),
                "scalar={scalar} hardware_off={hardware_off}: {}",
                String::from_utf8_lossy(&actual.stderr)
            );
            assert_eq!(actual.stdout, expected.stdout);
            assert_eq!(actual.stderr, expected.stderr);
        }
    }
}
