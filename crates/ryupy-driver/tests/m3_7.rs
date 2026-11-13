//! M3.7 项目入口阻断：map 的直接调用与一等值统一走动态协议。
use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_map_dynamic_calls_and_first_class_value() {
    let path = std::env::temp_dir().join("ryupy_m3_7_map.py");
    std::fs::write(&path, "def add(a, b):\n    return a + b\nmapping = map\nprint(list(map(str, [1, 2])))\nprint(list(mapping(add, [1], [2])))\ndef shadow(map):\n    return map(str, [3])\nprint(shadow(lambda fn, values: values))\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("map调用和一等值应完成动态降级");
    insta::assert_snapshot!("m3_7_map_mir", output);
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_future_capability_and_position_diagnostics() {
    let path = std::env::temp_dir().join("ryupy_m3_7_future.py");
    let mut output = String::new();
    for (label, source) in [
        (
            "复杂延迟注解",
            "from __future__ import annotations\ndef function(value: unknown()):\n    pass\n",
        ),
        ("比较语法切换", "from __future__ import barry_as_FLUFL\n"),
        ("未知指令", "from __future__ import missing\n"),
        ("非开头导入", "value = 1\nfrom __future__ import division\n"),
        (
            "条件内导入",
            "if True:\n    from __future__ import division\n",
        ),
    ] {
        std::fs::write(&path, source).unwrap();
        let mut session = Session::default();
        assert!(dump(&mut session, &path, Stage::Mir).is_err(), "{label}");
        assert!(session.diagnostics.iter().any(|d| d.code == Some("E0100")));
        output.push_str(&format!(
            "{label}\n{}\n",
            session
                .render_diagnostics(false)
                .replace(path.to_str().unwrap(), "future.py")
        ));
    }
    std::fs::remove_file(path).unwrap();
    let output = output
        .lines()
        .map(str::trim_end)
        .collect::<Vec<_>>()
        .join("\n");
    insta::assert_snapshot!("m3_7_future_diagnostics", output);
}

#[test]
fn native_testcase_identity_is_explicit_and_local_source_wins() {
    let directory =
        std::env::temp_dir().join(format!("ryupy_m3_7_native_base_{}", std::process::id()));
    std::fs::create_dir_all(&directory).unwrap();
    let path = directory.join("native_base.py");
    std::fs::write(&path, "import unittest as testing\nfrom unittest import TestCase as Base\nclass First(testing.TestCase):\n    def setUp(self):\n        self.value = 1\nclass Second(Base):\n    pass\n").unwrap();
    for (stage, name) in [
        (Stage::Hir, "m3_7_native_base_hir"),
        (Stage::Mir, "m3_7_native_base_mir"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("真实原生TestCase应保留基类值");
        insta::assert_snapshot!(name, output);
    }
    // 同名源码不是原生模块，不能按拼写假定它导出的TestCase具有已验证语义。
    std::fs::write(directory.join("unittest.py"), "class TestCase:\n    pass\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("同名源码导出的普通类应走运行时守卫");
    assert!(session.diagnostics.is_empty());
    assert!(output.contains("ValidateOrdinaryBase"));
    std::fs::remove_file(directory.join("unittest.py")).unwrap();
    std::fs::remove_file(path).unwrap();
    std::fs::remove_dir(directory).unwrap();
}

#[test]
fn snapshot_warning_hierarchy_typedcatch_and_user_subclass() {
    let path =
        ryupy_driver::runtime::workspace_root().join("tests/e2e/phase3/m3_7_warning_classes.py");
    for (stage, name) in [
        (Stage::Hir, "m3_7_warning_hir"),
        (Stage::Mir, "m3_7_warning_mir"),
    ] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("警告类必须作为真实异常构造和捕获");
        assert!(
            session.diagnostics.is_empty(),
            "{}",
            session.render_diagnostics(false)
        );
        insta::assert_snapshot!(name, output);
    }
}

#[test]
fn snapshot_dir_dynamic_call_and_first_class_value() {
    let path = std::env::temp_dir().join("ryupy_m3_7_dir.py");
    std::fs::write(
        &path,
        "listing = dir\nprint(dir([1]))\nprint(listing('text'))\n",
    )
    .unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir).expect("dir应复用动态调用与一等值入口");
    insta::assert_snapshot!("m3_7_dir_mir", output);
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_enum_members_finalize_before_public_binding() {
    let path = std::env::temp_dir().join("ryupy_m3_7_enum.py");
    std::fs::write(&path, "from enum import Enum\nclass Color(Enum):\n    RED = 1\n    ALIAS = RED\n    BLUE = 2\nprint(Color(1) is Color.RED)\n").unwrap();
    for (stage, name) in [(Stage::Hir, "m3_7_enum_hir")] {
        let mut session = Session::default();
        let output = dump(&mut session, &path, stage).expect("Enum成员应显式完成转换后再发布类值");
        assert!(session.diagnostics.is_empty());
        insta::assert_snapshot!(name, output);
    }
    std::fs::remove_file(path).unwrap();
}

#[test]
fn snapshot_enum_explicit_unsupported_boundaries() {
    let path = std::env::temp_dir().join("ryupy_m3_7_enum_boundaries.py");
    let mut output = String::new();
    for (label, source) in [
        (
            "动态成员",
            "from enum import Enum\nclass C(Enum):\n    A = int('1')\n",
        ),
        (
            "重复成员",
            "from enum import Enum\nclass C(Enum):\n    A = 1\n    A = 2\n",
        ),
        (
            "构造钩子",
            "from enum import Enum\nclass C(Enum):\n    def __new__(cls):\n        pass\n",
        ),
        (
            "继承已有Enum",
            "from enum import Enum\nclass C(Enum):\n    A = 1\nclass D(C):\n    B = 2\n",
        ),
    ] {
        std::fs::write(&path, source).unwrap();
        let mut session = Session::default();
        assert!(dump(&mut session, &path, Stage::Mir).is_err(), "{label}");
        assert!(session.diagnostics.iter().any(|d| d.code == Some("E0100")));
        output.push_str(&format!(
            "{label}\n{}\n",
            session
                .render_diagnostics(false)
                .replace(path.to_str().unwrap(), "enum_boundaries.py")
        ));
    }
    std::fs::remove_file(path).unwrap();
    let output = output
        .lines()
        .map(str::trim_end)
        .collect::<Vec<_>>()
        .join("\n");
    insta::assert_snapshot!("m3_7_enum_diagnostics", output);
}

#[test]
fn snapshot_not_implemented_value_call_and_local_shadow() {
    let path = std::env::temp_dir().join("ryupy_m3_7_not_implemented.py");
    std::fs::write(&path, "def result():\n    return NotImplemented\ndef shadow(NotImplemented):\n    return NotImplemented\nvalue = NotImplemented\nprint(value, result(), shadow(3))\ntry:\n    NotImplemented()\nexcept TypeError:\n    print('not callable')\n").unwrap();
    let mut session = Session::default();
    let output = dump(&mut session, &path, Stage::Mir)
        .expect("NotImplemented是动态单例值，局部同名绑定仍优先");
    insta::assert_snapshot!("m3_7_not_implemented_mir", output);
    std::fs::remove_file(path).unwrap();
}
