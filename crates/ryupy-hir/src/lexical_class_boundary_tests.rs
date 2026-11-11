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
    // 当前真实parser入口固定3.11；沿既有class_cell_generic_scope_guard惯例，
    // 分开证明版本拒绝与HIR泛型边界，不能把前者冒充后者或改变生产target。
    let source = "def make():\n    class C[T]: pass\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("generic_boundary.py", source);
    let (parsed, parse_diagnostics) = ryupy_syntax::parse(file, source);
    assert_eq!(parse_diagnostics.len(), 1, "{parse_diagnostics:?}");
    assert_eq!(parse_diagnostics[0].code, Some("E0002"));
    // 版本诊断不等于损坏语法：确认完整def/class/type-params/pass节点仍在。
    let [ryupy_syntax::ast::Stmt::FunctionDef(function)] = parsed.module.body.as_slice() else {
        panic!("预期完整函数AST");
    };
    assert_eq!(function.name.as_str(), "make");
    let [ryupy_syntax::ast::Stmt::ClassDef(class)] = function.body.as_slice() else {
        panic!("预期完整类AST");
    };
    assert_eq!(class.name.as_str(), "C");
    assert!(class.type_params.is_some());
    assert!(matches!(
        class.body.as_slice(),
        [ryupy_syntax::ast::Stmt::Pass(_)]
    ));
    let (_, hir_diagnostics) = crate::lower_module(&parsed, &crate::ModuleInfo::default(), source);
    assert!(
        hir_diagnostics.iter().any(|d| d.code == Some("E0100")
            && d.message == "定义位置类暂不支持多重继承、类关键字、星基类或类型参数"),
        "{hir_diagnostics:?}"
    );
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
