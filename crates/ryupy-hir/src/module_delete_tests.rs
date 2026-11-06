//! 模块删除预登记、公开身份与实例作用域边界；先写语义断言，不以源码拼写代替绑定。
use crate::*;

fn lower(source: &str, instance_globals: bool) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("delete.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    lower_module(
        &parsed,
        &ModuleInfo {
            instance_globals,
            ..Default::default()
        },
        source,
    )
}

#[test]
fn module_delete_names_are_registered_without_turning_all_globals_dynamic() {
    let (module, diagnostics) = lower("x = 1\ny = 2\ndel x, missing\n", false);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (_, var) in module.vars.iter() {
        assert_eq!(var.may_delete, matches!(var.name.as_str(), "x" | "missing"));
        if var.name.as_str() == "x" {
            assert!(!var.is_global);
        }
    }
    let body = &module.functions[module.main].body;
    let targets = body
        .stmts
        .iter()
        .find_map(|(_, s)| match &s.kind {
            StmtKind::Delete(targets) => Some(targets),
            _ => None,
        })
        .unwrap();
    assert_eq!(targets.len(), 2);
    assert!(
        targets
            .iter()
            .all(|id| matches!(body.exprs[*id].kind, ExprKind::ModuleVar(_)))
    );
}

#[test]
fn module_delete_function_and_class_reads_never_bypass_public_bindings() {
    let source = "def read():\n    return f, C\ndef f():\n    return f\nclass C:\n    pass\ndef erase():\n    global f, C\n    del f, C\nclass D(C):\n    pass\n";
    let (module, diagnostics) = lower(source, false);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let f = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "f")
        .unwrap()
        .0;
    let c = module
        .classes
        .iter()
        .find(|(_, c)| c.name.as_str() == "C")
        .unwrap()
        .0;
    assert!(module.classes[c].decorated_binding.is_some());
    assert!(
        module
            .functions
            .iter()
            .all(|(_, function)| function.body.exprs.iter().all(
                |(_, expr)| !matches!(expr.kind, ExprKind::Func(id) if id == f)
                    && !matches!(expr.kind, ExprKind::Class(id) if id == c)
            ))
    );
    let d = module
        .classes
        .iter()
        .find(|(_, c)| c.name.as_str() == "D")
        .unwrap()
        .1;
    assert!(matches!(
        d.base,
        Some(ClassBase::Expr(_) | ClassBase::Runtime(_))
    ));
}

#[test]
fn module_delete_exception_targets_are_runtime_reads() {
    let (module, diagnostics) = lower(
        "class E(Exception):\n    pass\ndel E\ntry:\n    raise ValueError()\nexcept E:\n    pass\n",
        false,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        module.functions[module.main]
            .body
            .handlers
            .iter()
            .all(|(_, h)| h.type_expr.is_some())
    );
}

#[test]
fn instance_module_delete_accepts_only_module_or_own_function_global_targets() {
    for source in [
        "del x, missing\n",
        "def f():\n    global x\n    del x\n",
        "class C:\n    def f(self):\n        global x\n        del x\n",
        "def outer():\n    def inner():\n        global x\n        del x\n    return inner\n",
    ] {
        let (_, diagnostics) = lower(source, true);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
    }
    for source in [
        "del x.attr\n",
        "del x[0]\n",
        "def f():\n    del x\n",
        "class C:\n    del x\n",
        "def outer():\n    global x\n    def inner():\n        del x\n",
        "def f():\n    x = 1\n    def g():\n        nonlocal x\n        del x\n",
    ] {
        let (_, diagnostics) = lower(source, true);
        assert!(
            diagnostics.iter().any(|d| d.code == Some("E0100")),
            "{source}: {diagnostics:?}"
        );
    }
}

#[test]
fn module_delete_scope_collection_does_not_promote_local_or_class_targets() {
    let source = "x = 1\ny = 2\ndef f():\n    x = 3\n    del x\nclass C:\n    def method(self):\n        global y\n        del y\n";
    let (module, diagnostics) = lower(source, false);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (_, var) in module.vars.iter() {
        assert_eq!(var.may_delete, var.name.as_str() == "y");
    }
}

#[test]
fn module_delete_keeps_custom_default_builtins_and_mixed_definitions_explicit() {
    for source in [
        "__builtins__ = {'len': replacement}\nlen = 1\ndel len\nprint(len([]))\n",
        "del __builtins__\n",
        "class C:\n    pass\ndel C\nclass C:\n    pass\n",
        "class C:\n    pass\ndef C():\n    pass\ndel C\n",
    ] {
        let (_, diagnostics) = lower(source, false);
        assert!(!diagnostics.is_empty(), "{source}");
    }
}

