//! 平坦序列解包目标降级；名字、属性、下标可作为普通或星号叶目标。
//! 只记录目标表达式，运行时在完整拆出本层值后按源码顺序求值与写入。

use crate::hir::{Name, SequenceItem, SequenceTarget};
use crate::lower::StmtLowerCtx;
use ryupy_base::{Diagnostic, Ranged, Span, TextRange};
use ryupy_syntax::ast;

impl StmtLowerCtx<'_> {
    pub(crate) fn lower_sequence_target(
        &mut self,
        elements: &[ast::Expr],
        range: TextRange,
    ) -> Option<Vec<SequenceItem>> {
        let mut items = Vec::new();
        let mut has_star = false;
        for element in elements {
            let (leaf, starred) = match element {
                ast::Expr::Starred(star) => (star.value.as_ref(), true),
                other => (other, false),
            };
            if starred && has_star {
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, range), "解包目标中只能有一个星号目标")
                        .with_code("E0100")
                        .with_primary_label("多个星号解包"),
                );
                return None;
            }
            has_star |= starred;
            let target = match leaf {
                ast::Expr::Name(name) => {
                    SequenceTarget::Binding(self.resolve_binding(&Name::from(name.id.as_str()))?)
                }
                ast::Expr::Attribute(attr) => SequenceTarget::Attribute {
                    base: self.lower_expr(&attr.value),
                    name: Name::from(attr.attr.as_str()),
                },
                ast::Expr::Subscript(sub) => SequenceTarget::Subscript {
                    base: self.lower_expr(&sub.value),
                    index: self.lower_subscript_index(&sub.slice),
                },
                _ => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, leaf.range()),
                            "暂不支持嵌套的解包目标",
                        )
                        .with_code("E0100")
                        .with_primary_label("当前支持平坦的变量、属性、下标及其星号目标"),
                    );
                    return None;
                }
            };
            items.push(if starred {
                SequenceItem::Rest(target)
            } else {
                SequenceItem::One(target)
            });
        }
        Some(items)
    }
}
