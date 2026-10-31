//! 语法操作符与类型标注到 HIR 的映射辅助。
//!
//! 职责：
//! 1. 将 Ruff AST 的各类运算操作符（算术、位运算、比较、一元、逻辑）转换为 HIR 的对应枚举；
//! 2. 将源码中的类型标注（如 `int`、`float`、`bool` 等）解析为 [`TypeRef`]；
//! 3. 遇到不受支持的操作符或未知类型时，记录相应的编译期诊断。

use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

use crate::hir::{BinOp, BoolOp, CmpOp, Name, TypeRef, UnaryOp};
use crate::scope::{GlobalScope, Resolution};

/// 映射二元算术与位运算符。
pub fn map_bin_op(op: ast::Operator) -> Option<BinOp> {
    Some(match op {
        ast::Operator::Add => BinOp::Add,
        ast::Operator::Sub => BinOp::Sub,
        ast::Operator::Mult => BinOp::Mul,
        ast::Operator::Div => BinOp::TrueDiv,
        ast::Operator::FloorDiv => BinOp::FloorDiv,
        ast::Operator::Mod => BinOp::Mod,
        ast::Operator::Pow => BinOp::Pow,
        ast::Operator::BitAnd => BinOp::BitAnd,
        ast::Operator::BitOr => BinOp::BitOr,
        ast::Operator::BitXor => BinOp::BitXor,
        ast::Operator::LShift => BinOp::LShift,
        ast::Operator::RShift => BinOp::RShift,
        ast::Operator::MatMult => return None,
    })
}

/// 映射一元运算符。
pub fn map_unary_op(op: ast::UnaryOp) -> Option<UnaryOp> {
    Some(match op {
        ast::UnaryOp::Invert => UnaryOp::Invert,
        ast::UnaryOp::Not => UnaryOp::Not,
        ast::UnaryOp::UAdd => UnaryOp::Pos,
        ast::UnaryOp::USub => UnaryOp::Neg,
    })
}

/// 映射比较运算符。
pub fn map_cmp_op(op: ast::CmpOp) -> Option<CmpOp> {
    Some(match op {
        ast::CmpOp::Eq => CmpOp::Eq,
        ast::CmpOp::NotEq => CmpOp::NotEq,
        ast::CmpOp::Lt => CmpOp::Lt,
        ast::CmpOp::LtE => CmpOp::LtE,
        ast::CmpOp::Gt => CmpOp::Gt,
        ast::CmpOp::GtE => CmpOp::GtE,
        ast::CmpOp::In => CmpOp::In,
        ast::CmpOp::NotIn => CmpOp::NotIn,
        ast::CmpOp::Is => CmpOp::Is,
        ast::CmpOp::IsNot => CmpOp::IsNot,
    })
}

/// 映射短路布尔运算符。
pub fn map_bool_op(op: ast::BoolOp) -> BoolOp {
    match op {
        ast::BoolOp::And => BoolOp::And,
        ast::BoolOp::Or => BoolOp::Or,
    }
}

/// 将 AST 类型标注表达式降级为 HIR [`TypeRef`]。
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
