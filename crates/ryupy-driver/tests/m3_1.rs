//! M3.1 三类急切推导式：逐层检查独立作用域、共享捕获与明确拒绝的语法。

use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

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
        ("集合海象表达式", "print({(y := x) for x in [1]})\n"),
        ("字典海象表达式", "print({x: (y := x) for x in [1]})\n"),
        ("字典异步推导式", "print({x: x async for x in []})\n"),
        ("集合异步推导式", "print({x async for x in []})\n"),
        ("生成器海象", "print(((y := x) for x in [1]))\n"),
        ("生成器异步", "print((x async for x in []))\n"),
        ("生成器零参super", "print((super() for x in [1]))\n"),
        (
            "生成器lambda默认super",
            "print((lambda x=super(): x for i in [1]))\n",
        ),
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
    // 诊断绘图的行尾填充与语义无关，避免平台/渲染器产生空白差异。
    let output = output
        .lines()
        .map(str::trim_end)
        .collect::<Vec<_>>()
        .join("\n");
    insta::assert_snapshot!("m3_1_unsupported", output);
}

/// 锁定 dict key 的隐藏临时量与 set.add，不把字典赋值的 value-first 规则误套进推导式。
#[test]
fn snapshot_m3_1_dict_set() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase3/m3_1_comprehensions.py");
    for (stage, name) in [
        (Stage::Hir, "m3_1_dict_set_hir"),
        (Stage::Types, "m3_1_dict_set_types"),
        (Stage::Mir, "m3_1_dict_set_mir"),
        (Stage::Cil, "m3_1_dict_set_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &case, stage).expect("三类推导式应完成所有编译阶段");
        insta::assert_snapshot!(name, output);
    }
}

#[test]
fn snapshot_m3_1_generator_expression() {
    let path = std::env::temp_dir().join("ryupy_test_generator_expression.py");
    std::fs::write(
        &path,
        "values = (x * 2 for x in [1, 2] if x > 1)\nprint(list(values))\n",
    )
    .unwrap();
    for (stage, name) in [
        (Stage::Hir, "m3_1_genexpr_hir"),
        (Stage::Mir, "m3_1_genexpr_mir"),
        (Stage::Cil, "m3_1_genexpr_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("生成器表达式应完成降级");
        insta::assert_snapshot!(name, output.replace(path.to_str().unwrap(), "genexpr.py"));
    }
    std::fs::remove_file(path).unwrap();
}
