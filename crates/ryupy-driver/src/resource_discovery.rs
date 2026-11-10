//! JIT空资源子包的可选发现：只读原硬图、一次补缺，不执行Python或改变调用。
//! import拼写只是保守提示；callee可重绑，候选失败不能污染诊断或变成强依赖。
use super::{ResourceDiscovery, SourceKind, SourceModule, logical_source, resolve_in};
use crate::Session;
use ryupy_base::SourceDb;
use ryupy_syntax::ast::visitor::{Visitor, walk_expr};
use ryupy_syntax::{ParsedModule, ast};
use std::collections::{BTreeMap, BTreeSet};
use std::path::PathBuf;

pub(super) fn extend(
    session: &mut Session,
    roots: &[PathBuf],
    entry: &SourceModule,
    modules: &mut BTreeMap<String, SourceModule>,
) {
    // 全部候选均对同一原硬图验证，收齐后才插入；新包不能充当下一轮父包。
    let mut accepted = BTreeMap::new();
    for source in std::iter::once(entry).chain(modules.values()) {
        for anchor in anchors(source) {
            if modules.contains_key(&anchor) || accepted.contains_key(&anchor) {
                continue;
            }
            let Some((parent_name, child)) = anchor.rsplit_once('.') else {
                continue;
            };
            let Some(parent) = modules
                .get(parent_name)
                .filter(|p| p.kind == SourceKind::Package)
            else {
                continue;
            };
            let Some(directory) = parent.resolved_source_path.parent() else {
                continue;
            };
            let expected = directory.join(child).join("__init__.py");
            let Some((resolved, SourceKind::Package)) = resolve_in(roots, &anchor) else {
                continue;
            };
            let absolute = if resolved.is_absolute() {
                resolved
            } else if let Ok(cwd) = std::env::current_dir() {
                cwd.join(resolved)
            } else {
                continue;
            };
            // 与runtime严格lexical父子位置合同相同，不canonicalize放宽别名。
            if absolute.as_os_str() != expected.as_os_str() {
                continue;
            }
            let Some((text, mut parsed)) = empty_source(&expected) else {
                continue;
            };
            // 接纳后只登记已解析的同一文本；不二次读取，不合并探测诊断。
            parsed.file = session
                .sources
                .add(expected.display().to_string(), text.clone());
            accepted.insert(
                anchor.clone(),
                SourceModule {
                    kind: SourceKind::Package,
                    resource_discovery: Some(ResourceDiscovery {
                        source: source.name.clone(),
                        anchor: anchor.clone(),
                        reason: "literal-files-empty-direct-package",
                    }),
                    resolved_source_path: expected,
                    name: anchor.clone(),
                    package: anchor.clone(),
                    file: "__init__.py".into(),
                    logical_source: logical_source(&anchor, "__init__.py", SourceKind::Package),
                    parsed,
                    text,
                },
            );
        }
    }
    modules.extend(accepted);
}

fn empty_source(path: &std::path::Path) -> Option<(String, ParsedModule)> {
    let bytes = std::fs::read(path).ok()?;
    // 解析器接收str，不负责PEP263；BOM/编码声明保守拒绝，不尝试自造codec。
    if bytes.starts_with(b"\xef\xbb\xbf")
        || bytes.starts_with(b"\xff\xfe")
        || bytes.starts_with(b"\xfe\xff")
    {
        return None;
    }
    let text = String::from_utf8(bytes).ok()?;
    let newlines = text.replace("\r\n", "\n").replace('\r', "\n");
    if newlines.lines().take(2).any(|line| {
        let line = line.trim_start_matches([' ', '\t', '\x0c']);
        line.starts_with('#')
            && line
                .as_bytes()
                .windows(7)
                .any(|part| part == b"coding:" || part == b"coding=")
    }) {
        return None;
    }
    let mut private = SourceDb::default();
    let file = private.add(path.display().to_string(), text.clone());
    let (parsed, diagnostics) = ryupy_syntax::parse(file, &text);
    (diagnostics.is_empty() && parsed.module.body.is_empty()).then_some((text, parsed))
}

fn anchors(source: &SourceModule) -> BTreeSet<String> {
    let mut spellings = BTreeSet::new();
    for import in ryupy_hir::imports::collect_imports(&source.parsed.module.body, &source.package) {
        let spelling = match (import.module.as_str(), import.name.as_deref()) {
            ("importlib.resources", None) if import.bind_root => "importlib.resources.files".into(),
            ("importlib.resources", None) | ("importlib", Some("resources")) => {
                format!("{}.files", import.bind)
            }
            ("importlib.resources", Some("files")) => import.bind,
            _ => continue,
        };
        spellings.insert(spelling);
    }
    let mut scan = Calls {
        spellings,
        anchors: BTreeSet::new(),
    };
    for statement in &source.parsed.module.body {
        scan.visit_stmt(statement);
    }
    scan.anchors
}

struct Calls {
    spellings: BTreeSet<String>,
    anchors: BTreeSet<String>,
}
impl<'a> Visitor<'a> for Calls {
    fn visit_expr(&mut self, expression: &'a ast::Expr) {
        if let ast::Expr::Call(call) = expression
            && spelling(&call.func).is_some_and(|name| self.spellings.contains(&name))
        {
            let literal = match (
                call.arguments.args.as_ref(),
                call.arguments.keywords.as_ref(),
            ) {
                ([value], []) => Some(value),
                ([], [keyword])
                    if keyword
                        .arg
                        .as_ref()
                        .is_some_and(|name| name.as_str() == "anchor") =>
                {
                    Some(&keyword.value)
                }
                _ => None,
            };
            if let Some(ast::Expr::StringLiteral(literal)) = literal {
                let name = literal.value.to_str();
                if ordinary_name(name) {
                    self.anchors.insert(name.to_owned());
                }
            }
        }
        walk_expr(self, expression);
    }
}
fn spelling(expression: &ast::Expr) -> Option<String> {
    match expression {
        ast::Expr::Name(name) => Some(name.id.to_string()),
        ast::Expr::Attribute(attribute) => Some(format!(
            "{}.{}",
            spelling(&attribute.value)?,
            attribute.attr
        )),
        _ => None,
    }
}
fn ordinary_name(name: &str) -> bool {
    name.contains('.')
        && name.split('.').next() != Some("__main__")
        && name.split('.').all(|part| {
            let mut bytes = part.bytes();
            bytes
                .next()
                .is_some_and(|b| b.is_ascii_alphabetic() || b == b'_')
                && bytes.all(|b| b.is_ascii_alphanumeric() || b == b'_')
        })
}

#[cfg(test)]
#[path = "resource_discovery_tests.rs"]
mod tests;
