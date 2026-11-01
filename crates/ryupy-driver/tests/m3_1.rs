//! M3.1 列表推导式：逐层检查独立作用域、共享捕获与明确拒绝的语法。

use ryupy_driver::{Session, Stage, dump};

#[test]
fn snapshot_m3_1_list() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase3/m3_1_list_comprehensions.py");
    for (stage, name) in [
        (Stage::Hir, "m3_1_list_hir"),
        (Stage::Types, "m3_1_list_types"),
        (Stage::Mir, "m3_1_list_mir"),
        (Stage::Cil, "m3_1_list_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &case, stage).expect("列表推导式应完成所有编译阶段");
        insta::assert_snapshot!(name, output);
    }
}

#[test]
fn snapshot_m3_1_unsupported() {
    let cases = [
        ("海象表达式", "print([(y := x) for x in [1]])\n"),
        ("异步推导式", "print([x async for x in []])\n"),
        ("类体作用域", "class C:\n    values = [x for x in [1]]\n"),
    ];
    let path = std::env::temp_dir().join("ryupy_test_comprehension_diagnostics.py");
    let mut output = String::new();
    for (label, source) in cases {
        std::fs::write(&path, source).unwrap();
        let mut session = Session::default();
        assert!(
            dump(&mut session, &path, Stage::Mir).is_err(),
            "应拒绝：{label}"
        );
        assert!(
            session.diagnostics.iter().any(|d| d.code == Some("E0100")),
            "应有 E0100：{label}"
        );
        output.push_str(&format!(
            "{label}\n{}\n",
            session
                .render_diagnostics(false)
                .replace(path.to_str().unwrap(), "comprehensions.py")
        ));
    }
    std::fs::remove_file(path).unwrap();
    insta::assert_snapshot!("m3_1_unsupported", output);
}
