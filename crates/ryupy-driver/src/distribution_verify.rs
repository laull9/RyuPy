//! 离线复用发布布局的文件闭集与摘要验证，只报告完整性，不赋予清单身份或执行资格。
use anyhow::{Context, Result};
use serde::Serialize;
use std::path::Path;

/// 相对清单的内容完整性摘要；目标 RID 只是清单声明，并非实际二进制探测结果。
#[derive(Clone, Debug, PartialEq, Eq, Serialize)]
pub struct DistributionVerification {
    pub report_schema_version: u32,
    pub kind: &'static str,
    pub content_integrity_verified: bool,
    pub manifest_schema_version: u64,
    pub reported_target_rid: Option<String>,
    pub native_executable: String,
    pub artifact_count: usize,
    /// 所有清单产物的字节总和，不含清单自身与目录。
    pub total_bytes: u64,
    pub authenticity: &'static str,
    pub runtime_compatibility: &'static str,
    pub schema_semantics: &'static str,
}

/// 只读核对 AOT 发布目录，无需 SDK、Python 或启动原生程序。
///
/// 复用已有清单版本 1–8 的文件闭集、唯一 native 角色、路径与流式摘要规则；
/// 这不是完整 schema 语义认证、二进制格式/执行权限检查、签名认证或依赖兼容保证。
/// 额外空目录沿用底座的允许行为。调用者必须避免并发修改；这不是原子快照，
/// 不对恶意并发替换提供无竞态沙箱，也不保证验证返回之后的文件保持不变。
/// 不先 canonicalize 根目录，以免隐藏根自身的符号链接/reparse point。
pub fn verify_distribution(root: &Path) -> Result<DistributionVerification> {
    let manifest = crate::aot_layout::verify_distribution_manifest(root)?;
    // 只从刚完成底座校验的同一解析值提取已核字段，不重新读清单或输出任意 JSON。
    let artifacts = manifest["artifacts"]
        .as_array()
        .context("清单缺artifacts")?;
    let mut total_bytes = 0_u64;
    let mut native_executable = None;
    for artifact in artifacts {
        total_bytes = total_bytes
            .checked_add(artifact["size"].as_u64().context("产物缺size")?)
            .context("产物总大小溢出")?;
        if artifact["role"] == "native_executable" {
            native_executable = Some(
                artifact["relative_path"]
                    .as_str()
                    .context("产物缺路径")?
                    .to_owned(),
            );
        }
    }
    Ok(DistributionVerification {
        report_schema_version: 1,
        kind: "ryupy-distribution-integrity-verification",
        content_integrity_verified: true,
        manifest_schema_version: manifest["schema_version"]
            .as_u64()
            .context("清单缺schema_version")?,
        reported_target_rid: manifest["target"]["rid"].as_str().map(str::to_owned),
        native_executable: native_executable.context("清单缺native产物")?,
        artifact_count: artifacts.len(),
        total_bytes,
        authenticity: "not_checked",
        runtime_compatibility: "not_checked",
        schema_semantics: "not_checked",
    })
}
