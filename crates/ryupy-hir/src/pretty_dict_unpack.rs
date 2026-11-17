//! 普通/混合字典字面量的HIR转储；共享父模块表达式格式，保留普通字典的历史输出。
use crate::hir::{Body, DictItem, ExprId, Module};

pub(super) fn dump_plain(module: &Module, body: &Body, pairs: &[(ExprId, ExprId)]) -> String {
    let parts: Vec<_> = pairs
        .iter()
        .map(|(key, value)| {
            format!(
                "{}: {}",
                super::dump_expr(module, body, *key),
                super::dump_expr(module, body, *value)
            )
        })
        .collect();
    format!("{{{}}}", parts.join(", "))
}

pub(super) fn dump(module: &Module, body: &Body, items: &[DictItem]) -> String {
    let parts: Vec<_> = items
        .iter()
        .map(|item| match item {
            DictItem::Pair(key, value) => format!(
                "{}: {}",
                super::dump_expr(module, body, *key),
                super::dump_expr(module, body, *value)
            ),
            DictItem::Unpack(source) => format!("**{}", super::dump_expr(module, body, *source)),
        })
        .collect();
    format!("{{{}}}", parts.join(", "))
}
