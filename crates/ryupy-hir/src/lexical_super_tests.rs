//! super读取的隐式类cell需求必须与CP词法规则一致，不能看callee名字猜运行值。
use crate::*;

fn lower(source: &str, instance: bool) -> Module {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("super.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
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
fn lexical_super_shadow_names_still_request_real_class_cell() {
    let module = lower(
        "class C:\n    def direct(self):\n        return super()\n    def param(self, super):\n        return super()\n    def local(self):\n        super = lambda: 1\n        return super()\n    def global_value(self):\n        global super\n        return super()\n",
        true,
    );
    let class = module.classes.iter().next().unwrap().0;
    for name in ["direct", "param", "local", "global_value"] {
        let function = module
            .functions
            .iter()
            .find(|(_, f)| f.name.as_str() == name)
            .unwrap()
            .1;
        assert_eq!(function.closure.class_cell, Some(class), "{name}");
        assert!(
            function
                .closure
                .free
                .iter()
                .any(|name| name.as_str() == "__class__")
        );
    }
}

#[test]
fn lexical_super_class_local_and_cell_are_not_free_class_bindings() {
    let module = lower(
        "alias = super\nclass C:\n    def local(self):\n        __class__ = 42\n        def inner():\n            return __class__\n        return super()\n    def param(self, __class__):\n        return super()\n    def global_value(self):\n        global __class__\n        return super()\n    def missing(self):\n        return alias()\n    def present(self):\n        __class__\n        return alias()\n",
        true,
    );
    for name in ["local", "param", "global_value", "missing"] {
        let function = module
            .functions
            .iter()
            .find(|(_, f)| f.name.as_str() == name)
            .unwrap()
            .1;
        assert!(function.closure.class_cell.is_none(), "{name}");
        assert!(
            !function
                .closure
                .free
                .iter()
                .any(|name| name.as_str() == "__class__")
        );
        if name == "local" {
            assert!(
                function
                    .closure
                    .cells
                    .iter()
                    .any(|name| name.as_str() == "__class__")
            );
        }
    }
    let present = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "present")
        .unwrap()
        .1;
    assert!(present.closure.class_cell.is_some());
}

#[test]
fn lexical_super_default_mode_does_not_add_class_cells() {
    let module = lower(
        "class C:\n    def f(self):\n        return super()\n",
        false,
    );
    assert!(
        module
            .classes
            .iter()
            .all(|(_, class)| !class.has_class_cell)
    );
    assert!(
        module
            .functions
            .iter()
            .all(|(_, function)| function.closure.class_cell.is_none())
    );
}
