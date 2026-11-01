//! M3.0 的逐层快照与非法作用域声明诊断。运行结果由 Python 差分用例验收。

use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_m3_0_closures() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase3/m3_0_closures.py");
    for (stage, name) in [
        (Stage::Hir, "m3_0_hir"),
        (Stage::Types, "m3_0_types"),
        (Stage::Mir, "m3_0_mir"),
        (Stage::Cil, "m3_0_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &case, stage).expect("闭包用例应能完成所有编译阶段");
        insta::assert_snapshot!(name, output);
    }
}

#[test]
fn snapshot_m3_0_nonlocal_diagnostics() {
    let cases = [
        ("没有外层绑定", "def call():\n    nonlocal missing\n"),
        ("模块声明", "nonlocal value\n"),
        (
            "模块变量不能当外层绑定",
            "value = 1\ndef call():\n    nonlocal value\n",
        ),
        (
            "参数冲突",
            "def outer():\n    value = 1\n    def inner(value):\n        nonlocal value\n",
        ),
        (
            "声明冲突",
            "def outer():\n    value = 1\n    def inner():\n        global value\n        nonlocal value\n",
        ),
        (
            "读取在声明前",
            "def outer():\n    value = 1\n    def inner():\n        print(value)\n        nonlocal value\n",
        ),
        (
            "赋值在声明前",
            "def outer():\n    value = 1\n    def inner():\n        value = 2\n        nonlocal value\n",
        ),
        (
            "global 截断外层查找",
            "def outer():\n    value = 1\n    def middle():\n        global value\n        def inner():\n            nonlocal value\n",
        ),
    ];
    let path = std::env::temp_dir().join("ryupy_test_nonlocal.py");
    let mut output = String::new();
    for (label, source) in cases {
        std::fs::write(&path, source).unwrap();
        let mut session = Session::default();
        assert!(
            dump(&mut session, &path, Stage::Mir).is_err(),
            "应拒绝：{label}"
        );
        assert!(
            session.diagnostics.iter().any(|d| d.code == Some("E0103")),
            "应有 E0103：{label}"
        );
        output.push_str(&format!(
            "{label}\n{}\n",
            session
                .render_diagnostics(false)
                .replace(path.to_str().unwrap(), "nonlocal.py")
        ));
    }
    std::fs::remove_file(path).unwrap();
    insta::assert_snapshot!("m3_0_nonlocal_diagnostics", output);
}
