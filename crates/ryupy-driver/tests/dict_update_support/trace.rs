//! 仅规整路径、冻结原源中的逐行回显及其caret；完整保留用户帧、cause/context与未知行。
use std::process::Output;

pub(super) fn project(output: &Output, filename: &str, source: &str) -> Vec<String> {
    let text = std::str::from_utf8(&output.stderr).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let source_lines: Vec<_> = source.lines().collect();
    let mut result = Vec::new();
    let mut echo = None;
    let mut caret_allowed = false;
    for raw in text.lines() {
        let line = raw.trim();
        if let Some(file) = line.strip_prefix("File \"") {
            let (path, position) = file.rsplit_once("\", line ").expect(line);
            let basename = path.rsplit(['/', '\\']).next().unwrap();
            result.push(format!("File {basename}, line {position}"));
            // 只删除本次原源准确行号所对应的回显，未知文件/行号/额外输出全部保留。
            echo = if basename == filename {
                position
                    .split_once(", in ")
                    .and_then(|(number, _)| number.parse::<usize>().ok())
                    .and_then(|number| number.checked_sub(1))
                    .and_then(|index| source_lines.get(index))
                    .map(|line| line.trim())
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
