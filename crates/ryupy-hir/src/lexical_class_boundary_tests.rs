//! 新域的结构边界在编译期拒绝，不执行一半或回放旧顶层路径。
fn diagnostics(source: &str) -> Vec<ryupy_base::Diagnostic> {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("boundary.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    crate::lower_module(&parsed, &crate::ModuleInfo::default(), source).1
}
#[test]
fn lexical_class_structural_boundaries_are_explicit_diagnostics() {
    for source in [
        "def make():\n    class C(A, B): pass\n",
        "def make():\n    class C(metaclass=M): pass\n",
        "def make():\n    class C(*bases): pass\n",
        "def make():\n    class C[T]: pass\n",
        "def make():\n    yield 1\n    class C: pass\n",
        "async def make():\n    class C: pass\n",
        "def make():\n    class C:\n        global x\n",
        "def make():\n    class C:\n        import math\n",
        "def make():\n    class C:\n        raise ValueError()\n",
    ] {
        assert!(
            diagnostics(source).iter().any(|d| d.code == Some("E0100")),
            "{source}"
        );
    }
}
#[test]
fn legacy_outer_cell_boundary_survives_class_if_merge_without_rejecting_methods() {
    let source = "class Outer:\n    if flag:\n        value = 1\n    else:\n        value = 2\n    class Inner:\n        value = __class__\n";
    assert!(
        diagnostics(source)
            .iter()
            .any(|d| d.message.contains("默认顶层旧类到嵌套类体的外层__class__"))
    );
    let source = "def make():\n    class Outer:\n        class Inner:\n            def read(self): return __class__\n    return Outer\n";
    assert!(
        !diagnostics(source)
            .iter()
            .any(|d| d.message.contains("默认顶层旧类到嵌套类体"))
    );
}
