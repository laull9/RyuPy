//! Enum/Flag的明确语法边界：Enum只接标量/别名，Flag额外接非负整数的有限位运算。
//! 不推测动态元类行为，成员仍在类命名空间按源码顺序求值。
use crate::hir::Name;
use rustc_hash::FxHashSet;
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ast;

pub(crate) fn members(
    class: &ast::StmtClassDef,
    flag: bool,
    file: FileId,
    diagnostics: &mut Vec<Diagnostic>,
) -> Vec<Name> {
    let mut members = Vec::new();
    let mut assigned = FxHashSet::default();
    for stmt in &class.body {
        let problem = match stmt {
            ast::Stmt::Pass(_) => None,
            ast::Stmt::Expr(expr) if matches!(&*expr.value, ast::Expr::StringLiteral(_)) => None,
            ast::Stmt::Assign(assign) if assign.targets.len() == 1 => {
                if let ast::Expr::Name(target) = &assign.targets[0] {
                    let name = Name::from(target.id.as_str());
                    if special(name.as_str()) {
                        Some("暂不支持Enum特殊名字或元类配置")
                    } else if !assigned.insert(name.clone()) {
                        Some("Enum成员/方法名字不能重复声明")
                    } else if flag && !flag_value(&assign.value, &members, 0) {
                        Some("Flag成员仅支持非负整数字面量、此前成员及有限|/&/^组合")
                    } else if !flag && !literal_or_alias(&assign.value, &members) {
                        Some("Enum成员仅支持int/str/bool/None字面量及此前成员别名")
                    } else {
                        members.push(name);
                        None
                    }
                } else {
                    Some("Enum成员只支持单名字赋值")
                }
            }
            ast::Stmt::FunctionDef(function) => {
                let name = Name::from(function.name.as_str());
                if special(name.as_str()) || matches!(name.as_str(), "name" | "value") {
                    Some("暂不支持Enum特殊方法、构造钩子或覆盖name/value描述符")
                } else if !function.decorator_list.is_empty() {
                    Some("暂不支持Enum方法装饰器，不能静默遗漏装饰后变成的成员")
                } else if !assigned.insert(name) {
                    Some("Enum成员/方法名字不能重复声明")
                } else {
                    None
                }
            }
            _ => Some("暂不支持该Enum类体语句，不能作为普通类静默执行"),
        };
        if let Some(problem) = problem {
            diagnostics
                .push(Diagnostic::error(Span::new(file, stmt.range()), problem).with_code("E0100"));
        }
    }
    members
}
fn special(name: &str) -> bool {
    name.starts_with("__") || (name.starts_with('_') && name.ends_with('_')) || name == "mro"
}
fn literal_or_alias(expr: &ast::Expr, members: &[Name]) -> bool {
    match expr {
        ast::Expr::StringLiteral(_) | ast::Expr::BooleanLiteral(_) | ast::Expr::NoneLiteral(_) => {
            true
        }
        ast::Expr::NumberLiteral(number) => matches!(number.value, ast::Number::Int(_)),
        ast::Expr::UnaryOp(unary)
            if matches!(unary.op, ast::UnaryOp::UAdd | ast::UnaryOp::USub) =>
        {
            matches!(&*unary.operand, ast::Expr::NumberLiteral(number) if matches!(number.value, ast::Number::Int(_)))
        }
        ast::Expr::Name(name) => members
            .iter()
            .any(|member| member.as_str() == name.id.as_str()),
        _ => false,
    }
}

/// 有界递归只验证已由解析器接受的表达式形状，不重复计算成员值或提前执行Python代码。
fn flag_value(expr: &ast::Expr, members: &[Name], depth: usize) -> bool {
    if depth > 128 {
        return false;
    }
    match expr {
        ast::Expr::NumberLiteral(number) => matches!(number.value, ast::Number::Int(_)),
        ast::Expr::BooleanLiteral(_) => true,
        ast::Expr::Name(name) => members
            .iter()
            .any(|member| member.as_str() == name.id.as_str()),
        ast::Expr::UnaryOp(unary) if matches!(unary.op, ast::UnaryOp::UAdd) => {
            matches!(&*unary.operand, ast::Expr::NumberLiteral(number) if matches!(number.value, ast::Number::Int(_)))
        }
        ast::Expr::BinOp(binary)
            if matches!(
                binary.op,
                ast::Operator::BitOr | ast::Operator::BitAnd | ast::Operator::BitXor
            ) =>
        {
            flag_value(&binary.left, members, depth + 1)
                && flag_value(&binary.right, members, depth + 1)
        }
        _ => false,
    }
}
