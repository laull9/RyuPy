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
        match stage {
            Stage::Hir => {
                assert!(output.contains("target.value = __chain$3"), "{name}");
                assert!(output.contains("target[index] = __chain$3"));
                assert!(output.contains("payload = __chain$3"));
                assert!(output.contains("target[(yield 0)] = __chain$2"));
            }
            Stage::Types => {
                assert!(
                    output.contains(
                        "func Idx::<Function>(0) [Dynamic]: [Dynamic, Dynamic] -> Dynamic"
                    ),
                    "{name}"
                );
                assert!(output.contains("func Idx::<Function>(1) [Dynamic]: [] -> Dynamic"));
                assert!(output.contains("func Idx::<Function>(2) [Dynamic]: [Dynamic] -> Dynamic"));
            }
            Stage::Mir => {
                let bind = output
                    .split("fn bind(")
                    .nth(1)
                    .unwrap()
                    .split("fn integers(")
                    .next()
                    .unwrap();
                assert_eq!(bind.matches("MakeList([])").count(), 1, "{name}");
                assert!(bind.find("RuntimeFn::SetItem").unwrap() < bind.find("_2 = _3;").unwrap());
                assert!(
                    bind.find("RuntimeFn::SetAttr").unwrap()
                        < bind.find("RuntimeFn::SetItem").unwrap()
                );
                assert!(
                    bind.contains("SetAttr(_0, const \"value\", _3)")
                        && bind.contains("SetItem(_0, _1, _3)")
                );
                let integers = output
                    .split("fn integers(")
                    .nth(1)
                    .unwrap()
                    .split("fn paused(")
                    .next()
                    .unwrap();
                assert_eq!(integers.matches("box<I64>(const 1000000_i64)").count(), 1);
                assert!(
                    integers.contains("_0 = _2;")
                        && integers.contains("_1 = _2;")
                        && integers.contains("RuntimeFn::IsSame")
                );
                assert!(output.contains("MakeGenerator(paused"));
                let resume = output.rsplit("fn paused(").next().unwrap();
                assert!(
                    resume.find("RuntimeFn::GeneratorInput").unwrap()
                        < resume.find("RuntimeFn::SetItem").unwrap()
                );
            }
            _ => unreachable!(),
        }
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
