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

    let (hir, diags) = lower_module(&parsed);
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

    let (hir, diags) = lower_module(&parsed);
    assert!(diags.is_empty());

    let (_types, diags) = check_module(&hir);
    assert_eq!(diags.len(), 1);
    assert_eq!(diags[0].code, Some("E0203"));
    assert!(diags[0].message.contains("frozen=True"));
}
