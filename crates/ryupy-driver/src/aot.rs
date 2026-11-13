//! Native AOT 的源码直入口与保守薄宿主发布：只编排 SDK 与自有目录，不解释 Python 或生成运行时反射。
//!
//! 每次成功保留独立工作根；高层OwnedAttempt保失败诊断树，低层失败只删本轮根。
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
#[path = "aot_process.rs"]
mod process;
#[path = "aot_sdk_paths.rs"]
mod sdk_paths;
#[path = "aot_work_paths.rs"]
mod work_paths;

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

/// 仅普通源码AOT保证可执行PE；绑定/无报告及Windows的Win32资源仍走旧宿主。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
enum EntryMode {
    Direct,
    ThinHost,
}
impl EntryMode {
    fn select(platform: HostPlatform, source_aot: bool) -> Self {
        if source_aot && platform != HostPlatform::WindowsX64 {
            Self::Direct
        } else {
            Self::ThinHost
        }
    }
}

fn project(
    host: &str,
    input_name: &str,
    dll: &Path,
    runtime: &Path,
    sdk_record: Option<&Path>,
    entry: EntryMode,
) -> Result<String> {
    let framework = ryupy_codegen_cil::runtimeconfig::TARGET_FRAMEWORK_MAJOR;
    let dll = path_literal(dll)?;
    let runtime = path_literal(runtime)?;
    let input_name = literal(input_name);
    // 不替换SDK内部编译目标链；仅在公开CoreCompile边界提供本次已验证managed字节。
    let (properties, items, supply) = match entry {
        EntryMode::ThinHost => (
            String::new(),
            format!(
                r#"    <Compile Include="Program.cs" />
    <Reference Include="{input_name}"><HintPath>{dll}</HintPath></Reference>
"#
            ),
            String::new(),
        ),
        EntryMode::Direct => (
            String::from(
                r#"    <SkipCompilerExecution>true</SkipCompilerExecution>
    <ProduceReferenceAssembly>false</ProduceReferenceAssembly>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>
    <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute>
    <UseAppHost>false</UseAppHost>
"#,
            ),
            String::new(),
            format!(
                r#"  <Target Name="RyuPySupplyExistingManagedInput" BeforeTargets="CoreCompile">
    <Copy SourceFiles="{dll}" DestinationFiles="@(IntermediateAssembly)" />
  </Target>
"#
            ),
        ),
    };
    let sdk_target = if let Some(path) = sdk_record {
        let path = path_literal(path)?;
        format!(
            r#"  <Target Name="RyuPyRecordSdkVersion" BeforeTargets="Publish">
    <WriteLinesToFile File="{path}" Lines="$(NETCoreSdkVersion)" Overwrite="true" Encoding="UTF-8" />
  </Target>
"#
        )
    } else {
        String::new()
    };
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
{properties}  </PropertyGroup>
  <ItemGroup>
{items}    <Reference Include="RyuPy.Runtime"><HintPath>{runtime}</HintPath></Reference>
  </ItemGroup>
{supply}{sdk_target}</Project>
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

// 唯一性已由外层attempt提供；只缩目录，不改变程序集身份生成和保留名避让。
fn short_owned_root(
    out: &Path,
    input_name: &str,
    owned: &crate::aot_layout::OwnedAttempt,
) -> Result<(PathBuf, String)> {
    let time = SystemTime::now().duration_since(UNIX_EPOCH)?.as_nanos();
    for _ in 0..64 {
        let serial = NEXT.fetch_add(1, Ordering::Relaxed);
        let host = format!("RyuPyAotHost_{}_{time}_{serial}", std::process::id());
        if host.eq_ignore_ascii_case(input_name) || host.eq_ignore_ascii_case(RUNTIME_ASSEMBLY) {
            continue;
        }
        return Ok((owned.create_child(out, "ryupy-aot-0")?, host));
    }
    bail!("无法选择不冲突的AOT宿主程序集名")
}

pub(crate) fn publish(dll: &Path, out: &Path) -> Result<PathBuf> {
    Ok(publish_internal(dll, out, None, None, None)?.executable)
}

/// 本次编译入口必须携带真实报告；不能因上下文缺失而悄悄退回低层无清单成功。
pub(crate) struct ManifestPublication {
    pub executable: PathBuf,
    pub manifest: PathBuf,
}
struct Publication {
    executable: PathBuf,
    manifest: Option<PathBuf>,
}
pub(crate) fn publish_with_manifest(
    dll: &Path,
    out: &Path,
    context: &crate::manifest::ManifestContext,
    resources: Option<&crate::resources::ResourcePlan>,
    owned: Option<&crate::aot_layout::OwnedAttempt>,
) -> Result<ManifestPublication> {
    let result = publish_internal(
        dll,
        out,
        Some(AotManifest::Ordinary(context)),
        resources,
        owned,
    )?;
    Ok(ManifestPublication {
        executable: result.executable,
        manifest: result.manifest.context("完整 AOT 发布缺少清单")?,
    })
}

#[derive(Clone, Copy)]
enum AotManifest<'a> {
    Ordinary(&'a crate::manifest::ManifestContext),
    Binding(
        &'a crate::native_binding_manifest::BindingManifest,
        &'a crate::native_binding::BindingPlan,
    ),
}
impl AotManifest<'_> {
    fn optimization(self) -> crate::AotOptimization {
        match self {
            Self::Ordinary(c) => c.aot_optimization,
            Self::Binding(c, _) => c.aot_optimization,
        }
    }
    fn verify_managed_input(self, input: &crate::manifest_files::FileDigest) -> Result<()> {
        match self {
            Self::Ordinary(c) => c.verify_managed_input(input),
            Self::Binding(c, _) => c.verify_managed_input(input),
        }
    }
    fn render(
        self,
        platform: HostPlatform,
        sdk: &str,
        artifacts: &[crate::manifest_files::FileDigest],
        inputs: &[crate::manifest_files::FileDigest],
    ) -> Result<Vec<u8>> {
        match self {
            Self::Ordinary(c) => c.render(
                platform.rid(),
                platform.native_format(),
                platform.architecture(),
                sdk,
                artifacts,
                inputs,
            ),
            Self::Binding(c, plan) => {
                let mut inputs = inputs.to_vec();
                inputs.push(plan.input.digest.clone());
                c.render(plan, Some(sdk), artifacts, &inputs)
            }
        }
    }
}
pub(crate) fn publish_binding(
    dll: &Path,
    out: &Path,
    context: &crate::native_binding_manifest::BindingManifest,
    plan: &crate::native_binding::BindingPlan,
    resources: Option<&crate::resources::ResourcePlan>,
    owned: Option<&crate::aot_layout::OwnedAttempt>,
) -> Result<ManifestPublication> {
    let result = publish_internal(
        dll,
        out,
        Some(AotManifest::Binding(context, plan)),
        resources,
        owned,
    )?;
    Ok(ManifestPublication {
        executable: result.executable,
        manifest: result.manifest.context("绑定AOT缺少清单")?,
    })
}

