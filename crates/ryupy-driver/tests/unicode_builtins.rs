//! 项目驱动的 Unicode 内建：直接调用走统一运行时，一等值缺口明确诊断。
use ryupy_driver::{Session, Stage, dump};

#[test]
fn snapshot_unicode_builtins() {
    let path = std::env::temp_dir().join("ryupy_test_unicode_builtins.py");
    std::fs::write(&path, "print(ord('🎉'), chr(65))\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("ord/chr应走动态内建分派");
    insta::assert_snapshot!(output.replace(path.to_str().unwrap(), "unicode.py"));
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_unicode_builtin_value_diagnostic() {
    let path = std::env::temp_dir().join("ryupy_test_unicode_builtin_value.py");
    std::fs::write(&path, "f = ord\ng = chr\n").unwrap();
    let mut session = Session::default();
    assert!(dump(&mut session, &path, Stage::Mir).is_err());
    assert_eq!(
        session
            .diagnostics
            .iter()
            .filter(|d| d.code == Some("E0305"))
            .count(),
        2
    );
    let output = session
        .render_diagnostics(false)
        .replace(path.to_str().unwrap(), "unicode.py");
    insta::assert_snapshot!(
        output
            .lines()
            .map(str::trim_end)
            .collect::<Vec<_>>()
            .join("\n")
    );
    std::fs::remove_file(path).unwrap();
}
