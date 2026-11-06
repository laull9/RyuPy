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
            echo = if basename == "errors_order.py" {
                match position {
                    "188, in <module>" => Some("raise LookupError('sub-cause')"),
                    "194, in <module>" => {
                        Some("raise RuntimeError('sub-ambient') from saved_cause")
                    }
                    "197, in <module>" => Some("sub_outer()"),
                    "184, in sub_outer" => Some("return sub_middle()"),
                    "180, in sub_middle" => {
                        Some("return trace_pattern.sub(callback_middle, trace_view)")
                    }
                    "176, in callback_middle" => Some("return callback_leaf(match)"),
                    "172, in callback_leaf" => Some("raise ValueError('sub-callback-body')"),
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
