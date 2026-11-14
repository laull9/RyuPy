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
        "class C:\n    class Inner:\n        pass\n",
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
        "async def f():\n    pass\n",
        "x = [v for v in []]\n",
        "x = globals()\n",
        "def f():\n    x = 1\n    del x\n",
        "try:\n    pass\nexcept (ValueError, TypeError):\n    pass\n",
    ] {
        for dead in [false, true] {
            let source = if dead {
                format!(
                    "def valid():\n    pass\nif False:\n{}",
                    boundary
                        .lines()
                        .map(|line| format!("    {line}\n"))
                        .collect::<String>()
                )
            } else {
                format!("def valid():\n    pass\n{boundary}")
            };
            assert!(!assess(&source).is_empty(), "{source}");
        }
    }
    // parser 默认目标仍为3.11；两项3.12语法单独核版本诊断及完整AST，
    // 再断言 AOT 语义拒绝，不能把 E0002 当成 E0100 通过。
    for (boundary, type_alias) in [
        ("type Alias = int\n", true),
        ("def generic[T](x):\n    return x\n", false),
    ] {
        for dead in [false, true] {
            let source = if dead {
                format!(
                    "def valid():\n    pass\nif False:\n{}",
                    boundary
                        .lines()
                        .map(|line| format!("    {line}\n"))
                        .collect::<String>()
                )
            } else {
                format!("def valid():\n    pass\n{boundary}")
            };
            assert_python312_boundary(&source, dead, type_alias);
        }
    }
    assert!(!assess("x = 1\n").is_empty());
}

#[test]
fn explicit_aot_lambda_positive_domain_has_no_cells_or_free_names() {
    for expression in [
        "lambda: None",
        "lambda: 1",
        "lambda: 1.5",
        "lambda: True",
        "lambda: 'text'",
        "lambda: b'bytes'",
        "lambda value: value.encode()",
        "lambda value, /: value.encode()",
        "lambda *, value: value.encode()",
        "lambda f, x, /, y, *, z: f(x, y, z=z)",
    ] {
        let source = format!("def make():\n    return {expression}\n");
        assert!(assess(&source).is_empty(), "{source}");
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("lambda.py", &source);
        let (parsed, errors) = ryupy_syntax::parse(file, &source);
        assert!(errors.is_empty());
        let (hir, errors) = crate::lower_module(
            &parsed,
            &crate::ModuleInfo {
                instance_globals: true,
                ..Default::default()
            },
            &source,
        );
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
        "lambda x: outside",
        "lambda x: len(x)",
        "lambda x=1: x",
        "lambda *args: args",
        "lambda **kwargs: kwargs",
        "lambda x: lambda: x",
        "lambda x: (x := 1)",
        "lambda x: (yield x)",
        "lambda x: [v for v in x]",
        "lambda f, x: f(*x)",
        "lambda f, x: f(**x)",
        "lambda x: x + 1",
        "lambda x: [x]",
        "lambda x: f'{x}'",
        "lambda: 1j",
        "lambda: ...",
        "lambda x: x.__class__",
        "lambda globals: globals()",
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

/// 仅供这两个已知3.12节点：保留生产parser的3.11拒绝，也核实返回的AST形状。
fn assert_python312_boundary(source: &str, dead: bool, type_alias: bool) {
    use ryupy_syntax::ast::Stmt;
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("python312_boundary.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert_eq!(errors.len(), 1, "{source}: {errors:?}");
    assert_eq!(errors[0].code, Some("E0002"));
    assert!(errors[0].message.contains("Python 3.11"));
    assert!(errors[0].message.contains("Python 3.12"));
    let statement = &parsed.module.body[1];
    let statement = if dead {
        let Stmt::If(branch) = statement else {
            panic!("版本负例必须保留完整不可达分支AST");
        };
        &branch.body[0]
    } else {
        statement
    };
    let expected = if type_alias {
        assert!(matches!(statement, Stmt::TypeAlias(_)));
        "注解与类型别名"
    } else {
        assert!(matches!(statement, Stmt::FunctionDef(def) if def.type_params.is_some()));
        "PEP695 泛型函数"
    };
    let errors = crate::validate_instance_aot(&parsed);
    assert!(errors.iter().all(|error| error.code == Some("E0100")));
    assert!(errors.iter().any(|error| error.message.contains(expected)));
}

#[test]
fn explicit_aot_ordinary_class_scope_contract() {
    assert!(assess("class C(Base):\n    def read(self, x=1): return helper(x)\n").is_empty());
    for source in [
        "class C(A,B):\n    def f(self): return 1\n",
        "class C(metaclass=M):\n    def f(self): return 1\n",
        "class C:\n    __slots__=[]\n    def f(self): return 1\n",
        "class C:\n    def f(self):\n        def nested(): return 1\n        return nested()\n",
        "class C:\n    def f(self): yield 1\n",
        "class C:\n    if False: __init_subclass__=hook\n    def f(self): return 1\n",
    ] {
        assert!(!assess(source).is_empty(), "{source}");
    }
}
