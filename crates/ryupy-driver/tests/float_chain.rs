//! 短浮点链必须在真实源码命中；差分覆盖精确守卫失败、逐次舍入与原异常位置。
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
fn real_numeric_kernels_expose_guarded_float_chains() {
    let root = ryupy_driver::runtime::workspace_root();
    for (file, name) in [("matmul.py", "matmul"), ("nbody.py", "advance")] {
        let mut session = Session::default();
        let mir = support::dump(
            &mut session,
            &root.join("tests/benchmarks").join(file),
            Stage::Mir,
        )
        .unwrap();
        let function = section(&mir, name);
        assert!(function.contains("is_boxed_f64"), "{name} 没有真实守卫");
        assert!(
            function.contains("unbox_exact_f64"),
            "{name} 没有真实直接浮点拆箱"
        );
        assert!(function.contains("dyn.Mul"), "必须保留原动态失败路径");
    }
}

#[test]
fn float_chain_fixture_matches_pinned_python_after_real_specialization() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/e2e/phase3/m3_10_float_chain.py");
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
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    for name in ["mul_add", "norm_squared"] {
        assert!(
            section(&mir, name).contains("unbox_exact_f64"),
            "真实语义差分必须命中 {name}"
        );
    }
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input,
            out_dir: root.join("target/test_float_chain"),
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
fn guard_failure_retains_original_user_protocol_traceback() {
    let root = ryupy_driver::runtime::workspace_root();
    let dir = root.join("target/test_float_chain_trace");
    std::fs::create_dir_all(&dir).unwrap();
    let input = dir.join("float_chain_error.py");
    std::fs::write(
        &input,
        "class Bad:\n    def __radd__(self, other):\n        raise ValueError('原回调')\ndef fused(a, b, c):\n    return a * b + c\nprint(fused(2.0, 3.0, Bad()))\n",
    )
    .unwrap();
    let mut session = Session::default();
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(section(&mir, "fused").contains("unbox_exact_f64"));
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
    let error = String::from_utf8_lossy(&actual.stderr);
    assert!(error.contains("line 3, in __radd__"), "{error}");
    assert!(error.contains("line 5, in fused"), "{error}");
    assert!(error.contains("ValueError: 原回调"), "{error}");
}
