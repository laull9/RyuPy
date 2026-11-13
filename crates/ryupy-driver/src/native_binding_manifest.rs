//! schema3 只用于显式绑定：真实编译源码与生成模块来源分列，使用同次 Resolution 的实际 PInvoke 报告。
use crate::{
    manifest::SourceSummary,
    manifest_files::{self, FileDigest},
    native_binding::{BindingPlan, SIGNATURE},
};
use anyhow::{Result, ensure};
use ryupy_codegen_cil::{EmissionReport, MethodDescriptor, fixed_bindings::BindingEmission};
use serde_json::{Value, json};
pub(crate) struct BindingManifest {
    pub(crate) aot_optimization: crate::AotOptimization,
    report: EmissionReport,
    sources: Vec<SourceSummary>,
    binding: BindingEmission,
    entry: u32,
    managed_sha256: String,
    managed_size: u64,
    options: Value,
}
impl BindingManifest {
    pub(crate) fn new(
        report: EmissionReport,
        sources: Vec<SourceSummary>,
        mut bindings: Vec<BindingEmission>,
        entry: u32,
        bytes: &[u8],
        session: &crate::Session,
        plan: &BindingPlan,
    ) -> Result<Self> {
        ensure!(bindings.len() == 1, "绑定报告必须恰有一个生成模块");
        let binding = bindings.remove(0);
        crate::manifest::validate_report_version(report.schema_version)?;
        ensure!(
            report.modules.len() == sources.len() + 1 && entry < (sources.len() as u32),
            "绑定报告/源码数量或入口不符"
        );
        ensure!(
            binding.binding_id == "b0"
                && binding.module_id == sources.len() as u32
                && binding.library == plan.library
                && binding.symbol == plan.symbol,
            "实际PInvoke与声明身份不符"
        );
        ensure!(
            binding.native.signature == "static long (int, int)"
                && binding.body.signature == "static object (object[])"
                && binding.calling_convention == "Cdecl"
                && binding.character_set == "NotSpecified"
                && binding.no_mangle
                && binding.preserve_sig
                && binding.is_static
                && !binding.has_body
                && !binding.supports_last_error,
            "实际PInvoke签名或标志不符"
        );
        for (index, s) in sources.iter().enumerate() {
            ensure!(s.module_id as usize == index, "实际源码编号不连续");
            manifest_files::safe_relative(&s.logical_source)?;
            ensure!(
                report
                    .modules
                    .iter()
                    .filter(|m| m.module_id == s.module_id && m.module_name == s.module_name)
                    .count()
                    == 1,
                "实际源码与报告不符"
            );
        }
        ensure!(
            report
                .modules
                .iter()
                .any(|m| m.module_id == binding.module_id
                    && m.module_name == plan.module
                    && m.exports == vec![plan.export.clone()]),
            "生成模块报告不符"
        );
        ensure!(
            report
                .delegate_references
                .iter()
                .any(|r| r.role == "declared_native_body"
                    && r.module_id == Some(binding.module_id)
                    && r.target.as_ref() == Some(&binding.body)),
            "报告缺少实际wrapper委托引用"
        );
        Ok(Self {
            aot_optimization: session.aot_optimization,
            report,
            sources,
            binding,
            entry,
            managed_sha256: manifest_files::hash_bytes(bytes),
            managed_size: bytes.len() as u64,
            options: json!({"experimental_direct_accumulator":session.experimental_direct_accumulator,"experimental_foreach_accumulator":session.experimental_foreach_accumulator,"experimental_leaf_inline":session.experimental_leaf_inline,"diagnostic_scalar_slp":session.diagnostic_scalar_slp,"experimental_packed_slp":session.experimental_packed_slp,"instance_modules":session.instance_modules,"module_environment_policy":session.module_environment_policy.identity()}),
        })
    }
    pub(crate) fn verify_managed_input(&self, input: &FileDigest) -> Result<()> {
        ensure!(
            input.size == self.managed_size && input.sha256 == self.managed_sha256,
            "绑定托管输入不等于本次发射字节"
        );
        Ok(())
    }
    pub(crate) fn render(
        &self,
        plan: &BindingPlan,
        sdk: Option<&str>,
        artifacts: &[FileDigest],
        inputs: &[FileDigest],
    ) -> Result<Vec<u8>> {
        ensure!(
            inputs.len() == 3
                && inputs[0].role == "compiled_managed_input"
                && inputs[1].role == "runtime_managed_input"
                && inputs[2] == plan.input.digest,
            "绑定构建输入不完整"
        );
        self.verify_managed_input(&inputs[0])?;
        let role = if plan.aot {
            "native_executable"
        } else {
            "managed_entry"
        };
        ensure!(
            artifacts.iter().filter(|a| a.role == role).count() == 1,
            "绑定部署入口数量不符"
        );
        ensure!(plan.aot == sdk.is_some(), "SDK记录与模式不一致");
        if plan.aot {
            ensure!(
                !artifacts.iter().any(|a| a.role == "native_shared"),
                "静态AOT不能靠shared补救"
            );
        } else {
            ensure!(
                artifacts.iter().any(|a| a.role == "native_shared"
                    && a.relative_path == plan.platform.relative_file()
                    && a.sha256 == plan.input.digest.sha256
                    && a.size == plan.input.digest.size),
                "JIT缺少冻结shared"
            );
        }
        let modules:Vec<Value>=self.report.modules.iter().map(|m|json!({"id":m.module_id,"name":m.module_name,"environment_reason":m.environment_reason,"init":method(&m.init),"getter":m.getter.as_ref().map(method),"setter":m.setter.as_ref().map(method),"exports":m.exports,"origin":if m.module_id==self.binding.module_id{json!({"kind":"declared_native_binding","binding_id":"b0","declaration_sha256":plan.declaration_sha256})}else{json!({"kind":"compiled_source","source_module_id":m.module_id})}})).collect();
        let refs:Vec<Value>=self.report.delegate_references.iter().map(|r|json!({"site":{"source_method":method(&r.source),"instruction":r.instruction},"target":r.target.as_ref().map(method),"target_display":r.target_display,"role":r.role,"module_id":r.module_id,"function_id":r.function_id,"class_id":r.class_id})).collect();
        let b = &self.binding;
        let value = json!({"schema_version":3,"kind":if plan.aot{"ryupy-native-aot-artifact"}else{"ryupy-managed-binding-artifact"},
 "compiler":{"package_version":env!("CARGO_PKG_VERSION"),"build_revision":Value::Null,"codegen_report_version":self.report.schema_version,"options":self.options},"target":{"rid":plan.platform.rid(),"native_format":plan.platform.native_format(),"architecture":plan.platform.architecture()},"toolchain":{"sdk_version":sdk,"target_framework_major":ryupy_codegen_cil::runtimeconfig::TARGET_FRAMEWORK_MAJOR},
 "entry":{"module_id":self.entry,"managed_entry":method(&self.report.entry)},"sources":self.sources,"modules":modules,"emitted_delegate_references":refs,
 "native_bindings":[{"binding_id":"b0","module_id":b.module_id,"module":plan.module,"export":plan.export,"signature":SIGNATURE,"library":b.library,"symbol":b.symbol,"declaration_sha256":plan.declaration_sha256,"linkage":if plan.aot{"aot_static"}else{"jit_shared"},"relative_file":if plan.aot{None}else{Some(plan.platform.relative_file())},"initializer_id":b.initializer_id,"export_id":b.export_id,"body":method(&b.body),"native":method(&b.native),"pinvoke":{"calling_convention":b.calling_convention,"character_set":b.character_set,"no_mangle":b.no_mangle,"supports_last_error":b.supports_last_error,"preserve_sig":b.preserve_sig,"is_static":b.is_static,"has_body":b.has_body}}],
 "build_inputs":inputs,"artifacts":artifacts,"scope":{"dynamic_dependency_closure_complete":false,"resources_included":false,"native_retention_proven":false,"manifest_self_hash_included":false}});
        let mut bytes = serde_json::to_vec_pretty(&value)?;
        bytes.push(b'\n');
        Ok(bytes)
    }
}
fn method(m: &MethodDescriptor) -> Value {
    json!({"owner":m.owner,"name":m.name,"signature":m.signature})
}
