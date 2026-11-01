//! 普通 yield 首切：逐层验证状态机，并锁定尚不能保证的暂停语义诊断。

use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

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
fn snapshot_prepared_dataclass_runtime_support() {
    let path = std::env::temp_dir().join("ryupy_test_prepared_dataclass.py");
    std::fs::write(&path, "from dataclasses import dataclass\n@dataclass\nclass P:\n    x: int\ndef g():\n    return P((yield 1))\n").unwrap();
    let mut session = Session::default();
    // M3.6默认数据类改为完整运行时对象；旧E0305边界已由真实差分解除。
    let output = dump(&mut session, &path, Stage::Mir).expect("运行时数据类支持暂停实参");
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

#[test]
fn snapshot_m3_2_starred_call() {
    let path = std::env::temp_dir().join("ryupy_test_starred_call.py");
    std::fs::write(&path, "def g(f, items):\n    return f(*items, (yield 1))\n").unwrap();
    for (stage, name) in [
        (Stage::Mir, "m3_2_starred_call_mir"),
        (Stage::Cil, "m3_2_starred_call_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("star展开应在Suspend之前");
        insta::assert_snapshot!(name, output.replace(path.to_str().unwrap(), "generator.py"));
    }
    std::fs::remove_file(path).unwrap();
}

// 原E0100两例迁移到真实MIR守卫验收；不删除其首/尾动态键及暂停覆盖。
#[test]
fn snapshot_m3_2_large_dictionary_dynamic_scalar_guard() {
    let cases = [
        (
            "大字典首项动态键",
            "def g(k):\n    return {k: 0, 1: 1, 2: 2, 3: 3, 4: 4, 5: 5, 6: 6, 7: 7, 8: 8, 9: 9, 10: 10, 11: 11, 12: 12, 13: 13, 14: 14, 15: 15, 16: 16, 17: (yield 1)}\n",
        ),
        (
            "大字典尾项动态键",
            "def g(k):\n    return {0: 0, 1: 1, 2: 2, 3: 3, 4: 4, 5: 5, 6: 6, 7: 7, 8: 8, 9: 9, 10: 10, 11: 11, 12: 12, 13: 13, 14: 14, 15: 15, 16: 16, k: 17}\n",
        ),
    ];
    let path = std::env::temp_dir().join("ryupy_m3_2_large_scalar_guard.py");
    let mut result = String::new();
    for (label, source) in cases {
        std::fs::write(&path, source).unwrap();
        let mut session = Session::default();
        let output =
            dump(&mut session, &path, Stage::Mir).expect("动态键应由真实值守卫而非一律拒绝编译");
        assert!(output.contains("DictSetScalarKey"));
        result.push_str(&format!("{label}\n{output}\n"));
        std::fs::write(&path, source.replace("k:", "0:")).unwrap();
        let mut session = Session::default();
        let output = dump(&mut session, &path, Stage::Mir).unwrap();
        assert!(
            !output.contains("DictSetScalarKey"),
            "全标量字面量键仍免守卫"
        );
    }
    std::fs::remove_file(path).unwrap();
    insta::assert_snapshot!("m3_2_large_scalar_guard_mir", result);
}
