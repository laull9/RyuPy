//! AOT 清单的版本化 JSON：绑定当前编译文本/托管字节及同次 codegen 报告，不宣称资源或动态依赖闭包。
use crate::manifest_files::{self, FileDigest};
use anyhow::{Result, ensure};
use ryupy_codegen_cil::{EmissionReport, MethodDescriptor};
use serde::Serialize;
use serde_json::{Value, json};
use std::path::Path;

#[derive(Debug, Clone, Serialize)]
pub(crate) struct SourceSummary {
    pub module_id: u32,
    pub module_name: String,
    pub package: String,
    pub logical_source: String,
    pub compiled_text_sha256: String,
}

/// 仅从 compile_linked 的当前文本建立；不为清单再读磁盘源文件。
impl SourceSummary {
    pub(crate) fn new(id: u32, source: &crate::modules::SourceModule) -> Self {
        Self {
            module_id: id,
            module_name: source.name.clone(),
            package: source.package.clone(),
            logical_source: source.logical_source.clone(),
            compiled_text_sha256: manifest_files::hash_bytes(source.text.as_bytes()),
        }
    }
}

pub(crate) struct ManifestContext {
    pub report: EmissionReport,
    pub sources: Vec<SourceSummary>,
    pub entry_module_id: u32,
    pub managed_sha256: String,
    pub managed_size: u64,
    pub options: Value,
}

impl ManifestContext {
    pub(crate) fn new(
        report: EmissionReport,
        sources: Vec<SourceSummary>,
        entry_module_id: u32,
        managed: &[u8],
        session: &crate::Session,
    ) -> Result<Self> {
        let context = Self {
            report,
            sources,
            entry_module_id,
            managed_sha256: manifest_files::hash_bytes(managed),
            managed_size: managed.len() as u64,
            options: json!({"experimental_direct_accumulator": session.experimental_direct_accumulator,
                "experimental_foreach_accumulator": session.experimental_foreach_accumulator,
                "experimental_leaf_inline": session.experimental_leaf_inline,
                "diagnostic_scalar_slp": session.diagnostic_scalar_slp,
                "experimental_packed_slp": session.experimental_packed_slp,
                "instance_modules": session.instance_modules,
                "module_environment_policy": session.module_environment_policy.identity()}),
        };
        context.validate()?;
        Ok(context)
    }

    fn validate(&self) -> Result<()> {
        ensure!(self.report.schema_version == 1, "不支持的 codegen 报告版本");
        ensure!(
            !self.sources.is_empty() && self.report.modules.len() == self.sources.len(),
            "报告与实际源模块数量不符"
        );
        ensure!(
            (self.entry_module_id as usize) < self.sources.len(),
            "清单入口模块不存在"
        );
        check_method(&self.report.entry)?;
        for (index, source) in self.sources.iter().enumerate() {
            ensure!(source.module_id as usize == index, "源模块编号不连续");
            manifest_files::safe_relative(&source.logical_source)?;
            ensure!(
                source.logical_source.starts_with("entry/")
                    || source.logical_source.starts_with("modules/"),
                "源逻辑身份前缀无效"
            );
            let module = self
                .report
                .modules
                .iter()
                .find(|m| m.module_id == source.module_id)
                .ok_or_else(|| anyhow::anyhow!("codegen 报告缺少实际源模块"))?;
            ensure!(
                module.module_name == source.module_name,
                "codegen 模块身份与编译文本不符"
            );
            for method in std::iter::once(&module.init)
                .chain(module.getter.iter())
                .chain(module.setter.iter())
            {
                check_method(method)?;
            }
        }
        for reference in &self.report.delegate_references {
            check_method(&reference.source)?;
            if let Some(target) = &reference.target {
                check_method(target)?;
            }
            ensure!(
                reference
                    .module_id
                    .is_none_or(|id| (id as usize) < self.sources.len()),
                "委托引用模块编号越界"
            );
            ensure!(!reference.role.is_empty(), "委托引用没有角色");
        }
        Ok(())
    }

