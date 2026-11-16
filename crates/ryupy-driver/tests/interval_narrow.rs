//! 真实分支合流必须命中自动拆箱；身份与异常矩阵不能只靠未优化路径通过。
use ryupy_driver::{Session, Stage};
mod support;
#[test]
fn joined_constant_ranges_really_remove_dynamic_add() {
    let source = "def choose(flag):\n    if flag:\n        value = 1000\n    else:\n        value = 1007\n    original = value\n    result = value + 0\n    return result, result is original\n\ndef mixed(flag):\n    if flag:\n        value = 3\n    else:\n        value = True\n    return value + 2\nprint(choose(True), choose(False), mixed(False))\n";
    let input = std::env::temp_dir().join("ryupy_interval_narrow.py");
    std::fs::write(&input, source).unwrap();
    let mut session = Session::default();
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    let choose = mir
        .split("fn choose(")
        .nth(1)
        .unwrap()
        .split("fn mixed(")
        .next()
        .unwrap();
    assert!(
        choose.contains("unbox_exact_i64"),
        "没有真实命中范围证明：{choose}"
    );
    assert!(!choose.contains("dyn.Add"), "安全Add仍走动态分派：{choose}");
    assert!(
        choose.contains("box<I64>(const 1000_i64)") && choose.contains("box<I64>(const 1007_i64)"),
        "原被观察盒不能删除"
    );
    let mixed = mir
        .split("fn mixed(")
        .nth(1)
        .unwrap()
        .split("fn main(")
        .next()
        .unwrap();
    assert!(
        mixed.contains("dyn.Add") && !mixed.contains("unbox_exact_i64"),
        "bool不可冒充精确Int64：{mixed}"
    );
    std::fs::remove_file(input).unwrap();
}
#[test]
fn interval_identity_and_fallback_fixture_matches_pinned_python() {
    let root = ryupy_driver::runtime::workspace_root();
    let input = root.join("tests/e2e/phase3/m3_9_interval_narrow.py");
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
    let choose = mir
        .split("fn choose(")
        .nth(1)
        .unwrap()
        .split("fn mixed(")
        .next()
        .unwrap();
    assert!(
        choose.contains("unbox_exact_i64"),
        "身份diff必须实际命中自动拆箱"
    );
    let artifact = ryupy_driver::build(
        &mut session,
        &ryupy_driver::BuildOptions {
            input,
            out_dir: root.join("target/test_interval_identity"),
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
