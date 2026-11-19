//! f-string递归规格降级；静态规格保持旧常量表示，动态规格保留原表达式与来源。
use crate::expr::ExprLowerCtx;
use crate::hir::{Expr, ExprKind, FStringPart, FStringSpec};
use ryupy_syntax::ast;

impl ExprLowerCtx<'_> {
    pub(crate) fn lower_fstring(&mut self, fstring: &ast::ExprFString) -> ExprKind {
        let points = ryupy_syntax::fstring_codepoints(fstring, &self.global_scope.source_text);
        let mut parts = Vec::new();
        for part in &fstring.value {
            match part {
                ast::FStringPartRef::Literal(text) => {
                    let value = ast::StringLiteralValue::single(text.clone());
                    parts.push(
                        ryupy_syntax::string_codepoints(&value, &self.global_scope.source_text)
                            .map_or_else(
                                || FStringPart::Literal(text.value.clone()),
                                |points| FStringPart::Unicode(points.into_boxed_slice()),
                            ),
                    );
                }
                ast::FStringPartRef::FString(text) => {
                    parts.extend(self.lower_format_parts(text.elements.iter(), &points))
                }
            }
        }
        ExprKind::FString(parts)
    }

    fn lower_format_parts<'e>(
        &mut self,
        elements: impl IntoIterator<Item = &'e ast::InterpolatedStringElement>,
        points: &std::collections::HashMap<ryupy_base::TextRange, Vec<u32>>,
    ) -> Vec<FStringPart> {
        elements
            .into_iter()
            .map(|element| match element {
                ast::InterpolatedStringElement::Literal(literal) => {
                    points.get(&literal.range).map_or_else(|| FStringPart::Literal(literal.value.clone()), |value| FStringPart::Unicode(value.clone().into_boxed_slice()))
                }
                ast::InterpolatedStringElement::Interpolation(item) => {
                    let value = self.lower_expr(&item.expression);
                    let spec = item.format_spec.as_ref().map(|spec| {
                        if spec
                            .elements
                            .iter()
                            .all(|p| matches!(p, ast::InterpolatedStringElement::Literal(value) if !points.contains_key(&value.range)))
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
                            let parts = self.lower_format_parts(spec.elements.iter(), points);
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
