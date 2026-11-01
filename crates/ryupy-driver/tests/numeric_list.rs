//! 数值列表岛必须在真实矩阵生产/消费两端命中，不能用运行时单测代替编译器集成。
use ryupy_driver::{Session, Stage};
mod support;

#[test]
fn real_matrix_uses_identity_safe_numeric_producer_and_consumer() {
    let root = ryupy_driver::runtime::workspace_root();
    let mut session = Session::default();
    let mir = support::dump(
        &mut session,
        &root.join("tests/benchmarks/matmul.py"),
        Stage::Mir,
    )
    .unwrap();
    let body = mir
        .split("fn matmul(")
        .nth(1)
        .expect("缺少真实矩阵函数")
        .split("\nfn ")
        .next()
        .unwrap();
    for marker in [
        "NumericListCreate",
        "NumericListAppendF64",
        "NumericListPeekList",
        "NumericListCanReadF64",
        "NumericListReadCheckedF64",
    ] {
        assert!(body.contains(marker), "数值岛缺少{marker}：{body}");
    }
    // 纯预检失败仍必须保留原有访问和动态算术，不能把守卫当静态类型承诺。
    assert!(body.contains("GetItem"), "原下标访问回退丢失：{body}");
    assert!(
        body.contains("dyn.Add.inplace"),
        "原数值协议回退丢失：{body}"
    );
}

#[test]
fn numeric_list_observation_and_fallback_match_pinned_python() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/e2e/phase3/m3_10_numeric_list_island.py");
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
    assert!(
        expected.status.success(),
        "{}",
        String::from_utf8_lossy(&expected.stderr)
    );
    let mut session = Session::default();
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    for name in ["produce", "indexed"] {
        let marker = format!("fn {name}(");
        let producer = mir
            .split(&marker)
            .nth(1)
            .expect("缺少生产函数")
            .split("\nfn ")
            .next()
            .unwrap();
        for marker in [
            "NumericListCreate",
            "NumericListAppendF64",
            "NumericListReadCheckedF64",
        ] {
            assert!(
                producer.contains(marker),
                "身份/索引差分必须实际命中{name}/{marker}：{producer}"
            );
        }
    }
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("target/test_numeric_list"),
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
