//! 实例模块名字解析与首切编译期边界的回归测试。
use crate::{ExprKind, ModuleInfo, StmtKind, lower_module};

fn lower(source: &str, instance_globals: bool) -> (crate::Module, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("instance.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    lower_module(
        &parsed,
        &ModuleInfo {
            instance_globals,
            ..ModuleInfo::default()
        },
        source,
    )
}

#[test]
fn instance_globals_reads_builtins_from_state() {
    let (module, diagnostics) = lower("print(len([1]))\nfor i in range(2):\n    print(i)\n", true);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let body = &module.functions[module.main].body;
    assert!(
        !body
            .exprs
            .iter()
            .any(|(_, e)| matches!(e.kind, ExprKind::Builtin(_)))
    );
    assert!(
        body.stmts
            .iter()
            .any(|(_, s)| matches!(s.kind, StmtKind::ForEach { .. }))
    );
    assert!(
        !body
            .stmts
            .iter()
            .any(|(_, s)| matches!(s.kind, StmtKind::ForRange { .. }))
    );
    for name in ["print", "len", "range"] {
        assert!(module.vars.iter().any(|(_, v)| v.name.as_str() == name));
    }
}

#[test]
fn instance_globals_does_not_overwrite_metadata() {
    let (module, diagnostics) = lower("pass\n", true);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        !module.functions[module.main]
            .body
            .stmts
            .iter()
            .any(|(_, s)| matches!(s.kind, StmtKind::Assign { .. }))
    );
}

#[test]
fn instance_globals_rejects_unsupported_even_when_unreachable() {
    for source in [
        "async def f():\n    pass\n",
        "x = [a for a in []]\n",
        "x = {a for a in []}\n",
        "x = {a:a for a in []}\n",
        "from . import x\n",
        "x: int = 1\n",
        "del x.value\n",
        "try:\n    pass\nexcept:\n    pass\n",
        "globals = 1\n",
        "x = locals\n",
        "x = vars\n",
        "eval = 1\n",
        "exec = 1\n",
        "compile = 1\n",
        "x = __import__\n",
        "import os as globals\n",
        "from builtins import globals as g\n",
        "x = builtins.globals\n",
        "builtins.locals = None\n",
        "try:\n    pass\nexcept* Exception:\n    pass\n",
    ] {
        let unreachable = format!(
            "if False:\n{}",
            source
                .lines()
                .map(|line| format!("    {line}\n"))
                .collect::<String>()
        );
        let (_, diagnostics) = lower(&unreachable, true);
        assert!(
            diagnostics.iter().any(|d| d.code == Some("E0100")),
            "未拒绝 {source:?}: {diagnostics:?}"
        );
    }
}

#[test]
fn instance_globals_allows_code_named_attributes_and_keyword_labels() {
    let source = "\
def use(obj, replacement):
    alias = obj.compile
    obj.exec = replacement
    obj.eval += 1
    return alias(compile=obj.eval, exec='exec', eval='compile')
# compile/exec/eval 只是注释；字符串与 keyword 标签不读取同名环境变量。
text = 'compile exec eval globals locals super __class__'
result = provider.exec(compile=1, exec=2, eval=3)
";
    let (module, diagnostics) = lower(source, true);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let function = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "use")
        .unwrap()
        .1;
    for name in ["compile", "eval"] {
        assert!(function.body.exprs.iter().any(|(_, expr)| {
            matches!(&expr.kind, ExprKind::Attribute { name: attr, .. } if attr.as_str() == name)
        }));
    }
    // keyword/属性名不能误建模块全局槽；只有真正的 receiver/实参名字参加名字解析。
    assert!(
        !module
            .vars
            .iter()
            .any(|(_, variable)| { matches!(variable.name.as_str(), "compile" | "exec" | "eval") })
    );
}

#[test]
fn instance_globals_code_attributes_still_check_receivers_and_arguments() {
    for (source, rejected) in [
        ("result = compile.eval()\n", "compile"),
        ("result = factory(exec).compile()\n", "exec"),
        ("result = objects[eval].exec()\n", "eval"),
        ("result = obj.compile(exec=eval)\n", "eval"),
        ("result = obj.eval(compile=exec)\n", "exec"),
        ("obj.exec = compile\n", "compile"),
        ("result = f'{eval}'\n", "eval"),
    ] {
        let (_, diagnostics) = lower(source, true);
        assert_eq!(diagnostics.len(), 1, "{source}: {diagnostics:?}");
        let diagnostic = &diagnostics[0];
        assert_eq!(diagnostic.code, Some("E0100"));
        let range = diagnostic.primary.span.range;
        assert_eq!(
            &source[usize::from(range.start())..usize::from(range.end())],
            rejected,
            "只拒绝真实裸名，不能报到同名属性或 keyword 标签"
        );
    }
}

