//! 完整异常链的帧/类名/消息/分隔合同；仅规整本次源路径和精确源码/caret，未知行及空行逐字保留。
use std::path::Path;

type Frame = (usize, &'static str, &'static str);
const FRAMES: [&[Frame]; 3] = [
    &[
        (26, "<module>", "original_site()"),
        (21, "original_site", "raise original"),
    ],
    &[
        (27, "<module>", "except targets.error:"),
        (18, "error", "raise replacement"),
    ],
    &[(35, "<module>", "raise final from caught")],
];
const ERRORS: [&str; 3] = [
    "Original: original-chain",
    "Replacement: replacement-chain",
    "Final: final-chain",
];
const SEPARATORS: [&str; 2] = [
    "During handling of the above exception, another exception occurred:",
    "The above exception was the direct cause of the following exception:",
];

fn contract() -> String {
    let mut result = String::new();
    for (index, frames) in FRAMES.iter().enumerate() {
        if index != 0 {
            result.push_str(&format!("\n{}\n\n", SEPARATORS[index - 1]));
        }
        result.push_str("Traceback (most recent call last):\n");
        for (line, function, _) in *frames {
            result.push_str(&format!(
                "  File \"chain.py\", line {line}, in {function}\n"
            ));
        }
        result.push_str(ERRORS[index]);
        result.push('\n');
    }
    result
}

pub(super) fn checked(bytes: &[u8], source: &Path, cpython: bool) -> String {
    let raw = std::str::from_utf8(bytes).unwrap();
    assert!(raw.ends_with('\n'), "完整异常报告必须换行结束");
    assert!(!raw.contains("Unhandled exception"), "{raw}");
    let input = source.join("chain.py");
    let code = std::fs::read_to_string(&input).unwrap();
    let source_lines: Vec<_> = code.lines().collect();
    for &(number, _, statement) in FRAMES.iter().flat_map(|frames| frames.iter()) {
        assert_eq!(
            source_lines[number - 1].trim(),
            statement,
            "固定帧源码合同改变"
        );
    }
    let mut normalized = Vec::new();
    let mut lines = raw.split_terminator('\n').peekable();
    while let Some(line) = lines.next() {
        if let Some(frame) = line.strip_prefix("  File \"") {
            let (path, position) = frame.rsplit_once("\", line ").expect(line);
            assert!(
                Path::new(path) == input || path == "chain.py",
                "仅允许本次完整源路径或编译器已登记的精确短名：{path}"
            );
            let (number, function) = position.split_once(", in ").expect(line);
            let number: usize = number.parse().unwrap();
            let statement = FRAMES
                .iter()
                .flat_map(|frames| frames.iter())
                .find(|&&(line, name, _)| line == number && name == function)
                .expect("出现未知用户帧")
                .2;
            normalized.push(format!("  File \"chain.py\", line {position}"));
            let echo = format!("    {statement}");
            let displayed = lines.peek().is_some_and(|line| *line == echo);
            if cpython {
                assert!(displayed, "CP必须回显固定源码位置");
            }
            if displayed {
                lines.next();
                // 只允许这两个已知调用/属性目标的精确位置标记，不丢任意插入行。
                let caret = match number {
                    26 => Some("    ^^^^^^^^^^^^^^^"),
                    27 => Some("           ^^^^^^^^^^^^^"),
                    _ => None,
                };
                if caret.is_some_and(|caret| lines.peek().is_some_and(|line| *line == caret)) {
                    lines.next();
                }
            }
        } else {
            normalized.push(line.to_owned());
        }
    }
    let normalized = format!("{}\n", normalized.join("\n"));
    assert_eq!(
        normalized,
        contract(),
        "完整帧链/异常类型/消息/空行合同不符"
    );
    normalized
}
