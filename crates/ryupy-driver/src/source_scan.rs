//! 仅遍历 AST 的依赖证据；包含函数、死分支与星号导入，绝不求值 Python 表达式。
//! 动态调用按语法候选记录，别名和遮蔽均不能据此取得模块来源权限。
use ast::visitor::{Visitor, walk_expr, walk_stmt};
use ryupy_syntax::{ParsedModule, ast};
use serde::Serialize;
use std::collections::{BTreeMap, BTreeSet};

#[derive(Default)]
pub(crate) struct Scan {
    pub imports: BTreeMap<String, BTreeSet<String>>,
    pub dynamic: Vec<Dynamic>,
    aliases: BTreeMap<String, BTreeSet<String>>,
    package: String,
}

#[derive(Clone, Serialize)]
pub(crate) struct Dynamic {
    pub operation: String,
    pub literal_target: Option<String>,
    pub reason: &'static str,
}

pub(crate) fn collect(parsed: &ParsedModule, package: &str) -> Scan {
    let mut scan = Scan {
        package: package.to_owned(),
        ..Default::default()
    };
    for import in ryupy_hir::imports::collect_all_imports(&parsed.module.body, package) {
        let target = match import.name {
            Some(name) => format!("{}.{name}", import.module),
            None if import.bind_root => import.module.split('.').next().unwrap_or("").to_owned(),
            None => import.module,
        };
        scan.aliases.entry(import.bind).or_default().insert(target);
    }
    for statement in &parsed.module.body {
        scan.visit_stmt(statement);
    }
    scan
}

impl Scan {
    /// 全分支扫描只积累可能目标；后续分支的同名绑定不能擦掉先前证据。
    /// 多目标仍是语法候选，不推断实际控制流或授予来源权限。
    fn targets(&self, expr: &ast::Expr) -> BTreeSet<String> {
        match expr {
            ast::Expr::Name(name) => self
                .aliases
                .get(name.id.as_str())
                .cloned()
                .unwrap_or_else(|| BTreeSet::from([name.id.to_string()])),
            ast::Expr::Attribute(attr) => self
                .targets(&attr.value)
                .into_iter()
                .map(|target| format!("{target}.{}", attr.attr))
                .collect(),
            _ => BTreeSet::new(),
        }
    }
}

impl<'a> Visitor<'a> for Scan {
    fn visit_stmt(&mut self, statement: &'a ast::Stmt) {
        match statement {
            ast::Stmt::Import(import) => {
                for alias in &import.names {
                    self.imports.entry(alias.name.to_string()).or_default();
                }
            }
            ast::Stmt::ImportFrom(import) => {
                if let Some(module) = ryupy_hir::imports::absolute_module(
                    import.module.as_ref().map(|name| name.as_str()),
                    import.level,
                    &self.package,
                ) {
                    self.imports
                        .entry(module)
                        .or_default()
                        .extend(import.names.iter().map(|alias| alias.name.to_string()));
                } else {
                    self.dynamic.push(Dynamic {
                        operation: "relative_import".into(),
                        literal_target: None,
                        reason: "相对导入超出当前包边界，无法静态绝对化",
                    });
                }
            }
            ast::Stmt::Assign(assign) => {
                // 只保留简单别名候选；重绑定无法证明实际 callee，仍标为不确定。
                let targets = self.targets(&assign.value);
                for left in &assign.targets {
                    if let ast::Expr::Name(name) = left {
                        self.aliases
                            .entry(name.id.to_string())
                            .or_default()
                            .extend(targets.iter().cloned());
                    }
                }
            }
            _ => {}
        }
        walk_stmt(self, statement);
    }

    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        if let ast::Expr::Call(call) = expr {
            for target in self.targets(&call.func).into_iter().filter(|target| {
                matches!(
                    target.as_str(),
                    "__import__"
                        | "builtins.__import__"
                        | "importlib.import_module"
                        | "importlib.util.find_spec"
                        | "exec"
                        | "eval"
                        | "compile"
                        | "builtins.exec"
                        | "builtins.eval"
                        | "builtins.compile"
                        | "getattr"
                        | "builtins.getattr"
                )
            }) {
                let argument = call.arguments.args.first().or_else(|| {
                    call.arguments
                        .keywords
                        .iter()
                        .find(|keyword| {
                            keyword
                                .arg
                                .as_ref()
                                .is_some_and(|name| name.as_str() == "name")
                        })
                        .map(|keyword| &keyword.value)
                });
                let literal_target = argument.and_then(|argument| {
                    if let ast::Expr::StringLiteral(value) = argument {
                        Some(value.value.to_str().to_owned())
                    } else {
                        None
                    }
                });
                self.dynamic.push(Dynamic {
                    operation: target,
                    literal_target,
                    reason: "语法候选；callee 可被遮蔽或重绑定，字符串只作线索，不授予来源权限",
                });
            }
        }
        walk_expr(self, expr);
    }
}
