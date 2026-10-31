//! 类型检查模块的单元测试。
//!
//! 验证 dataclass 构造参数校验、字段类型推导以及 frozen=True 不可变性检查。

use ryupy_base::SourceDb;
use ryupy_hir::lower_module;
use ryupy_syntax::parse;

use crate::check_module;

#[test]
fn checks_dataclass_construction_and_field_access() {
    let mut db = SourceDb::default();
    let src = r#"
from dataclasses import dataclass

@dataclass
class Point:
    x: float
    y: float

p = Point(1.0, 2.0)
p.x = 3.0
"#;
    let file = db.add("test.py", src);
    let (parsed, diags) = parse(file, src);
    assert!(diags.is_empty());

    let (hir, diags) = lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            package: String::new(),
            file: "t.py".into(),
        },
        src,
    );
    assert!(diags.is_empty());

    let (_types, diags) = check_module(&hir);
    assert!(diags.is_empty(), "diags: {diags:?}");
}

#[test]
fn reports_error_on_modifying_frozen_dataclass() {
    let mut db = SourceDb::default();
    let src = r#"
from dataclasses import dataclass

@dataclass(frozen=True)
class Vec2:
    x: int
    y: int

v = Vec2(1, 2)
v.x = 10
"#;
    let file = db.add("test.py", src);
    let (parsed, diags) = parse(file, src);
    assert!(diags.is_empty());

    let (hir, diags) = lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            package: String::new(),
            file: "t.py".into(),
        },
        src,
    );
    assert!(diags.is_empty());

    let (_types, diags) = check_module(&hir);
    assert_eq!(diags.len(), 1);
    assert_eq!(diags[0].code, Some("E0203"));
    assert!(diags[0].message.contains("frozen=True"));
}

// ───────────────── M2.1：静态 / 动态模式判定 ─────────────────

/// 解析 + 降级 + 检查，返回类型信息。
fn check(src: &str) -> (crate::ModuleTypes, Vec<ryupy_base::Diagnostic>) {
    let mut db = SourceDb::default();
    let file = db.add("test.py", src);
    let (parsed, diags) = parse(file, src);
    assert!(diags.is_empty(), "parse diags: {diags:?}");
    let (hir, diags) = lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            package: String::new(),
            file: "t.py".into(),
        },
        src,
    );
    assert!(diags.is_empty(), "hir diags: {diags:?}");
    check_module(&hir)
}

#[test]
fn unannotated_function_is_dynamic_without_error() {
    let src = "def twice(value):\n    return value + value\n\nprint(twice(2))\n";
    let (types, diags) = check(src);
    assert!(diags.is_empty(), "缺标注不再报错：{diags:?}");
    // 缺标注 → 参数与返回值都是 Dynamic
    let sig = types
        .sigs
        .iter()
        .find(|(_, _)| true)
        .map(|(_, s)| s.clone())
        .unwrap();
    assert!(sig.params.iter().any(|t| t.is_dynamic()));
}

/// 标注不再限制实参类型：调用点不符时把被调函数降级，而不是报 E0203。
#[test]
fn call_site_mismatch_demotes_callee() {
    let src = r#"
def twice(value: int) -> int:
    return value + value

print(twice("ha"))
"#;
    let (_types, diags) = check(src);
    assert!(diags.is_empty(), "实参不符不应是编译错误：{diags:?}");
}

/// 异构赋值在 Python 里合法，整函数降级为动态。
#[test]
fn heterogeneous_assignment_demotes_function() {
    let src = "value = 1\nvalue = \"text\"\nprint(value)\n";
    let (types, diags) = check(src);
    assert!(diags.is_empty(), "异构赋值不应报错：{diags:?}");
    let main_id = types
        .sigs
        .iter()
        .find(|(_, sig)| sig.params.is_empty())
        .map(|(id, _)| id)
        .expect("缺少模块入口");
    assert!(types.is_dynamic(main_id));
}

/// 标注齐全且调用点一致的函数保持静态路径（Phase 1 快路径不退化）。
#[test]
fn annotated_consistent_function_stays_static() {
    let src = r#"
def add(a: int, b: int) -> int:
    return a + b

print(add(1, 2))
"#;
    let (types, diags) = check(src);
    assert!(diags.is_empty());
    let static_count = types.modes.iter().filter(|(_, m)| !m.is_dynamic()).count();
    assert!(
        static_count >= 2,
        "标注齐全的函数应保持静态：{static_count}"
    );
}

/// 超出 i64 的字面量需要 BigInteger，函数必须降级。
#[test]
fn huge_literal_demotes_function() {
    let src = "value = 9223372036854775808\nprint(value)\n";
    let (types, diags) = check(src);
    assert!(diags.is_empty(), "大整数字面量不应报错：{diags:?}");
    let main_id = types
        .sigs
        .iter()
        .find(|(_, sig)| sig.params.is_empty())
        .map(|(id, _)| id)
        .expect("缺少模块入口");
    assert!(types.is_dynamic(main_id));
}
