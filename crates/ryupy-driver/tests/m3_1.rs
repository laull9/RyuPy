//! M3.1 三类急切推导式：逐层检查独立作用域、共享捕获与明确拒绝的语法。

use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_m3_1_list() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase3/m3_1_list_comprehensions.py");
    for stage in [Stage::Hir, Stage::Types, Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &case, stage).expect("列表推导式应完成所有编译阶段");
        let required: &[&str] = match stage {
            Stage::Hir => &[
                "def [nested] <listcomp>(.0):",
                "cells=[Name(\"seed\")]",
                ".result.append(",
            ],
            Stage::Types => &[
                "[Dynamic]: [Dynamic] -> Dynamic",
                "[Dynamic]: [Dynamic, Dynamic] -> Dynamic",
            ],
            Stage::Mir => &[
                "fn <listcomp>",
                "IterStart",
                "IterNextOk",
                "CellGetFree",
                "const \"append\"",
            ],
            Stage::Cil => &[
                "PyOps.IterStart",
                "PyOps.IterNextOk",
                "PyOps.CellGetFree",
                "PyOps.MakeList",
            ],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
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
    let scope = support::temp_scope::TempScope::new("m3_1");
    let path = scope.path("ryupy_test_comprehension_diagnostics.py");
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
            "集合海象表达式" => ":=",
            "字典海象表达式" => ":=",
            "字典异步推导式" => "async",
            "集合异步推导式" => "async",
            "生成器海象" => ":=",
            "生成器异步" => "async",
            "生成器零参super" => "super()",
            "生成器lambda默认super" => "super()",
            "海象表达式" => ":=",
            "异步推导式" => "async",
            "类体作用域" => "[x",
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

/// 锁定 dict key 的隐藏临时量与 set.add，不把字典赋值的 value-first 规则误套进推导式。
#[test]
fn snapshot_m3_1_dict_set() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase3/m3_1_comprehensions.py");
    for stage in [Stage::Hir, Stage::Types, Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &case, stage).expect("三类推导式应完成所有编译阶段");
        let required: &[&str] = match stage {
            Stage::Hir => &[
                "def [nested] <dictcomp>(.0):",
                "def [nested] <setcomp>(.0):",
                ".result.add(",
                ".key",
            ],
            Stage::Types => &["[Dynamic]: [Dynamic] -> Dynamic"],
            Stage::Mir => &[
                "fn <dictcomp>",
                "fn <setcomp>",
                "MakeDict",
                "SetItem",
                "const \"add\"",
            ],
            Stage::Cil => &[
                "PyOps.MakeDict",
                "PyOps.SetItem",
                "PyOps.MakeSet",
                "PyOps.IterNextOk",
            ],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
    }
}

#[test]
fn snapshot_m3_1_generator_expression() {
    let scope = support::temp_scope::TempScope::new("m3_1");
    let path = scope.path("ryupy_test_generator_expression.py");
    std::fs::write(
        &path,
        "values = (x * 2 for x in [1, 2] if x > 1)\nprint(list(values))\n",
    )
    .unwrap();
    for stage in [Stage::Hir, Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("生成器表达式应完成降级");
        let required: &[&str] = match stage {
            Stage::Hir => &[
                "def [nested] <genexpr>(.0):",
                "yield (x * 2)",
                "if (x Gt 1):",
            ],
            Stage::Mir => &[
                "MakeGenerator(<genexpr>",
                "GeneratorInput",
                "ArraySet",
                "ArrayGet",
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
