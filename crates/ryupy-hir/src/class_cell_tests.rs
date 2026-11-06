//! 隐式类 cell 的词法来源测试；普通 namespace 与局部/global 遮蔽都不能误记为类绑定。
use crate::*;
fn lower(source: &str, instance: bool) -> (Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("cell.py", source);
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
#[test]
fn lexical_class_cell_distinguishes_namespace_shadowing_and_forwarding() {
    let source = "__class__ = 'module'\nx = 'module'\nclass C:\n    __class__ = 'namespace'\n    x = 'namespace'\n    def method(self):\n        return lambda: __class__\n    def local(self):\n        __class__ = 'local'\n        return lambda: __class__\n    def parameter(self, __class__):\n        return lambda: __class__\n    def global_value(self):\n        global __class__\n        return __class__\n    def ordinary(self):\n        return x\n";
    let (module, diagnostics) = lower(source, true);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let class = module.classes.iter().next().unwrap().0;
    assert!(module.classes[class].has_class_cell);
    let method = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "method")
        .unwrap()
        .1;
    assert_eq!(method.closure.class_cell, Some(class));
    assert_eq!(
        method
            .closure
            .free
            .iter()
            .map(|n| n.as_str())
            .collect::<Vec<_>>(),
        ["__class__"]
    );
    for name in ["local", "parameter", "global_value", "ordinary"] {
        let function = module
            .functions
            .iter()
            .find(|(_, f)| f.name.as_str() == name)
            .unwrap()
            .1;
        assert_eq!(function.closure.class_cell, None, "{name}");
    }
    assert_eq!(
        module
            .functions
            .iter()
            .filter(|(_, f)| f.closure.class_cell == Some(class))
            .count(),
        2
    );
}
#[test]
fn class_cell_lambdas_use_definition_phase_not_syntactic_containment() {
    let source = "@(lambda: decorate)()\nclass C((lambda: Base)()):\n    value = lambda: __class__\n    @decorator(lambda: __class__)\n    def method(self, arg=lambda: __class__):\n        return __class__\n";
    let (module, diagnostics) = lower(source, true);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let captured = module
        .functions
        .iter()
        .filter(|(_, f)| f.closure.class_cell.is_some())
        .count();
    assert_eq!(
        captured, 4,
        "类体/default/decorator lambda 和 method 各捕获一次"
    );
    assert_eq!(
        module
            .functions
            .iter()
            .filter(|(_, f)| f.name.as_str() == "<lambda>" && f.closure.class_cell.is_none())
            .count(),
        2
    );
}
#[test]
fn class_cell_plain_classes_and_default_mode_do_not_allocate_implicit_cells() {
    for instance in [false, true] {
        let (module, diagnostics) =
            lower("class C:\n    def f(self):\n        return 1\n", instance);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert!(!module.classes.iter().next().unwrap().1.has_class_cell);
        assert!(
            module
                .functions
                .iter()
                .all(|(_, function)| function.closure.class_cell.is_none())
        );
    }
}
#[test]
fn class_cell_explicit_protocol_and_annotation_scopes_stay_diagnostic() {
    for source in [
        "class C:\n    def f(self):\n        nonlocal __class__\n        return __class__\n",
        "class C:\n    __classcell__ = 1\n    def f(self):\n        return __class__\n",
        "class C:\n    def __classcell__(self):\n        return 1\n",
        "def f():\n    class C:\n        def ident(self):\n            return __class__\n",
        "class C:\n    def f(self):\n        return self.__class__\n",
    ] {
        let (_, diagnostics) = lower(source, true);
        assert!(
            diagnostics.iter().any(|d| d.code == Some("E0100")),
            "{source}: {diagnostics:?}"
        );
    }
}

/// 当前解析入口固定 Python 3.11，先准确记录版本诊断；随后验证真实恢复AST的第二道能力守卫。
#[test]
fn class_cell_generic_scope_guard_does_not_depend_on_parser_version_rejection() {
    for source in [
        "class C:\n    def f[__class__](self):\n        return __class__\n",
        "class C[__class__]:\n    def f(self):\n        return __class__\n",
        "class C:\n    def f(self):\n        def nested[__class__]():\n            return __class__\n        return nested\n",
    ] {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("generic.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(!diagnostics.is_empty());
        assert!(
            diagnostics.iter().all(|d| d.code == Some("E0002")),
            "{diagnostics:?}"
        );
        let (_, diagnostics) = lower_module(
            &parsed,
            &ModuleInfo {
                instance_globals: true,
                ..Default::default()
            },
            source,
        );
        assert!(
            diagnostics
                .iter()
                .any(|d| d.code == Some("E0100") && d.message.contains("PEP695")),
            "{diagnostics:?}"
        );
    }
}
