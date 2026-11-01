//! 有限 PEP563 注解字符串化：依据 AST 优先级生成，不读源码切片、不求值名字。
//! 括号、下标元组和字符串引号均有专门规则；未知节点明确诊断，不能冒称完整 ast.unparse。
use crate::hir::{Body, Expr, ExprId, ExprKind, Literal};
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

pub(crate) fn lower(
    expr: &ast::Expr,
    file: FileId,
    body: &mut Body,
    diagnostics: &mut Vec<Diagnostic>,
) -> ExprId {
    let kind = match render(expr, 0) {
        Some(value) => ExprKind::Literal(Literal::Str(value.into())),
        None => {
            diagnostics.push(
                Diagnostic::error(
                    Span::new(file, expr.range()),
                    "暂不支持该延迟注解表达式的规范字符串化",
                )
                .with_code("E0100")
                .with_primary_label("不执行注解或使用原始源码代替规范字符串"),
            );
            ExprKind::Missing
        }
    };
    body.exprs.alloc(Expr {
        kind,
        range: expr.range(),
    })
}

fn render(expr: &ast::Expr, parent: u8) -> Option<String> {
    let (text, precedence) = match expr {
        ast::Expr::Name(value) => (value.id.to_string(), 3),
        ast::Expr::Attribute(value) => {
            // 整数后紧跟点会被词法器读成浮点，CPython规范文本在点前加空格。
            let space = if matches!(value.value.as_ref(), ast::Expr::NumberLiteral(n) if matches!(n.value, ast::Number::Int(_)))
            {
                " "
            } else {
                ""
            };
            (
                format!("{}{space}.{}", render(&value.value, 3)?, value.attr),
                3,
            )
        }
        ast::Expr::Subscript(value) => {
            let sub = match value.slice.as_ref() {
                ast::Expr::Tuple(tuple) if !tuple.elts.is_empty() => tuple_items(&tuple.elts)?,
                other => render(other, 0)?,
            };
            (format!("{}[{sub}]", render(&value.value, 3)?), 3)
        }
        ast::Expr::BinOp(value) if value.op == ast::Operator::BitOr => (
            format!("{} | {}", render(&value.left, 1)?, render(&value.right, 2)?),
            1,
        ),
        ast::Expr::Tuple(value) => (format!("({})", tuple_items(&value.elts)?), 3),
        ast::Expr::List(value) => (format!("[{}]", items(&value.elts)?), 3),
        ast::Expr::NoneLiteral(_) => ("None".into(), 3),
        ast::Expr::BooleanLiteral(value) => {
            ((if value.value { "True" } else { "False" }).into(), 3)
        }
        ast::Expr::EllipsisLiteral(_) => ("...".into(), 3),
        ast::Expr::StringLiteral(value) => (quote(value.value.to_str())?, 3),
        ast::Expr::NumberLiteral(value) => match &value.value {
            ast::Number::Int(number) => (number.to_string(), 3),
            // 浮点/复数规范文本需要独立验证科学记数与特殊值，不能复用 Rust Display 假称一致。
            _ => return None,
        },
        _ => return None,
    };
    Some(if precedence < parent {
        format!("({text})")
    } else {
        text
    })
}
fn items(values: &[ast::Expr]) -> Option<String> {
    values
        .iter()
        .map(|value| render(value, 0))
        .collect::<Option<Vec<_>>>()
        .map(|parts| parts.join(", "))
}
fn tuple_items(values: &[ast::Expr]) -> Option<String> {
    let mut text = items(values)?;
    if values.len() == 1 {
        text.push(',');
    }
    Some(text)
}
fn quote(value: &str) -> Option<String> {
    let delimiter = if value.contains('\'') && !value.contains('"') {
        '"'
    } else {
        '\''
    };
    let mut text = String::from(delimiter);
    for ch in value.chars() {
        match ch {
            '\\' => text.push_str("\\\\"),
            '\n' => text.push_str("\\n"),
            '\r' => text.push_str("\\r"),
            '\t' => text.push_str("\\t"),
            c if c == delimiter => {
                text.push('\\');
                text.push(c);
            }
            c if c.is_ascii_control() => text.push_str(&format!("\\x{:02x}", c as u32)),
            c if c.is_ascii() || c.is_alphanumeric() => text.push(c),
            // 非ASCII符号/组合符的Python可打印性依赖Unicode版本，暂不猜测转义规范。
            _ => return None,
        }
    }
    text.push(delimiter);
    Some(text)
}

impl crate::lower::StmtLowerCtx<'_> {
    /// 注解与普通表达式明确分流，不能意外读取尚未定义的类型或创建捕获cell。
    pub(crate) fn lower_annotation(&mut self, expr: &ast::Expr) -> ExprId {
        if self.global_scope.postponed_annotations {
            lower(expr, self.file, self.body, self.diagnostics)
        } else {
            self.lower_expr(expr)
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    fn formatted(source: &str) -> Option<String> {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("annotation.py", source);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
        assert!(diagnostics.is_empty());
        let ast::Stmt::Expr(statement) = &parsed.module.body[0] else {
            panic!("测试输入须为表达式")
        };
        render(&statement.value, 0)
    }
    #[test]
    fn normalizes_precedence_subscripts_and_quotes() {
        for (input, expected) in [
            ("(A | B) | C", "A | B | C"),
            ("A | (B | C)", "A | (B | C)"),
            ("(A | B).Item", "(A | B).Item"),
            ("(1).real", "1 .real"),
            ("T[(A,)]", "T[A,]"),
            ("T[()]", "T[()]"),
            ("Callable[[str], None]", "Callable[[str], None]"),
            ("\"Forward\"", "'Forward'"),
            ("Literal[\"a'b\"]", "Literal[\"a'b\"]"),
            ("Literal[True, 0xff, ...]", "Literal[True, 255, ...]"),
        ] {
            assert_eq!(formatted(input).as_deref(), Some(expected), "{input}");
        }
    }
    #[test]
    fn rejects_complex_forms_instead_of_evaluating_them() {
        for source in ["unknown()", "[x for x in X]", "A + B", "1.25"] {
            assert!(formatted(source).is_none());
        }
    }
}
