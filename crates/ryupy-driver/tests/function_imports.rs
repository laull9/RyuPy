//! 导入语句仍留在函数执行位置；局部/共享 cell/模块槽目标由 HIR 明确给出。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;
#[test]
fn snapshots_local_and_closure_import_binding() {
    let path = std::env::temp_dir().join("ryupy_function_import_snapshot.py");
    std::fs::write(&path, "def outer():\n    alias = None\n    def change():\n        nonlocal alias\n        from math import sqrt as alias\n    change()\n    return alias(9)\ndef load():\n    import math\n    return math\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "function_import_hir"),
        (Stage::Mir, "function_import_mir"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("函数导入应使用词法绑定");
        insta::assert_snapshot!(name, output);
    }
    std::fs::remove_file(path).unwrap();
}
