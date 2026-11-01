//! Flag使用已有Enum创建/完成ABI，快照锁原始整数类体与完成后公开绑定。
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
        insta::assert_snapshot!(name, output);
    }
    std::fs::remove_file(path).unwrap();
}
