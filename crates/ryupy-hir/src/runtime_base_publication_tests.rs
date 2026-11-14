//! 动态单基只取得延后发布合同，源导入身份、实际MI与布局一律不静态推断。
use crate::*;

fn lower(source: &str, instance: bool) -> Module {
    let mut db = ryupy_base::SourceDb::default();
    let (parsed, diagnostics) = ryupy_syntax::parse(db.add("runtime_base.py", source), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (module, diagnostics) = lower_module(
        &parsed,
        &ModuleInfo {
            instance_globals: instance,
            ..Default::default()
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    module
}

#[test]
fn imported_alias_attribute_rebinding_and_local_descendants_are_runtime_contracts() {
    let module = lower(
        "from source import Base as Alias\nimport source\nclass A(Alias):\n    pass\nclass B(source.Base):\n    pass\nAlias=list\nclass C(Alias):\n    pass\nclass D(A):\n    pass\n",
        false,
    );
    for (_, class) in module.classes.iter() {
        assert!(class.deferred_runtime_base, "{}", class.name);
        assert!(class.decorated_binding.is_some());
        assert!(class.ordinary_mi_bases.is_none());
        assert!(matches!(class.base, Some(ClassBase::Runtime(_))));
    }
}

#[test]
fn decorated_local_base_is_not_assumed_to_keep_its_original_layout() {
    let module = lower(
        "def decorate(cls):\n    return cls\n@decorate\nclass Base:\n    pass\nclass Child(Base):\n    pass\nclass Grandchild(Child):\n    pass\n",
        false,
    );
    for (_, class) in module.classes.iter() {
        assert_eq!(class.deferred_runtime_base, class.name.as_str() != "Base");
    }
}

#[test]
fn static_single_inheritance_and_exception_path_remain_unchanged() {
    let module = lower(
        "class Base:\n    pass\nclass Child(Base):\n    pass\nclass Error(Exception):\n    pass\n",
        false,
    );
    for (_, class) in module.classes.iter() {
        assert!(!class.deferred_runtime_base);
        assert!(class.ordinary_mi_bases.is_none());
    }
}

#[test]
fn instance_classes_keep_their_existing_explicit_contract() {
    let module = lower(
        "from source import Base\nclass Child(Base):\n    pass\n",
        true,
    );
    assert!(
        module
            .classes
            .iter()
            .all(|(_, class)| !class.deferred_runtime_base)
    );
}
