//! 导入语句按定义位置执行，绑定目标保留 local / global / nonlocal 身份。
use crate::hir::{Name, Stmt, StmtId, StmtKind};
use crate::{imports, lower::StmtLowerCtx};
use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_syntax::ast;

impl StmtLowerCtx<'_> {
    /// 一条 import 可能绑定多个名字，因此可能展开成多条 HIR 语句。
    pub(crate) fn lower_import_to_root(&mut self, stmt: &ast::Stmt) {
        let mut out = Vec::new();
        self.lower_import_into(stmt, &mut out);
        self.body.root.extend(out);
    }

    pub(crate) fn lower_import_into(&mut self, stmt: &ast::Stmt, out: &mut Vec<StmtId>) {
        let line = self.line_of(stmt.range().start().to_usize());
        match stmt {
            ast::Stmt::Import(import) => {
                for alias in &import.names {
                    let module = alias.name.as_str().to_owned();
                    let (bind_name, bind_root) = match &alias.asname {
                        Some(name) => (name.as_str().to_owned(), false),
                        None => (
                            module.split('.').next().unwrap_or_default().to_owned(),
                            module.contains('.'),
                        ),
                    };
                    let Some(bind) = self.resolve_binding(&Name::from(bind_name.as_str())) else {
                        continue;
                    };
                    let id = self.body.stmts.alloc(Stmt {
                        kind: StmtKind::Import {
                            module,
                            bind,
                            bind_root,
                        },
                        range: alias.range,
                        line,
                    });
                    out.push(id);
                }
            }
            ast::Stmt::ImportFrom(from) => {
                let level = from.level;
                let Some(module) = imports::absolute_module(
                    from.module.as_ref().map(|m| m.as_str()),
                    level,
                    self.package,
                ) else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, stmt.range()),
                            "无法解析相对导入：超出顶层包",
                        )
                        .with_code("E0100")
                        .with_primary_label("相对导入层级过深"),
                    );
                    return;
                };
                let mut names = Vec::new();
                for alias in &from.names {
                    let name = alias.name.as_str();
                    if name == "*" {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, alias.range),
                                "暂不支持 `from ... import *`",
                            )
                            .with_code("E0100")
                            .with_primary_label("请显式列出要导入的名字"),
                        );
                        continue;
                    }
                    let bind_name = alias
                        .asname
                        .as_ref()
                        .map(|a| a.as_str().to_owned())
                        .unwrap_or_else(|| name.to_owned());
                    let Some(bind) = self.resolve_binding(&Name::from(bind_name.as_str())) else {
                        continue;
                    };
                    names.push((name.to_owned(), bind));
                }
                if names.is_empty() {
                    return;
                }
                let id = self.body.stmts.alloc(Stmt {
                    kind: StmtKind::ImportFrom { module, names },
                    range: stmt.range(),
                    line,
                });
                out.push(id);
            }
            _ => {}
        }
    }
}
