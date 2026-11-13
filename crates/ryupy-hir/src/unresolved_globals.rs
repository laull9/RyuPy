//! 真正未知的全局读取延迟到运行时；不把未实现的标准内建伪装成缺失用户变量。
use crate::hir::{ExprKind, Module, ModuleVar, Name, TypeRef};
use rustc_hash::FxHashMap;

/// HIR 全部函数体降级后统一登记，避免 AST 全名字扫描把局部/属性/注解误导出。
pub(crate) fn resolve(module: &mut Module) {
    let mut names: FxHashMap<Name, _> = module
        .vars
        .iter()
        .map(|(id, var)| (var.name.clone(), id))
        .collect();
    for (_, function) in module.functions.iter_mut() {
        for (_, expression) in function.body.exprs.iter_mut() {
            let ExprKind::UnresolvedGlobal(name) = &expression.kind else {
                continue;
            };
            let id = *names.entry(name.clone()).or_insert_with(|| {
                module.vars.alloc(ModuleVar {
                    name: name.clone(),
                    range: expression.range,
                    annotation: Some(TypeRef::Runtime),
                    is_global: true,
                    may_delete: false,
                })
            });
            expression.kind = ExprKind::ModuleVar(id);
        }
    }
}

/// 固定 CPython 3.12 的内建名称中，当前尚未实现的子集；用户绑定在本检查之前解析。
/// 新增内建实现时 resolve_builtin 优先命中，因此不会受此拒绝表阻挡。
pub(crate) fn unsupported_builtin(name: &str) -> bool {
    matches!(
        name,
        "__build_class__"
            | "__import__"
            | "aiter"
            | "anext"
            | "breakpoint"
            | "compile"
            | "complex"
            | "credits"
            | "copyright"
            | "delattr"
            | "dir"
            | "divmod"
            | "eval"
            | "exec"
            | "filter"
            | "globals"
            | "help"
            | "license"
            | "locals"
            | "memoryview"
            | "reversed"
            | "vars"
            | "BaseExceptionGroup"
            | "ExceptionGroup"
            | "BlockingIOError"
            | "BrokenPipeError"
            | "ChildProcessError"
            | "ConnectionAbortedError"
            | "ConnectionError"
            | "ConnectionRefusedError"
            | "ConnectionResetError"
            | "EncodingWarning"
            | "EnvironmentError"
            | "InterruptedError"
            | "IOError"
            | "ProcessLookupError"
            | "StopAsyncIteration"
            | "SyntaxError"
            | "IndentationError"
            | "TabError"
            | "TimeoutError"
            | "UnicodeTranslateError"
            | "BufferError"
            | "ReferenceError"
            | "SystemError"
            | "FloatingPointError"
            | "ascii"
            | "bin"
            | "hex"
            | "oct"
            | "slice"
            | "Ellipsis"
            | "__debug__"
            | "__doc__"
            | "__loader__"
            | "__spec__"
    )
}

