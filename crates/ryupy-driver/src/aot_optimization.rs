//! NativeAOT显式优化偏好与同次SDK有效值收据；不修改交付manifest或默认为缓存资格。
use anyhow::{Context, Result, ensure};
use serde::{Deserialize, Serialize};
use std::io::{Read, Write};
use std::path::Path;

/// 原生优化取舍；Default不设置SDK属性，保留SDK及其环境决定。
#[derive(Debug, Default, Clone, Copy, PartialEq, Eq, Deserialize, Serialize)]
#[serde(rename_all = "lowercase")]
pub enum AotOptimization {
    #[default]
    Default,
    Size,
    Speed,
}
impl std::str::FromStr for AotOptimization {
    type Err = String;
    fn from_str(text: &str) -> std::result::Result<Self, Self::Err> {
        match text {
            "default" => Ok(Self::Default),
            "size" => Ok(Self::Size),
            "speed" => Ok(Self::Speed),
            _ => Err("AOT优化偏好只支持default/size/speed".into()),
        }
    }
}
impl AotOptimization {
    pub(crate) fn sdk_value(self) -> Option<&'static str> {
        match self {
            Self::Default => None,
            Self::Size => Some("Size"),
            Self::Speed => Some("Speed"),
        }
    }
    pub(crate) fn validate(self, aot: bool) -> Result<()> {
        ensure!(aot || self == Self::Default, "AOT优化偏好不能用于非AOT构建");
        Ok(())
    }
}

/// 只从枚举生成固定属性；路径由现有SDK字面量转义器处理，不接受任意MSBuild片段。
pub(crate) fn configure(
    project: String,
    preference: AotOptimization,
    escaped_record: &str,
) -> String {
    let property = preference.sdk_value().map_or(String::new(), |value|
        format!("  <PropertyGroup><OptimizationPreference>{value}</OptimizationPreference></PropertyGroup>\n"));
    let target = format!(
        r#"{property}  <Target Name="RyuPyRecordAotOptimization" BeforeTargets="Publish">
    <WriteLinesToFile File="{escaped_record}" Lines="effective=$(OptimizationPreference)" Overwrite="true" Encoding="UTF-8" />
  </Target>
</Project>"#
    );
    project.replace("</Project>", &target)
}

fn parse(text: &str, requested: AotOptimization) -> Result<String> {
    let text = text.strip_prefix('\u{feff}').unwrap_or(text);
    let line = text
        .strip_suffix("\r\n")
        .or_else(|| text.strip_suffix('\n'))
        .unwrap_or(text);
    let value = line
        .strip_prefix("effective=")
        .context("缺少SDK优化偏好有效值标记")?;
    ensure!(
        value.len() <= 128 && !value.chars().any(char::is_control),
        "SDK优化偏好必须为有界单行值"
    );
    if let Some(expected) = requested.sdk_value() {
        ensure!(
            value == expected,
            "SDK优化偏好与请求不符：请求{expected}，实际{value}"
        );
    }
    Ok(value.to_owned())
}

/// 记录与本次原生文件绑定；位于SDK工作根，不进入publish与可搬迁dist。
pub(crate) fn receipt(
    root: &Path,
    record: &Path,
    native: &Path,
    requested: AotOptimization,
) -> Result<()> {
    let metadata = crate::manifest_files::plain_metadata(record)?;
    ensure!(
        metadata.is_file() && metadata.len() <= 256,
        "SDK优化偏好记录不是有界普通文件"
    );
    let mut bytes = Vec::new();
    std::fs::File::open(record)?
        .take(257)
        .read_to_end(&mut bytes)?;
    ensure!(bytes.len() <= 256, "SDK优化偏好记录过大");
    let effective = parse(
        std::str::from_utf8(&bytes).context("SDK优化偏好记录不是UTF8")?,
        requested,
    )?;
    let digest = crate::manifest_files::digest_file(native, "native", "native_executable")?.sha256;
    let value = serde_json::json!({"schema_version":1,"requested":requested,
        "sdk_property":"OptimizationPreference","effective":effective,"native_sha256":digest,
        "scope":"同次SDK有效属性与原生产物；default不设置属性，不代表缓存资格或性能收益"});
    let mut output = std::fs::OpenOptions::new()
        .create_new(true)
        .write(true)
        .open(root.join("aot-optimization.json"))?;
    output.write_all(&serde_json::to_vec_pretty(&value)?)?;
    output.sync_all()?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn strict_request_and_effective_values() {
        for (name, value) in [
            ("default", AotOptimization::Default),
            ("size", AotOptimization::Size),
            ("speed", AotOptimization::Speed),
        ] {
            assert_eq!(name.parse::<AotOptimization>().unwrap(), value);
            assert_eq!(
                serde_json::from_str::<AotOptimization>(&format!("\"{name}\"")).unwrap(),
                value
            );
        }
        for bad in ["", "Size", "fast", "size</PropertyGroup>"] {
            assert!(bad.parse::<AotOptimization>().is_err());
        }
        assert_eq!(
            parse("\u{feff}effective=Size\r\n", AotOptimization::Size).unwrap(),
            "Size"
        );
        assert_eq!(parse("effective=\n", AotOptimization::Default).unwrap(), "");
        assert_eq!(
            parse("effective=Speed\n", AotOptimization::Default).unwrap(),
            "Speed"
        );
        for bad in [
            "",
            "Size",
            "effective=Speed",
            "effective=Size\nextra",
            "effective=Size\n\n",
        ] {
            assert!(parse(bad, AotOptimization::Size).is_err());
        }
        assert!(AotOptimization::Size.validate(false).is_err());
    }
    #[test]
    fn default_omits_property_but_records_actual_sdk_value() {
        let template = "<Project><PropertyGroup><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup></Project>";
        for choice in [
            AotOptimization::Default,
            AotOptimization::Size,
            AotOptimization::Speed,
        ] {
            let text = configure(template.into(), choice, "/tmp/sdk.optimization");
            assert_eq!(
                text.contains("<OptimizationPreference>"),
                choice != AotOptimization::Default
            );
            assert!(text.contains("effective=$(OptimizationPreference)"));
            assert!(text.contains("<TreatWarningsAsErrors>true</TreatWarningsAsErrors>"));
        }
    }
}

#[cfg(test)]
#[path = "aot_optimization_tests.rs"]
mod integration_tests;
