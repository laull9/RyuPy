//! 链式赋值用一个动态隐藏槽保对象身份，目标仍逐条晚求值；类体及嵌套解包不偷偷扩范围。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_shared_value_and_suspended_target_order() {
    let path = std::env::temp_dir().join("ryupy_m3_7_chain.py");
    std::fs::write(&path, "def bind(target, index):\n    target.value = target[index] = payload = []\n    return payload\ndef integers() -> bool:\n    a = b = 1000000\n    return a is b\ndef paused(target):\n    first = target[(yield 0)] = []\n    return first\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "m3_7_chain_hir"),
        (Stage::Types, "m3_7_chain_types"),
        (Stage::Mir, "m3_7_chain_mir"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("链式赋值应保单次值与顺序目标");
        assert!(session.diagnostics.is_empty());
        insta::assert_snapshot!(name, output);
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn class_chains_and_nested_unpacking_remain_explicitly_unsupported() {
    let path = std::env::temp_dir().join("ryupy_m3_7_chain_unsupported.py");
    for source in [
        "class C:\n    a = b = 1\n",
        "whole = a, (b, c) = [1, [2, 3]]\n",
    ] {
        std::fs::write(&path, source).unwrap();
        let mut session = Session::default();
        assert!(dump(&mut session, &path, Stage::Mir).is_err());
        assert!(session.diagnostics.iter().any(|d| d.code == Some("E0100")));
    }
    std::fs::remove_file(path).unwrap();
}
