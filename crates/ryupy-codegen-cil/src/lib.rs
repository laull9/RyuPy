//! MIR → .NET 程序集。
//!
//! 只依赖 `ryupy-mir`，对 Python 一无所知。分三步：
//! 1. [`types`]：`MirType` → CIL 类型签名；
//! 2. [`stackify`]：每个 MIR 函数体 → CIL 指令序列（局部变量一一映射为 CIL local，
//!    表达式按「压栈操作数 → 运算指令 → stloc」展开；基本块跳转先记录块号，最后回填为指令下标）；
//! 3. [`assembly`]：组装 `dotnetdll::Resolution`（程序集、类型、方法、对 `System.Runtime` /
//!    `RyuPy.Runtime` 的引用），序列化为 PE 字节。
//!
//! 参考：dotnetdll 自带测试 `tests/hello_world.rs` 与 `tests/common/mod.rs`
//! 演示了从零构造一个可执行程序集的全过程。

pub mod assembly;
mod callables;
mod entry;
mod literals;
mod modules;
mod refs;
pub mod runtimeconfig;
mod stackify;
mod stackify_operands;
mod stackify_refs;
mod stackify_rvalue;
mod types;

use ryupy_mir::LinkedProgram;

#[derive(Debug, Clone)]
pub struct EmitOptions {
    /// 程序集名，也是输出文件名（不带 `.dll`）。
    pub assembly_name: String,
}

#[derive(Debug, thiserror::Error)]
pub enum CodegenError {
    #[error("暂不支持的 MIR 构造：{0}")]
    Unsupported(String),
    #[error("dotnetdll 序列化失败：{0}")]
    Write(String),
}

/// 生成程序集字节。
pub fn emit_assembly(program: &LinkedProgram, opts: &EmitOptions) -> Result<Vec<u8>, CodegenError> {
    entry::emit(program, opts)
}

/// 生成可读的 CIL 文本（供 `ryupy dump --emit cil` 与快照测试）。
pub fn dump_cil(program: &LinkedProgram, opts: &EmitOptions) -> Result<String, CodegenError> {
    entry::dump(program, opts)
}
