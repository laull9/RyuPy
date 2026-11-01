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
            "lambda默认super",
            "def g():\n    yield (lambda x=super(): x)\n",
        ),
        (
            "嵌套函数默认super",
            "def g():\n    def f(x=super()):\n        return x\n    yield f\n",
        ),
        (
            "零参 super",
            "class A:\n    def values(self):\n        yield super().value()\n",
        ),
        ("模块表达式yield", "value = (yield 1)\n"),
        (
            "暂停多参内建构造",
            "def g():\n    return int((yield 1), (yield 2))\n",
        ),
        (
            "字典字面量暂停",
            "def g(key):\n    return {key: 1, 'x': (yield 2)}\n",
        ),
        (
            "暂停调用展开",
            "def g(f, items):\n    f(*items, (yield 1))\n",
        ),
        (
            "暂停关键字展开",
            "def g(f, items):\n    f(**items, x=(yield 1))\n",
        ),
        (
            "字典key内部yield",
            "def f():\n    return {(yield x): x for x in [1]}\n",
        ),
        ("模块默认值yield", "def f(x=(yield 1)):\n    return x\n"),
        (
            "列表推导式内部yield",
            "def f():\n    return [(yield x) for x in [1]]\n",
        ),
        (
            "生成器表达式内部yield",
            "def f():\n    return ((yield x) for x in [1])\n",
        ),
        ("yield from", "def f():\n    yield from []\n"),
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

#[test]
fn snapshot_m3_2_return_value() {
    let path = std::env::temp_dir().join("ryupy_test_generator_return.py");
    std::fs::write(
        &path,
        "def values():\n    yield 1\n    return 9\n\nprint(list(values()))\n",
    )
    .unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("完成边应保留返回值");
    insta::assert_snapshot!(
        "m3_2_return_mir",
        output.replace(path.to_str().unwrap(), "generator.py")
    );
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_send() {
    let path = std::env::temp_dir().join("ryupy_test_generator_send.py");
    std::fs::write(&path, "def echo():\n    value = yield 1\n    return value\n\niterator = echo()\nprint(next(iterator))\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "m3_2_send_hir"),
        (Stage::Mir, "m3_2_send_mir"),
        (Stage::Cil, "m3_2_send_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("yield表达式应有恢复接收槽");
        insta::assert_snapshot!(name, output.replace(path.to_str().unwrap(), "generator.py"));
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_prepared_call() {
    let path = std::env::temp_dir().join("ryupy_test_prepared_call.py");
    std::fs::write(
        &path,
        "def call(target):\n    yield target.method((yield 1))\n",
    )
    .unwrap();
    for (stage, name) in [
        (Stage::Mir, "m3_2_prepared_mir"),
        (Stage::Cil, "m3_2_prepared_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("暂停调用须先绑定目标");
        insta::assert_snapshot!(name, output.replace(path.to_str().unwrap(), "generator.py"));
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_prepared_dataclass_boundary() {
    let path = std::env::temp_dir().join("ryupy_test_prepared_dataclass.py");
    std::fs::write(&path, "from dataclasses import dataclass\n@dataclass\nclass P:\n    x: int\ndef g():\n    return P((yield 1))\n").unwrap();
    let mut session = Session::default();
    assert!(dump(&mut session, &path, Stage::Mir).is_err());
    assert!(session.diagnostics.iter().any(|d| d.code == Some("E0305")));
    let output = session
        .render_diagnostics(false)
        .replace(path.to_str().unwrap(), "generator.py");
    let output = output
        .lines()
        .map(str::trim_end)
        .collect::<Vec<_>>()
        .join("\n");
    insta::assert_snapshot!("m3_2_prepared_dataclass", output);
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_starred_containers() {
    let path = std::env::temp_dir().join("ryupy_test_starred_containers.py");
    std::fs::write(
        &path,
        "def values():\n    result = (0, *(yield 1))\n    return {0, *result, (yield 2)}\n",
    )
    .unwrap();
    for (stage, name) in [
        (Stage::Mir, "m3_2_starred_mir"),
        (Stage::Cil, "m3_2_starred_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("星号容器应保持原类型和插入边界");
        insta::assert_snapshot!(name, output.replace(path.to_str().unwrap(), "generator.py"));
    }
    std::fs::remove_file(path).unwrap();
}
