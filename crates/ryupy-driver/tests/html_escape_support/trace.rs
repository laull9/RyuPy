//! 只投影唯一已核CP html内部帧；所有用户帧/异常链完整保留，未知stderr行不得吞掉。
use std::path::Path;
use std::process::Output;

pub(super) fn project(output: &Output, cp_html: Option<&Path>) -> Vec<String> {
    let text = std::str::from_utf8(&output.stderr).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let mut projected = Vec::new();
    let mut source_line_allowed = false;
    let mut removed = 0;
    for raw in text.lines() {
        let line = raw.trim();
        if let Some(file) = line.strip_prefix("File \"") {
            let (path, position) = file.rsplit_once("\", line ").expect(line);
            source_line_allowed = true;
            if cp_html.is_some_and(|html| html == Path::new(path)) && position == "19, in escape" {
                removed += 1;
                continue;
            }
            let basename = path.rsplit(['/', '\\']).next().unwrap();
            projected.push(format!("File {basename}, line {position}"));
        } else if source_line_allowed && raw.starts_with("    ") {
            // 只允许每个已识别File帧紧随的一条源码展示，不吞任意缩进异常文本。
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
    assert_eq!(
        removed,
        if cp_html.is_some() { 1 } else { 0 },
        "只允许CP固定文件line19的一个escape帧；JIT不删除任何帧"
    );
    projected
}
