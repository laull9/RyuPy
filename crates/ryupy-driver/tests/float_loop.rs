//! 内层浮点累加必须真实命中；不以旧entry-holder或普通短链通过冒充跨迭代验证。
use ryupy_driver::{BuildOptions, Session, Stage};
mod support;

fn section<'a>(mir: &'a str, name: &str) -> &'a str {
    mir.split(&format!("fn {name}("))
        .nth(1)
        .unwrap_or_else(|| panic!("缺少被测试函数 {name}"))
        .split("\nfn ")
        .next()
        .unwrap()
}

#[test]
fn real_matrix_and_nested_fixture_use_cross_iteration_float_state() {
    let root = ryupy_driver::runtime::workspace_root();
    for (path, names) in [
        ("tests/benchmarks/matmul.py", vec!["matmul"]),
        (
            "tests/e2e/phase3/m3_10_float_loop.py",
            vec!["accumulate", "repeated", "zero_identity"],
        ),
    ] {
        let mut session = Session::default();
        let mir = support::dump(&mut session, &root.join(path), Stage::Mir).unwrap();
        for name in names {
            let body = section(&mir, name);
            assert!(body.contains("$float_loop_raw"), "未真实命中{name}：{body}");
            assert!(body.contains("$float_loop_mode"), "缺少慢路状态：{body}");
            assert!(body.contains("dyn.Add.inplace"), "原协议回退丢失：{body}");
        }
        if path.ends_with("m3_10_float_loop.py") {
            for name in ["observed", "interrupted", "early_return"] {
                assert!(
                    !section(&mir, name).contains("$float_loop_raw"),
                    "有观察或其它出口的 {name} 不得进入原生累加状态"
                );
            }
        }
    }
}

#[test]
fn loop_identity_reentry_and_protocol_fixture_match_pinned_python() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/e2e/phase3/m3_10_float_loop.py");
    let python = std::env::var_os("PYTHON").unwrap_or_else(|| "python3".into());
    let version = std::process::Command::new(&python)
        .arg("--version")
        .output()
        .unwrap();
    let pinned = std::fs::read_to_string(root.join("tests/pinned-python.txt")).unwrap();
    assert!(String::from_utf8_lossy(&version.stdout).contains(pinned.trim()));
    let expected = std::process::Command::new(&python)
        .arg(&input)
        .output()
        .unwrap();
    assert!(expected.status.success());
    let mut session = Session::default();
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input,
            out_dir: root.join("target/test_float_loop"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert_eq!(
        actual.status.code(),
        expected.status.code(),
        "{}",
        String::from_utf8_lossy(&actual.stderr)
    );
    assert_eq!(actual.stdout, expected.stdout);
    assert_eq!(actual.stderr, expected.stderr);
}

#[test]
fn failed_protocol_observes_materialized_previous_sum_and_original_trace() {
    let root = ryupy_driver::runtime::workspace_root();
    let dir = root.join("target/test_float_loop_trace");
    std::fs::create_dir_all(&dir).unwrap();
    let input = dir.join("float_loop_error.py");
    std::fs::write(
        &input,
        "class Bad:\n    def __mul__(self, other):\n        return self\n    def __radd__(self, other):\n        print(other)\n        raise ValueError('原回调')\ndef accumulate(values):\n    for outer in range(1):\n        total = 0.0\n        for index in range(len(values)):\n            total += values[index] * 1.0\n    return total\nprint(accumulate([1.25, Bad()]))\n",
    )
    .unwrap();
    let mut session = Session::default();
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(section(&mir, "accumulate").contains("$float_loop_raw"));
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input,
            out_dir: dir.join("build"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let actual = ryupy_driver::dotnet::run_dll(&artifact.dll).unwrap();
    assert_eq!(actual.status.code(), Some(1));
    assert_eq!(actual.stdout, b"1.25\n");
    let error = String::from_utf8_lossy(&actual.stderr);
    assert!(error.contains("line 6, in __radd__"), "{error}");
    assert!(error.contains("line 11, in accumulate"), "{error}");
    assert!(error.contains("ValueError: 原回调"), "{error}");
}
