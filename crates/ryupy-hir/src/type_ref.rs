//! 类型标注与操作符降级辅助。

use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

use crate::hir::{BinOp, Name, TypeRef};
use crate::scope::{GlobalScope, Resolution};

pub fn map_bin_op(op: ast::Operator) -> Option<BinOp> {
    Some(match op {
        ast::Operator::Add => BinOp::Add,
        ast::Operator::Sub => BinOp::Sub,
        ast::Operator::Mult => BinOp::Mul,
        ast::Operator::FloorDiv => BinOp::FloorDiv,
        ast::Operator::Mod => BinOp::Mod,
        ast::Operator::Pow => BinOp::Pow,
        ast::Operator::BitAnd => BinOp::BitAnd,
        ast::Operator::BitOr => BinOp::BitOr,
        ast::Operator::BitXor => BinOp::BitXor,
        ast::Operator::LShift => BinOp::LShift,
        ast::Operator::RShift => BinOp::RShift,
        _ => return None,
    })
}

pub fn lower_type_ref(
    expr: &ast::Expr,
    file: FileId,
    globals: &GlobalScope,
    diags: &mut Vec<Diagnostic>,
) -> TypeRef {
    match expr {
        ast::Expr::Name(name) => match name.id.as_str() {
            "int" => TypeRef::Int,
            "float" => TypeRef::Float,
            "bool" => TypeRef::Bool,
            "str" => TypeRef::Str,
            "None" => TypeRef::None,
            other => {
                if let Some(Resolution::Class(cls_id)) = globals.names.get(&Name::from(other)) {
                    TypeRef::Class(*cls_id)
                } else {
                    diags.push(
                        Diagnostic::error(
                            Span::new(file, name.range()),
                            format!("无法识别的类型标注 `{other}`"),
                        )
                        .with_code("E0101")
                        .with_primary_label("未知的类型"),
                    );
                    TypeRef::Error
                }
            }
        },
        ast::Expr::NoneLiteral(_) => TypeRef::None,
        _ => {
            diags.push(
                Diagnostic::error(Span::new(file, expr.range()), "暂不支持复杂的类型表达式")
                    .with_code("E0101")
                    .with_primary_label("类型表达式暂不支持"),
            );
            TypeRef::Error
        }
    }
}