fn publish_internal(
    dll: &Path,
    out: &Path,
    context: Option<AotManifest<'_>>,
    resources: Option<&crate::resources::ResourcePlan>,
    owned: Option<&crate::aot_layout::OwnedAttempt>,
) -> Result<Publication> {
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
    let (root, host) = if let Some(attempt) = owned {
        short_owned_root(&out, input_name, attempt)?
    } else {
        owned_root(&out, input_name)?
    };
    let result = publish_owned(&root, &host, input_name, &dll, &runtime, context, resources);
    if owned.is_some() {
        result
    } else {
        finish_owned(&root, result)
    }
}

// 低层SDK/清单失败只清理本轮根；高层失败不调用此函数，由外层ledger记录。
fn finish_owned<T>(root: &Path, result: Result<T>) -> Result<T> {
    match result {
        Ok(executable) => Ok(executable),
        Err(error) => match std::fs::remove_dir_all(root) {
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
    context: Option<AotManifest<'_>>,
    resources: Option<&crate::resources::ResourcePlan>,
) -> Result<Publication> {
    let platform = HostPlatform::current()?;
    // SDK只见已完成资源快照；冻结失败由外层按高层留证/低层清理策略处理。
    let staged = resources.map(|plan| plan.stage(root)).transpose()?;
    let archive = match context {
        Some(AotManifest::Binding(_, plan)) => {
            Some(plan.input.freeze(&root.join("binding-static.a"))?)
        }
        _ => None,
    };
    let source = root.join("host");
    let publish = root.join("publish");
    std::fs::create_dir(&source)?;
    std::fs::create_dir(&publish)?;
    let project_path = source.join("Host.csproj");
    let sdk_record = source.join("sdk.version");
    let optimization_record = source.join("sdk.optimization");
    crate::manifest_files::ensure_absent(&optimization_record)?;
    let optimization = context.map_or(crate::AotOptimization::Default, AotManifest::optimization);
    crate::manifest_files::ensure_absent(&sdk_record)?;
    // 尚不存在的文件不能直接 canonicalize；转换已创建父目录后拼固定安全文件名。
    let sdk_source = sdk_paths::sdk_path(&source)?;
    work_paths::preflight(&sdk_source, host, platform.rid())?;
    let sdk_record_path = sdk_source.join("sdk.version");
    let input_digests = if let Some(context) = context {
        let inputs = crate::manifest::input_digests(dll, runtime)?;
        context.verify_managed_input(&inputs[0])?;
        Some(inputs)
    } else {
        None
    };
    let sdk_dll = sdk_paths::sdk_path(dll)?;
    let sdk_runtime = sdk_paths::sdk_path(runtime)?;
    // Ordinary context只由source-AOT发射链建立；不能把绑定的library PE当作主入口。
    let entry = EntryMode::select(platform, matches!(context, Some(AotManifest::Ordinary(_))));
    let mut project_text = project(
        host,
        input_name,
        &sdk_dll,
        &sdk_runtime,
        context.map(|_| sdk_record_path.as_path()),
        entry,
    )?;
    project_text = crate::aot_optimization::configure(
        project_text,
        optimization,
        &path_literal(&sdk_source.join("sdk.optimization"))?,
    );
    if let (Some(AotManifest::Binding(_, plan)), Some(archive)) = (context, &archive) {
        let path = path_literal(&sdk_paths::sdk_path(&archive.path)?)?;
        // 同一archive只登记一次，每个实际符号精确DirectPInvoke；显式别名去重。
        let mut symbols = std::collections::HashSet::new();
        let mut direct = String::new();
        for export in &plan.exports {
            if symbols.insert(&export.symbol) {
                let exact = literal(&format!("{}!{}", plan.library, export.symbol));
                direct.push_str(&format!("<DirectPInvoke Include=\"{exact}\" />"));
            }
        }
        project_text = project_text.replace(
            "</Project>",
            &format!(
                "  <ItemGroup>{direct}<NativeLibrary Include=\"{path}\" /></ItemGroup>\n</Project>"
            ),
        );
    }
    std::fs::write(&project_path, project_text)?;
    if entry == EntryMode::ThinHost {
        std::fs::write(
            source.join("Program.cs"),
            "internal static class Program { private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args); }\n",
        )?;
    }
    let sdk_project = sdk_paths::sdk_path(&project_path)?;
    let sdk_publish = sdk_paths::sdk_path(&publish)?;
    let trace = crate::stage_trace::StageTrace::new(
        matches!(context, Some(AotManifest::Ordinary(c)) if c.diagnostic_stages),
    );
    let mut command = Command::new(crate::dotnet::dotnet_exe());
    command
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
        .current_dir(&sdk_source);
    let output = process::run(&mut command, root, &trace)?;
    // 完整原流仍随失败返回；成功不把全部SDK输出读入父进程内存。
    if !output.status.success() {
        bail!(
            "Native AOT dotnet publish 失败（{}）\n{}",
            output.status,
            output.diagnostics()
        );
    }
    let checked = (|| -> Result<Publication> {
        if context.is_some() {
            anyhow::ensure!(
                crate::manifest_files::plain_metadata(&publish)?.is_dir(),
                "publish 目录无效"
            );
            anyhow::ensure!(
                crate::manifest_files::plain_metadata(&source)?.is_dir(),
                "SDK 记录目录无效"
            );
        }
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
        if context.is_some() {
            crate::manifest_files::plain_metadata(&executable)?;
        }
        platform.validate_image(&executable)?;
        crate::aot_optimization::receipt(root, &optimization_record, &executable, optimization)?;
        let manifest = if let Some(context) = context {
            platform.validate_manifest_image(&executable)?;
            let sdk_version = crate::manifest_sdk::read_version(&sdk_record)?;
            let inputs = crate::manifest::input_digests(dll, runtime)?;
            anyhow::ensure!(
                input_digests.as_ref() == Some(&inputs),
                "SDK 发布期间构建输入发生变化"
            );
            context.verify_managed_input(&inputs[0])?;
            if let Some(staged) = &staged {
                staged.publish(&publish)?;
            }
            let mut artifacts = crate::manifest_files::collect(&publish, &executable)?;
            if let Some(staged) = &staged {
                staged.mark_artifacts(&mut artifacts)?;
            }
            if let Some(archive) = &archive {
                archive.verify()?;
            }
            let bytes = context.render(platform, &sdk_version, &artifacts, &inputs)?;
            let bytes = if let Some(staged) = &staged {
                crate::resources::extend_manifest(&bytes, staged)?
            } else {
                bytes
            };
            Some(crate::manifest_files::write_atomic(&publish, &bytes)?)
        } else {
            None
        };
        Ok(Publication {
            executable,
            manifest,
        })
    })();
    checked.map_err(|error| {
        error.context(format!(
            "SDK 已退出（{}），但发布校验/归档失败\n{}",
            output.status,
            output.diagnostics()
        ))
    })
}

#[cfg(test)]
#[path = "aot_tests.rs"]
mod tests;
