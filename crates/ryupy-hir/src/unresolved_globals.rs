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
            | "exit"
            | "filter"
            | "globals"
            | "help"
            | "license"
            | "locals"
            | "memoryview"
            | "quit"
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
            | "MemoryError"
            | "ReferenceError"
            | "SystemError"
            | "FloatingPointError"
            | "ascii"
            | "bin"
            | "format"
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
        for source in ["locals()", "eval('1')", "format(1)"] {
            let (_, diagnostics) = lower(source);
            assert!(diagnostics.iter().any(|d| d.code == Some("E0100")));
        }
        let (_, diagnostics) = lower("def locals():\n    return 1\nprint(locals())\n");
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
    }
}