#[test]
fn module_delete_default_module_name_has_explicit_metadata_boundary() {
    for source in [
        "del __name__\n",
        "def erase():\n    global __name__\n    del __name__\n",
        "if False:\n    del __name__\n",
    ] {
        let (_, diagnostics) = lower(source, false);
        assert!(
            diagnostics
                .iter()
                .any(|diagnostic| diagnostic.code == Some("E0100")),
            "{source}: {diagnostics:?}"
        );
        let (module, diagnostics) = lower(source, true);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        assert!(
            module
                .vars
                .iter()
                .any(|(_, var)| var.name.as_str() == "__name__" && var.may_delete)
        );
    }
}

#[test]
fn module_delete_default_doc_has_explicit_initial_binding_boundary() {
    for source in [
        "del __doc__\n",
        "\"\"\"module documentation\"\"\"\ndel __doc__\n",
        "def erase():\n    global __doc__\n    del __doc__\n",
        "if False:\n    del __doc__\n",
    ] {
        let (_, diagnostics) = lower(source, false);
        assert!(
            diagnostics.iter().any(|diagnostic| {
                diagnostic.code == Some("E0100") && diagnostic.message.contains("__doc__")
            }),
            "{source}: {diagnostics:?}"
        );
        let (module, diagnostics) = lower(source, true);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
        assert!(
            module
                .vars
                .iter()
                .any(|(_, var)| var.name.as_str() == "__doc__" && var.may_delete)
        );
    }
}

#[test]
fn module_delete_custom_default_builtins_rejects_ordinary_deleted_names() {
    for source in [
        "__builtins__ = {'ordinary': 11}\nordinary = 3\ndel ordinary\ndef f():\n    return ordinary\n",
        "__builtins__ = {'ordinary': 11}\nordinary = 3\ndef erase():\n    global ordinary\n    del ordinary\n",
        "__builtins__ = {'ordinary': 11}\nif False:\n    del ordinary\n",
    ] {
        let (_, diagnostics) = lower(source, false);
        assert!(
            diagnostics.iter().any(|diagnostic| {
                diagnostic.code == Some("E0100") && diagnostic.message.contains("__builtins__")
            }),
            "{source}: {diagnostics:?}"
        );
        let (_, diagnostics) = lower(source, true);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
    }
    // 没有模块绑定删除时，仍保留原有自定义builtins的独立能力边界。
    let (_, diagnostics) = lower("__builtins__ = {'ordinary': 11}\nordinary = 3\n", false);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
}

#[test]
fn module_delete_range_calls_keep_public_binding_before_and_after_deletion() {
    for source in [
        "range = lambda n: [7]\nfor x in range(2):\n    print(x)\ndel range\nfor x in range(2):\n    print(x)\nrange = lambda n: [8]\nfor x in range(2):\n    print(x)\n",
        "def erase():\n    global range\n    del range\nfor x in range(2):\n    print(x)\n",
        "if False:\n    del range\nfor x in range(2):\n    print(x)\n",
    ] {
        for instance_globals in [false, true] {
            let (module, diagnostics) = lower(source, instance_globals);
            assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
            let body = &module.functions[module.main].body;
            assert!(
                !body
                    .stmts
                    .iter()
                    .any(|(_, statement)| { matches!(statement.kind, StmtKind::ForRange { .. }) }),
                "{source}: instance_globals={instance_globals}"
            );
            let mut loops = 0;
            for (_, statement) in body.stmts.iter() {
                if let StmtKind::ForEach { iter, .. } = statement.kind {
                    loops += 1;
                    let ExprKind::Call { callee, .. } = body.exprs[iter].kind else {
                        panic!("range迭代必须保留真实调用");
                    };
                    assert!(matches!(body.exprs[callee].kind, ExprKind::ModuleVar(_)));
                }
            }
            assert!(loops > 0);
        }
    }
    // 无删除程序仍保持已有原生计数循环，不为新能力支付通用迭代成本。
    let (module, diagnostics) = lower("for x in range(2):\n    print(x)\n", false);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        module.functions[module.main]
            .body
            .stmts
            .iter()
            .any(|(_, statement)| matches!(statement.kind, StmtKind::ForRange { .. }))
    );
}

#[test]
fn module_delete_default_annotations_has_explicit_namespace_boundary() {
    for source in [
        "x: int = 1\ndel __annotations__\n",
        "from __future__ import annotations\nx: int = 1\ndel __annotations__\n",
        "del __annotations__\n",
        "def erase():\n    global __annotations__\n    del __annotations__\n",
        "if False:\n    del __annotations__\n",
    ] {
        let (_, diagnostics) = lower(source, false);
        assert!(
            diagnostics.iter().any(|diagnostic| {
                diagnostic.code == Some("E0100") && diagnostic.message.contains("__annotations__")
            }),
            "{source}: {diagnostics:?}"
        );
    }
    // B2的annotation既有边界不扩大；纯名称删除仍由显式globals决定存在性。
    for source in [
        "del __annotations__\n",
        "def erase():\n    global __annotations__\n    del __annotations__\n",
        "if False:\n    del __annotations__\n",
    ] {
        let (_, diagnostics) = lower(source, true);
        assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
    }
}
