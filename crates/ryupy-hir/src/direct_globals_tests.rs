//! 直接globals只放行真实语法位置，推导式与其它frame协议仍拒绝。
fn lower(source: &str, enabled: bool) -> Vec<ryupy_base::Diagnostic> {
    lower_mode(source, enabled, true)
}

fn lower_mode(source: &str, enabled: bool, instance: bool) -> Vec<ryupy_base::Diagnostic> {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("globals.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let mut info = crate::ModuleInfo {
        instance_globals: instance,
        ..Default::default()
    };
    info.instance_capabilities.direct_globals = enabled;
    crate::lower_module(&parsed, &info, source).1
}
#[test]
fn direct_globals_is_explicit_and_resolves_real_callee() {
    for source in [
        "def f():\n    return globals()\n",
        "def f(globals):\n    return globals()\n",
        "globals = callback\ndef f():\n    return globals()\ndel globals\n",
        "def f():\n    global globals\n    globals = callback\n    return globals()\n",
    ] {
        assert!(!lower(source, false).is_empty());
        assert!(lower(source, true).is_empty(), "{source}");
    }
}
#[test]
fn indirect_and_eager_frames_stay_outside_a1() {
    for source in [
        "def f():\n    return globals\n",
        "def f():\n    return globals(1)\n",
        "def f(globals):\n    return globals(1)\n",
        "def f():\n    return globals(*[])\n",
        "def f():\n    return [x for x in globals()]\n",
        "def f():\n    yield globals()\n",
        "def f():\n    return locals()\n",
    ] {
        assert!(!lower(source, true).is_empty(), "{source}");
    }
}

#[test]
fn direct_globals_rejects_all_comprehensions_without_changing_old_generator_domain() {
    for expression in [
        "[x for x in globals()]",
        "{x for x in globals()}",
        "{x: x for x in globals()}",
        "(x for x in globals())",
        "(x for x in [1])",
        "(globals() for x in [1])",
        "(x for x in [1] if globals())",
    ] {
        let source = format!("def f():\n    return {expression}\n");
        assert!(!lower(&source, true).is_empty(), "{source}");
        let unreachable = format!("if False:\n    value = {expression}\n");
        assert!(!lower(&unreachable, true).is_empty(), "{unreachable}");
    }
    let old = "def f():\n    return (x for x in [1])\n";
    assert!(lower(old, false).is_empty());
    assert!(lower_mode(old, false, false).is_empty());
}

#[test]
fn direct_globals_generator_functions_and_parameter_forms_keep_exact_boundaries() {
    for source in [
        "def f():\n    yield globals()\n",
        "def f():\n    yield from globals()\n",
        "def f(globals):\n    return globals(1)\n",
        "def f():\n    return globals(**{})\n",
        "def f():\n    return globals(x=1)\n",
        "def f():\n    return globals(*[])\n",
    ] {
        assert!(!lower(source, true).is_empty(), "{source}");
    }
    assert!(lower("def f(globals):\n    return globals()\n", true).is_empty());
    assert!(lower_mode("def f(globals):\n    return globals(1)\n", false, false).is_empty());
}

#[test]
fn direct_globals_preserves_old_plain_generator_and_legacy_comprehension_boundaries() {
    for source in [
        "def f():\n    yield 1\n",
        "def f():\n    return (x for x in [1])\n",
    ] {
        assert!(lower(source, false).is_empty(), "{source}");
        assert!(lower_mode(source, false, false).is_empty(), "{source}");
    }
    for expression in [
        "[x for x in [1]]",
        "{x for x in [1]}",
        "{x: x for x in [1]}",
    ] {
        let source = format!("def f():\n    return {expression}\n");
        assert!(
            !lower(&source, false).is_empty(),
            "old Instance eager boundary: {source}"
        );
        assert!(
            !lower(&source, true).is_empty(),
            "A1 eager boundary: {source}"
        );
        assert!(
            lower_mode(&source, false, false).is_empty(),
            "Legacy: {source}"
        );
    }
}
