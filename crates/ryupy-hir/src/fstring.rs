//! f-string递归规格降级；静态规格保持旧常量表示，动态规格保留原表达式与来源。
use crate::expr::ExprLowerCtx;
use crate::hir::{Expr, ExprKind, FStringPart, FStringSpec};
use ryupy_syntax::ast;

impl ExprLowerCtx<'_> {
    pub(crate) fn lower_fstring(&mut self, fstring: &ast::ExprFString) -> ExprKind {
        ExprKind::FString(self.lower_format_parts(fstring.value.elements()))
    }

    fn lower_format_parts<'e>(
        &mut self,
        elements: impl IntoIterator<Item = &'e ast::InterpolatedStringElement>,
    ) -> Vec<FStringPart> {
        elements
            .into_iter()
            .map(|element| match element {
                ast::InterpolatedStringElement::Literal(literal) => {
                    FStringPart::Literal(literal.value.clone())
                }
                ast::InterpolatedStringElement::Interpolation(item) => {
                    let value = self.lower_expr(&item.expression);
                    let spec = item.format_spec.as_ref().map(|spec| {
                        if spec
                            .elements
                            .iter()
                            .all(|p| matches!(p, ast::InterpolatedStringElement::Literal(_)))
                        {
                            let text: String = spec
                                .elements
                                .iter()
                                .filter_map(|p| match p {
                                    ast::InterpolatedStringElement::Literal(literal) => {
                                        Some(literal.value.as_ref())
                                    }
                                    _ => None,
                                })
                                .collect();
                            FStringSpec::Literal(text.into_boxed_str())
                        } else {
                            let parts = self.lower_format_parts(spec.elements.iter());
                            FStringSpec::Dynamic(self.body.exprs.alloc(Expr {
                                kind: ExprKind::FString(parts),
                                range: spec.range,
                            }))
                        }
                    });
                    FStringPart::Value {
                        value,
                        spec,
                        conversion: item.conversion.to_byte().unwrap_or(0),
                    }
                }
            })
            .collect()
    }
}
