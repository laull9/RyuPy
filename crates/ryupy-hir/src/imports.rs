//! `import` 语句的名字解析（M2.7）。
//!
//! 这里只做**纯字符串**处理：把 `from .x import y` 这种相对导入换算成点分绝对模块名，
//! 并列出模块级语句里出现的导入请求。读文件、搜索路径与模块图由 driver 负责，
//! HIR 只使用 driver 已经加载好的模块身份。
//!
//! 递归范围：模块顶层可能出现的 `if` / `for` / `while` / `try` / `with` 体。
//! 函数体与类体内的导入不参与模块图（函数内 import 暂不支持，lowering 会报诊断）。

use ryupy_syntax::ast;

/// 一条模块级导入请求。
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct ImportRequest {
    /// 点分绝对模块名。
    pub module: String,
    /// `from m import name` 里的 name；`m` 自身被导入时为 `None`。
    pub name: Option<String>,
    /// 导入方命名空间里新增的名字（别名优先）。
    pub bind: String,
    /// `import a.b` 且没有别名：只绑定顶层包 `a`，但必须先把 `a.b` 初始化。
    pub bind_root: bool,
}

/// 把 `from <level 个点><module> import ...` 换算成绝对模块名。
///
/// - `level == 0`：绝对导入，原样返回 `module`；
/// - `level >= 1`：相对导入，基准是当前模块的 `__package__`；`level` 每多一个点就上移一层。
///
/// `module` 为空（`from . import x`）时返回当前包名。无法解析时返回 `None`。
pub fn absolute_module(module: Option<&str>, level: u32, package: &str) -> Option<String> {
    if level == 0 {
        return module.map(str::to_owned);
    }
    let mut base = package.to_owned();
    for _ in 1..level {
        base = base.rsplit_once('.')?.0.to_owned();
    }
    if base.is_empty() {
        return None;
    }
    match module {
        Some(name) => Some(format!("{base}.{name}")),
        None => Some(base),
    }
}

/// 收集模块级语句里的全部导入请求（按源码顺序重复出现）。
pub fn collect_imports(stmts: &[ast::Stmt], package: &str) -> Vec<ImportRequest> {
    let mut out = Vec::new();
    walk(stmts, package, &mut out);
    out
}

fn walk(stmts: &[ast::Stmt], package: &str, out: &mut Vec<ImportRequest>) {
    for stmt in stmts {
        match stmt {
            ast::Stmt::Import(import) => {
                for alias in &import.names {
                    let module = alias.name.as_str().to_owned();
                    let (bind, bind_root) = match &alias.asname {
                        Some(alias_name) => (alias_name.as_str().to_owned(), false),
                        None => (
                            module.split('.').next().unwrap_or_default().to_owned(),
                            module.contains('.'),
                        ),
                    };
                    out.push(ImportRequest {
                        module,
                        name: None,
                        bind,
                        bind_root,
                    });
                }
            }
            ast::Stmt::ImportFrom(from) => {
                let level = from.level;
                let Some(module) =
                    absolute_module(from.module.as_ref().map(|m| m.as_str()), level, package)
                else {
                    continue;
                };
                for alias in &from.names {
                    if alias.name.as_str() == "*" {
                        continue;
                    }
                    let name = alias.name.as_str().to_owned();
                    let bind = alias
                        .asname
                        .as_ref()
                        .map(|a| a.as_str().to_owned())
                        .unwrap_or_else(|| name.clone());
                    out.push(ImportRequest {
                        module: module.clone(),
                        name: Some(name),
                        bind,
                        bind_root: false,
                    });
                }
            }
            ast::Stmt::If(s) => {
                walk(&s.body, package, out);
                for clause in &s.elif_else_clauses {
                    walk(&clause.body, package, out);
                }
            }
            ast::Stmt::For(s) => {
                walk(&s.body, package, out);
                walk(&s.orelse, package, out);
            }
            ast::Stmt::While(s) => {
                walk(&s.body, package, out);
                walk(&s.orelse, package, out);
            }
            ast::Stmt::Try(s) => {
                walk(&s.body, package, out);
                for handler in &s.handlers {
                    let ast::ExceptHandler::ExceptHandler(h) = handler;
                    walk(&h.body, package, out);
                }
                walk(&s.orelse, package, out);
                walk(&s.finalbody, package, out);
            }
            ast::Stmt::With(s) => walk(&s.body, package, out),
            _ => {}
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn resolves_relative_modules() {
        assert_eq!(
            absolute_module(Some("state"), 1, "fixtures.module_case"),
            Some("fixtures.module_case.state".to_owned())
        );
        assert_eq!(
            absolute_module(None, 1, "fixtures.module_case"),
            Some("fixtures.module_case".to_owned())
        );
        assert_eq!(
            absolute_module(Some("x"), 2, "fixtures.module_case"),
            Some("fixtures.x".to_owned())
        );
        assert_eq!(absolute_module(Some("os"), 0, ""), Some("os".to_owned()));
        // 超出顶层包的相对导入无法解析
        assert_eq!(absolute_module(Some("x"), 3, "fixtures"), None);
    }
}
