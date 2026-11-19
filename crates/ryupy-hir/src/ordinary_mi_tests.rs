//! 新族发布槽在降级任何类体/回调之前建立；旧非MI名字模型不变。
use crate::*;
fn lower(source: &str, instance: bool) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("ordinary_mi.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    lower_module(
        &parsed,
        &ModuleInfo {
            instance_globals: instance,
            ..Default::default()
        },
        source,
    )
}
const DIAMOND: &str = "class A:\n    pass\nclass B(A):\n    pass\nclass C(A):\n    pass\nclass D(B,C):\n    seen=D\nclass E(D):\n    pass\n";
#[test]
fn static_mi_family_gets_real_dynamic_slots_but_old_roots_do_not() {
    let (module, diagnostics) = lower(DIAMOND, false);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    for (_, class) in module.classes.iter() {
        let selected = matches!(class.name.as_str(), "D" | "E");
        assert_eq!(class.ordinary_mi_bases.is_some(), selected);
        assert_eq!(class.decorated_binding.is_some(), selected);
        if selected {
            assert!(class.base.is_none());
        }
    }
    let d = module
        .classes
        .iter()
        .find(|(_, cls)| cls.name.as_str() == "D")
        .unwrap()
        .1;
    let public = d.decorated_binding.unwrap();
    assert!(
        module.functions[module.main]
            .body
            .exprs
            .iter()
            .any(|(_, e)| matches!(e.kind, ExprKind::ModuleVar(id) if id == public))
    );
}
#[test]
fn instance_mi_is_not_accidentally_enabled() {
    let (_, diagnostics) = lower(DIAMOND, true);
    assert!(!diagnostics.is_empty());
}
#[test]
fn repeated_target_keeps_one_public_slot() {
    let (module, diagnostics) = lower(
        "class A:\n    pass\nclass B:\n    pass\nTarget=object()\nclass Target(A,B):\n    seen=Target\n",
        false,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let target = module
        .classes
        .iter()
        .find(|(_, c)| c.name.as_str() == "Target")
        .unwrap()
        .1;
    let binding = target.decorated_binding.unwrap();
    assert_eq!(module.vars[binding].name.as_str(), "Target");
}
