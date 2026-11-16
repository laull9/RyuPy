//! 仅收集普通import别名形状的literal提示；不证明callee、不改静态导入身份。
//! 被遮蔽、重绑定或死分支中的提示同样只能准备隔离候选。
use ast::visitor::{Visitor, walk_expr};
use ryupy_syntax::{ParsedModule, ast};
use std::collections::{BTreeMap, BTreeSet};
#[derive(Default)]
pub(crate) struct Hints {
    pub metadata: bool,
    pub modules: Vec<String>,
    pub groups: Vec<String>,
}
pub(crate) fn collect(parsed: &ParsedModule) -> Hints {
    let mut aliases: BTreeMap<String, BTreeSet<String>> = BTreeMap::new();
    for request in ryupy_hir::imports::collect_all_imports(&parsed.module.body, "") {
        let value = if let Some(name) = request.name {
            format!("{}.{name}", request.module)
        } else if request.bind_root {
            request.module.split('.').next().unwrap_or("").to_owned()
        } else {
            request.module
        };
        aliases.entry(request.bind).or_default().insert(value);
    }
    let mut scan = Scan {
        aliases,
        hints: Hints::default(),
    };
    for stmt in &parsed.module.body {
        scan.visit_stmt(stmt);
    }
    scan.hints
}
struct Scan {
    aliases: BTreeMap<String, BTreeSet<String>>,
    hints: Hints,
}
impl Scan {
    fn target(&self, expr: &ast::Expr, target: &str) -> bool {
        match expr {
            ast::Expr::Name(name) => self
                .aliases
                .get(name.id.as_str())
                .is_some_and(|values| values.contains(target)),
            ast::Expr::Attribute(attr) => target
                .strip_suffix(&format!(".{}", attr.attr))
                .is_some_and(|parent| self.target(&attr.value, parent)),
            _ => false,
        }
    }
}
fn literal(expr: &ast::Expr) -> Option<String> {
    if let ast::Expr::StringLiteral(value) = expr {
        Some(value.value.to_str().to_owned())
    } else {
        None
    }
}
impl<'a> Visitor<'a> for Scan {
    fn visit_expr(&mut self, expr: &'a ast::Expr) {
        if let ast::Expr::Call(call) = expr {
            if self.target(&call.func, "importlib.util.find_spec") {
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
                if let Some(name) = argument.and_then(literal)
                    && valid_name(&name)
                {
                    self.hints.modules.push(name);
                }
            }
            if self.target(&call.func, "importlib.metadata.entry_points") {
                self.hints.metadata = true;
                for keyword in &call.arguments.keywords {
                    if keyword
                        .arg
                        .as_ref()
                        .is_some_and(|name| name.as_str() == "group")
                        && let Some(group) = literal(&keyword.value)
                    {
                        self.hints.groups.push(group);
                    }
                }
            }
        }
        walk_expr(self, expr);
    }
}
pub(crate) fn valid_name(name: &str) -> bool {
    name.split('.').all(|part| {
        let mut bytes = part.bytes();
        bytes
            .next()
            .is_some_and(|c| c.is_ascii_alphabetic() || c == b'_')
            && bytes.all(|c| c.is_ascii_alphanumeric() || c == b'_')
    })
}

#[cfg(test)]
mod tests {
    #[test]
    fn default_source_alias_candidates_are_not_overwritten() {
        let mut db = ryupy_base::SourceDb::default();
        let text = "import importlib.util as util\nutil.find_spec('outer')\ndef unused():\n    import math as util\nclass Scope:\n    import math as util\nimport math as util\nfrom importlib import metadata as md\ndef nested():\n    import math as md\nmd.entry_points(group='group')\n";
        let file = db.add("hint.py", text);
        let (parsed, diagnostics) = ryupy_syntax::parse(file, text);
        assert!(diagnostics.is_empty());
        let hints = super::collect(&parsed);
        assert_eq!(hints.modules, ["outer"]);
        assert_eq!(hints.groups, ["group"]);
        assert!(hints.metadata);
    }
}
