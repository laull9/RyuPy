//! 仅规整冻结源码的准确回显/caret；保留每条File帧、cause/context和未知stderr行。
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
            echo = if basename == "error_chain.py" {
                match position {
                    "65, in <module>" => Some("raise LookupError('ascii-cause')"),
                    "71, in <module>" => {
                        Some("raise RuntimeError('ascii-ambient') from saved_cause")
                    }
                    "74, in <module>" => Some("argument_outer()"),
                    "51, in argument_outer" => Some("return argument_middle()"),
                    "47, in argument_middle" => {
                        Some("return bytearray(b'A').isdigit(argument_leaf())")
                    }
                    "43, in argument_leaf" => Some("raise ValueError('ascii-argument-body')"),
                    "76, in <module>" => Some("binder_outer()"),
                    "60, in binder_outer" => Some("return binder_middle()"),
                    "56, in binder_middle" => Some("return method(b'A', 1, x=2)"),
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
