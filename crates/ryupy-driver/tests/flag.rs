//! Flag使用已有Enum创建/完成ABI，结构断言锁原始整数类体与完成后公开绑定。
use ryupy_driver::{Session, Stage};
mod support;
#[test]
fn snapshot_flag_keeps_integer_member_expressions_until_finalize() {
    let path = std::env::temp_dir().join("ryupy_flag_snapshot.py");
    std::fs::write(
        &path,
        "from enum import Flag\nclass Bits(Flag):\n    A = 1\n    B = 2\n    BOTH = A | B\n",
    )
    .unwrap();
    for (stage, name) in [
        (Stage::Hir, "flag_hir"),
        (Stage::Types, "flag_types"),
        (Stage::Mir, "flag_mir"),
    ] {
        let mut session = Session::default();
        let output = support::dump(&mut session, &path, stage).unwrap();
        assert!(session.diagnostics.is_empty());
        match stage {
            Stage::Hir => {
                assert!(
                    output.contains("class Bits(native Flag: global.Flag)"),
                    "{name}"
                );
                assert!(output.contains("A = 1") && output.contains("B = 2"));
                assert!(output.contains("BOTH = (class_namespace.A | class_namespace.B)"));
            }
            Stage::Types => {
                assert_eq!(output.matches(": Dynamic").count(), 5, "{name}");
                assert!(output.contains("[Dynamic]: [] -> None"));
            }
            Stage::Mir => {
                let create = output.find("RuntimeFn::EnumCreate").unwrap();
                let combine = output.find("dyn.BitOr").unwrap();
                let finalize = output.find("RuntimeFn::EnumFinalize").unwrap();
                assert!(create < combine && combine < finalize, "{name}");
                assert!(
                    output[create..combine].contains("const 1_i64")
                        && output[create..combine].contains("const 2_i64")
                );
                assert!(output[finalize..].contains("[const \"A\", const \"B\", const \"BOTH\"]"));
                let line = output
                    .lines()
                    .find(|line| line.contains(" = RuntimeFn::EnumFinalize"))
                    .unwrap();
                let result = line.trim().split(" = ").next().unwrap();
                assert!(
                    output[finalize..]
                        .lines()
                        .any(|line| line.trim().starts_with('g')
                            && line.ends_with(&format!(" = {result};")))
                );
            }
            _ => unreachable!(),
        }
    }
    std::fs::remove_file(path).unwrap();
}
