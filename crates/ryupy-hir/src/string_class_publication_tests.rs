//! 字符串基类只扩运行时准入与原子公开槽；真实布局由运行时决定。
use crate::*;

fn lower(source: &str) -> Module {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("string_publication.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (module, diagnostics) = lower_module_with_native_exceptions(
        &parsed,
        &ModuleInfo::default(),
        source,
        &["typing", "enum"],
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    module
}

fn assert_public_slot(module: &Module, name: &str) -> ModuleVarId {
    let mut found = None;
    for (_, class) in module
        .classes
        .iter()
        .filter(|(_, c)| c.name.as_str() == name)
    {
        let binding = class.decorated_binding.expect("必须先分配独立公开槽");
        assert_eq!(module.vars[binding].name.as_str(), name);
        assert!(module.vars[binding].is_global);
        if let Some(previous) = found {
            assert_eq!(previous, binding, "重定义必须共享同一公开绑定");
        }
        found = Some(binding);
    }
    found.expect("测试类必须存在")
}

#[test]
fn direct_string_alias_and_three_descendants_have_public_slots() {
    let module = lower(
        "class Root(str):\n    pass\nclass First(Root):\n    pass\nclass Second(First):\n    pass\nclass Third(Second):\n    pass\nAlias = str\nclass ViaAlias(Alias):\n    pass\n",
    );
    for name in ["Root", "First", "Second", "Third", "ViaAlias"] {
        assert_public_slot(&module, name);
        let class = module
            .classes
            .iter()
            .find(|(_, c)| c.name.as_str() == name)
            .unwrap()
            .1;
        let Some(ClassBase::Runtime(base)) = class.base else {
            panic!("自然继承链必须读取真实基值: {name}");
        };
        if name == "Root" {
            assert!(matches!(
                module.functions[module.main].body.exprs[base].kind,
                ExprKind::Builtin(Builtin::Str)
            ));
        } else {
            assert!(matches!(
                module.functions[module.main].body.exprs[base].kind,
                ExprKind::ModuleVar(_)
            ));
        }
        assert!(!class.is_exception);
    }
}

#[test]
fn publication_fixed_point_is_not_dependent_on_definition_scan_order() {
    // 此处只测公开槽预扫描，不声称前向基类在运行时合法。
    let source = "class Last(Middle):\n    pass\nclass Middle(Root):\n    pass\nclass Root(str):\n    pass\n";
    let mut db = ryupy_base::SourceDb::default();
    let (parsed, diagnostics) = ryupy_syntax::parse(db.add("order.py", source), source);
    assert!(diagnostics.is_empty());
    let mut globals = crate::scope::GlobalScope::default();
    crate::runtime_bases::register(
        &parsed.module.body,
        &mut globals,
        &rustc_hash::FxHashMap::default(),
    );
    for name in ["Root", "Middle", "Last"] {
        assert!(globals.dynamic_names.contains(&Name::from(name)));
    }
    assert_eq!(globals.dynamic_names.len(), 3);
}

#[test]
fn redefinitions_and_body_defaults_read_public_binding_not_private_shell() {
    let module = lower(
        "def decorate(value):\n    return value\nS = 'old'\n@decorate\nclass S(str):\n    previous = S\n    def read(self, value=S):\n        return S\nclass Child(S):\n    pass\nclass S(str):\n    previous = S\n",
    );
    let slot = assert_public_slot(&module, "S");
    assert_public_slot(&module, "Child");
    assert_eq!(
        module
            .classes
            .iter()
            .filter(|(_, c)| c.name.as_str() == "S")
            .count(),
        2
    );
    let class_ids: Vec<_> = module
        .classes
        .iter()
        .filter(|(_, c)| c.name.as_str() == "S")
        .map(|(id, _)| id)
        .collect();
    let mut public_reads = 0;
    for (_, function) in module.functions.iter() {
        for (_, expr) in function.body.exprs.iter() {
            if matches!(expr.kind, ExprKind::ModuleVar(id) if id == slot) {
                public_reads += 1;
            }
            assert!(
                !matches!(expr.kind, ExprKind::Class(id) if class_ids.contains(&id)),
                "不能从源码绕回私有类壳"
            );
        }
    }
    assert!(public_reads >= 4, "类体/default/方法/后代都应实际读公开槽");
}

#[test]
fn source_shadow_and_unrelated_special_class_routes_are_preserved() {
    let module = lower(
        "class str:\n    pass\nclass User(str):\n    pass\nclass Plain:\n    pass\nclass Error(ValueError):\n    pass\nfrom typing import TypedDict\nclass Record(TypedDict):\n    value: int\n",
    );
    for name in ["str", "User", "Plain", "Error"] {
        let class = module
            .classes
            .iter()
            .find(|(_, c)| c.name.as_str() == name)
            .unwrap()
            .1;
        assert!(
            class.decorated_binding.is_none(),
            "不扩同名普通类/异常的发布策略: {name}"
        );
        match name {
            "User" => assert!(matches!(class.base, Some(ClassBase::User(_)))),
            "Error" => assert!(
                class.is_exception && matches!(class.base, Some(ClassBase::Builtin("ValueError")))
            ),
            _ => assert!(class.base.is_none()),
        }
    }
    let record = module
        .classes
        .iter()
        .find(|(_, c)| c.name.as_str() == "Record")
        .unwrap()
        .1;
    assert!(matches!(record.base, Some(ClassBase::TypedDict { .. })));
    let rebound = lower("class Plain:\n    pass\nstr = Plain\nclass Actual(str):\n    pass\n");
    assert_public_slot(&rebound, "Actual");
    let actual = rebound
        .classes
        .iter()
        .find(|(_, c)| c.name.as_str() == "Actual")
        .unwrap()
        .1;
    let Some(ClassBase::Runtime(base)) = actual.base else {
        panic!("重绑必须读实际模块槽")
    };
    assert!(matches!(
        rebound.functions[rebound.main].body.exprs[base].kind,
        ExprKind::ModuleVar(_)
    ));
}
