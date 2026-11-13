//! 声明式 C 绑定的显式发射记录；兼容旧版两参数入口，v3 使用 MIR 静态合同。
use dotnetdll::prelude::*;
use ryupy_mir::{ExportGuard, FnId, GlobalId, LinkedProgram, MirType, ModuleId};

use crate::{CodegenError, EmitOptions};

/// 编译器内部实验入口的数据；三个ID必须对应同一真实模块、初始化函数和动态导出。
#[derive(Debug, Clone)]
pub struct FixedI32Binding {
    pub module: ModuleId,
    pub initializer: FnId,
    pub export: GlobalId,
    pub module_ref: String,
    pub entry_point: String,
    /// None 保留 v1/v2 固定签名；Some 为已验证 v3 静态合同。
    pub signature: Option<ryupy_mir::native_abi::AbiSignature>,
}

/// 有界多导出共享单个顶层模块、库和初始化器。所有检查发生在构造Resolution之前。
pub(crate) fn validate(
    program: &LinkedProgram,
    bindings: &[FixedI32Binding],
) -> Result<(), CodegenError> {
    let bad = |text: &str| CodegenError::Unsupported(format!("fixed C binding: {text}"));
    if bindings.len() > 64 {
        return Err(bad("at most 64 exports are supported"));
    }
    let mut exports = std::collections::HashSet::new();
    let mut symbols = std::collections::HashMap::new();
    for b in bindings {
        let first = &bindings[0];
        if b.module != first.module
            || b.initializer != first.initializer
            || b.module_ref != first.module_ref
            || !exports.insert(b.export)
        {
            return Err(bad("expected distinct exports from one module and library"));
        }
        if let Some(previous) = symbols.insert(&b.entry_point, &b.signature) {
            let compatible = match (previous, &b.signature) {
                (None, None) => true,
                (Some(left), Some(right)) => left.same_abi(right),
                _ => false,
            };
            if !compatible {
                return Err(bad("one symbol requires one consistent ABI signature"));
            }
        }
    }
    for binding in bindings {
        if let Some(signature) = &binding.signature {
            signature.validate().map_err(|message| bad(&message))?;
        }
        let module = program
            .modules
            .iter()
            .find_map(|(id, value)| (id == binding.module).then_some(value))
            .ok_or_else(|| bad("missing module"))?;
        let init = program
            .functions
            .iter()
            .find_map(|(id, value)| (id == binding.initializer).then_some(value))
            .ok_or_else(|| bad("missing initializer"))?;
        let global = program
            .globals
            .iter()
            .find_map(|(id, value)| (id == binding.export).then_some(value))
            .ok_or_else(|| bad("missing export global"))?;
        let export = module
            .exports
            .iter()
            .find(|e| e.value == binding.export)
            .ok_or_else(|| bad("missing module export"))?;
        let names: std::collections::HashSet<_> = module.exports.iter().map(|e| &e.name).collect();
        let identifier = |value: &str| {
            !value.is_empty()
                && value.bytes().enumerate().all(|(i, c)| {
                    c == b'_' || c.is_ascii_alphabetic() || (i > 0 && c.is_ascii_digit())
                })
        };
        if !identifier(&module.name) || module.name == "__main__" || !module.package.is_empty() {
            return Err(bad("expected a non-entry top-level module"));
        }
        if !identifier(&binding.module_ref) || !identifier(&binding.entry_point) {
            return Err(bad("library and symbol must be fixed ASCII identifiers"));
        }
        if module.init != binding.initializer
            || program.entry == binding.module
            || global.ty != MirType::Dynamic
            || module.exports.len() != bindings.len()
            || names.len() != bindings.len()
            || !identifier(&export.name)
            || matches!(
                export.name.as_str(),
                "__file__" | "__name__" | "__package__"
            )
            || !matches!(export.guard, ExportGuard::ValueSentinel)
            || !module.protected_exports.is_empty()
            || !module.file.is_empty()
            || !matches!(module.location, ryupy_mir::ModuleLocation::LegacyDiagnostic)
        {
            return Err(bad("module/export protocol mismatch"));
        }
        if !init.params.is_empty()
            || init.ret != MirType::Unit
            || !init.param_meta.is_empty()
            || init.env.is_some()
            || init.function_global.is_some()
            || init.signature_global.is_some()
            || init.defaults_global.is_some()
            || init.specialization_identity_global.is_some()
        {
            return Err(bad("initializer cannot be a Python function"));
        }
        // 专用initializer只能替换空的Return占位，不能静默丢掉任意用户MIR。
        if !init.body.locals.is_empty()
            || !init.body.regions.is_empty()
            || init.body.blocks.len() != 1
            || !init.body.blocks.iter().any(|(id, block)| {
                id == init.body.entry
                    && block.stmts.is_empty()
                    && matches!(block.terminator, ryupy_mir::Terminator::Return(None))
            })
        {
            return Err(bad("initializer must be an empty return placeholder"));
        }
        if program.functions.iter().any(|(id, function)| {
            id != binding.initializer
                && [
                    function.function_global,
                    function.defaults_global,
                    function.signature_global,
                    function.specialization_identity_global,
                ]
                .contains(&Some(binding.export))
        }) {
            return Err(bad("export global is reserved by another function"));
        }
        for (id, other) in program.modules.iter() {
            if id != binding.module
                && (other.name == module.name
                    || other.name.starts_with(&format!("{}.", module.name))
                    || module.name.starts_with(&format!("{}.", other.name)))
            {
                return Err(bad("source module name conflict"));
            }
            if id != binding.module
                && (other.init == binding.initializer
                    || other.exports.iter().any(|e| e.value == binding.export))
            {
                return Err(bad("initializer/export belongs to another module"));
            }
        }
    }
    Ok(())
}

