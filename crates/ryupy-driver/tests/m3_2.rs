//! 普通 yield 首切：逐层验证状态机，并锁定尚不能保证的暂停语义诊断。

use ryupy_driver::{Session, Stage, dump};

#[test]
fn snapshot_m3_2_basic() {
    let path = std::env::temp_dir().join("ryupy_test_generator_basic.py");
    std::fs::write(&path, "def values(limit):\n    for value in range(limit):\n        yield value\n    return\n\nprint(list(values(2)))\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "m3_2_basic_hir"),
        (Stage::Types, "m3_2_basic_types"),
        (Stage::Mir, "m3_2_basic_mir"),
        (Stage::Cil, "m3_2_basic_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("普通生成器应完成所有阶段");
        insta::assert_snapshot!(name, output.replace(path.to_str().unwrap(), "generator.py"));
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_unsupported() {
    let cases = [
        ("模块 yield", "yield 1\n"),
        (
            "零参 super",
            "class A:\n    def values(self):\n        yield super().value()\n",
        ),
        ("接收 send", "def f():\n    value = yield 1\n"),
        ("yield from", "def f():\n    yield from []\n"),
        ("返回值", "def f():\n    yield 1\n    return 2\n"),
        (
            "异常区域",
            "def f():\n    try:\n        yield 1\n    finally:\n        print(2)\n",
        ),
        (
            "资源区域",
            "def f():\n    with resource:\n        yield 1\n",
        ),
    ];
    let path = std::env::temp_dir().join("ryupy_test_generator_diagnostics.py");
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
                .replace(path.to_str().unwrap(), "generator.py")
        ));
    }
    std::fs::remove_file(path).unwrap();
    let output = output
        .lines()
        .map(str::trim_end)
        .collect::<Vec<_>>()
        .join("\n");
    insta::assert_snapshot!("m3_2_unsupported", output);
}
