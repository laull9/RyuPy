//! M3.0 的逐层结构合同与非法作用域声明诊断。运行结果由 Python 差分用例验收。

use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_m3_0_closures() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase3/m3_0_closures.py");
    for stage in [Stage::Hir, Stage::Types, Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &case, stage).expect("闭包用例应能完成所有编译阶段");
        let required: &[&str] = match stage {
            Stage::Hir => &[
                "cells=[Name(\"value\")]",
                "def [nested] add",
                "free=[Name(\"value\")]",
            ],
            Stage::Types => &[
                "[Dynamic]: [Dynamic] -> Dynamic",
                "[Dynamic]: [] -> Dynamic",
            ],
            Stage::Mir => &[
                "CellNew",
                "CellSet",
                "CellGetFree",
                "MakeFunction(add; env=[",
            ],
            Stage::Cil => &[
                "PyOps.CellNew",
                "PyOps.CellSet",
                "PyOps.CellGetFree",
                "PyFunction.get_Env",
            ],
            _ => unreachable!("测试只列出已约定阶段"),
        };
        for token in required {
            assert!(output.contains(token), "缺少结构 {token}：{output}");
        }
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
    let scope = support::temp_scope::TempScope::new("m3_0");
    let path = scope.path("ryupy_test_nonlocal.py");
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

        // 精确核对出错语法的源码区间，诊断renderer布局由此解耦。
        let marker = match label {
            "没有外层绑定" => "missing",
            "模块声明" => "value",
            "模块变量不能当外层绑定" => "value",
            "参数冲突" => "value",
            "声明冲突" => "value",
            "读取在声明前" => "value",
            "赋值在声明前" => "value",
            "global 截断外层查找" => "value",
            _ => unreachable!("用例必须声明其出错位置"),
        };
        let start = source.rfind(marker).unwrap();
        let end = start + marker.len();
        assert!(
            session.diagnostics.iter().any(|d| {
                let range = d.primary.span.range;
                d.code == Some("E0103")
                    && (u32::from(range.start()) as usize) < end
                    && (u32::from(range.end()) as usize) > start
            }),
            "{label}：{}",
            session.render_diagnostics(false)
        );
    }
    std::fs::remove_file(path).unwrap();
}
