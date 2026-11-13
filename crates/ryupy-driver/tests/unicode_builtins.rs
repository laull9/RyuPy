//! 项目驱动的 Unicode 内建：直接调用与一等内建函数值共享运行时分派。
use ryupy_driver::{Session, Stage, dump};

#[test]
fn snapshot_unicode_builtins() {
    let path = std::env::temp_dir().join("ryupy_test_unicode_builtins.py");
    std::fs::write(&path, "print(ord('🎉'), chr(65))\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("ord/chr应走动态内建分派");
    assert!(output.contains("DynBuiltin(const \"ord\""));
    assert!(output.contains("DynBuiltin(const \"chr\""));
    assert!(output.contains("🎉") && output.contains("const 65_i64"));
    assert!(
        output.find("DynBuiltin(const \"chr\"").unwrap()
            < output.find("RuntimeFn::PrintTarget").unwrap()
    );
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_unicode_builtin_values() {
    let path = std::env::temp_dir().join("ryupy_test_unicode_builtin_value.py");
    std::fs::write(&path, "f = ord\ng = chr\nprint(f(g(65)))\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("ord/chr作为可调用内建值");
    assert!(output.contains("BuiltinFunction(const \"ord\")"));
    assert!(output.contains("BuiltinFunction(const \"chr\")"));
    assert_eq!(output.matches("dyn.call(").count(), 2);
    assert!(!output.contains("DynBuiltin(const \"ord\""));
    std::fs::remove_file(path).unwrap();
}
