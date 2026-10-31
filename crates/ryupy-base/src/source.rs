//! 源文件登记表。
//!
//! 所有位置信息统一用「文件 + 字节偏移区间」表示，与 ruff 的 `TextRange` 一致。
//! 注意：ariadne 默认按字符偏移计算，渲染时必须切换到字节偏移（见 `diagnostic.rs`）。

use la_arena::{Arena, Idx};
use ruff_text_size::TextRange;

pub type FileId = Idx<SourceFile>;

#[derive(Debug)]
pub struct SourceFile {
    /// 显示用的路径（诊断里打印的那个）。
    pub name: String,
    pub text: String,
}

/// 一次编译会话里出现过的所有源文件。
#[derive(Debug, Default)]
pub struct SourceDb {
    files: Arena<SourceFile>,
}

impl SourceDb {
    pub fn add(&mut self, name: impl Into<String>, text: impl Into<String>) -> FileId {
        self.files.alloc(SourceFile {
            name: name.into(),
            text: text.into(),
        })
    }

    pub fn get(&self, id: FileId) -> &SourceFile {
        &self.files[id]
    }

    pub fn iter(&self) -> impl Iterator<Item = (FileId, &SourceFile)> {
        self.files.iter()
    }
}

/// 跨文件的源码位置。
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub struct Span {
    pub file: FileId,
    pub range: TextRange,
}

impl Span {
    pub fn new(file: FileId, range: TextRange) -> Self {
        Self { file, range }
    }
}
