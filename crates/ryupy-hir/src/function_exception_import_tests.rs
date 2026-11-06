//! 局部异常身份的支配/作用域回归；失效扫描另用AST级测试避免被其它语法诊断掩盖。
use crate::{ModuleInfo, lower_module_with_native_exceptions};
use ryupy_syntax::ast;

fn lower(source: &str, native: &[&str]) -> (crate::Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("local_exceptions.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    lower_module_with_native_exceptions(&parsed, &ModuleInfo::default(), source, native)
}

#[test]
fn direct_import_forms_resolve_in_independent_local_scopes_without_exports() {
    let source = "options = 'ordinary export'\ndef one():\n    import getopt as options\n    try:\n        pass\n    except (options.GetoptError, options.error):\n        pass\ndef two():\n    from io import UnsupportedOperation as ValueError\n    try:\n        pass\n    except ValueError:\n        pass\ndef three():\n    from getopt import error as E\n    if True:\n        try:\n            pass\n        except E:\n            pass\n";
    let (module, diagnostics) = lower(source, &["getopt", "io"]);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let types: Vec<_> = module
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.handlers.iter().filter_map(|(_, h)| h.types.as_ref()))
        .flatten()
        .map(|s| s.as_str())
        .collect();
    assert_eq!(
        types,
        [
            "getopt.GetoptError",
            "getopt.GetoptError",
            "io.UnsupportedOperation",
            "getopt.GetoptError"
        ]
    );
    assert!(module.protected_exports.is_empty());
    assert!(!module.vars.iter().any(|(_, var)| var.name.as_str() == "E"));
}

#[test]
fn dynamic_locals_never_fall_back_to_module_or_builtin_identity() {
    for source in [
        "import getopt as E\ndef f(E):\n    try:\n        pass\n    except E.GetoptError:\n        pass\n",
        "def f():\n    from io import UnsupportedOperation as ValueError\n    ValueError = None\n    try:\n        pass\n    except ValueError:\n        pass\n",
        "def f():\n    from getopt import error as E\n    try:\n        pass\n    except (E, unknown):\n        pass\n",
        "def f():\n    try:\n        pass\n    except E:\n        pass\n    from getopt import error as E\n",
        "def f():\n    try:\n        from getopt import error as E\n    except E:\n        pass\n",
        "def f():\n    if True:\n        from getopt import error as E\n    try:\n        pass\n    except E:\n        pass\n",
        "def f():\n    from getopt import error as E\n    def inner():\n        return E\n    try:\n        pass\n    except E:\n        pass\n",
        "def f():\n    from getopt import error as E\n    def inner():\n        try:\n            pass\n        except E:\n            pass\n",
    ] {
        assert_dynamic(source, &["getopt", "io"]);
    }
    let source = "def f():\n    from getopt import error as E\n    try:\n        pass\n    except E:\n        pass\n";
    assert_dynamic(source, &[]);
}

// 不lower不支持的match/class/walrus，用纯AST候选结果直接证明扫描拒绝，而非搭便车E0100。
fn candidate(source: &str) -> bool {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("scan.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
    let ast::Stmt::FunctionDef(def) = &parsed.module.body[0] else {
        panic!("函数fixture")
    };
    let mut scope = crate::scope::FunctionScope::default();
    scope.locals.insert(
        crate::hir::Name::from("E"),
        la_arena::Idx::from_raw(la_arena::RawIdx::from(0u32)),
    );
    let native = ["getopt".to_owned()].into_iter().collect();
    crate::function_exception_imports::collect(def, &native, &scope)
        .contains_key(&crate::hir::Name::from("E"))
}

#[test]
fn every_binding_form_invalidates_the_candidate_even_after_the_handler() {
    for statement in [
        "E = None",
        "E: object",
        "E += 1",
        "E = other = None",
        "E, other = pair",
        "[*E] = values",
        "del E",
        "del (E, other)",
        "for E in values:\n        pass",
        "with context as E:\n        pass",
        "try:\n        pass\n    except Exception as E:\n        pass",
        "if (E := None):\n        pass",
        "def E():\n        pass",
        "class E:\n        pass",
        "def other(value=(E := None)):\n        pass",
        "callback = lambda value=(E := None): value",
        "values = [(E := item) for item in items]",
        "from getopt import error as E",
        "if True:\n        from getopt import error as E",
        "global E",
        "nonlocal E",
        "def other():\n        nonlocal E\n        del E",
        "match value:\n        case E:\n            pass",
        "match value:\n        case [*E]:\n            pass",
        "match value:\n        case {'a': item, **E}:\n            pass",
        "class Local:\n        nonlocal E\n        E = None",
    ] {
        let source = format!(
            "def f():\n    from getopt import error as E\n    try:\n        pass\n    except E:\n        pass\n    {statement}\n"
        );
        assert!(!candidate(&source), "{source}");
    }
    for parameters in ["E", "E=1", "*E", "**E", "*, E", "E, /"] {
        assert!(!candidate(&format!(
            "def f({parameters}):\n    from getopt import error as E\n"
        )));
    }
}

#[test]
fn unrelated_nested_local_writes_do_not_poison_the_outer_candidate() {
    assert!(candidate(
        "def f():\n    from getopt import error as E\n    def inner():\n        E = 1\n        del E\n    return 1\n"
    ));
    let source = "def outer():\n    from getopt import error as E\n    def inner():\n        from io import UnsupportedOperation as E\n        try:\n            pass\n        except E:\n            pass\n    try:\n        pass\n    except E:\n        pass\n";
    assert!(lower(source, &["getopt", "io"]).1.is_empty());
}

// 原拒绝案例现在保真实表达式，仍不得借同名导入获得typed身份。
fn assert_dynamic(source: &str, native: &[&str]) {
    let (module, diagnostics) = lower(source, native);
    assert!(diagnostics.is_empty(), "{source}: {diagnostics:?}");
    let handlers: Vec<_> = module
        .functions
        .iter()
        .flat_map(|(_, f)| f.body.handlers.iter().map(|(_, h)| h))
        .collect();
    assert!(!handlers.is_empty(), "{source}");
    assert!(
        handlers
            .iter()
            .all(|h| h.types.is_none() && h.type_expr.is_some()),
        "{source}"
    );
    assert!(module.protected_exports.is_empty(), "{source}");
}
