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
            echo = if basename == "compare_callbacks.py" {
                match position {
                    "87, in <module>" => Some("raise ValueError('slice-cause')"),
                    "91, in <module>" => {
                        Some("raise RuntimeError('slice-ambient') from saved_cause")
                    }
                    "95, in <module>" => Some("slice_outer()"),
                    "85, in slice_outer" => Some("slice_middle()"),
                    "83, in slice_middle" => Some("slice(None).indices(ChainIndex())"),
                    "81, in __index__" => Some("raise LookupError('slice-index')"),
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
