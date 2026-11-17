//! 由官方.NET SDK CreateAppHost任务生成根入口；目标是internal中的原编译程序集，不加Python/C#业务胶水。
use anyhow::{Context, Result, ensure};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Work(PathBuf);
impl Drop for Work { fn drop(&mut self) { let _ = std::fs::remove_dir_all(&self.0); } }

pub(crate) fn create(root: &Path, dll: &Path) -> Result<PathBuf> {
    let name = dll.file_stem().and_then(|n| n.to_str()).context("JIT程序集名不是UTF8")?;
    let target = root.join(crate::application_layout::entry_name(name));
    crate::manifest_files::ensure_absent(&target)?;
    let parent = root.parent().context("JIT候选缺父目录")?;
    let work = parent.join(format!(".ryupy-apphost-{}-{}", std::process::id(), NEXT.fetch_add(1, Ordering::Relaxed)));
    std::fs::create_dir(&work)?; let guard = Work(work.clone());
    let source = crate::aot::path_literal(dll)?;
    let destination = crate::aot::path_literal(&target)?;
    let binary = format!("internal/{}", dll.file_name().context("程序集缺文件名")?.to_string_lossy());
    let binary = crate::aot::literal(&binary);
    let framework = ryupy_codegen_cil::runtimeconfig::TARGET_FRAMEWORK_MAJOR;
    let project = format!(r#"<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><TargetFramework>net{framework}.0</TargetFramework><OutputType>Exe</OutputType><UseAppHost>true</UseAppHost><SelfContained>false</SelfContained></PropertyGroup>
<Target Name="RyuPyAppHost" DependsOnTargets="_GetAppHostPaths;_GetAppHostCreationConfiguration">
<Error Condition="'$(AppHostSourcePath)' == ''" Text="SDK没有提供当前RID的官方apphost" />
<CreateAppHost AppHostSourcePath="$(AppHostSourcePath)" AppHostDestinationPath="{destination}" AppBinaryName="{binary}" IntermediateAssembly="{source}" WindowsGraphicalUserInterface="false" EnableMacOSCodeSign="$(_EnableMacOSCodeSign)" />
</Target></Project>"#);
    let project_path = work.join("AppHost.proj"); std::fs::write(&project_path, project)?;
    let output = std::process::Command::new(crate::dotnet::dotnet_exe()).arg("msbuild").arg(&project_path)
        .args(["-restore", "-target:RyuPyAppHost", "-nologo", "-verbosity:quiet",
            "-p:ImportDirectoryBuildProps=false", "-p:ImportDirectoryBuildTargets=false"])
        .current_dir(&work).output().context("无法执行官方SDK CreateAppHost")?;
    ensure!(output.status.success(), "官方apphost生成失败（{}）\nstdout:\n{}\nstderr:\n{}", output.status,
        String::from_utf8_lossy(&output.stdout), String::from_utf8_lossy(&output.stderr));
    ensure!(crate::manifest_files::plain_metadata(&target)?.is_file(), "SDK未生成本次apphost");
    drop(guard); Ok(target)
}
