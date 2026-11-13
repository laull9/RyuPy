//! M3.6：数据类运行时表示与定义位置注解的分层契约快照。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn dataclass_defaults_and_annotation_values_are_runtime_semantics() {
    let path = std::env::temp_dir().join("ryupy_m3_6_metadata.py");
    std::fs::write(&path, "from dataclasses import dataclass, field, FrozenInstanceError\nfrom typing import Optional\n@dataclass(frozen=True)\nclass Record:\n    value: int = 1\n    items: list = field(default_factory=list)\ndef identity(value: Optional[int]) -> Optional[int]:\n    return value\ntry:\n    Record().value = 2\nexcept FrozenInstanceError:\n    print('frozen')\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "m3_6_metadata_hir"),
        (Stage::Types, "m3_6_metadata_types"),
        (Stage::Mir, "m3_6_metadata_mir"),
        (Stage::Cil, "m3_6_metadata_cil"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("数据类与注解应完成所有阶段");
        if matches!(stage, Stage::Cil) {
            metadata_cil_contract(&output);
            let broken =
                output.replace("PyOps.FinalizeOrdinaryClass(", "PyOps.RemovedContractCall(");
            assert!(std::panic::catch_unwind(|| metadata_cil_contract(&broken)).is_err());
        } else {
            insta::assert_snapshot!(name, output.replace(path.to_str().unwrap(), "metadata.py"));
        }
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_dead_box_elimination_keeps_observed_and_captured_targets() {
    let path = std::env::temp_dir().join("ryupy_m3_6_dead_box.py");
    std::fs::write(&path, "def unused(n):\n    for i in range(n):\n        pass\ndef observed(n):\n    for i in range(n):\n        pass\n    return i\ndef captured(n):\n    for i in range(n):\n        fn = lambda: i\n    return fn\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("死值消除不能破坏闭包和循环后读取");
    insta::assert_snapshot!("m3_6_dead_values_mir", output);
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_numeric_accumulator_keeps_observed_aliases_dynamic() {
    let path = std::env::temp_dir().join("ryupy_m3_6_numeric_accumulator.py");
    std::fs::write(&path, "def accumulate(initial, values):\n    total = initial\n    for i in range(len(values)):\n        total += values[i]\n    return total, total\ndef observed(initial, values):\n    total = initial\n    seen = []\n    for i in range(len(values)):\n        seen.append(total)\n        total += values[i]\n    return total, seen\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("守卫累加器不能改变被观察别名");
    insta::assert_snapshot!("m3_6_numeric_accumulator_mir", output);
    std::fs::remove_file(path).unwrap();
}

// 保留数据类冻结异常、类体完成/装饰次序和真实函数注解写入，而非保存整份生成IR。
fn metadata_cil_contract(output: &str) {
    let cil = support::cil_contract::Cil::parse(output);
    let main = cil.exact("m0___module_main");
    for member in [
        "PyOps.MakeClass",
        "PyOps.FinalizeOrdinaryClass",
        "PyOps.DeclareDataclass",
        "PyOps.ApplyDataclass",
        "PyOps.FunctionMetadata",
    ] {
        assert_eq!(main.runtime_count(member), 1);
    }
    assert_eq!(main.runtime_count("PyOps.TypeObject"), 5);
    assert_eq!(main.runtime_count("PyOps.ClassNamespaceGet"), 2);
    assert_eq!(main.runtime_count("PyOps.SetItem"), 2);
    main.ordered_calls(&[
        "PyOps.MakeClass",
        "PyOps.MakeDict",
        "PyOps.FinalizeOrdinaryClass",
        "PyOps.DeclareDataclass",
        "PyOps.ApplyDataclass",
        "PyOps.FunctionMetadata",
    ]);
    assert_eq!(main.opcode_count("Leave"), 4);
    assert_eq!(main.opcode_count("Throw"), 1);
    let catches: Vec<_> = main
        .eh
        .iter()
        .map(|line| {
            line.split(" catch ")
                .nth(1)
                .unwrap()
                .split(" handler ")
                .next()
                .unwrap()
        })
        .collect();
    assert_eq!(
        catches,
        vec![
            "[RyuPy.Runtime]RyuPy.Runtime.PyFrozenInstanceError",
            "[RyuPy.Runtime]RyuPy.Runtime.PyBaseException"
        ]
    );
    let identity = cil.method("identity");
    assert_eq!(identity.opcode_count("Return"), 1);
    assert_eq!(identity.runtime_count("PyTraceback.PushFrameWithOrigin"), 1);
    assert_eq!(identity.runtime_count("PyTraceback.PopTo"), 1);
}
