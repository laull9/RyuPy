//! 类体嵌套控制流的文本转储，独立于普通语句格式化。
use crate::hir::{Body, ClassItem, Module};
use crate::pretty::dump_expr;
use std::fmt::Write;
pub(crate) fn dump_items(
    out: &mut String,
    module: &Module,
    body: &Body,
    items: &[ClassItem],
    pad: &str,
) {
    for item in items {
        match &item.kind {
            crate::hir::ClassItemKind::If {
                test,
                body: then_body,
                orelse,
            } => {
                writeln!(out, "{pad}    if {}:", dump_expr(module, body, *test)).unwrap();
                dump_items(out, module, body, then_body, &format!("{pad}    "));
                writeln!(out, "{pad}    else:").unwrap();
                dump_items(out, module, body, orelse, &format!("{pad}    "));
            }
            crate::hir::ClassItemKind::Annotation { name, value } => {
                writeln!(out, "{pad}    {name}: {}", dump_expr(module, body, *value)).unwrap();
            }
            crate::hir::ClassItemKind::Attr { name, value } => {
                writeln!(
                    out,
                    "{pad}    {} = {}",
                    name,
                    dump_expr(module, body, *value)
                )
                .unwrap();
            }
            crate::hir::ClassItemKind::Method { func, decorators } => {
                let decorators: Vec<String> = decorators
                    .iter()
                    .map(|d| match d {
                        crate::hir::Decorator::Property => "@property".into(),
                        crate::hir::Decorator::StaticMethod => "@staticmethod".into(),
                        crate::hir::Decorator::ClassMethod => "@classmethod".into(),
                        crate::hir::Decorator::Dataclass => "@dataclass".into(),
                        crate::hir::Decorator::Expr(expr) => {
                            format!("@{}", dump_expr(module, body, *expr))
                        }
                    })
                    .collect();
                for decorator in decorators {
                    writeln!(out, "{pad}    {decorator}").unwrap();
                }
                writeln!(
                    out,
                    "{pad}    def {}(self, ...)",
                    module.functions[*func].name
                )
                .unwrap();
            }
            crate::hir::ClassItemKind::Expr(value) => {
                writeln!(out, "{pad}    {}", dump_expr(module, body, *value)).unwrap();
            }
        }
    }
}
