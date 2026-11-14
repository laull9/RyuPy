//! 单文件源码测试：在仓库外编译 .NET 解析器负例，禁止向源码目录写入生成产物。
use super::*;
#[test]
fn payload_boundaries() -> Result<()> {
    let root = work_root()?;
    let result = (|| -> Result<()> {
        std::fs::write(
            root.join("Payload.cs"),
            include_str!("../../../runtime/RyuPy.OneFile/Payload.cs"),
        )?;
        std::fs::write(
            root.join("DecodedPayloadStream.cs"),
            include_str!("../../../runtime/RyuPy.OneFile/DecodedPayloadStream.cs"),
        )?;
        std::fs::write(
            root.join("Tests.cs"),
            include_str!("../../../runtime/RyuPy.OneFile/Tests.cs"),
        )?;
        std::fs::write(
            root.join("Tests.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup></Project>",
        )?;
        sdk(
            &root,
            "tests",
            &["run", "--project", "Tests.csproj", "-c", "Release"],
        )
    })();
    if result.is_ok() {
        std::fs::remove_dir_all(&root)?;
    }
    result.with_context(|| format!("onefile 测试失败，现场 {}", root.display()))
}

#[test]
fn rejects_links_and_output_inside_payload() -> Result<()> {
    let root = work_root()?;
    let result = (|| -> Result<()> {
        std::fs::write(root.join("entry"), "input")?;
        assert!(package(&root, &root.join("entry"), &root.join("output")).is_err());
        #[cfg(unix)]
        {
            std::os::unix::fs::symlink("entry", root.join("alias"))?;
            assert!(archive::write(&root, &root.join("entry"), &root.join("raw")).is_err());
        }
        Ok(())
    })();
    std::fs::remove_dir_all(&root)?;
    result
}

/// 显式运行真实双层 NativeAOT 发布；需要同宿主 SDK 与原生链接器。
#[test]
#[ignore = "真实 NativeAOT 发布：显式运行 onefile_native_process --ignored"]
fn onefile_native_process() -> Result<()> {
    let root = work_root()?;
    let result = (|| -> Result<()> {
        let source = root.join("source");
        let dist = root.join("fixture.dist");
        std::fs::create_dir(&source)?;
        std::fs::write(
            source.join("Program.cs"),
            include_str!("../../../runtime/RyuPy.OneFile/Fixture.cs"),
        )?;
        std::fs::write(
            source.join("Fixture.csproj"),
            "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><PublishAot>true</PublishAot></PropertyGroup></Project>",
        )?;
        sdk(
            &source,
            "fixture",
            &[
                "publish",
                "Fixture.csproj",
                "-c",
                "Release",
                "-o",
                dist.to_str().context("临时路径不是UTF-8")?,
            ],
        )?;
        std::fs::write(dist.join("资源.txt"), "资源成功")?;
        let inner = dist.join(if cfg!(windows) {
            "Fixture.exe"
        } else {
            "Fixture"
        });
        let binary = package(
            &dist,
            &inner,
            &root.join(if cfg!(windows) {
                "独立 单文件.exe"
            } else {
                "独立 单文件"
            }),
        )?;
        std::fs::remove_dir_all(&dist)?;
        std::fs::remove_dir_all(&source)?;
        let mut workers = Vec::new();
        for _ in 0..4 {
            let binary = binary.clone();
            let root = root.clone();
            workers.push(std::thread::spawn(move || -> Result<String> {
                use std::io::Write;
                use std::process::Stdio;
                let mut child = Command::new(binary)
                    .args(["space value", "中文", "", "line\nbreak"])
                    .env("PATH", "")
                    .current_dir(&root)
                    .stdin(Stdio::piped())
                    .stdout(Stdio::piped())
                    .stderr(Stdio::piped())
                    .spawn()?;
                child
                    .stdin
                    .take()
                    .context("缺 stdin")?
                    .write_all(b"stdin\nbytes")?;
                let output = child.wait_with_output()?;
                ensure!(
                    output.status.code() == Some(37),
                    "子退出码改变：{:?}",
                    output
                );
                let expected = format!(
                    "{}\nc3BhY2UgdmFsdWU=\n5Lit5paH\n\nbGluZQpicmVhaw==\n资源成功\nstdin\nbytes",
                    root.canonicalize()?.display()
                );
                ensure!(
                    String::from_utf8(output.stdout)?.replace("\r\n", "\n") == expected,
                    "参数、资源或stdin不一致"
                );
                let extracted = String::from_utf8(output.stderr)?.trim().to_owned();
                ensure!(!Path::new(&extracted).exists(), "正常退出后解包目录仍存在");
                Ok(extracted)
            }));
        }
        let mut paths = std::collections::HashSet::new();
        for worker in workers {
            let path = worker
                .join()
                .map_err(|_| anyhow::anyhow!("进程验收线程失败"))??;
            ensure!(paths.insert(path), "并发进程复用了解包目录");
        }
        Ok(())
    })();
    if result.is_ok() {
        std::fs::remove_dir_all(&root)?;
    }
    result.with_context(|| format!("NativeAOT onefile 验收现场 {}", root.display()))
}
