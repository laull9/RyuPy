//! ABC冷类合同候选：复用稳定导入分析，绝不代替运行时真实基值/C3/布局验证。
//! 仅额外识别纯导入的Python 2兼容try形式；不把任意条件或副作用路径当必定执行。
use crate::exception_imports::ExceptionImport;
use crate::hir::Name;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::Ranged;
use ryupy_syntax::ast;

pub(crate) const NAMES: &[&str] = &[
    "Iterable",
    "Iterator",
    "Generator",
    "Sized",
    "Container",
    "Collection",
    "Sequence",
    "MutableSequence",
    "Mapping",
    "MutableMapping",
    "Set",
    "MutableSet",
    "Callable",
    "Reversible",
    "MappingView",
    "KeysView",
    "ItemsView",
    "ValuesView",
];

pub(crate) fn candidate(expr: &ast::Expr, imports: &FxHashMap<Name, ExceptionImport>) -> bool {
    let Some((root, path)) = crate::exception_imports::attribute_path(expr) else {
        return false;
    };
    imports
        .get(&Name::from(root))
        .is_some_and(|binding| binding.peek_abc_base(&path, expr.range().start()))
}

pub(crate) fn imports(
    stmts: &[ast::Stmt],
    native: &FxHashSet<String>,
) -> FxHashMap<Name, ExceptionImport> {
    // 普通直接导入复用GlobalScope已有分析；只有真正含有限fallback的模块另建分析输入。
    if !native.contains("collections.abc")
        || !stmts.iter().any(|stmt| import_fallback(stmt).is_some())
    {
        return FxHashMap::default();
    }
    // 只规范化候选分析输入，不改实际AST、导入执行顺序、handler或公开值。
    let normalized: Vec<_> = stmts
        .iter()
        .flat_map(|stmt| {
            if let Some(body) = import_fallback(stmt) {
                body.to_vec()
            } else {
                vec![stmt.clone()]
            }
        })
        .collect();
    let native: Vec<_> = native.iter().map(String::as_str).collect();
    crate::exception_imports::collect(&normalized, &native)
}

fn import_fallback(stmt: &ast::Stmt) -> Option<&[ast::Stmt]> {
    let ast::Stmt::Try(guard) = stmt else {
        return None;
    };
    if guard.is_star
        || !guard.orelse.is_empty()
        || !guard.finalbody.is_empty()
        || guard.handlers.len() != 1
    {
        return None;
    }
    let ast::ExceptHandler::ExceptHandler(handler) = &guard.handlers[0];
    if handler.name.is_some()
        || !matches!(handler.type_.as_deref(), Some(ast::Expr::Name(name)) if name.id.as_str() == "ImportError")
    {
        return None;
    }
    let primary = from_names(&guard.body, "collections.abc")?;
    let fallback = from_names(&handler.body, "collections")?;
    (primary == fallback).then_some(guard.body.as_slice())
}

fn from_names(body: &[ast::Stmt], module: &str) -> Option<Vec<(String, String)>> {
    if body.is_empty() {
        return None;
    }
    let mut names = Vec::new();
    for stmt in body {
        let ast::Stmt::ImportFrom(import) = stmt else {
            return None;
        };
        if import.level != 0 || import.module.as_ref().map(|name| name.as_str()) != Some(module) {
            return None;
        }
        for alias in &import.names {
            if !NAMES.contains(&alias.name.as_str()) {
                return None;
            }
            let bound = alias
                .asname
                .as_ref()
                .map_or(alias.name.as_str(), |name| name.as_str());
            if names.iter().any(|(_, previous)| previous == bound) {
                return None;
            }
            names.push((alias.name.to_string(), bound.to_owned()));
        }
    }
    Some(names)
}

#[cfg(test)]
#[path = "abc_bases_tests.rs"]
mod tests;
