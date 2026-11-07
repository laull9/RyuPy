//! AOT 新域单独测试，旧 JIT 资格和实例预检不收紧。
fn assess(source: &str) -> Vec<ryupy_base::Diagnostic> {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("subject.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    crate::validate_instance_aot(&parsed)
}
#[test]
fn explicit_aot_accepts_relative_and_name_handlers() {
    assert!(assess(
        "from . import other\ndef f(x):\n    try:\n        return other.f(x)\n    except ValueError:\n        return 0\n"
    ).is_empty());
}
#[test]
fn explicit_aot_scans_dead_code_defaults_and_decorators() {
    for boundary in [
        "class C:\n    pass\n",
        "def typed(x: int) -> int:\n    return x\n",
        "def outer():\n    def inner():\n        return 1\n    return inner\n",
        "f = lambda: outside\n",
        "def generator():\n    if False:\n        yield 1\n",
        "def generator():\n    yield from []\n",
        "g = (i for i in [])\n",
        "def default(x=lambda: outside):\n    return x\n",
        "@(lambda f: outside)\ndef decorated():\n    return 1\n",
        "def outer():\n    def inner(x=(yield 1)):\n        return x\n",
        "def outer():\n    @(yield 1)\n    def inner():\n        return 1\n",
        "x: int = 1\n",
        "type Alias = int\n",
        "async def f():\n    pass\n",
        "def generic[T](x):\n    return x\n",
        "x = [v for v in []]\n",
        "x = globals()\n",
        "def f():\n    x = 1\n    del x\n",
        "try:\n    pass\nexcept (ValueError, TypeError):\n    pass\n",
    ] {
        for dead in [false, true] {
            let source = if dead {
                format!("def valid():\n    pass\nif False:\n{}", boundary.lines()
                    .map(|line| format!("    {line}\n")).collect::<String>())
            } else {
                format!("def valid():\n    pass\n{boundary}")
            };
            assert!(!assess(&source).is_empty(), "{source}");
        }
    }
    assert!(!assess("x = 1\n").is_empty());
}

#[test]
fn explicit_aot_lambda_positive_domain_has_no_cells_or_free_names() {
    for expression in [
        "lambda: None", "lambda: 1", "lambda: 1.5", "lambda: True",
        "lambda: 'text'", "lambda: b'bytes'", "lambda value: value.encode()",
        "lambda value, /: value.encode()", "lambda *, value: value.encode()",
        "lambda f, x, /, y, *, z: f(x, y, z=z)",
    ] {
        let source = format!("def make():\n    return {expression}\n");
        assert!(assess(&source).is_empty(), "{source}");
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("lambda.py", &source);
        let (parsed, errors) = ryupy_syntax::parse(file, &source);
        assert!(errors.is_empty());
        let (hir, errors) = crate::lower_module(&parsed, &crate::ModuleInfo {
            instance_globals: true, ..Default::default()
        }, &source);
        assert!(errors.is_empty(), "{errors:?}");
        for (_, function) in hir.functions.iter() {
            assert!(function.closure.free.is_empty(), "{source}");
            assert!(function.closure.cells.is_empty(), "{source}");
        }
    }
}

#[test]
fn explicit_aot_lambda_negative_domain_is_position_independent() {
    for expression in [
        "lambda x: outside", "lambda x: len(x)", "lambda x=1: x",
        "lambda *args: args", "lambda **kwargs: kwargs", "lambda x: lambda: x",
        "lambda x: (x := 1)", "lambda x: (yield x)", "lambda x: [v for v in x]",
        "lambda f, x: f(*x)", "lambda f, x: f(**x)", "lambda x: x + 1",
        "lambda x: [x]", "lambda x: f'{x}'", "lambda: 1j", "lambda: ...",
        "lambda x: x.__class__", "lambda globals: globals()",
    ] {
        for source in [
            format!("def make():\n    return {expression}\n"),
            format!("def make():\n    if False:\n        return {expression}\n"),
            format!("def make(value={expression}):\n    return value\n"),
            format!("@({expression})\ndef make():\n    pass\n"),
        ] {
            assert!(!assess(&source).is_empty(), "{source}");
        }
    }
    assert!(!assess("def make(x):\n    return lambda: x\n").is_empty());
    assert!(!assess("f = lambda x: x\n").is_empty());
}