#[test]
fn instance_globals_keeps_code_bare_names_aliases_and_shadowing_guarded() {
    for name in ["compile", "exec", "eval"] {
        for source in [
            format!("{name}('1')\n"),
            format!("renamed = {name}\n"),
            format!("{name} = replacement\n"),
            format!("def shadow({name}):\n    return {name}('1')\n"),
            format!("def use():\n    global {name}\n    {name} = replacement\n"),
            format!(
                "def outer({name}):\n    def inner():\n        nonlocal {name}\n        return {name}\n    return inner\n"
            ),
            format!("from builtins import {name} as renamed\n"),
            format!("from provider import value as {name}\n"),
            format!("import provider as {name}\n"),
            format!("import package.{name} as renamed\n"),
        ] {
            let unreachable = format!(
                "if False:\n{}",
                source
                    .lines()
                    .map(|line| format!("    {line}\n"))
                    .collect::<String>()
            );
            let (_, diagnostics) = lower(&unreachable, true);
            assert!(
                diagnostics.iter().any(|d| d.code == Some("E0100")
                    && d.message.contains(&format!("环境名字 `{name}`"))),
                "未保留裸名/导入守卫：{source}: {diagnostics:?}"
            );
        }
    }
}

#[test]
fn instance_globals_keeps_other_environment_protocol_boundaries() {
    for name in ["globals", "locals", "vars", "__import__", "__classcell__"] {
        for source in [
            format!("result = {name}\n"),
            format!("result = obj.{name}\n"),
        ] {
            let (_, diagnostics) = lower(&source, true);
            assert_eq!(diagnostics.len(), 1, "{source}: {diagnostics:?}");
            assert_eq!(diagnostics[0].code, Some("E0100"));
        }
    }
    let (_, diagnostics) = lower("result = obj.__class__\n", true);
    assert_eq!(diagnostics.len(), 1, "{diagnostics:?}");
    let (_, diagnostics) = lower("del obj.compile\n", true);
    assert_eq!(diagnostics.len(), 1, "{diagnostics:?}");
    assert!(diagnostics[0].message.contains("非模块/global裸名del目标"));
}

