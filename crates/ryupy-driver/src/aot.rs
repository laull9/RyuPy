//! Native AOT 的薄宿主发布：只编排 SDK 与自有目录，不解释 Python 或生成运行时反射。
//!
//! 每次成功保留独立工作根及 publish 目录；失败只删除本次独占创建的工作根。
//! 所有诊断保留 SDK 退出码及完整输出，不能把上次的原生产物冒充本次成功。
use std::path::{Path, PathBuf};
use std::process::Command;
use std::sync::atomic::{AtomicU64, Ordering};
use std::time::{SystemTime, UNIX_EPOCH};

use anyhow::{Context, Result, bail};

static NEXT: AtomicU64 = AtomicU64::new(0);
const RUNTIME_ASSEMBLY: &str = "RyuPy.Runtime";
#[path = "aot_platform.rs"]
mod platform;
use platform::HostPlatform;
#[path = "aot_sdk_paths.rs"]
mod sdk_paths;

/// 在 build 的任何产物写入之前调用。运行时保留名对 JIT 同样适用，避免 DLL 被覆盖。
pub(crate) fn validate_before_build(input: &Path, aot: bool) -> Result<()> {
    if input.file_stem().is_some_and(|name| {
        name.to_string_lossy()
            .eq_ignore_ascii_case(RUNTIME_ASSEMBLY)
    }) {
        bail!("输入程序集名 RyuPy.Runtime 为运行时保留名，不能生成同名产物");
    }
    if aot {
        validate_platform()?;
    }
    Ok(())
}

fn validate_platform() -> Result<()> {
    HostPlatform::current().map(|_| ())
}

/// SDK 使用绝对 HintPath；先转义 MSBuild 特殊字符，再转义 XML，不经过 shell。
fn literal(value: &str) -> String {
    let mut escaped = String::new();
    for ch in value.chars() {
        match ch {
            '%' => escaped.push_str("%25"),
            '$' => escaped.push_str("%24"),
            '@' => escaped.push_str("%40"),
            ';' => escaped.push_str("%3B"),
            '\'' => escaped.push_str("%27"),
            '(' => escaped.push_str("%28"),
            ')' => escaped.push_str("%29"),
            '*' => escaped.push_str("%2A"),
            '?' => escaped.push_str("%3F"),
            '&' => escaped.push_str("&amp;"),
            '<' => escaped.push_str("&lt;"),
            '>' => escaped.push_str("&gt;"),
            '"' => escaped.push_str("&quot;"),
            _ => escaped.push(ch),
        }
    }
    escaped
}

fn path_literal(path: &Path) -> Result<String> {
    let text = path
        .to_str()
        .context("AOT 项目引用路径必须可表示为 UTF-8")?;
    if text.chars().any(char::is_control) {
        bail!("AOT 项目引用路径不能包含控制字符");
    }
    Ok(literal(text))
}

fn project(host: &str, input_name: &str, dll: &Path, runtime: &Path) -> Result<String> {
    let framework = ryupy_codegen_cil::runtimeconfig::TARGET_FRAMEWORK_MAJOR;
    let dll = path_literal(dll)?;
    let runtime = path_literal(runtime)?;
    let input_name = literal(input_name);
    Ok(format!(
        r#"<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net{framework}.0</TargetFramework>
    <AssemblyName>{host}</AssemblyName><PublishAot>true</PublishAot><Optimize>true</Optimize>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <IlcTreatWarningsAsErrors>true</IlcTreatWarningsAsErrors>
    <ILLinkTreatWarningsAsErrors>true</ILLinkTreatWarningsAsErrors>
    <TrimmerSingleWarn>false</TrimmerSingleWarn>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
    <Reference Include="{input_name}"><HintPath>{dll}</HintPath></Reference>
    <Reference Include="RyuPy.Runtime"><HintPath>{runtime}</HintPath></Reference>
  </ItemGroup>
</Project>
"#
    ))
}

fn owned_root(out: &Path, input_name: &str) -> Result<(PathBuf, String)> {
    let time = SystemTime::now()
        .duration_since(UNIX_EPOCH)
        .context("系统时钟早于 UNIX epoch")?
        .as_nanos();
    for _ in 0..64 {
        let serial = NEXT.fetch_add(1, Ordering::Relaxed);
        let suffix = format!("{}_{}_{}", std::process::id(), time, serial);
        let host = format!("RyuPyAotHost_{suffix}");
        if host.eq_ignore_ascii_case(input_name) || host.eq_ignore_ascii_case(RUNTIME_ASSEMBLY) {
            continue;
        }
        let root = out.join(format!("ryupy-aot-{suffix}"));
        match std::fs::create_dir(&root) {
            Ok(()) => return Ok((root, host)),
            Err(error) if error.kind() == std::io::ErrorKind::AlreadyExists => continue,
            Err(error) => return Err(error).context("无法创建独占 AOT 工作目录"),
        }
    }
    bail!("无法在有限重试内创建独占 AOT 工作目录")
}

