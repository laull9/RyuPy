//! 动态except两遍准入、真实作用域与静态快路结构；不执行目标程序。
use crate::{ExprKind, Module, ModuleInfo};

fn lower(source: &str, native: &[&str]) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("dynamic_except.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    crate::lower_module_with_native_exceptions(&parsed, &ModuleInfo::default(), source, native)
}

#[test]
fn mixed_chain_keeps_all_target_expressions_and_does_not_protect_exports() {
    let source = "import re\ndef f(candidate):\n    try:\n        pass\n    except re.error:\n        pass\n    except candidate.problem.Error:\n        pass\n    except ValueError:\n        pass\n    except:\n        pass\n";
    let (module, diagnostics) = lower(source, &["re"]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(module.protected_exports.is_empty());
    let function = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "f")
        .unwrap()
        .1;
    let handlers: Vec<_> = function.body.handlers.iter().map(|(_, h)| h).collect();
    assert_eq!(handlers.len(), 4);
    for handler in &handlers[..3] {
        assert!(handler.types.is_none());
        assert!(handler.type_expr.is_some());
    }
    assert!(handlers[3].types.is_none() && handlers[3].type_expr.is_none());
    assert!(matches!(
        function.body.exprs[handlers[0].type_expr.unwrap()].kind,
        ExprKind::Attribute { .. }
    ));
    assert!(matches!(
        function.body.exprs[handlers[2].type_expr.unwrap()].kind,
        ExprKind::Builtin(crate::Builtin::Exception("ValueError"))
    ));
}

#[test]
fn static_only_handlers_add_no_target_expressions_and_keep_protection() {
    let source = "import re\ntry:\n    pass\nexcept (ValueError, re.error):\n    pass\n";
    let (module, diagnostics) = lower(source, &["re"]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let body = &module.functions[module.main].body;
    // 原模块初始化固定有三个metadata字符串，不应新增任何handler目标读取。
    assert_eq!(body.exprs.len(), 3);
    for (_, expr) in body.exprs.iter() {
        assert!(
            matches!(&expr.kind, ExprKind::Literal(crate::Literal::Str(value)) if value.is_empty())
        );
    }
    let assigned: Vec<_> = body
        .root
        .iter()
        .take(3)
        .map(|id| {
            let crate::StmtKind::Assign {
                target: crate::AssignTarget::ModuleVar(var),
                ..
            } = &body.stmts[*id].kind
            else {
                panic!("原模块metadata初始化");
            };
            module.vars[*var].name.as_str()
        })
        .collect();
    assert_eq!(assigned, ["__name__", "__package__", "__file__"]);
    let handler = body.handlers.iter().next().unwrap().1;
    assert_eq!(handler.types.as_ref().unwrap(), &["ValueError", "re.error"]);
    assert!(handler.type_expr.is_none());
    assert_eq!(module.protected_exports, ["re"]);
}

#[test]
fn mixed_tuple_keeps_nested_shape_and_empty_tuple_is_dynamic() {
    let source = "def f(kind):\n    try:\n        pass\n    except (ValueError, (kind,)):\n        pass\n    except ():\n        pass\n";
    let (module, diagnostics) = lower(source, &[]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let body = &module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "f")
        .unwrap()
        .1
        .body;
    let handlers: Vec<_> = body.handlers.iter().map(|(_, h)| h).collect();
    let ExprKind::Tuple(items) = &body.exprs[handlers[0].type_expr.unwrap()].kind else {
        panic!("原tuple");
    };
    assert_eq!(items.len(), 2);
    assert!(matches!(&body.exprs[items[1]].kind, ExprKind::Tuple(inner) if inner.len() == 1));
    assert!(
        matches!(&body.exprs[handlers[1].type_expr.unwrap()].kind, ExprKind::Tuple(items) if items.is_empty())
    );
}

#[test]
fn target_reads_preserve_free_cells_and_nonlocal_bindings() {
    let source = "def outer(kind):\n    def change(value):\n        nonlocal kind\n        kind = value\n    def inner():\n        try:\n            pass\n        except kind:\n            pass\n    return inner\n";
    let (module, diagnostics) = lower(source, &[]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let outer = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "outer")
        .unwrap()
        .1;
    let inner = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "inner")
        .unwrap()
        .1;
    assert!(outer.closure.cells.iter().any(|n| n.as_str() == "kind"));
    assert!(inner.closure.free.iter().any(|n| n.as_str() == "kind"));
    let handler = inner.body.handlers.iter().next().unwrap().1;
    assert!(matches!(
        inner.body.exprs[handler.type_expr.unwrap()].kind,
        ExprKind::Free(_)
    ));
}

#[test]
fn unsupported_target_shapes_remain_explicit_diagnostics() {
    for target in [
        "factory()",
        "items[0]",
        "factory().Error",
        "(kind, *others)",
        "kind if flag else other",
    ] {
        let source = format!(
            "def f(kind, factory, items, others, flag, other):\n    try:\n        pass\n    except {target}:\n        pass\n"
        );
        let (_, diagnostics) = lower(&source, &[]);
        assert!(
            diagnostics.iter().any(|d| d.code == Some("E0100")),
            "{target}"
        );
    }
}

#[test]
fn only_new_dynamic_generator_domain_is_rejected() {
    let dynamic = "def gen(kind):\n    try:\n        yield 1\n    except kind:\n        pass\n";
    let (_, diagnostics) = lower(dynamic, &[]);
    assert!(diagnostics.iter().any(|d| d.code == Some("E0100")));
    let static_generator = "def outer(kind):\n    def gen():\n        try:\n            yield 1\n        except ValueError:\n            pass\n    try:\n        pass\n    except kind:\n        pass\n    return gen\n";
    let (module, diagnostics) = lower(static_generator, &[]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let outer = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "outer")
        .unwrap()
        .1;
    let gen_fn = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "gen")
        .unwrap()
        .1;
    assert!(!outer.is_generator);
    assert!(gen_fn.is_generator);
    assert!(
        gen_fn
            .body
            .handlers
            .iter()
            .all(|(_, h)| h.type_expr.is_none())
    );
}
