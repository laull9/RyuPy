//! 新类模板的词法规划回归；实际执行支持仍须同源产品差分门验证。
use crate::*;
fn lower(source: &str) -> Module {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("factory.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (module, diagnostics) = lower_module(&parsed, &ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    module
}
#[test]
fn lexical_class_templates_have_actual_owner_and_fresh_cell_capture_plan() {
    let module = lower(
        "def make(x):\n    class C:\n        def read(self): return x, __class__\n    return C\n",
    );
    let (owner, function) = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "make")
        .unwrap();
    let (class, definition) = module
        .classes
        .iter()
        .find(|(_, c)| c.lexical.is_some())
        .unwrap();
    let lexical = definition.lexical.as_ref().unwrap();
    assert_eq!(lexical.owner, Some(owner));
    assert_eq!(lexical.qualname, "make.<locals>.C");
    assert!(matches!(
        lexical.target,
        Some(LexicalBinding::Outer(Binding::Local(_)))
    ));
    assert!(definition.has_class_cell);
    let method = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "read")
        .unwrap()
        .1;
    assert_eq!(method.closure.class_cell, Some(class));
    assert!(method.closure.free.iter().any(|name| name.as_str() == "x"));
    assert!(
        function
            .body
            .locals
            .iter()
            .any(|(_, local)| local.name.as_str() == "C")
    );
    assert!(
        !function
            .body
            .exprs
            .iter()
            .any(|(_, expr)| matches!(expr.kind, ExprKind::Class(id) if id == class))
    );
}
#[test]
fn lexical_class_static_namespace_binding_uses_global_not_outer_cell_fallback() {
    let module = lower(
        "x='module'\ndef make(x):\n    class C:\n        x=x\n        y=x\n        def method(self, value=x): return x, value\n    return C\n",
    );
    let definition = module
        .classes
        .iter()
        .find(|(_, c)| c.lexical.is_some())
        .unwrap()
        .1;
    let owner = definition.lexical.as_ref().unwrap().owner.unwrap();
    let body = &module.functions[owner].body;
    for item in &definition.body {
        if let ClassItemKind::Attr { value, .. } = item.kind {
            let ExprKind::ClassLookup { fallback, .. } = body.exprs[value].kind else {
                panic!("namespace lookup missing")
            };
            assert!(matches!(
                body.exprs[fallback].kind,
                ExprKind::ModuleVar(_) | ExprKind::UnresolvedGlobal(_)
            ));
        }
    }
    let method = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "method")
        .unwrap()
        .1;
    assert!(method.closure.free.iter().any(|name| name.as_str() == "x"));
    let default = method.params[1].default.unwrap();
    assert!(matches!(
        body.exprs[default].kind,
        ExprKind::ClassLookup { .. }
    ));
}
#[test]
fn lexical_class_nested_namespace_keeps_definition_owner_not_module_singleton() {
    let module = lower(
        "def make(x):\n    class Outer:\n        class Inner:\n            def read(self): return x, __class__\n    return Outer\n",
    );
    let definitions: Vec<_> = module
        .classes
        .iter()
        .filter_map(|(_, c)| c.lexical.as_ref())
        .collect();
    assert_eq!(definitions.len(), 2);
    assert_eq!(definitions[0].owner, definitions[1].owner);
    assert_eq!(definitions[1].qualname, "make.<locals>.Outer.Inner");
    assert!(matches!(
        definitions[1].target,
        Some(LexicalBinding::Namespace(_))
    ));
}

#[test]
fn nested_class_body_outer_cell_is_not_a_global_or_its_own_cell() {
    let module = lower(
        "def make():\n    class Outer:\n        class Inner:\n            value = __class__\n    return Outer\n",
    );
    let (outer, definition) = module
        .classes
        .iter()
        .find(|(_, c)| c.name.as_str() == "Outer")
        .unwrap();
    assert!(definition.has_class_cell);
    let owner = definition.lexical.as_ref().unwrap().owner.unwrap();
    assert!(
        module.functions[owner]
            .body
            .exprs
            .iter()
            .any(|(_, expr)| matches!(expr.kind, ExprKind::ClassCellRead(id) if id == outer))
    );
}

#[test]
fn global_class_binding_has_global_qualname_but_actual_function_owner() {
    let module = lower(
        "def make():\n    global Product\n    class Product:\n        def method(self): return __class__\n    return Product\n",
    );
    let (_, class) = module
        .classes
        .iter()
        .find(|(_, c)| c.lexical.is_some())
        .unwrap();
    let definition = class.lexical.as_ref().unwrap();
    assert_eq!(definition.qualname, "Product");
    assert_eq!(
        module.functions[definition.owner.unwrap()].name.as_str(),
        "make"
    );
}

#[test]
fn lexical_class_annotation_reads_propagate_to_real_function_env() {
    let module = lower(
        "def outer():\n    x = 1\n    def make(flag):\n        class C:\n            if flag:\n                y: x\n        return C\n    return make\n",
    );
    let (_, make) = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "make")
        .unwrap();
    assert!(make.closure.free.iter().any(|name| name.as_str() == "x"));
}

#[test]
fn class_body_forward_local_uses_free_cell_not_fast_local() {
    let module =
        lower("def make():\n    class C:\n        value = later\n    later = 1\n    return C\n");
    let (_, function) = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "make")
        .unwrap();
    assert!(
        function
            .closure
            .cells
            .iter()
            .any(|name| name.as_str() == "later")
    );
    assert!(
        function
            .body
            .exprs
            .iter()
            .any(|(_, expr)| matches!(expr.kind, ExprKind::ClassFreeLocal(_)))
    );
}

#[test]
fn namespace_shadowed_unsupported_builtin_keeps_lazy_fallback() {
    for source in [
        "def make():\n    class C:\n        eval = 1\n        copied = eval\n    return C.copied\n",
        "def make(flag):\n    class C:\n        if flag: eval = 1\n        copied = eval\n    return C.copied\n",
        "eval = 7\ndef make(flag):\n    class C:\n        if flag: eval = 1\n        copied = eval\n    return C.copied\n",
        "def make(eval):\n    class C:\n        copied = eval\n    return C.copied\n",
        "def outer(eval):\n    def make():\n        class C:\n            copied = eval\n        return C.copied\n    return make\n",
    ] {
        lower(source);
    }
}
