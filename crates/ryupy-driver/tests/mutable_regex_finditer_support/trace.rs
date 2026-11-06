//! 精确源码回显/caret规整；保全部用户帧、cause/context、未知行及原始stderr。
use std::process::Output;

pub(super) fn project(output: &Output) -> Vec<String> {
    let text = std::str::from_utf8(&output.stderr).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let mut result = Vec::new();
    let mut echo = None;
    let mut caret_allowed = false;
    for raw in text.lines() {
        let line = raw.trim();
        if let Some(file) = line.strip_prefix("File \"") {
            let (path, position) = file.rsplit_once("\", line ").expect(line);
            let basename = path.rsplit(['/', '\\']).next().unwrap();
            result.push(format!("File {basename}, line {position}"));
            echo = if basename == "creation_errors_order.py" {
                match position {
                    "195, in <module>" => Some("raise LookupError('finditer-cause')"),
                    "200, in <module>" => {
                        Some("raise RuntimeError('finditer-ambient') from saved_cause")
                    }
                    "204, in <module>" => Some("finditer_outer()"),
                    "191, in finditer_outer" => Some("return finditer_middle()"),
                    "187, in finditer_middle" => {
                        Some("return trace_pattern.finditer(trace_view, TraceIndex())")
                    }
                    "183, in __index__" => Some("return index_leaf()"),
                    "178, in index_leaf" => Some("raise ValueError('finditer-index-body')"),
                    _ => None,
                }
            } else {
                None
            };
            caret_allowed = false;
            continue;
        }
        if raw.starts_with(char::is_whitespace) && echo == Some(line) {
            echo = None;
            caret_allowed = true;
            continue;
        }
        echo = None;
        if caret_allowed
            && raw.starts_with(char::is_whitespace)
            && !line.is_empty()
            && line.chars().all(|ch| matches!(ch, '^' | '~' | ' '))
        {
            caret_allowed = false;
            continue;
        }
        caret_allowed = false;
        if !line.is_empty() {
            result.push(line.to_owned());
        }
    }
    result
}