pub(crate) fn publish(dll: &Path, out: &Path) -> Result<PathBuf> {
    validate_platform()?;
    validate_before_build(dll, true)?;
    let dll = dll.canonicalize().context("AOT 输入 DLL 不存在")?;
    let out = out.canonicalize().context("AOT 输出目录不存在")?;
    let runtime = out
        .join("RyuPy.Runtime.dll")
        .canonicalize()
        .context("AOT 运行时 DLL 不存在")?;
    let input_name = dll
        .file_stem()
        .and_then(|name| name.to_str())
        .context("AOT 输入程序集名必须为 UTF-8")?;
    let (root, host) = owned_root(&out, input_name)?;
    let result = publish_owned(&root, &host, input_name, &dll, &runtime);
    match result {
        Ok(executable) => Ok(executable),
        Err(error) => match std::fs::remove_dir_all(&root) {
            Ok(()) => Err(error),
            Err(cleanup) => Err(error.context(format!(
                "同时无法清理本次 AOT 工作目录 {}：{cleanup}",
                root.display()
            ))),
        },
    }
}

fn publish_owned(
    root: &Path,
    host: &str,
    input_name: &str,
    dll: &Path,
    runtime: &Path,
) -> Result<PathBuf> {
    let platform = HostPlatform::current()?;
    let source = root.join("host");
    let publish = root.join("publish");
    std::fs::create_dir(&source)?;
    std::fs::create_dir(&publish)?;
    let project_path = source.join("Host.csproj");
    let sdk_dll = sdk_paths::sdk_path(dll)?;
    let sdk_runtime = sdk_paths::sdk_path(runtime)?;
    std::fs::write(
        &project_path,
        project(host, input_name, &sdk_dll, &sdk_runtime)?,
    )?;
    std::fs::write(
        source.join("Program.cs"),
        "internal static class Program { private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args); }\n",
    )?;
    let sdk_project = sdk_paths::sdk_path(&project_path)?;
    let sdk_publish = sdk_paths::sdk_path(&publish)?;
    let sdk_source = sdk_paths::sdk_path(&source)?;
    let output = Command::new(crate::dotnet::dotnet_exe())
        .arg("publish")
        .arg(&sdk_project)
        .args([
            "-c",
            "Release",
            "-r",
            platform.rid(),
            "--self-contained",
            "true",
            "-o",
        ])
        .arg(&sdk_publish)
        .args([
            "-p:ImportDirectoryBuildProps=false",
            "-p:ImportDirectoryBuildTargets=false",
        ])
        .current_dir(&sdk_source)
        .output()
        .context("无法启动 Native AOT dotnet publish（可用 DOTNET 指定 SDK）")?;
    // 失败清理工作目录后，完整输出仍随错误交给 CLI/调用者归档。
    if !output.status.success() {
        bail!(
            "Native AOT dotnet publish 失败（{}）\nstdout:\n{}\nstderr:\n{}",
            output.status,
            String::from_utf8_lossy(&output.stdout),
            String::from_utf8_lossy(&output.stderr)
        );
    }
    let checked = (|| -> Result<PathBuf> {
        std::fs::write(root.join("publish.stdout.log"), &output.stdout)?;
        std::fs::write(root.join("publish.stderr.log"), &output.stderr)?;
        let executable = publish.join(platform.executable_name(host));
        let metadata = std::fs::symlink_metadata(&executable)
            .with_context(|| format!("SDK 成功但没有本次原生产物 {}", executable.display()))?;
        if !metadata.is_file() || metadata.len() == 0 {
            bail!("SDK 成功但本次原生产物不是非空普通文件");
        }
        #[cfg(unix)]
        {
            use std::os::unix::fs::PermissionsExt;
            if metadata.permissions().mode() & 0o111 == 0 {
                bail!("本次原生产物没有执行权限");
            }
        }
        platform.validate_image(&executable)?;
        Ok(executable)
    })();
    checked.map_err(|error| {
        error.context(format!(
            "SDK 已退出（{}），但发布校验/归档失败\nstdout:\n{}\nstderr:\n{}",
            output.status,
            String::from_utf8_lossy(&output.stdout),
            String::from_utf8_lossy(&output.stderr)
        ))
    })
}

#[cfg(test)]
#[path = "aot_tests.rs"]
mod tests;