#[cfg(test)]
mod tests {
    use crate::*;
    fn lower(source: &str) -> (Module, Vec<ryupy_base::Diagnostic>) {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("globals.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty());
        lower_module(&parsed, &ModuleInfo::default(), source)
    }
    #[test]
    fn unknown_names_are_interned_once_without_local_or_annotation_exports() {
        let (module, diagnostics) = lower(
            "from __future__ import annotations\ndef f(value: Missing):\n    local = 1\n    return absent + local\ndef g():\n    return absent\n",
        );
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert_eq!(
            module
                .vars
                .iter()
                .filter(|(_, var)| var.name.as_str() == "absent")
                .count(),
            1
        );
        assert!(
            !module
                .vars
                .iter()
                .any(|(_, var)| matches!(var.name.as_str(), "Missing" | "local" | "value"))
        );
        assert!(module.functions.iter().all(|(_, f)| {
            f.body
                .exprs
                .iter()
                .all(|(_, e)| !matches!(e.kind, ExprKind::UnresolvedGlobal(_)))
        }));
    }
    #[test]
    fn unsupported_builtin_stays_diagnostic_but_user_binding_wins() {
        for source in ["globals()", "eval('1')"] {
            let (_, diagnostics) = lower(source);
            assert!(diagnostics.iter().any(|d| d.code == Some("E0100")));
        }
        let (_, diagnostics) = lower("def locals():\n    return 1\nprint(locals())\n");
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
    }
    #[test]
    fn site_quitters_are_dynamic_builtins_but_user_bindings_win() {
        let (module, diagnostics) = lower("saved = exit\nother = quit\nexit(code=None)\nquit()\n");
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        for name in ["exit", "quit"] {
            assert!(module.functions.iter().any(|(_, f)| f.body.exprs.iter().any(|(_, e)|
                matches!(&e.kind, ExprKind::Builtin(crate::Builtin::Dynamic(value)) if *value == name))));
        }
        for source in [
            "def f(exit, quit):\n    return exit(quit)\n",
            "exit = 7\nquit = 8\nprint(exit, quit)\n",
            "def f():\n    exit = 7\n    quit = 8\n    return exit + quit\n",
        ] {
            let (module, diagnostics) = lower(source);
            assert!(diagnostics.is_empty(), "{diagnostics:?}");
            assert!(
                module
                    .functions
                    .iter()
                    .all(|(_, f)| f.body.exprs.iter().all(|(_, e)| !matches!(
                        &e.kind,
                        ExprKind::Builtin(crate::Builtin::Dynamic("exit" | "quit"))
                    )))
            );
        }
        let (_, diagnostics) = lower("help()\n");
        assert!(diagnostics.iter().any(|d| d.code == Some("E0100")));
    }
    #[test]
    fn format_is_dynamic_and_saved_alias_calls_its_module_binding() {
        let (module, diagnostics) = lower("saved = format\nformat(1)\nsaved(2, 'x')\n");
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let body = &module.functions[module.main].body;
        let saved = module
            .vars
            .iter()
            .find(|(_, var)| var.name.as_str() == "saved")
            .unwrap()
            .0;
        assert_eq!(
            body.exprs
                .iter()
                .filter(|(_, expr)| matches!(
                    expr.kind,
                    ExprKind::Builtin(Builtin::Dynamic("format"))
                ))
                .count(),
            2
        );
        assert!(body.stmts.iter().any(|(_, stmt)| matches!(stmt.kind,
            StmtKind::Assign { target: AssignTarget::ModuleVar(id), value }
                if id == saved && matches!(body.exprs[value].kind,
                    ExprKind::Builtin(Builtin::Dynamic("format")))
        )));
        assert!(body.exprs.iter().any(|(_, expr)| matches!(&expr.kind,
            ExprKind::Call { callee, .. } if matches!(body.exprs[*callee].kind,
                ExprKind::Builtin(Builtin::Dynamic("format")))
        )));
        assert!(body.exprs.iter().any(|(_, expr)| matches!(&expr.kind,
            ExprKind::Call { callee, .. } if matches!(body.exprs[*callee].kind,
                ExprKind::ModuleVar(id) if id == saved)
        )));
    }

    #[test]
    fn format_user_bindings_remain_real_call_targets() {
        for source in [
            "def f(format):\n    return format(1)\n",
            "def f():\n    format = callback\n    return format(1)\n",
            "format = callback\nformat(1)\n",
        ] {
            let (module, diagnostics) = lower(source);
            assert!(diagnostics.is_empty(), "{diagnostics:?}");
            let mut named_calls = 0;
            for (_, function) in module.functions.iter() {
                for (_, expression) in function.body.exprs.iter() {
                    assert!(!matches!(
                        expression.kind,
                        ExprKind::Builtin(Builtin::Dynamic("format"))
                    ));
                    if let ExprKind::Call { callee, .. } = &expression.kind {
                        match function.body.exprs[*callee].kind {
                            ExprKind::Local(id)
                                if function.body.locals[id].name.as_str() == "format" =>
                            {
                                named_calls += 1
                            }
                            ExprKind::ModuleVar(id)
                                if module.vars[id].name.as_str() == "format" =>
                            {
                                named_calls += 1
                            }
                            _ => {}
                        }
                    }
                }
            }
            assert_eq!(named_calls, 1, "{source}");
        }
    }
}
