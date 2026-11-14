//! 普通 yield 首切：逐层验证状态机，并锁定尚不能保证的暂停语义诊断。

use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_m3_2_basic() {
    let scope = support::temp_scope::TempScope::new("m3_2");
    let path = scope.path("ryupy_test_generator_basic.py");
    std::fs::write(&path, "def values(limit):\n    for value in range(limit):\n        yield value\n    return\n\nprint(list(values(2)))\n").unwrap();
    for stage in [Stage::Hir, Stage::Types, Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("普通生成器应完成所有阶段");
        let required: &[&str] = match stage {
            Stage::Hir => &[
                "def values(limit):",
                "for value in range(0, limit):",
                "yield value",
            ],
            Stage::Types => &["[Dynamic]: [Dynamic] -> Dynamic"],
            Stage::Mir => &[
                "MakeGenerator(values",
                "GeneratorInput",
                "ArrayGet",
                "branch ",
            ],
            Stage::Cil => &["PyOps.GeneratorInput", "PyOps.ArraySet", "PyOps.ArrayGet"],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
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
    let scope = support::temp_scope::TempScope::new("m3_2");
    let path = scope.path("ryupy_test_generator_diagnostics.py");
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

        // 精确核对出错语法的源码区间，诊断renderer布局由此解耦。
        let marker = match label {
            "模块 yield" => "yield",
            "lambda默认super" => "super()",
            "嵌套函数默认super" => "super()",
            "零参 super" => "super()",
            "模块表达式yield" => "yield",
            "暂停多参内建构造" => "int(",
            "暂停关键字展开" => "f(**",
            "字典key内部yield" => "yield",
            "模块默认值yield" => "yield",
            "列表推导式内部yield" => "yield",
            "生成器表达式内部yield" => "yield",
            _ => unreachable!("用例必须声明其出错位置"),
        };
        let start = source.rfind(marker).unwrap();
        let end = start + marker.len();
        assert!(
            session.diagnostics.iter().any(|d| {
                let range = d.primary.span.range;
                d.code == Some("E0100")
                    && (u32::from(range.start()) as usize) < end
                    && (u32::from(range.end()) as usize) > start
            }),
            "{label}：{}",
            session.render_diagnostics(false)
        );
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_return_value() {
    let scope = support::temp_scope::TempScope::new("m3_2");
    let path = scope.path("ryupy_test_generator_return.py");
    std::fs::write(
        &path,
        "def values():\n    yield 1\n    return 9\n\nprint(list(values()))\n",
    )
    .unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("完成边应保留返回值");
    assert!(output.contains("MakeGenerator(values"), "{output}");
    assert!(output.contains("GeneratorInput"), "{output}");
    assert!(output.contains("box<I64>(const 9_i64)"), "{output}");
    assert!(output.contains("box<I64>(const -1_i64)"), "{output}");
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_send() {
    let scope = support::temp_scope::TempScope::new("m3_2");
    let path = scope.path("ryupy_test_generator_send.py");
    std::fs::write(&path, "def echo():\n    value = yield 1\n    return value\n\niterator = echo()\nprint(next(iterator))\n").unwrap();
    for stage in [Stage::Hir, Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("yield表达式应有恢复接收槽");
        let required: &[&str] = match stage {
            Stage::Hir => &["value = (yield 1)", "return value"],
            Stage::Mir => &["MakeGenerator(echo", "GeneratorInput", "ArrayGet"],
            Stage::Cil => &["PyOps.GeneratorInput", "PyOps.ArraySet", "PyOps.ArrayGet"],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_prepared_call() {
    let scope = support::temp_scope::TempScope::new("m3_2");
    let path = scope.path("ryupy_test_prepared_call.py");
    std::fs::write(
        &path,
        "def call(target):\n    yield target.method((yield 1))\n",
    )
    .unwrap();
    for stage in [Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("暂停调用须先绑定目标");
        let required: &[&str] = match stage {
            Stage::Mir => &["PrepareMethod", "GeneratorInput", "dyn.call"],
            Stage::Cil => &[
                "PyOps.PrepareMethod",
                "PyOps.GeneratorInput",
                "PyPic.CallValue1",
            ],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
        // 入口准备/类完成必须先于后续恢复或公开消费。
        if matches!(stage, Stage::Mir) {
            assert!(
                output.find("RuntimeFn::PrepareMethod").unwrap()
                    < output.find("RuntimeFn::GeneratorInput").unwrap(),
                "{output}"
            );
        }
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_prepared_dataclass_runtime_support() {
    let scope = support::temp_scope::TempScope::new("m3_2");
    let path = scope.path("ryupy_test_prepared_dataclass.py");
    std::fs::write(&path, "from dataclasses import dataclass\n@dataclass\nclass P:\n    x: int\ndef g():\n    return P((yield 1))\n").unwrap();
    let mut session = Session::default();
    // M3.6默认数据类改为完整运行时对象；旧E0305边界已由真实差分解除。
    let output = dump(&mut session, &path, Stage::Mir).expect("运行时数据类支持暂停实参");
    assert!(output.contains("ApplyDataclass"), "{output}");
    assert!(output.contains("MakeGenerator(g"), "{output}");
    assert!(output.contains("GeneratorInput"), "{output}");
    assert!(output.contains("dyn.call"), "{output}");
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_starred_containers() {
    let scope = support::temp_scope::TempScope::new("m3_2");
    let path = scope.path("ryupy_test_starred_containers.py");
    std::fs::write(
        &path,
        "def values():\n    result = (0, *(yield 1))\n    return {0, *result, (yield 2)}\n",
    )
    .unwrap();
    for stage in [Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("星号容器应保持原类型和插入边界");
        let required: &[&str] = match stage {
            Stage::Mir => &[
                "ListExtend",
                "DynBuiltin(const \"tuple\"",
                "SetExtend",
                "SetAdd",
                "GeneratorInput",
            ],
            Stage::Cil => &[
                "PyOps.ListExtend",
                "PyOps.SetExtend",
                "PyOps.SetAdd",
                "PyOps.GeneratorInput",
            ],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_m3_2_starred_call() {
    let scope = support::temp_scope::TempScope::new("m3_2");
    let path = scope.path("ryupy_test_starred_call.py");
    std::fs::write(&path, "def g(f, items):\n    return f(*items, (yield 1))\n").unwrap();
    for stage in [Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("star展开应在Suspend之前");
        let required: &[&str] = match stage {
            Stage::Mir => &["ListExtend", "StarArg", "GeneratorInput", "CallValue"],
            Stage::Cil => &[
                "PyOps.ListExtend",
                "PyOps.StarArg",
                "PyOps.GeneratorInput",
                "PyPic.CallValue",
            ],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
        // 入口准备/类完成必须先于后续恢复或公开消费。
        if matches!(stage, Stage::Mir) {
            assert!(
                output.find("RuntimeFn::StarArg").unwrap()
                    < output.find("RuntimeFn::GeneratorInput").unwrap(),
                "{output}"
            );
        }
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
    let scope = support::temp_scope::TempScope::new("m3_2");
    let path = scope.path("ryupy_m3_2_large_scalar_guard.py");
    for (label, source) in cases {
        std::fs::write(&path, source).unwrap();
        let mut session = Session::default();
        let output =
            dump(&mut session, &path, Stage::Mir).expect("动态键应由真实值守卫而非一律拒绝编译");
        assert!(output.contains("DictSetScalarKey"));
        assert_eq!(
            output.matches("DictSetScalarKey").count(),
            18,
            "{label}：{output}"
        );
        std::fs::write(&path, source.replace("k:", "0:")).unwrap();
        let mut session = Session::default();
        let output = dump(&mut session, &path, Stage::Mir).unwrap();
        assert!(
            !output.contains("DictSetScalarKey"),
            "全标量字面量键仍免守卫"
        );
    }
    std::fs::remove_file(path).unwrap();
}
