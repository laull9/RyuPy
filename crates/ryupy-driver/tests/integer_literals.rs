//! 大整数字面量的真实HIR容量诊断；超过上限时不给运行时/宿主留下无界构造。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn significant_decimal_capacity_is_explicit_for_every_radix() {
    let path = std::env::temp_dir().join("ryupy_integer_capacity.py");
    for token in [
        "9".repeat(4301),
        format!("0x2{}", "0".repeat(3571)),
        format!("0o4{}", "0".repeat(4761)),
        format!("0b1{}", "0".repeat(14285)),
    ] {
        std::fs::write(&path, format!("value = {token}\n")).unwrap();
        let mut session = Session::default();
        assert!(dump(&mut session, &path, Stage::Mir).is_err());
        assert!(
            session
                .diagnostics
                .iter()
                .any(|diagnostic| diagnostic.code == Some("E0100"))
        );
        let message = session.render_diagnostics(false);
        assert!(message.contains("4300"), "{message}");
        assert!(!message.contains("64 位无符号整数"), "不能保留旧范围诊断");
    }
    for token in [
        "9".repeat(4300),
        format!("0x1{}", "0".repeat(3571)),
        format!("0o2{}", "0".repeat(4761)),
        format!("0b1{}", "0".repeat(14284)),
    ] {
        std::fs::write(&path, format!("value = {token}\n")).unwrap();
        let mut session = Session::default();
        assert!(dump(&mut session, &path, Stage::Hir).is_ok());
        assert!(session.diagnostics.is_empty());
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_big_defaults_do_not_demote_unrelated_small_integer_function() {
    let path = std::env::temp_dir().join("ryupy_big_integer_types.py");
    std::fs::write(&path, "def huge(value: int = -184467440737095516160)->int:\n    return value\ndef small(value: int)->int:\n    return value + 3\nprint(huge(), small(4))\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "integer_literal_hir"),
        (Stage::Types, "integer_literal_types"),
        (Stage::Mir, "integer_literal_mir"),
        (Stage::Cil, "integer_literal_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).unwrap();
        assert!(session.diagnostics.is_empty());
        if matches!(stage, Stage::Types) {
            assert!(
                output.contains("[Static]: [Int] -> Int"),
                "小整数函数不能被全局降级"
            );
            assert!(
                output.contains("[Dynamic]: [Dynamic] -> Dynamic"),
                "巨大默认值不能塞进i64参数"
            );
        }
        match stage {
            Stage::Hir => {
                assert!(
                    output.contains("def huge(value: int=...) -> int:"),
                    "{name}"
                );
                assert!(output.contains("def small(value: int) -> int:"));
                assert!(output.contains("return (value + 3)"));
            }
            Stage::Types => {}
            Stage::Mir => {
                assert!(output.contains("fn huge(_0: dyn"), "{name}");
                assert!(output.contains("fn small(_0: i64) -> i64"));
                assert!(output.contains("box<Dynamic>(const 184467440737095516160_bigint)"));
                assert!(output.contains("dyn.Neg("));
                assert!(output.contains("Add(_0, const 3_i64)"));
            }
            Stage::Cil => {
                assert!(output.contains("m0_small(long)"), "{name}");
                assert!(output.contains("PyOps.FromDecimalString(string)"));
                assert!(output.contains("LoadString(\"184467440737095516160\")"));
                assert!(output.contains("AddOverflow(Signed)"));
            }
            _ => unreachable!(),
        }
    }
    std::fs::remove_file(path).unwrap();
}
