//! HIR 降级模块的单元测试。
//!
//! 覆盖函数定义、前向引用、控制流（if/elif/else）以及 for 循环降级等行为。

use crate::*;
use ryupy_base::SourceDb;

#[test]
fn lowers_dataclass_definition_and_usage() {
    let mut db = SourceDb::default();
    let src = r#"
from dataclasses import dataclass

@dataclass
class Point:
    x: float
    y: float

p = Point(1.0, 2.0)
p.x = 3.0
print(p.x)
"#;
    let file = db.add("test.py", src);
    let (parsed, diags) = ryupy_syntax::parse(file, src);
    assert!(diags.is_empty(), "diags: {diags:?}");

    let (hir, diags) = lower_module(
        &parsed,
        &ModuleInfo {
            instance_globals: false,
            resolve_file_at_runtime: false,
            name: "__main__".into(),
            package: String::new(),
            file: "t.py".into(),
        },
        src,
    );
    assert!(diags.is_empty(), "diags: {diags:?}");
    assert_eq!(hir.classes.len(), 1);
    let cls = &hir.classes[la_arena::Idx::from_raw(la_arena::RawIdx::from(0u32))];
    assert_eq!(cls.name.as_str(), "Point");
    assert!(cls.runtime_dataclass);
    assert_eq!(cls.fields.len(), 2);
    assert_eq!(cls.fields[0].annotation, TypeRef::Float);
    assert_eq!(cls.declared_fields[0].as_str(), "x");
    assert_eq!(cls.declared_fields[1].as_str(), "y");
}

#[test]
fn lowers_function_definitions_and_forward_reference() {
    let mut db = SourceDb::default();
    let src = r#"
def f(x: int) -> int:
    return g(x)

def g(y: int) -> int:
    return y + 1

print(f(10))
"#;
    let file = db.add("test.py", src);
    let (parsed, diags) = ryupy_syntax::parse(file, src);
    assert!(diags.is_empty());

    let (hir, diags) = lower_module(
        &parsed,
        &ModuleInfo {
            instance_globals: false,
            resolve_file_at_runtime: false,
            name: "__main__".into(),
            package: String::new(),
            file: "t.py".into(),
        },
        src,
    );
    assert!(diags.is_empty(), "diags: {diags:?}");
    assert_eq!(hir.functions.len(), 3); // f, g, main
}

#[test]
fn lowers_if_elif_else_control_flow() {
    let mut db = SourceDb::default();
    let src = r#"
def classify(n: int) -> int:
    if n < 0:
        return -1
    elif n == 0:
        return 0
    else:
        return 1
"#;
    let file = db.add("test.py", src);
    let (parsed, diags) = ryupy_syntax::parse(file, src);
    assert!(diags.is_empty());

    let (hir, diags) = lower_module(
        &parsed,
        &ModuleInfo {
            instance_globals: false,
            resolve_file_at_runtime: false,
            name: "__main__".into(),
            package: String::new(),
            file: "t.py".into(),
        },
        src,
    );
    assert!(diags.is_empty(), "diags: {diags:?}");
    assert_eq!(hir.functions.len(), 2); // classify, main
}

#[test]
fn lowers_for_range_loop() {
    let mut db = SourceDb::default();
    let src = r#"
total = 0
for i in range(10):
    total += i
"#;
    let file = db.add("test.py", src);
    let (parsed, diags) = ryupy_syntax::parse(file, src);
    assert!(diags.is_empty());

    let (hir, diags) = lower_module(
        &parsed,
        &ModuleInfo {
            instance_globals: false,
            resolve_file_at_runtime: false,
            name: "__main__".into(),
            package: String::new(),
            file: "t.py".into(),
        },
        src,
    );
    assert!(diags.is_empty(), "diags: {diags:?}");
    let main_fn = &hir.functions[hir.main];
    // 前三条是 `__name__` / `__package__` / `__file__` 的前言。
    assert_eq!(main_fn.body.root.len(), 5);
    let for_stmt = &main_fn.body.stmts[*main_fn.body.root.last().unwrap()];
    assert!(matches!(for_stmt.kind, StmtKind::ForRange { .. }));
}

#[test]
fn integer_literals_above_u64_use_exact_big_integer_hir() {
    for literal in [
        "18446744073709551615",
        "18446744073709551616",
        "1267650600228229401496703205376",
    ] {
        let mut db = SourceDb::default();
        let source = format!("print({literal})\n");
        let file = db.add("integer.py", &source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, &source);
        assert!(diagnostics.is_empty());
        let (hir, diagnostics) = lower_module(
            &parsed,
            &ModuleInfo {
                instance_globals: false,
                resolve_file_at_runtime: false,
                name: "__main__".into(),
                package: String::new(),
                file: "integer.py".into(),
            },
            &source,
        );
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        if literal != "18446744073709551615" {
            assert!(hir.functions[hir.main].body.exprs.iter().any(|(_, expr)|
                matches!(&expr.kind, ExprKind::Literal(Literal::BigInt(value)) if value.as_ref() == literal)));
        }
    }
}
