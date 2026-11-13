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
    DelegateReference, EmissionArtifact, EmissionReport, FunctionEmission, MethodDescriptor,
    ModuleEmission,
};
mod candidate_sources;
mod class_cell_refs;
#[cfg(test)]
mod class_cell_tests;
mod distribution_metadata;
#[cfg(test)]
mod dynamic_origin_tests;
mod dynamic_value_refs;
#[cfg(test)]
mod emission_report_tests;
mod function_refs;
mod generator_refs;
mod instance_refs;
mod lexical_super;
#[cfg(test)]
mod lexical_super_tests;
mod literals;
mod locals_refs;
mod modules;
mod origin_refs;
mod owned_function_refs;
mod provider_refs;
mod refs;
pub mod runtimeconfig;
mod source_provenance;
pub use source_provenance::SourceModules;
#[cfg(test)]
mod source_provenance_tests;
mod stackify;
mod stackify_operands;
mod stackify_refs;
mod stackify_runtime;
mod stackify_rvalue;
mod system_refs;
mod trace_product_refs;
mod types;
mod unpack_refs;

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

/// 兼容既有手工MIR的发射入口；真实源码driver必须选择emit_source_assembly。
pub fn emit_assembly(program: &LinkedProgram, opts: &EmitOptions) -> Result<Vec<u8>, CodegenError> {
    entry::emit(program, opts)
}

/// 兼容手工MIR：返回同一次Resolution的字节/报告；源码必须选择source专用入口。
pub fn emit_assembly_with_report(
    program: &LinkedProgram,
    opts: &EmitOptions,
) -> Result<EmissionArtifact, CodegenError> {
    ryupy_mir::generator_product::validate_emission(program).map_err(CodegenError::Unsupported)?;
    crate::source_provenance::validate(program)?;
    let (res, _, report) = assembly::build_resolution_with_report(program, opts);
    crate::distribution_metadata::validate_heap(program, &res)?;
    let bytes = res
        .write(dotnetdll::prelude::WriteOptions {
            is_32_bit: true,
            is_executable: false,
        })
        .map_err(|error| CodegenError::Write(error.to_string()))?;
    Ok(EmissionArtifact { bytes, report })
}

/// 兼容手工MIR的CIL文本出口；真实源码dump必须选择dump_source_cil。
pub fn dump_cil(program: &LinkedProgram, opts: &EmitOptions) -> Result<String, CodegenError> {
    entry::dump(program, opts)
}

/// 源码准入由调用链显式要求，不能因整条来源接线被擦掉而自动变为旧兼容输入。
pub fn emit_source_assembly(
    program: &LinkedProgram,
    opts: &EmitOptions,
) -> Result<Vec<u8>, CodegenError> {
    source_provenance::require(program)?;
    emit_assembly(program, opts)
}

/// 与普通源码出口同一准入；复用原同次Resolution报告，不建立另一发射流程。
pub fn emit_source_assembly_with_report(
    program: &LinkedProgram,
    opts: &EmitOptions,
) -> Result<EmissionArtifact, CodegenError> {
    source_provenance::require(program)?;
    emit_assembly_with_report(program, opts)
}

/// MIR/CIL转储也属于真实源码生产链，不能把缺来源的损坏产物显示成可用结果。
pub fn dump_source_cil(
    program: &LinkedProgram,
    opts: &EmitOptions,
) -> Result<String, CodegenError> {
    source_provenance::require(program)?;
    dump_cil(program, opts)
}

#[cfg(test)]
mod entry_tests;

#[cfg(test)]
mod exception_table_tests;

#[cfg(test)]
mod exception_constructor_tests;

mod builtin_lookup_refs;
#[cfg(test)]
mod builtin_lookup_tests;

mod class_refs;

mod exception_refs;
mod module_binding_refs;
mod module_deletion_refs;
#[cfg(test)]
mod module_deletion_tests;
mod program_refs;

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
#[cfg(test)]
mod module_resource_tests;

// 编译器Rust实验接口：显式记录，不是Python/CLI公开声明协议。
mod fixed_binding_emit;
mod fixed_binding_refs;
#[cfg(test)]
mod fixed_binding_tests;
#[doc(hidden)]
pub mod fixed_bindings;
mod native_abi_emit;
#[cfg(test)]
mod native_abi_tests;
pub mod native_abi_types;

#[cfg(test)]
mod module_capture_tests;

mod fixed_binding_deployment;
#[cfg(test)]
mod fixed_binding_deployment_tests;

#[cfg(test)]
mod ordinary_class_finalize_tests;

#[cfg(test)]
mod bad_zipfile_tests;

#[cfg(test)]
mod getopt_exception_tests;
#[cfg(test)]
mod optparse_exception_tests;

#[cfg(test)]
mod dynamic_except_tests;

#[cfg(test)]
mod function_locals_tests;

#[cfg(test)]
mod function_definition_tests;

#[cfg(test)]
mod instance_tests;

#[cfg(test)]
mod owned_function_tests;

#[cfg(test)]
mod instance_pe_tests;

#[cfg(test)]
mod instance_pe_roundtrip_tests;

#[cfg(test)]
mod explicit_super_pe_tests;

#[cfg(test)]
mod lexical_class_abi_tests;
