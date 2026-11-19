//! 源码字节位置与语句行号换算；独立于名字解析和模块编排。
use crate::lower::StmtLowerCtx;

/// 预先算出每一行的起始字节偏移，供 `TextRange` → 行号换算。
pub(crate) fn line_starts(source: &str) -> Vec<usize> {
    let mut starts = vec![0usize];
    for (idx, byte) in source.bytes().enumerate() {
        if byte == b'\n' {
            starts.push(idx + 1);
        }
    }
    starts
}

impl StmtLowerCtx<'_> {
    /// 字节偏移 → 1 起行号。
    pub(crate) fn line_of(&self, offset: usize) -> u32 {
        match self.line_starts.binary_search(&offset) {
            Ok(idx) => idx as u32 + 1,
            Err(idx) => idx as u32,
        }
    }
}
