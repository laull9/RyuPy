//! M3.6：数据类运行时表示与定义位置注解的分层契约结构合同。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn dataclass_defaults_and_annotation_values_are_runtime_semantics() {
    let scope = support::temp_scope::TempScope::new("m3_6");
    let path = scope.path("ryupy_m3_6_metadata.py");
    std::fs::write(&path, "from dataclasses import dataclass, field, FrozenInstanceError\nfrom typing import Optional\n@dataclass(frozen=True)\nclass Record:\n    value: int = 1\n    items: list = field(default_factory=list)\ndef identity(value: Optional[int]) -> Optional[int]:\n    return value\ntry:\n    Record().value = 2\nexcept FrozenInstanceError:\n    print('frozen')\n").unwrap();
    for stage in [Stage::Hir, Stage::Types, Stage::Mir, Stage::Cil] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("数据类与注解应完成所有阶段");
        let required: &[&str] = match stage {
            Stage::Hir => &[
                "def identity(value: <runtime>) -> <runtime>:",
                "default_factory=list",
                "except dataclasses.FrozenInstanceError",
            ],
            Stage::Types => &["[Dynamic]: [Dynamic] -> Dynamic"],
            Stage::Mir => &[
                "DeclareDataclass",
                "ApplyDataclass",
                "FunctionMetadata",
                "catch dataclasses.FrozenInstanceError",
            ],
            Stage::Cil => &[
                "PyOps.DeclareDataclass",
                "PyOps.ApplyDataclass",
                "PyOps.FunctionMetadata",
                "PyFrozenInstanceError handler",
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
fn snapshot_dead_box_elimination_keeps_observed_and_captured_targets() {
    let scope = support::temp_scope::TempScope::new("m3_6");
    let path = scope.path("ryupy_m3_6_dead_box.py");
    std::fs::write(&path, "def unused(n):\n    for i in range(n):\n        pass\ndef observed(n):\n    for i in range(n):\n        pass\n    return i\ndef captured(n):\n    for i in range(n):\n        fn = lambda: i\n    return fn\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("死值消除不能破坏闭包和循环后读取");
    assert!(output.contains("fn unused("), "{output}");
    assert!(output.contains("fn observed("), "{output}");
    assert!(output.contains("fn captured("), "{output}");
    assert!(output.contains("CellSet"), "{output}");
    assert!(output.contains("CellGetFree"), "{output}");
    // 只消除无人读取的循环装箱；返回值与捕获cell均保留物化。
    let unused = output
        .split("fn unused(")
        .nth(1)
        .unwrap()
        .split("fn observed(")
        .next()
        .unwrap();
    let observed = output
        .split("fn observed(")
        .nth(1)
        .unwrap()
        .split("fn captured(")
        .next()
        .unwrap();
    let captured = output
        .split("fn captured(")
        .nth(1)
        .unwrap()
        .split("fn main(")
        .next()
        .unwrap();
    assert_eq!(unused.matches(" = box<I64>").count(), 1, "{unused}");
    assert_eq!(observed.matches(" = box<I64>").count(), 2, "{observed}");
    assert!(captured.contains("CellSet"), "{captured}");
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_numeric_accumulator_keeps_observed_aliases_dynamic() {
    let scope = support::temp_scope::TempScope::new("m3_6");
    let path = scope.path("ryupy_m3_6_numeric_accumulator.py");
    std::fs::write(&path, "def accumulate(initial, values):\n    total = initial\n    for i in range(len(values)):\n        total += values[i]\n    return total, total\ndef observed(initial, values):\n    total = initial\n    seen = []\n    for i in range(len(values)):\n        seen.append(total)\n        total += values[i]\n    return total, seen\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("守卫累加器不能改变被观察别名");
    assert!(output.contains("NumericAccumulatorCreate"), "{output}");
    assert!(output.contains("NumericAccumulatorAdd"), "{output}");
    assert!(output.contains("NumericAccumulatorValue"), "{output}");
    assert!(output.contains("fn observed("), "{output}");
    let observed = output
        .split("fn observed(")
        .nth(1)
        .unwrap()
        .split("fn main(")
        .next()
        .unwrap();
    assert!(
        !observed.contains("NumericAccumulator"),
        "被append观察的别名必须走动态加法：{observed}"
    );
    assert!(observed.contains("dyn.Add"), "{observed}");
    std::fs::remove_file(path).unwrap();
}
