//! 函数来源来自真实定义表达式；同名/同一行、词法上下文及隐式函数分别验证。
use crate::*;

fn definitions(source: &str) -> Vec<(String, u32)> {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("definitions.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (module, diagnostics) = lower_module(&parsed, &ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let mut definitions: Vec<_> = module
        .functions
        .iter()
        .filter_map(|(_, function)| {
            function
                .definition
                .as_ref()
                .map(|info| (info.lexical_qualname.clone(), info.firstlineno))
        })
        .collect();
    assert!(module.functions[module.main].definition.is_none());
    definitions.sort_by_key(|(_, line)| *line);
    definitions
}

#[test]
fn function_definition_uses_first_decorator_expression_and_lexical_classes() {
    let source = "def retain(value):\n    return value\n@(\n retain\n)\n@retain\ndef decorated():\n    return 1\nclass Container:\n    __qualname__ = 'Changed'\n    @retain\n    def method(self):\n        @retain\n        def inner():\n            yield 1\n        return inner\n";
    assert_eq!(
        definitions(source),
        [
            ("retain".into(), 1),
            ("decorated".into(), 4),
            ("Container.method".into(), 11),
            ("Container.method.<locals>.inner".into(), 13),
        ]
    );
}

#[test]
fn function_definition_global_resets_but_nonlocal_preserves_lexical_name() {
    let source = "def outer():\n    original = None\n    if True:\n        global promoted\n    def promoted():\n        return 1\n    def middle():\n        nonlocal original\n        def original():\n            return 2\n        return original\n    return middle\n";
    assert_eq!(
        definitions(source),
        [
            ("outer".into(), 1),
            ("promoted".into(), 5),
            ("outer.<locals>.middle".into(), 7),
            ("outer.<locals>.middle.<locals>.original".into(), 9),
        ]
    );
}

#[test]
fn function_definition_lambda_source_is_distinct_even_on_same_line() {
    let source = "left, right = (lambda: 1), (lambda: 2)\nfactory = lambda: (lambda: 3)\nclass Holder:\n    callback = lambda: 4\n    def method(self, callback=lambda: 5):\n        return lambda: callback()\n";
    assert_eq!(
        definitions(source),
        [
            ("<lambda>".into(), 1),
            ("<lambda>".into(), 1),
            ("<lambda>".into(), 2),
            ("<lambda>.<locals>.<lambda>".into(), 2),
            ("Holder.<lambda>".into(), 4),
            ("Holder.method".into(), 5),
            ("Holder.<lambda>".into(), 5),
            ("Holder.method.<locals>.<lambda>".into(), 6),
        ]
    );
}

#[test]
fn function_definition_implicit_scopes_do_not_invent_origins() {
    let source = "items = [lambda: 1 for x in [0]]\nvalues = (lambda: 2 for x in [0])\ndef make():\n    return (lambda: 3 for x in [0])\n";
    assert_eq!(
        definitions(source),
        [
            ("<lambda>".into(), 1),
            ("<genexpr>.<lambda>".into(), 2),
            ("make".into(), 3),
            ("make.<locals>.<genexpr>.<lambda>".into(), 4),
        ]
    );
}

#[test]
fn function_definition_generator_unpack_targets_keep_expression_sources() {
    let source =
        include_str!("../../../tests/cases/function_definition/generator_target_lambda/entry.py");
    assert_eq!(
        definitions(source),
        [("capture".into(), 5), ("<genexpr>.<lambda>".into(), 9),]
    );
    // 同一只读遍历同时覆盖list目标的属性叶，不改变实际目标求值时机。
    let source = "class Holder:\n    pass\nholder = Holder()\nitems = (value for [(lambda: holder)().item, value] in [(1, 2)])\n";
    assert_eq!(definitions(source), [("<genexpr>.<lambda>".into(), 4)]);
}
