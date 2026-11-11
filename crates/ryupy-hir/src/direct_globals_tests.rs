//! 直接globals只放行真实语法位置，推导式与其它frame协议仍拒绝。
fn lower(source: &str, enabled: bool) -> Vec<ryupy_base::Diagnostic> {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("globals.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let mut info = crate::ModuleInfo {
        instance_globals: true,
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
