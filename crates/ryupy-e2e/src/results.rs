//! e2e 结果状态与 JSONL 输出；pending、可转正和真实通过保持分开统计。

use std::fmt::Write as _;
use std::io::Write;
use std::path::Path;

// ───────────────────────────── 结果记录 ─────────────────────────────

/// 一条用例结果。`status` 与进程退出码解耦，便于机器统计。
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum Status {
    Passed,
    Failed,
    /// 功能未实现（`pending`），不计入通过率。
    Pending,
    /// 标了 pending 但其实已经通过——提示该去掉标记。
    UnexpectedPass,
}

impl Status {
    pub fn as_str(self) -> &'static str {
        match self {
            Status::Passed => "passed",
            Status::Failed => "failed",
            Status::Pending => "pending",
            Status::UnexpectedPass => "unexpected-pass",
        }
    }
}

#[derive(Debug, Clone)]
pub struct CaseResult {
    pub name: String,
    pub status: Status,
    pub detail: String,
}

/// 追加写一行 JSON。字段固定为 `name` / `status` / `detail`，方便脚本消费。
pub fn write_jsonl(writer: &mut impl Write, result: &CaseResult) -> std::io::Result<()> {
    let detail: String = result.detail.chars().take(4000).collect();
    writeln!(
        writer,
        "{{\"name\":\"{}\",\"status\":\"{}\",\"detail\":\"{}\"}}",
        json_escape(&result.name),
        result.status.as_str(),
        json_escape(&detail)
    )
}

/// 最小 JSON 字符串转义（只处理引号、反斜杠与控制字符）。
pub fn json_escape(s: &str) -> String {
    let mut out = String::with_capacity(s.len());
    for ch in s.chars() {
        match ch {
            '"' => out.push_str("\\\""),
            '\\' => out.push_str("\\\\"),
            '\n' => out.push_str("\\n"),
            '\r' => out.push_str("\\r"),
            '\t' => out.push_str("\\t"),
            c if (c as u32) < 0x20 => {
                let _ = write!(out, "\\u{:04x}", c as u32);
            }
            c => out.push(c),
        }
    }
    out
}

/// 按本次运行的用例集合重写结果文件（而不是追加），保证文件与运行一致。
pub fn write_results(path: &Path, results: &[CaseResult]) -> std::io::Result<()> {
    if let Some(parent) = path.parent() {
        std::fs::create_dir_all(parent)?;
    }
    let mut buf = Vec::new();
    for r in results {
        write_jsonl(&mut buf, r)?;
    }
    std::fs::write(path, buf)
}
