//! 生成 `<name>.runtimeconfig.json`。
//!
//! `dotnet <name>.dll` 启动时靠它确定使用哪个共享框架；缺失会报
//! “The application to execute does not exist” 或找不到框架。

/// 目标框架主版本。与 `runtime/RyuPy.Runtime/RyuPy.Runtime.csproj` 的 `TargetFramework` 保持一致。
pub const TARGET_FRAMEWORK_MAJOR: u32 = 10;

pub fn render() -> String {
    let major = TARGET_FRAMEWORK_MAJOR;
    format!(
        r#"{{
  "runtimeOptions": {{
    "tfm": "net{major}.0",
    "framework": {{
      "name": "Microsoft.NETCore.App",
      "version": "{major}.0.0"
    }}
  }}
}}
"#
    )
}

#[cfg(test)]
mod tests {
    #[test]
    fn mentions_framework() {
        let s = super::render();
        assert!(s.contains("Microsoft.NETCore.App"));
        assert!(s.contains("net10.0"));
    }
}
