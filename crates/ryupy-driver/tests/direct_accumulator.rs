//! 直接累加快路必须在真实源码生成的MIR中命中；慢路仍报告原Python抛错帧。
use ryupy_driver::{Session, Stage};
mod support;

const FUNCTION: &str = "def accumulate(initial, values):\n    total = initial\n    for index in range(len(values)):\n        total += values[index]\n    return total, total\n";

#[test]
fn source_loop_really_uses_guarded_i64_locals_instead_of_holder() {
    let path = std::env::temp_dir().join("ryupy_direct_accumulator.py");
    std::fs::write(&path, FUNCTION).unwrap();
    let mut session = Session {
        experimental_direct_accumulator: true,
        ..Session::default()
    };
    let output = support::dump(&mut session, &path, Stage::Mir).unwrap();
    assert!(session.diagnostics.is_empty());
    assert!(
        output.contains("WrappingAdd"),
        "未命中直接累加pass：{output}"
    );
    assert!(output.contains("is_boxed_i64") && output.contains("unbox_exact_i64"));
    assert!(
        output.contains("RuntimeFn::DynBinary(const 257_i64,"),
        "不能移除完整动态回退"
    );
    assert!(!output.contains("NumericAccumulator"), "仍残留holder ABI");
    insta::assert_snapshot!("direct_accumulator_mir", output);
    std::fs::remove_file(path).unwrap();
}

#[test]
fn fallback_preserves_original_radd_traceback_line() {
    let root =
        ryupy_driver::runtime::workspace_root().join("target/test_direct_accumulator_traceback");
    std::fs::create_dir_all(&root).unwrap();
    let input = root.join("source.py");
    let source = format!(
        "{FUNCTION}class Raises:\n    def __radd__(self, other):\n        raise ValueError('original failure')\naccumulate(0, [1, Raises()])\n"
    );
    std::fs::write(&input, source).unwrap();
    let mut session = Session {
        experimental_direct_accumulator: true,
        ..Session::default()
    };
    let output = support::dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(output.contains("WrappingAdd"), "异常回归必须实际命中优化");
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("out"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let result = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert_eq!(result.status.code(), Some(1));
    let stderr = String::from_utf8_lossy(&result.stderr);
    assert!(
        stderr.contains("line 8, in __radd__"),
        "原抛错行丢失：{stderr}"
    );
    assert!(
        stderr.contains("line 4, in accumulate"),
        "累加源码位置丢失：{stderr}"
    );
    assert!(
        stderr.contains("ValueError: original failure"),
        "原异常丢失：{stderr}"
    );
    assert!(!stderr.contains("System."), "泄漏CLR异常：{stderr}");
}

#[test]
fn default_source_loop_still_uses_holder() {
    let path = std::env::temp_dir().join("ryupy_default_accumulator.py");
    std::fs::write(&path, FUNCTION).unwrap();
    let mut session = Session::default();
    assert!(!session.experimental_direct_accumulator);
    let output = support::dump(&mut session, &path, Stage::Mir).unwrap();
    assert!(
        output.contains("NumericAccumulatorCreate")
            && output.contains("NumericAccumulatorAdd")
            && output.contains("NumericAccumulatorValue")
    );
    assert!(!output.contains("WrappingAdd") && !output.contains("unbox_exact_i64"));
    std::fs::remove_file(path).unwrap();
}

#[test]
fn pinned_python_fixture_matches_holder_and_explicit_direct_mode() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/e2e/phase3/m3_9_direct_accumulator.py");
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = std::process::Command::new(&python)
        .arg("--version")
        .output()
        .unwrap();
    let expected_version = std::fs::read_to_string(root.join("tests/pinned-python.txt")).unwrap();
    assert!(
        String::from_utf8_lossy(&version.stdout).contains(expected_version.trim()),
        "请用PYTHON指定固定{}：当前{}",
        expected_version.trim(),
        String::from_utf8_lossy(&version.stdout)
    );
    let expected = std::process::Command::new(&python)
        .arg(&input)
        .output()
        .unwrap();
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    for direct in [false, true] {
        let mut session = Session {
            experimental_direct_accumulator: direct,
            ..Session::default()
        };
        let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
        assert_eq!(
            mir.contains("WrappingAdd"),
            direct,
            "两种模式必须实际选择对应pass"
        );
        let artifact = ryupy_driver::build(
            &mut session,
            &ryupy_driver::BuildOptions {
                input: input.clone(),
                out_dir: root.join(format!("target/test_accumulator_modes/{direct}")),
                aot: false,
                search_paths: vec![],
            },
        )
        .unwrap();
        let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
        assert_eq!(
            actual.status.code(),
            expected.status.code(),
            "模式{direct}: {}",
            String::from_utf8_lossy(&actual.stderr)
        );
        assert_eq!(actual.stdout, expected.stdout, "模式{direct}的stdout不一致");
        assert_eq!(actual.stderr, expected.stderr, "模式{direct}的stderr不一致");
    }
}