#[test]
fn instance_globals_admits_builtin_module_attributes_without_claiming_runtime_support() {
    // 此处只证 AST/HIR 准入；builtins 模块、动态代码及 frame 协议另由完整 pending 源留证。
    let (_, diagnostics) = lower(
        "import builtins as b\nx = b.eval\ny = b.compile\nz = b.exec\nalias = getattr(b, 'eval')\n",
        true,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
}

#[test]
fn ordinary_modules_keep_builtin_lowering() {
    let (module, diagnostics) = lower("print(1)\n", false);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(
        module.functions[module.main]
            .body
            .exprs
            .iter()
            .any(|(_, e)| matches!(e.kind, ExprKind::Builtin(_)))
    );
}

#[test]
fn instance_globals_preserves_metadata_reads_and_explicit_writes() {
    let (module, diagnostics) = lower(
        "__name__ = 'new'\nresult = (__name__, __file__, __spec__, __loader__, __cached__, __path__, unknown)\n",
        true,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let body = &module.functions[module.main].body;
    assert_eq!(
        body.stmts
            .iter()
            .filter(|(_, statement)| matches!(statement.kind, StmtKind::Assign { .. }))
            .count(),
        2
    );
    assert!(!body.exprs.iter().any(|(_, expression)| matches!(
        expression.kind,
        ExprKind::Builtin(_) | ExprKind::UnresolvedGlobal(_)
    )));
    for name in [
        "__name__",
        "__file__",
        "__spec__",
        "__loader__",
        "__cached__",
        "__path__",
        "unknown",
    ] {
        assert!(
            module
                .vars
                .iter()
                .any(|(_, variable)| variable.name.as_str() == name)
        );
    }
}

#[test]
fn instance_globals_allows_try_finally_and_absolute_imports() {
    let (_, diagnostics) = lower(
        "import os\ntry:\n    from sys import version\nfinally:\n    print(version)\n",
        true,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
}

#[test]
fn instance_globals_reports_all_unreachable_restrictions_before_lowering() {
    let (module, diagnostics) = lower(
        "if False:\n    def f():\n        return lambda: globals()\n",
        true,
    );
    assert_eq!(diagnostics.len(), 1, "{diagnostics:?}");
    assert!(module.top_functions.is_empty());
    assert_eq!(module.functions.len(), 1);
}

#[test]
fn instance_globals_raise_uses_the_actual_rebound_exception_name() {
    for source in [
        "raise RuntimeError('failure')\n",
        "RuntimeError = factory\nraise RuntimeError('failure') from cause\n",
    ] {
        let (module, diagnostics) = lower(source, true);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let body = &module.functions[module.main].body;
        let value = body
            .stmts
            .iter()
            .find_map(|(_, statement)| {
                if let StmtKind::Raise { value, .. } = statement.kind {
                    value
                } else {
                    None
                }
            })
            .unwrap();
        let ExprKind::Call { callee, .. } = body.exprs[value].kind else {
            panic!("raise 必须保留真实调用");
        };
        let ExprKind::ModuleVar(var) = body.exprs[callee].kind else {
            panic!("异常类型必须读当前实例绑定");
        };
        assert_eq!(module.vars[var].name.as_str(), "RuntimeError");
        assert!(
            !body
                .exprs
                .iter()
                .any(|(_, expression)| matches!(expression.kind, ExprKind::Builtin(_)))
        );
    }
}

/// 只看实际执行的 __doc__ 写入，不把 Function.docstring 元数据当作赋值。
fn docstring_writes(module: &crate::Module) -> Vec<&str> {
    let body = &module.functions[module.main].body;
    body.root
        .iter()
        .filter_map(|id| {
            let StmtKind::Assign {
                target: crate::AssignTarget::ModuleVar(var),
                value,
            } = &body.stmts[*id].kind
            else {
                return None;
            };
            if module.vars[*var].name.as_str() != "__doc__" {
                return None;
            }
            let ExprKind::Literal(crate::Literal::Str(text)) = &body.exprs[*value].kind else {
                panic!("docstring写入必须保留真实字符串");
            };
            Some(text.as_ref())
        })
        .collect()
}

#[test]
fn instance_globals_docstring_is_a_real_first_statement_write() {
    let (module, diagnostics) = lower(
        "('first ' 'doc')\nprint(__doc__)\n__doc__ = 'changed'\n",
        true,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert_eq!(docstring_writes(&module), ["first doc", "changed"]);
    let body = &module.functions[module.main].body;
    let StmtKind::Assign {
        target: crate::AssignTarget::ModuleVar(var),
        ..
    } = body.stmts[body.root[0]].kind
    else {
        panic!("首字符串必须在源位置写入");
    };
    assert_eq!(module.vars[var].name.as_str(), "__doc__");
    assert_eq!(body.stmts[body.root[0]].line, 1);
    assert!(module.functions[module.main].docstring.is_none());
}

#[test]
fn instance_globals_without_docstring_does_not_reset_existing_doc() {
    for source in [
        "",
        "pass\n",
        "pass\n'not a docstring'\n",
        "f'not a docstring'\n",
        "b'not a docstring'\n",
        "print(__doc__)\n",
    ] {
        let (module, diagnostics) = lower(source, true);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert!(
            docstring_writes(&module).is_empty(),
            "无docstring不应在重执行时覆盖已有键：{source:?}"
        );
    }
}

#[test]
fn ordinary_module_docstring_lowering_remains_unchanged() {
    let (module, diagnostics) = lower("'ordinary doc'\n", false);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(docstring_writes(&module).is_empty());
}

#[test]
fn instance_functions_preserve_local_free_and_global_scopes() {
    let (module, diagnostics) = lower(
        "__builtins__ = custom\ndef outer(__builtins__):\n    x = __builtins__\n    def inner(p):\n        nonlocal x\n        global g\n        x = p\n        g = x\n        return __builtins__\n    return inner\n",
        true,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let outer = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "outer")
        .unwrap()
        .1;
    assert!(
        outer
            .body
            .exprs
            .iter()
            .any(|(_, e)| matches!(e.kind, ExprKind::Local(_)))
    );
    let inner = module
        .functions
        .iter()
        .find(|(_, f)| f.name.as_str() == "inner")
        .unwrap()
        .1;
    assert_eq!(inner.closure.free.len(), 2);
    assert!(
        inner
            .body
            .exprs
            .iter()
            .any(|(_, e)| matches!(e.kind, ExprKind::Free(_)))
    );
    assert!(inner.body.stmts.iter().any(|(_, s)| matches!(
        s.kind,
        StmtKind::Assign {
            target: crate::AssignTarget::ModuleVar(_),
            ..
        }
    )));
    assert!(
        !module
            .vars
            .iter()
            .any(|(_, v)| v.name.as_str() == "p" || v.name.as_str() == "x")
    );
}

#[test]
fn debug_load_is_literal_true_in_legacy_and_instance_namespaces() {
    for instance in [false, true] {
        let (module, errors) = lower(
            "x = __debug__\ndef f(value=__debug__):\n    return __debug__\n",
            instance,
        );
        assert!(errors.is_empty(), "{errors:?}");
        let constants = module
            .functions
            .iter()
            .flat_map(|(_, f)| f.body.exprs.iter())
            .filter(|(_, e)| matches!(e.kind, ExprKind::Literal(crate::Literal::Bool(true))))
            .count();
        assert!(constants >= 3);
        assert!(!module.functions.iter().flat_map(|(_, f)| f.body.exprs.iter()).any(|(_, e)|
            matches!(&e.kind, ExprKind::UnresolvedGlobal(n) | ExprKind::ClassLocal(n) if n.as_str() == "__debug__")));
    }
}
