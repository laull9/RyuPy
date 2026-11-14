//! AOT 参数闭合 lambda 的正向语法域；不是纯函数保证，也不替代实例环境预检。
use ryupy_syntax::ast;

pub(super) fn eligible(lambda: &ast::ExprLambda) -> bool {
    let mut names = Vec::new();
    if let Some(parameters) = &lambda.parameters {
        if parameters.vararg.is_some() || parameters.kwarg.is_some() {
            return false;
        }
        if parameters.iter().any(|p| p.annotation().is_some()) {
            return false;
        }
        for parameter in parameters
            .posonlyargs
            .iter()
            .chain(&parameters.args)
            .chain(&parameters.kwonlyargs)
        {
            if parameter.default.is_some() {
                return false;
            }
        }
        names.extend(parameters.iter().map(|p| p.name().as_str()));
    }
    body(&lambda.body, &names)
}

fn body(expression: &ast::Expr, names: &[&str]) -> bool {
    match expression {
        ast::Expr::Name(name) => {
            name.ctx == ast::ExprContext::Load && names.contains(&name.id.as_str())
        }
        ast::Expr::NumberLiteral(number) => {
            matches!(number.value, ast::Number::Int(_) | ast::Number::Float(_))
        }
        ast::Expr::StringLiteral(_)
        | ast::Expr::BytesLiteral(_)
        | ast::Expr::BooleanLiteral(_)
        | ast::Expr::NoneLiteral(_) => true,
        ast::Expr::Attribute(attribute) => {
            attribute.ctx == ast::ExprContext::Load && body(&attribute.value, names)
        }
        ast::Expr::Call(call) => {
            body(&call.func, names)
                && call.arguments.args.iter().all(|arg| body(arg, names))
                && call
                    .arguments
                    .keywords
                    .iter()
                    .all(|keyword| keyword.arg.is_some() && body(&keyword.value, names))
        }
        // 未知节点默认拒绝，尤其展开、闭包、运算、容器和插值字符串。
        _ => false,
    }
}
