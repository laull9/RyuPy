//! 三类急切推导式的只读 AST 视图。
//!
//! 统一作用域与迭代规则，但保留结果种类；字典另外携带先于 value 求值的 key。
//! 生成器表达式不进入这个视图，避免把惰性求值误实现成急切容器。

use ryupy_base::TextRange;
use ryupy_syntax::ast;

#[derive(Clone, Copy, PartialEq, Eq)]
pub(crate) enum ComprehensionKind {
    List,
    Dict,
    Set,
}

#[derive(Clone, Copy)]
pub(crate) struct Comprehension<'a> {
    pub range: TextRange,
    pub generators: &'a [ast::Comprehension],
    pub elt: &'a ast::Expr,
    pub key: Option<&'a ast::Expr>,
    pub kind: ComprehensionKind,
}

impl<'a> Comprehension<'a> {
    pub(crate) fn from_expr(expr: &'a ast::Expr) -> Option<Self> {
        Some(match expr {
            ast::Expr::ListComp(comp) => Self {
                range: comp.range,
                generators: &comp.generators,
                elt: &comp.elt,
                key: None,
                kind: ComprehensionKind::List,
            },
            ast::Expr::DictComp(comp) => Self {
                range: comp.range,
                generators: &comp.generators,
                elt: &comp.value,
                key: comp.key.as_deref(),
                kind: ComprehensionKind::Dict,
            },
            ast::Expr::SetComp(comp) => Self {
                range: comp.range,
                generators: &comp.generators,
                elt: &comp.elt,
                key: None,
                kind: ComprehensionKind::Set,
            },
            _ => return None,
        })
    }

    pub(crate) fn name(self) -> &'static str {
        match self.kind {
            ComprehensionKind::List => "<listcomp>",
            ComprehensionKind::Dict => "<dictcomp>",
            ComprehensionKind::Set => "<setcomp>",
        }
    }
}
