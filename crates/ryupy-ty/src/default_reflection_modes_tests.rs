//! 默认值可变和函数对象逃逸触发Dynamic；没有反射的封闭数值helper保持Static。
use crate::{FuncMode, check_module};

fn modes(source: &str) -> Vec<(String, FuncMode)> {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("reflection.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (module, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = check_module(&module);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    module
        .functions
        .iter()
        .map(|(id, function)| (function.name.as_str().into(), types.mode(id)))
        .collect()
}

#[test]
fn mutable_defaults_demote_typed_core_and_unrelated_numeric_helper_stays_static() {
    let result = modes(
        "def defaults(value: int = 1) -> int:\n    return value\ndef numeric(value: int) -> int:\n    return value + 1\nprint(defaults(), numeric(2))\n",
    );
    assert!(result.contains(&("defaults".into(), FuncMode::Dynamic)));
    assert!(result.contains(&("numeric".into(), FuncMode::Static)));
}

#[test]
fn no_initial_default_still_demotes_when_function_object_escapes() {
    for suffix in [
        "alias = value\n",
        "value.__defaults__ = ('changed',)\n",
        "setattr(value, '__defaults__', ('changed',))\n",
        "saved = [value]\n",
        "def escape():\n    return value\n",
    ] {
        let result = modes(&format!(
            "def value(item: int) -> int:\n    return item\n{suffix}"
        ));
        assert!(
            result.contains(&("value".into(), FuncMode::Dynamic)),
            "{suffix}"
        );
    }
}

#[test]
fn global_and_module_dictionary_observers_demote_published_functions() {
    for suffix in ["locals()\n", "import sys\nsys.modules\n"] {
        let result = modes(&format!(
            "def value(item: int) -> int:\n    return item\n{suffix}"
        ));
        assert!(
            result.contains(&("value".into(), FuncMode::Dynamic)),
            "{suffix}"
        );
    }
}
