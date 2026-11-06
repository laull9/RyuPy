//! 完整链只规范路径及精确源码/位置的回显；未知行保留并触发差异。
use std::path::{Path, PathBuf};

// 设计期冻结的完整用户帧合同；首次CP不符必须停，不可自动更新。
pub(super) const EXPECTED: &str = concat!(
    "Traceback (most recent call last):\n",
    "  File \"chain.py\", line 26, in <module>\n",
    "  File \"chain.py\", line 21, in original_site\n",
    "Original: original-chain\n",
    "During handling of the above exception, another exception occurred:\n",
    "Traceback (most recent call last):\n",
    "  File \"chain.py\", line 27, in <module>\n",
    "  File \"chain.py\", line 18, in error\n",
    "Replacement: replacement-chain\n",
    "The above exception was the direct cause of the following exception:\n",
    "Traceback (most recent call last):\n",
    "  File \"chain.py\", line 35, in <module>\n",
    "Final: final-chain\n",
);
pub(super) fn project(bytes: &[u8], source: &Path) -> String {
    let raw = std::str::from_utf8(bytes).unwrap();
    assert!(!raw.contains("Unhandled exception"), "{raw}");
    let mut lines = Vec::new();
    let mut echo: Option<String> = None;
    let mut frame_number = 0;
    let mut after_echo = false;
    for line in raw.lines() {
        if line.trim().is_empty() {
            continue;
        }
        if let Some(frame) = line.strip_prefix("  File \"") {
            let (path, position) = frame.rsplit_once("\", line ").expect(line);
            let (number, _) = position.split_once(", in ").expect(line);
            let number: usize = number.parse().unwrap();
            let path = PathBuf::from(path);
            if path == source.join("chain.py") {
                let code = std::fs::read_to_string(&path).unwrap();
                echo = Some(format!(
                    "    {}",
                    code.lines().nth(number - 1).unwrap().trim()
                ));
                lines.push(format!("  File \"chain.py\", line {position}"));
            } else {
                echo = None;
                lines.push(line.to_owned());
            }
            frame_number = number;
            after_echo = false;
        } else if echo.as_deref() == Some(line) {
            echo = None;
            after_echo = true;
        } else if after_echo
            && matches!(
                (frame_number, line),
                (26, "    ^^^^^^^^^^^^^^^") | (27, "           ^^^^^^^^^^^^^")
            )
        {
            after_echo = false;
        } else {
            echo = None;
            after_echo = false;
            lines.push(line.to_owned());
        }
    }
    format!("{}\n", lines.join("\n"))
}
