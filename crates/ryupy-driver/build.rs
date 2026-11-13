//! 从随产品编译的 C# 源码生成字面模块注册事实，供探测报告使用，不执行运行时或用户源码。
mod runtime_registry_scan;
use std::{env, fs, io, path::PathBuf};
fn main() -> io::Result<()> {
    let root = PathBuf::from(
        env::var_os("CARGO_MANIFEST_DIR").ok_or_else(|| io::Error::other("缺少 crate 根"))?,
    );
    let runtime = root.join("../../runtime/RyuPy.Runtime");
    println!("cargo:rerun-if-changed={}", runtime.display());
    println!("cargo:rerun-if-changed=runtime_registry_scan.rs");
    let mut paths = fs::read_dir(&runtime)?
        .map(|entry| entry.map(|entry| entry.path()))
        .collect::<io::Result<Vec<_>>>()?;
    paths.retain(|path| path.extension().is_some_and(|ext| ext == "cs"));
    paths.sort();
    let mut generated = String::from("// 自动生成的静态注册事实；仅用于来源探测。\n&[\n");
    for path in paths {
        let source = fs::read_to_string(&path)?;
        let file = path
            .file_name()
            .ok_or_else(|| io::Error::other("注册源文件缺少名称"))?
            .to_string_lossy();
        for (name, line) in runtime_registry_scan::registrations(&source) {
            let origin = format!("runtime/RyuPy.Runtime/{file}:{line}");
            generated.push_str(&format!("({name:?}, {origin:?}),\n"));
        }
    }
    generated.push_str("]\n");
    let out =
        PathBuf::from(env::var_os("OUT_DIR").ok_or_else(|| io::Error::other("缺少构建输出目录"))?);
    fs::write(out.join("runtime_registry.rs"), generated)
}
