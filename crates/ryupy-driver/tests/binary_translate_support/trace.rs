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
            echo = if basename == "errors_chain.py" {
                match position {
                    "51, in <module>" => Some("raise ValueError('translate-cause')"),
                    "55, in <module>" => {
                        Some("raise RuntimeError('translate-ambient') from saved_cause")
                    }
                    "58, in <module>" => Some("outer()"),
                    "48, in outer" => Some("return middle()"),
                    "46, in middle" => Some("return inner()"),
                    "44, in inner" => Some("return bytearray().translate(None, delete=None)"),
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
