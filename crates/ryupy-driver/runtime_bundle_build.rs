//! 在Cargo的OUT_DIR构建并内嵌完整C#运行时闭包；生成字节只属于构建目录，不进入仓库。
use std::path::{Path, PathBuf};
use std::process::Command;

#[path = "src/runtime_assets.rs"]
mod assets;

fn watch_sources(directory: &Path) -> std::io::Result<()> {
    for entry in std::fs::read_dir(directory)? {
        let path = entry?.path();
        if path.is_dir() {
            if !matches!(
                path.file_name().and_then(|name| name.to_str()),
                Some("bin" | "obj")
            ) {
                watch_sources(&path)?;
            }
        } else if path.extension().is_some_and(|extension| {
            matches!(
                extension.to_str(),
                Some("cs" | "csproj" | "props" | "targets" | "txt")
            )
        }) {
            println!("cargo:rerun-if-changed={}", path.display());
        }
    }
    Ok(())
}

pub(crate) fn build() {
    let workspace = PathBuf::from(std::env::var_os("CARGO_MANIFEST_DIR").unwrap())
        .parent()
        .unwrap()
        .parent()
        .unwrap()
        .to_owned();
    let output = PathBuf::from(std::env::var_os("OUT_DIR").unwrap());
    println!("cargo:rerun-if-changed=build.rs");
    println!("cargo:rerun-if-changed=src/runtime_assets.rs");
    println!("cargo:rerun-if-env-changed=DOTNET");
    for project in ["RyuPy.Runtime", "RyuPy.CPython"] {
        watch_sources(&workspace.join("runtime").join(project)).expect("读取C#运行时源码失败");
    }
    let dotnet = std::env::var_os("DOTNET").unwrap_or_else(|| "dotnet".into());
    let artifacts = output.join("dotnet-artifacts");
    let result = Command::new(dotnet)
        .arg("build")
        .arg(workspace.join("runtime/RyuPy.CPython/RyuPy.CPython.csproj"))
        .args([
            "-c",
            "Release",
            "-p:Platform=AnyCPU",
            "-p:UseSharedCompilation=false",
            "-nologo",
            "-v",
            "q",
            "--artifacts-path",
        ])
        .arg(&artifacts)
        .env("MSBUILDDISABLENODEREUSE", "1")
        .env("DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER", "1")
        .output()
        .expect("构建RyuPy需要.NET SDK 10，请安装SDK或通过DOTNET指定宿主");
    assert!(
        result.status.success(),
        "构建内嵌C#运行时失败：\n{}\n{}",
        String::from_utf8_lossy(&result.stdout),
        String::from_utf8_lossy(&result.stderr)
    );
    let source = artifacts.join("bin/RyuPy.CPython/release");
    let os = std::env::var("CARGO_CFG_TARGET_OS").unwrap();
    let arch = std::env::var("CARGO_CFG_TARGET_ARCH").unwrap();
    let architecture = match arch.as_str() {
        "aarch64" => "arm64",
        "x86_64" => "x64",
        other => panic!("不支持的运行时架构：{other}"),
    };
    let (platform, library) = match os.as_str() {
        "macos" => ("osx", "libe_sqlite3.dylib"),
        "windows" => ("win", "e_sqlite3.dll"),
        "linux" => (
            if std::env::var("CARGO_CFG_TARGET_ENV").as_deref() == Ok("musl") {
                "linux-musl"
            } else {
                "linux"
            },
            "libe_sqlite3.so",
        ),
        other => panic!("不支持的运行时平台：{other}"),
    };
    let rid = format!("{platform}-{architecture}");
    let files = [
        assets::RUNTIME_DLL,
        assets::BRIDGE_DLL,
        assets::DEPENDENCIES,
        assets::LICENSE,
    ]
    .into_iter()
    .chain(assets::MANAGED_DEPENDENCIES.iter().copied())
    .map(str::to_owned)
    .chain(std::iter::once(format!("runtimes/{rid}/native/{library}")));
    let mut declarations = String::from("pub(super) static FILES: &[(&str, &[u8])] = &[\n");
    for name in files {
        let path = source.join(&name);
        assert!(path.is_file(), "内嵌运行时缺少 {}", path.display());
        declarations.push_str(&format!(
            "({name:?}, include_bytes!({:?})),\n",
            path.to_str().expect("构建路径必须为UTF-8")
        ));
    }
    declarations.push_str("];\n");
    // 标识一次构建生成的模块组，防新旧运行时混用；不对普通文件生成摘要。
    let timestamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    declarations.push_str(&format!(
        "pub(super) const BUNDLE_ID: &str = {:?};\n",
        format!(
            "{}-{rid}-{timestamp}",
            std::env::var("CARGO_PKG_VERSION").unwrap()
        )
    ));
    std::fs::write(output.join("runtime_payload.rs"), declarations)
        .expect("写入内嵌运行时索引失败");
}
