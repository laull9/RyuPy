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
        if matches!(stage, Stage::Cil) {
            let code = support::cil_contract::Cil::parse(&output);
            let small = code.method("small");
            assert_eq!(
                small.header,
                format!("public static long {}(long) cil managed", small.name)
            );
            assert_eq!(small.opcode_count("AddOverflow"), 1);
            for opcode in ["Box", "Unbox", "NewArray", "NewObject"] {
                assert_eq!(small.opcode_count(opcode), 0);
            }
            let huge = code.method("huge");
            assert_eq!(
                huge.header,
                format!(
                    "public static [System.Runtime]System.Object {}([System.Runtime]System.Object, [System.Runtime]System.Object) cil managed",
                    huge.name
                )
            );
            assert_eq!(huge.opcode_count("Return"), 1);
            let literals = code.exact("__init_literals");
            assert_eq!(literals.runtime_count("PyOps.FromDecimalString"), 1);
            let decimal = literals
                .instructions
                .iter()
                .position(|s| *s == "LoadString(\"184467440737095516160\")")
                .unwrap();
            assert_eq!(
                literals.instructions[decimal + 1],
                "Call(static object [RyuPy.Runtime]RyuPy.Runtime.PyOps.FromDecimalString(string))"
            );
            assert_eq!(
                literals.instructions[decimal + 2].split('(').next(),
                Some("StoreStaticField")
            );
            let main = code.exact("m0___module_main");
            assert_eq!(main.runtime_count("PyOps.Unary"), 1);
            assert_eq!(main.runtime_count("PyFunction.get_Defaults"), 3);
            assert_eq!(main.generated_call_count(small), 1);
        } else {
            insta::assert_snapshot!(name, output);
        }
    }
    std::fs::remove_file(path).unwrap();
}
