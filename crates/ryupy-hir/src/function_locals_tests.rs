//! 源码局部元数据的结构回归：顺序、隐藏槽与明确不支持的作用域各自固定。
use crate::*;

fn lower(source: &str) -> Module {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("locals.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (module, diagnostics) = lower_module(&parsed, &ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    module
}
fn names(module: &Module, name: &str) -> Option<Vec<String>> {
    let (_, function) = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == name)
        .unwrap();
    function.body.function_locals.as_ref().map(|ids| {
        ids.iter()
            .map(|&id| function.body.locals[id].name.to_string())
            .collect()
    })
}

#[test]
fn function_locals_parameter_and_real_access_order() {
    let module = lower("def f(a, /, b, *args, c, **kwargs):\n    x = y\n    y = 1\n    return x\n");
    assert_eq!(
        names(&module, "f").unwrap(),
        ["a", "b", "c", "args", "kwargs", "y", "x"]
    );
}
#[test]
fn function_locals_excludes_annotation_only_and_generated_temporaries() {
    let module = lower(
        "def f():\n    annotated: int\n    a = b = 1\n    with manager() as resource:\n        result = resource\n    return a\n",
    );
    assert_eq!(
        names(&module, "f").unwrap(),
        ["a", "b", "resource", "result"]
    );
    let function = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "f")
        .unwrap()
        .1;
    assert!(function.body.locals.len() > function.body.function_locals.as_ref().unwrap().len());
}
#[test]
fn function_locals_orders_imports_definitions_and_exception_normal_path() {
    let module = lower(
        "def f():\n    import sys as module\n    def nested():\n        return 1\n    try:\n        first = 1\n    except Exception as error:\n        failed = 2\n    else:\n        normal = 3\n    finally:\n        final = 4\n    return module\n",
    );
    assert_eq!(
        names(&module, "f").unwrap(),
        [
            "module", "nested", "first", "normal", "error", "failed", "final"
        ]
    );
}
#[test]
fn function_locals_augmented_reads_precede_right_hand_side() {
    let module = lower("def f():\n    left += right\n    right = 1\n");
    assert_eq!(names(&module, "f").unwrap(), ["left", "right"]);
}
#[test]
fn function_locals_rejects_generator_capture_and_inline_comprehension_boundary() {
    let module = lower(
        "def generator():\n    yield 1\ndef captured():\n    x = 1\n    def child():\n        return x\n    return child\ndef comp():\n    value = [x for x in []]\n    return value\n",
    );
    for name in ["generator", "captured", "child", "comp"] {
        assert_eq!(names(&module, name), None, "{name}");
    }
    assert!(module.functions[module.main].body.function_locals.is_none());
}
#[test]
fn function_locals_keeps_unreachable_stores_but_not_bare_annotations() {
    let module = lower(
        "def f():\n    if False:\n        dead = 1\n    return 1\n    later = 2\n    annotation: int\n",
    );
    assert_eq!(names(&module, "f").unwrap(), ["dead", "later"]);
}
#[test]
fn function_locals_accepts_lambda_and_source_temporary_like_names() {
    let module =
        lower("def f():\n    __with_manager = 1\n    return lambda a, *args, k=1, **kwargs: a\n");
    assert_eq!(names(&module, "f").unwrap(), ["__with_manager"]);
    let lambda = module
        .functions
        .iter()
        .find(|(_, f)| f.kind == FunctionKind::Nested)
        .unwrap()
        .1;
    let names: Vec<_> = lambda
        .body
        .function_locals
        .as_ref()
        .unwrap()
        .iter()
        .map(|&id| lambda.body.locals[id].name.as_str())
        .collect();
    assert_eq!(names, ["a", "k", "args", "kwargs"]);
}
