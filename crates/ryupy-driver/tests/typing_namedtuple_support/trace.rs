//! 固定源码位置+所有File帧，无库帧删除或宽泛suffix；仅归一化路径和源码展示。
use std::path::Path;
use std::process::Output;

pub(super) fn checked(output: &Output, input: &Path, source: &str, cpython: bool) -> Vec<String> {
    let source_lines: Vec<_> = source.lines().collect();
    let frames = [
        (13usize, "<module>", "outer()"),
        (11, "outer", "return inner()"),
        (8, "inner", "return Ready()"),
    ];
    for (number, _, text) in frames {
        assert_eq!(source_lines[number - 1].trim(), text);
    }
    assert_eq!(source_lines.len(), 13, "fixture变化必须同步审完整帧oracle");
    let stderr = std::str::from_utf8(&output.stderr).unwrap();
    assert!(stderr.ends_with('\n'));
    let mut lines = stderr.lines().peekable();
    assert_eq!(lines.next(), Some("Traceback (most recent call last):"));
    let mut normalized = vec!["Traceback (most recent call last):".to_owned()];
    let full_path = input.to_str().unwrap();
    let filename = input.file_name().unwrap().to_str().unwrap();
    for (number, function, statement) in frames {
        let raw = lines.next().expect("缺失用户File帧");
        let prefix = "  File \"";
        let file = raw.strip_prefix(prefix).expect("必须为完整File帧");
        let (path, position) = file.rsplit_once("\", line ").expect("帧位置缺失");
        assert!(
            path == full_path || path == filename,
            "未知路径不能按basename吞掉: {path}"
        );
        assert_eq!(position, format!("{number}, in {function}"));
        normalized.push(format!("File {filename}, line {number}, in {function}"));
        // CP显示源码，JIT可不显示；若显示必须恰为该固定行，不丢任意缩进行。
        let displayed = lines
            .peek()
            .is_some_and(|line| line.starts_with("    ") && line.trim() == statement);
        if displayed {
            lines.next();
        }
        if cpython {
            assert!(displayed, "CP必须显示已落盘固定源码行");
        }
        if displayed
            && lines.peek().is_some_and(|line| {
                line.starts_with("    ")
                    && !line.trim().is_empty()
                    && line.trim().chars().all(|c| matches!(c, '^' | '~' | ' '))
            })
        {
            lines.next();
        }
    }
    let error = "TypeError: Ready.__new__() missing 1 required positional argument: 'x'";
    assert_eq!(lines.next(), Some(error));
    normalized.push(error.to_owned());
    assert!(lines.next().is_none(), "额外stderr行/帧不能忽略");
    normalized
}
