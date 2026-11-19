//! 只规整当前冻结traceback源的准确回显与caret，保每条用户帧、cause/context及未知行。
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
            echo = if basename == "traceback.py" {
                match position {
                    "5, in <module>" => Some("raise LookupError('affix-cause')"),
                    "34, in <module>" => {
                        Some("raise RuntimeError('affix-ambient') from saved_cause")
                    }
                    "36, in <module>" => Some("caller()"),
                    "31, in caller" => Some("return match_window()"),
                    "25, in match_window" => {
                        Some("return 'abc'.startswith('a', Start(), Broken())")
                    }
                    "16, in __index__" => Some("return fail()"),
                    "11, in fail" => Some("raise ValueError('affix-index-body')"),
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
