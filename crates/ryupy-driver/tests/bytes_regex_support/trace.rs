//! 完整用户错误链：只规范路径/源码展示，不删除任何File帧或异常链。
use std::process::Output;

pub(super) fn project(output: &Output) -> Vec<String> {
    let text = std::str::from_utf8(&output.stderr).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let mut projected = Vec::new();
    let mut source_line_allowed = false;
    for raw in text.lines() {
        let line = raw.trim();
        if let Some(file) = line.strip_prefix("File \"") {
            let (path, position) = file.rsplit_once("\", line ").expect(line);
            source_line_allowed = true;
            let basename = path.rsplit(['/', '\\']).next().unwrap();
            projected.push(format!("File {basename}, line {position}"));
        } else if source_line_allowed && raw.starts_with("    ") {
            // 每个已识别File帧只允许一条源码展示；不删除任何File帧。
            source_line_allowed = false;
        } else if line.is_empty()
            || (raw.starts_with("    ")
                && line
                    .chars()
                    .all(|character| matches!(character, '^' | '~' | ' ')))
        {
            continue;
        } else {
            source_line_allowed = false;
            projected.push(line.to_owned());
        }
    }
    projected
}
