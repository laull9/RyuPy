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
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &case, stage).expect("闭包用例应能完成所有编译阶段");
        insta::assert_snapshot!(name, output);
    }
    let mut session = Session::default();
    let output = dump(&mut session, &case, Stage::Cil).unwrap();
    assert_closure_cil(&output);
    let corrupted = output.replacen(
        "PyOps.CellSet(object, object)",
        "PyOps.CellDelete(object, object)",
        1,
    );
    assert_ne!(corrupted, output);
    assert!(std::panic::catch_unwind(|| assert_closure_cil(&corrupted)).is_err());
}

// 保留共享cell、默认捕获和调用边身份，不再保存随内部方法命名变化的整份IR。
fn assert_closure_cil(output: &str) {
    use support::cil_contract::Cil;
    let cil = Cil::parse(output);
    let counter = cil.method("counter");
    let add = cil.method("add");
    let read = cil.method("read");
    assert_eq!(counter.runtime_count("PyOps.CellNew"), 1);
    assert_eq!(counter.runtime_count("PyOps.CellSet"), 1);
    counter.ordered_calls(&["PyOps.CellNew", "PyOps.CellSet", "PyOps.MakeTuple"]);
    // 两个闭包环境必须捕获同一cell local，而不是各复制一个cell。
    let cell = counter
        .instructions
        .iter()
        .position(|i| {
            i.starts_with("Call(static object [RyuPy.Runtime]RyuPy.Runtime.PyOps.CellNew(")
        })
        .unwrap();
    let local = counter.instructions[cell + 1]
        .strip_prefix("StoreLocal(")
        .unwrap();
    let load = format!("LoadLocal({local}");
    let captures = counter
        .instructions
        .windows(3)
        .filter(|w| {
            w[0] == "LoadConstantInt32(0)"
                && w[1] == load
                && w[2] == "StoreElement([System.Runtime]System.Object)"
        })
        .count();
    assert_eq!(captures, 2);
    assert_eq!(add.runtime_count("PyOps.CellGetFree"), 2);
    assert_eq!(add.runtime_count("PyOps.ArrayGet"), 1);
    assert_eq!(add.runtime_count("PyOps.CellSet"), 1);
    add.ordered_calls(&[
        "PyOps.CellGetFree",
        "PyPic.Binary",
        "PyOps.ArrayGet",
        "PyOps.CellSet",
        "PyOps.CellGetFree",
    ]);
    assert_eq!(read.runtime_count("PyOps.CellGetFree"), 1);
    assert_eq!(read.runtime_count("PyOps.CellSet"), 0);
    let lambdas = cil.methods_named("<lambda>");
    assert_eq!(lambdas.len(), 2);
    assert_eq!(lambdas[0].runtime_count("PyErrors.LoadName"), 1);
    assert_eq!(lambdas[1].runtime_count("PyErrors.LoadName"), 0);
    assert_eq!(lambdas[1].opcode_count("LoadArgument"), 2);
    for (index, core, env_count) in [
        (0, counter, 0),
        (1, lambdas[0], 0),
        (2, lambdas[1], 0),
        (3, add, 2),
        (4, read, 2),
    ] {
        let thunk = cil.exact(&format!("__thunk_{index}"));
        assert_eq!(thunk.runtime_count("PyFunction.get_Env"), env_count);
        assert_eq!(thunk.runtime_count("PyBinder.Bind"), 1);
        assert_eq!(thunk.generated_call_count(core), 2);
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
