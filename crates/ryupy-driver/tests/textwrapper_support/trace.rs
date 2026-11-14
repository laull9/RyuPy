//! T4仅投影实际CP库路径的两个固定wrap帧，顺序/次数严格；保留全部用户帧和移除记录。
use std::path::Path;
use std::process::Output;

pub(super) struct Projection {
    pub(super) lines: Vec<String>,
    pub(super) removed: Vec<String>,
    positions: Vec<String>,
}

impl Projection {
    // 先由caller保存原流/投影/移除记录，再验证；失败证据也完整留下。
    pub(super) fn check_library_frames(&self, cpython: bool) {
        let expected: &[&str] = if cpython {
            &["384, in wrap", "359, in wrap"]
        } else {
            &[]
        };
        assert_eq!(
            self.positions, expected,
            "CP只能按序投影两个固定库帧；JIT不删除任何帧"
        );
    }
}

pub(super) fn project(output: &Output, cp_library: Option<&Path>) -> Projection {
    let text = std::str::from_utf8(&output.stderr).unwrap();
    assert!(!text.contains("Unhandled exception"), "{text}");
    let mut projected = Vec::new();
    let mut removed = Vec::new();
    let mut positions = Vec::new();
    let mut source_line_allowed = false;
    for raw in text.lines() {
        let line = raw.trim();
        if let Some(file) = line.strip_prefix("File \"") {
            let (path, position) = file.rsplit_once("\", line ").expect(line);
            source_line_allowed = true;
            if cp_library.is_some_and(|library| library == Path::new(path))
                && matches!(position, "384, in wrap" | "359, in wrap")
            {
                removed.push(line.to_owned());
                positions.push(position.to_owned());
                continue;
            }
            let basename = path.rsplit(['/', '\\']).next().unwrap();
            projected.push(format!("File {basename}, line {position}"));
        } else if source_line_allowed && raw.starts_with("    ") {
            // 每个真实File帧只允许紧随一条源码展示，不能吞掉任意缩进异常文本。
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
    Projection {
        lines: projected,
        removed,
        positions,
    }
}
