//! 推导式与生成器表达式的只读 AST 视图。
//!
//! 统一作用域与迭代规则，但保留结果种类；字典另外携带先于 value 求值的 key。
//! 生成器表达式复用作用域规则，但函数体发出 Yield，不能急切物化。

use ryupy_base::TextRange;
use ryupy_syntax::ast;

#[derive(Clone, Copy, PartialEq, Eq)]
pub(crate) enum ComprehensionKind {
    List,
    Dict,
    Set,
    Generator,
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
            ast::Expr::Generator(comp) => Self {
                range: comp.range,
                generators: &comp.generators,
                elt: &comp.elt,
                key: None,
                kind: ComprehensionKind::Generator,
            },
            _ => return None,
        })
    }

    pub(crate) fn name(self) -> &'static str {
        match self.kind {
            ComprehensionKind::List => "<listcomp>",
            ComprehensionKind::Dict => "<dictcomp>",
            ComprehensionKind::Set => "<setcomp>",
            ComprehensionKind::Generator => "<genexpr>",
        }
    }
}
