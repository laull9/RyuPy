//! 模块函数名碰撞的公开槽：登记早于函数体，定义位置发布，局部/类原边界不变。
use crate::*;

fn lower_result(source: &str) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("module_bindings.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    lower_module(&parsed, &ModuleInfo::default(), source)
}

fn lower(source: &str) -> Module {
    let (module, diagnostics) = lower_result(source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    module
}

fn public_definitions(module: &Module, name: &str) -> (ModuleVarId, Vec<FuncId>) {
    let slots: Vec<_> = module
        .vars
        .iter()
        .filter(|(_, var)| var.name.as_str() == name)
        .collect();
    assert_eq!(slots.len(), 1, "同名必须只有一个公开槽");
    let (slot, var) = slots[0];
    assert!(var.is_global, "函数对象槽不能被第一次整数赋值静态化");
    let definitions: Vec<_> = module.functions[module.main]
        .body
        .stmts
        .iter()
        .filter_map(|(_, stmt)| match &stmt.kind {
            StmtKind::DecoratedDef {
                func,
                target: AssignTarget::ModuleVar(id),
                ..
            } if *id == slot => Some(*func),
            _ => None,
        })
        .collect();
    assert!(!definitions.is_empty());
    let mut reads = 0;
    for (_, function) in module.functions.iter() {
        for (_, expr) in function.body.exprs.iter() {
            if matches!(expr.kind, ExprKind::ModuleVar(id) if id == slot) {
                reads += 1;
            }
            assert!(
                !matches!(expr.kind, ExprKind::Func(id) if definitions.contains(&id)),
                "源码不能从公开绑定绕回任一私有FuncId"
            );
        }
    }
    assert!(reads > 0);
    (slot, definitions)
}

#[test]
fn module_function_binding_collects_existing_binding_forms_before_all_bodies() {
    // 全部用同一公共名字，但每个独立模块只覆盖一种既有收集器入口。
    for binding in [
        "f = 3\n",
        "f: int\n",
        "f = 1\nf += 2\n",
        "f = other = 1\n",
        "f, other = (1, 2)\n",
        "for f in [1, 2]:\n    pass\n",
        "with manager as f:\n    pass\n",
        "try:\n    pass\nexcept ValueError as f:\n    pass\n",
        "from math import sqrt as f\n",
        "import math as f\n",
        "def change():\n    global f\n    f = 9\n",
        "def outer():\n    def inner():\n        global f\n    return inner\n",
        "if False:\n    def f():\n        return 0\n",
    ] {
        let source =
            format!("def observe():\n    return f\n{binding}def f():\n    return f\nprint(f)\n");
        let module = lower(&source);
        let (slot, definitions) = public_definitions(&module, "f");
        assert_eq!(definitions.len(), 1, "只有直接顶层def走此发布节点");
        let observe = module
            .functions
            .iter()
            .find(|(_, f)| f.name.as_str() == "observe")
            .unwrap()
            .1;
        assert!(
            observe
                .body
                .exprs
                .iter()
                .any(|(_, expr)| matches!(expr.kind, ExprKind::ModuleVar(id) if id == slot)),
            "在碰撞/定义之前降级的函数体也必须late读公开槽"
        );
    }
}

#[test]
fn module_function_binding_repeated_def_has_distinct_ids_and_shared_public_slot() {
    let module = lower(
        "def f(value=[]):\n    return value\nsaved = f\ndef f(value=[]):\n    return f\nprint(saved, f)\n",
    );
    let (_, definitions) = public_definitions(&module, "f");
    assert_eq!(definitions.len(), 2);
    assert_ne!(definitions[0], definitions[1]);
    assert!(
        definitions
            .iter()
            .all(|id| module.top_functions.contains(id))
    );
    assert!(!module.functions[module.main].body.stmts.iter().any(|(_, stmt)|
        matches!(stmt.kind, StmtKind::FunctionDefaults(func) if definitions.contains(&func))));
}

#[test]
fn module_function_binding_defaults_read_old_public_value_and_locals_keep_priority() {
    let module = lower(
        "f = 1\ndef f(value=f):\n    return f\ndef local(f):\n    return f\ndef outer(f):\n    def inner():\n        return f\n    return inner\nprint(f)\n",
    );
    let (slot, definitions) = public_definitions(&module, "f");
    let default = module.functions[definitions[0]].params[0].default.unwrap();
    assert!(
        matches!(module.functions[module.main].body.exprs[default].kind, ExprKind::ModuleVar(id) if id == slot)
    );
    let local = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "local")
        .unwrap()
        .1;
    assert!(
        local
            .body
            .exprs
            .iter()
            .any(|(_, expr)| matches!(expr.kind, ExprKind::Local(_)))
    );
    assert!(
        !local
            .body
            .exprs
            .iter()
            .any(|(_, expr)| matches!(expr.kind, ExprKind::ModuleVar(id) if id == slot))
    );
    let inner = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "inner")
        .unwrap()
        .1;
    assert!(
        inner
            .body
            .exprs
            .iter()
            .any(|(_, expr)| matches!(expr.kind, ExprKind::Free(_)))
    );
}

#[test]
fn module_function_binding_preserves_clean_helpers_and_class_admission() {
    let module = lower(
        "def f(value: int) -> int:\n    return value\ndef local():\n    f = 1\n    return f\nprint(f(3))\n",
    );
    assert!(!module.vars.iter().any(|(_, var)| var.name.as_str() == "f"));
    assert!(
        module.functions[module.main]
            .body
            .exprs
            .iter()
            .any(|(_, expr)| matches!(expr.kind, ExprKind::Func(_)))
    );
    for source in [
        "class f:\n    pass\ndef f():\n    pass\nf = 1\n",
        "def f():\n    pass\nclass f:\n    pass\nf = 1\n",
        "class f:\n    pass\nclass f:\n    pass\n",
    ] {
        let (_, diagnostics) = lower_result(source);
        assert!(
            diagnostics.iter().any(|d| d.code == Some("E0102")),
            "{diagnostics:?}"
        );
    }
    // 原dynamic_names允许的重复装饰类不能被新增集合选择器收紧。
    let module = lower(
        "def keep(value):\n    return value\n@keep\nclass Ordered:\n    pass\ntry:\n    @keep\n    class Ordered:\n        pass\nexcept ValueError:\n    pass\n",
    );
    assert_eq!(module.classes.len(), 2);
    assert!(
        module
            .classes
            .iter()
            .all(|(_, class)| class.decorated_binding.is_some())
    );
}