    pub(crate) fn verify_managed_input(&self, input: &FileDigest) -> Result<()> {
        ensure!(
            input.size == self.managed_size && input.sha256 == self.managed_sha256,
            "AOT 托管输入已不等于本次 codegen 字节"
        );
        Ok(())
    }

    pub(crate) fn render(
        &self,
        rid: &str,
        native_format: &str,
        architecture: &str,
        sdk_version: &str,
        artifacts: &[FileDigest],
        build_inputs: &[FileDigest],
    ) -> Result<Vec<u8>> {
        self.validate()?;
        ensure!(
            artifacts
                .iter()
                .filter(|item| item.role == "native_executable")
                .count()
                == 1,
            "清单必须恰有一个实际原生产物"
        );
        ensure!(
            build_inputs.len() == 2
                && build_inputs[0].role == "compiled_managed_input"
                && build_inputs[1].role == "runtime_managed_input",
            "清单缺少本次两份构建输入"
        );
        self.verify_managed_input(&build_inputs[0])?;
        for input in artifacts.iter().chain(build_inputs) {
            manifest_files::safe_relative(&input.relative_path)?;
            ensure!(
                input.sha256.len() == 64
                    && input
                        .sha256
                        .bytes()
                        .all(|b| b.is_ascii_hexdigit() && !b.is_ascii_uppercase()),
                "清单文件摘要无效"
            );
        }
        let modules: Vec<Value> = self.report.modules.iter().map(|module| json!({
            "id":module.module_id,"name":module.module_name,"init":method(&module.init),
            "environment_reason":module.environment_reason,
            "getter":module.getter.as_ref().map(method),"setter":module.setter.as_ref().map(method),"exports":module.exports
        })).collect();
        let references: Vec<Value> = self.report.delegate_references.iter().map(|reference| json!({
            "site":{"source_method":method(&reference.source),"instruction":reference.instruction},
            "target":reference.target.as_ref().map(method),"target_display":reference.target_display,
            "role":reference.role,"module_id":reference.module_id,
            "function_id":reference.function_id,"class_id":reference.class_id
        })).collect();
        let value = json!({
            "schema_version":1,"kind":"ryupy-native-aot-artifact",
            "compiler":{"package_version":env!("CARGO_PKG_VERSION"),"build_revision":Value::Null,
                "codegen_report_version":self.report.schema_version,"options":self.options},
            "target":{"rid":rid,"native_format":native_format,"architecture":architecture},
            "toolchain":{"sdk_version":sdk_version,
                "target_framework_major":ryupy_codegen_cil::runtimeconfig::TARGET_FRAMEWORK_MAJOR},
            "entry":{"module_id":self.entry_module_id,"managed_entry":method(&self.report.entry)},
            "sources":self.sources,"modules":modules,"emitted_delegate_references":references,
            "build_inputs":build_inputs,"artifacts":artifacts,
            "scope":{"dynamic_dependency_closure_complete":false,"resources_included":false,
                "native_retention_proven":false,"manifest_self_hash_included":false}
        });
        let mut bytes = serde_json::to_vec_pretty(&value)?;
        bytes.push(b'\n');
        Ok(bytes)
    }
}

fn check_method(method: &MethodDescriptor) -> Result<()> {
    ensure!(
        !method.owner.is_empty() && !method.name.is_empty() && !method.signature.is_empty(),
        "实际发射方法描述不完整"
    );
    Ok(())
}

fn method(method: &MethodDescriptor) -> Value {
    json!({"owner":method.owner,"name":method.name,"signature":method.signature})
}

/// SDK 前后核同两份输入，禁止成功 SDK 消费期间被替换后再冒称当前输入摘要。
pub(crate) fn input_digests(dll: &Path, runtime: &Path) -> Result<Vec<FileDigest>> {
    Ok(vec![
        manifest_files::digest_file(dll, "managed-entry.dll", "compiled_managed_input")?,
        manifest_files::digest_file(runtime, "RyuPy.Runtime.dll", "runtime_managed_input")?,
    ])
}

#[cfg(test)]
#[path = "manifest_tests.rs"]
mod tests;
