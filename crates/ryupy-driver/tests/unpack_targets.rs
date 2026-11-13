//! 平坦属性/下标解包的 HIR、动态类型与 MIR 顺序回归；嵌套解包仍明确拒绝。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_unpack_before_ordered_leaf_stores() {
    let path = std::env::temp_dir().join("ryupy_m3_7_unpack_targets.py");
    std::fs::write(&path, "def bind(target, values):\n    target.first, target[0] = values\n    first, *target.rest = values\n    return first\ndef paused(target, values):\n    target.first, target[(yield 0)] = values\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "m3_7_unpack_targets_hir"),
        (Stage::Types, "m3_7_unpack_targets_types"),
        (Stage::Mir, "m3_7_unpack_targets_mir"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("平坦解包叶目标应成功降级");
        assert!(session.diagnostics.is_empty());
        match stage {
            Stage::Hir => {
                assert!(
                    output.contains("target.first, target[0] = values"),
                    "{name}"
                );
                assert!(output.contains("first, *target.rest = values"));
                assert!(output.contains("target.first, target[(yield 0)] = values"));
            }
            Stage::Types => {
                assert_eq!(
                    output
                        .matches("[Dynamic]: [Dynamic, Dynamic] -> Dynamic")
                        .count(),
                    2,
                    "{name}"
                );
                assert!(output.contains("[Static]: [] -> None"));
            }
            Stage::Mir => {
                let bind = output
                    .split("fn bind(")
                    .nth(1)
                    .unwrap()
                    .split("fn paused(")
                    .next()
                    .unwrap();
                let unpack = bind.find("RuntimeFn::UnpackSequence").unwrap();
                let attr = bind.find("RuntimeFn::SetAttr").unwrap();
                let item = bind.find("RuntimeFn::SetItem").unwrap();
                let starred = bind.find("RuntimeFn::UnpackEx").unwrap();
                assert!(
                    unpack < attr && attr < item && item < starred,
                    "{name}: {bind}"
                );
                assert_eq!(bind.matches("RuntimeFn::UnpackSequence").count(), 1);
                assert_eq!(bind.matches("RuntimeFn::UnpackEx").count(), 1);
                assert!(bind[starred..].contains("const \"rest\""));
                assert!(starred < bind.rfind("RuntimeFn::SetAttr").unwrap());
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
fn recursive_targets_remain_diagnostic() {
    let path = std::env::temp_dir().join("ryupy_m3_7_nested_unpack.py");
    for source in ["a, (b, c) = [1, [2, 3]]\n", "a, *(b, c) = [1, 2, 3]\n"] {
        std::fs::write(&path, source).unwrap();
        let mut session = Session::default();
        assert!(dump(&mut session, &path, Stage::Mir).is_err());
        assert!(
            session
                .diagnostics
                .iter()
                .any(|diagnostic| diagnostic.code == Some("E0100"))
        );
    }
    std::fs::remove_file(path).unwrap();
}
