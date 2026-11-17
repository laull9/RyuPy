//! 为Ruff丢弃的代理码元保留原源码点；其余转义仍交解析器，无业务源码执行。
use crate::ast;
use ryupy_base::TextRange;
use std::collections::{HashMap, HashSet};

/// 普通字符串保持原表示；只有真实surrogate escape才返回精确码点。
pub fn string_codepoints(value: &ast::StringLiteralValue, source: &str) -> Option<Vec<u32>> {
    let mut changed = false;
    let mut points = Vec::new();
    for literal in value {
        let token =
            source.get(usize::from(literal.range.start())..usize::from(literal.range.end()))?;
        if matches!(
            literal.flags.prefix(),
            ast::str_prefix::StringLiteralPrefix::Raw { .. }
        ) {
            points.extend(literal.value.chars().map(u32::from));
            continue;
        }
        let (protected, substitutions) = protect(token, literal.value.chars());
        if substitutions.is_empty() {
            points.extend(literal.value.chars().map(u32::from));
            continue;
        }
        let parsed = ruff_python_parser::parse_expression(&protected).ok()?;
        let ast::Expr::StringLiteral(reparsed) = parsed.syntax().body.as_ref() else {
            return None;
        };
        points.extend(restore(reparsed.value.to_str(), &substitutions));
        changed = true;
    }
    changed.then_some(points)
}

/// f-string字面片段和递归format spec共用原token恢复；双花括号与引号继续由Ruff解释。
pub fn fstring_codepoints(value: &ast::ExprFString, source: &str) -> HashMap<TextRange, Vec<u32>> {
    let Some(token) = source.get(usize::from(value.range.start())..usize::from(value.range.end()))
    else {
        return HashMap::new();
    };
    let mut original = Vec::new();
    collect(value.value.elements(), &mut original);
    let (protected, substitutions) =
        protect(token, original.iter().flat_map(|(_, text)| text.chars()));
    if substitutions.is_empty() {
        return HashMap::new();
    }
    let Ok(parsed) = ruff_python_parser::parse_expression(&format!("({protected})")) else {
        return HashMap::new();
    };
    let ast::Expr::FString(reparsed) = parsed.syntax().body.as_ref() else {
        return HashMap::new();
    };
    let mut recovered = Vec::new();
    collect(reparsed.value.elements(), &mut recovered);
    original
        .into_iter()
        .zip(recovered)
        .filter_map(|((range, _), (_, text))| {
            let points = restore(&text, &substitutions);
            points
                .iter()
                .any(|point| (0xd800..=0xdfff).contains(point))
                .then_some((range, points))
        })
        .collect()
}
fn collect<'a>(
    parts: impl IntoIterator<Item = &'a ast::InterpolatedStringElement>,
    output: &mut Vec<(TextRange, String)>,
) {
    for part in parts {
        match part {
            ast::InterpolatedStringElement::Literal(literal) => {
                output.push((literal.range, literal.value.to_string()))
            }
            ast::InterpolatedStringElement::Interpolation(value) => {
                if let Some(spec) = &value.format_spec {
                    collect(spec.elements.iter(), output)
                }
            }
        }
    }
}
fn restore(text: &str, substitutions: &HashMap<u32, u32>) -> Vec<u32> {
    text.chars()
        .map(|ch| {
            substitutions
                .get(&u32::from(ch))
                .copied()
                .unwrap_or(u32::from(ch))
        })
        .collect()
}

fn protect(token: &str, avoid: impl Iterator<Item = char>) -> (String, HashMap<u32, u32>) {
    let used: HashSet<_> = avoid.map(u32::from).collect();
    let mut substitutions = HashMap::new();
    let mut replacements = HashMap::new();
    let mut protected = String::with_capacity(token.len());
    let mut cursor = 0;
    let mut marker = 0xf0000;
    while cursor < token.len() {
        let remaining = &token[cursor..];
        if remaining.starts_with('\\') {
            let digits = match remaining.as_bytes().get(1) {
                Some(b'u') => 4,
                Some(b'U') => 8,
                _ => 0,
            };
            let escaped = remaining
                .get(2..2 + digits)
                .filter(|_| digits > 0)
                .and_then(|s| u32::from_str_radix(s, 16).ok());
            if let Some(point @ 0xd800..=0xdfff) = escaped {
                // 仅2048种代理码点，重复出现复用占位，任意长度token均不会耗尽scalar空间。
                let replacement = *replacements.entry(point).or_insert_with(|| {
                    while used.contains(&marker) || substitutions.contains_key(&marker) {
                        marker += 1;
                    }
                    substitutions.insert(marker, point);
                    marker
                });
                protected.push_str(&format!("\\U{replacement:08x}"));
                cursor += 2 + digits;
                continue;
            }
            protected.push('\\');
            cursor += 1;
            if let Some(next) = token[cursor..].chars().next() {
                protected.push(next);
                cursor += next.len_utf8();
            }
            continue;
        }
        if let Some(next) = remaining.chars().next() {
            protected.push(next);
            cursor += next.len_utf8();
        }
    }
    (protected, substitutions)
}

#[cfg(test)]
#[path = "string_codepoints_tests.rs"]
mod tests;
