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

mod accumulator_refs;
pub mod assembly;
mod assembly_refs;
#[cfg(test)]
mod assembly_refs_tests;
mod callables;
mod emission_report;
mod entry;
pub use emission_report::{
    DelegateReference, EmissionArtifact, EmissionReport, MethodDescriptor, ModuleEmission,
};
#[cfg(test)]
mod emission_report_tests;
mod function_refs;
mod generator_refs;
mod literals;
mod modules;
mod refs;
pub mod runtimeconfig;
mod stackify;
mod stackify_operands;
mod stackify_refs;
mod stackify_runtime;
mod stackify_rvalue;
mod system_refs;
mod types;

use ryupy_mir::LinkedProgram;

#[derive(Debug, Clone)]
pub struct EmitOptions {
    /// 程序集名，也是输出文件名（不带 `.dll`）。
    pub assembly_name: String,
    /// 入口脚本原始路径，独立于 traceback 的短显示名；未提供时使用模块显示名。
    pub entry_source_path: Option<String>,
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

/// 返回同一次Resolution的字节和实际委托引用报告；不新增发射根或运行时注册。
pub fn emit_assembly_with_report(
    program: &LinkedProgram,
    opts: &EmitOptions,
) -> Result<EmissionArtifact, CodegenError> {
    let (res, _, report) = assembly::build_resolution_with_report(program, opts);
    let bytes = res
        .write(dotnetdll::prelude::WriteOptions {
            is_32_bit: true,
            is_executable: false,
        })
        .map_err(|error| CodegenError::Write(error.to_string()))?;
    Ok(EmissionArtifact { bytes, report })
}

/// 生成可读的 CIL 文本（供 `ryupy dump --emit cil` 与快照测试）。
pub fn dump_cil(program: &LinkedProgram, opts: &EmitOptions) -> Result<String, CodegenError> {
    entry::dump(program, opts)
}

#[cfg(test)]
mod entry_tests;

#[cfg(test)]
mod exception_table_tests;

#[cfg(test)]
mod exception_constructor_tests;

mod class_refs;

mod module_binding_refs;

mod typed_dict_refs;

#[cfg(test)]
mod exact_i64_tests;

#[cfg(test)]
mod function_identity_tests;

#[cfg(test)]
mod exact_f64_tests;

mod math_refs;
mod numeric_list_refs;
#[cfg(test)]
mod numeric_list_tests;

mod vector_refs;
#[cfg(test)]
mod vector_tests;

#[cfg(test)]
mod module_location_tests;