/// 仅显式传入记录才发射绑定；旧emit/dump入口没有记录，不建立额外引用。
pub fn emit(
    program: &LinkedProgram,
    opts: &EmitOptions,
    bindings: &[FixedI32Binding],
) -> Result<Vec<u8>, CodegenError> {
    ryupy_mir::generator_product::validate_emission(program).map_err(CodegenError::Unsupported)?;
    crate::source_provenance::validate(program)?;
    if program.distribution_metadata.is_some() {
        return Err(CodegenError::Unsupported(
            "分发快照不支持外部原生绑定宿主".into(),
        ));
    }
    if program.trace_product.is_some() {
        return Err(CodegenError::Unsupported(
            "trace V1不接受外部原生绑定".into(),
        ));
    }
    validate(program, bindings)?;
    let (res, _) = crate::assembly::build_resolution_with_bindings(program, opts, bindings);
    res.write(WriteOptions {
        is_32_bit: true,
        is_executable: false,
    })
    .map_err(|error| CodegenError::Write(error.to_string()))
}

/// 构建层选择的固定部署相对名，不含宿主输入路径或JIT/AOT能力猜测。
#[derive(Debug, Clone)]
pub struct BindingDeployment {
    pub relative_file: String,
}

/// 同次Resolution的显式绑定身份与实际PInvoke元数据；不承诺原生依赖闭包。
#[derive(Debug, Clone, PartialEq, Eq)]
pub struct BindingEmission {
    pub binding_id: String,
    pub module_id: u32,
    pub initializer_id: u32,
    pub export_id: u32,
    pub body: crate::MethodDescriptor,
    pub native: crate::MethodDescriptor,
    pub library: String,
    pub symbol: String,
    pub calling_convention: String,
    pub character_set: String,
    pub no_mangle: bool,
    pub supports_last_error: bool,
    pub preserve_sig: bool,
    pub is_static: bool,
    pub has_body: bool,
}

#[derive(Debug)]
pub struct BindingEmissionArtifact {
    pub bytes: Vec<u8>,
    pub report: crate::EmissionReport,
    pub bindings: Vec<BindingEmission>,
}

/// B构建入口先验证全部记录，再一次发射并从该Resolution收集报告。
/// 旧A与无绑定入口不建立resolver引用、不支付报告扫描成本。
pub fn emit_with_report(
    program: &LinkedProgram,
    opts: &EmitOptions,
    bindings: &[FixedI32Binding],
    deployment: &BindingDeployment,
) -> Result<BindingEmissionArtifact, CodegenError> {
    ryupy_mir::generator_product::validate_emission(program).map_err(CodegenError::Unsupported)?;
    crate::source_provenance::validate(program)?;
    if program.distribution_metadata.is_some() {
        return Err(CodegenError::Unsupported(
            "分发快照不支持外部原生绑定宿主".into(),
        ));
    }
    if program.trace_product.is_some() {
        return Err(CodegenError::Unsupported(
            "trace V1不接受外部原生绑定".into(),
        ));
    }
    validate(program, bindings)?;
    if bindings.is_empty()
        || !matches!(
            deployment.relative_file.as_str(),
            "_internal/native/v1/b0/library.so"
                | "_internal/native/v1/b0/library.dylib"
                | "_internal/native/v1/b0/library.dll"
        )
    {
        return Err(CodegenError::Unsupported(
            "fixed C binding: expected exports and a controlled deployment path".into(),
        ));
    }
    let (res, report, records) =
        crate::assembly::build_binding_report(program, opts, bindings, deployment);
    let bytes = res
        .write(WriteOptions {
            is_32_bit: true,
            is_executable: false,
        })
        .map_err(|error| CodegenError::Write(error.to_string()))?;
    Ok(BindingEmissionArtifact {
        bytes,
        report,
        bindings: records,
    })
}

/// 真实源码加一个已验证原生占位模块；凭declare前来源集合防止把旧源码unit冒作例外。
pub fn emit_source_with_report(
    program: &LinkedProgram,
    opts: &EmitOptions,
    bindings: &[FixedI32Binding],
    deployment: &BindingDeployment,
    sources: &crate::SourceModules,
) -> Result<BindingEmissionArtifact, CodegenError> {
    validate(program, bindings)?;
    sources.validate_extension(program, bindings)?;
    emit_with_report(program, opts, bindings, deployment)
}
