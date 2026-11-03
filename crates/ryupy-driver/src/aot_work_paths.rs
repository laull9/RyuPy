//! Windows NativeAOT已知生成路径预算，独立于SDK项目路径校验；不修改系统长路径策略。
//! 后缀来自SDK10.0.403/ILCompiler10.0.14真实成功产物，不承诺覆盖未来所有SDK路径。
use anyhow::Result;
use std::path::Path;

pub(super) fn preflight(source: &Path, host: &str, rid: &str) -> Result<()> {
    #[cfg(windows)]
    {
        use anyhow::Context;
        check(
            source.to_str().context("SDK工作目录必须为Unicode")?,
            host,
            rid,
            ryupy_codegen_cil::runtimeconfig::TARGET_FRAMEWORK_MAJOR,
        )?;
    }
    #[cfg(not(windows))]
    let _ = (source, host, rid);
    Ok(())
}

#[cfg(any(windows, test))]
fn check(source: &str, host: &str, rid: &str, framework: u32) -> Result<()> {
    use anyhow::ensure;
    let obj = format!("{source}\\obj\\Release\\net{framework}.0\\{rid}\\native");
    let bin = format!("{source}\\bin\\Release\\net{framework}.0\\{rid}\\native");
    let mut paths = vec![format!("{obj}\\link.rsp")];
    for extension in ["obj", "def", "sourcelink", "ilc.rsp"] {
        paths.push(format!("{obj}\\{host}.{extension}"));
    }
    for extension in ["exe", "pdb"] {
        paths.push(format!("{bin}\\{host}.{extension}"));
    }
    for path in paths {
        let count = path.encode_utf16().count();
        ensure!(
            count < 260,
            "Windows NativeAOT中间路径需要{count}个UTF-16单位，达到260限制；请使用更短的 --out-dir。路径：{path}"
        );
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn longest_observed_suffix_boundary_is_checked() {
        let tail = "\\obj\\Release\\net10.0\\win-x64\\native\\Host.sourcelink";
        let source = format!("C:\\{}", "x".repeat(259 - tail.encode_utf16().count() - 3));
        check(&source, "Host", "win-x64", 10).unwrap();
        let error = check(&(source + "x"), "Host", "win-x64", 10).unwrap_err();
        assert!(error.to_string().contains("--out-dir"));
    }
    #[test]
    fn surrogate_pairs_count_as_two_utf16_units() {
        let tail = "\\obj\\Release\\net10.0\\win-x64\\native\\Host.sourcelink";
        let source = format!("C:\\{}", "x".repeat(258 - tail.encode_utf16().count() - 3));
        check(&(source.clone() + "中"), "Host", "win-x64", 10).unwrap();
        assert!(check(&(source + "😀"), "Host", "win-x64", 10).is_err());
    }
    #[test]
    fn actual_host_name_and_framework_affect_budget() {
        let source = format!("C:\\{}", "x".repeat(150));
        check(&source, "Short", "win-x64", 10).unwrap();
        assert!(check(&source, &"H".repeat(100), "win-x64", 10).is_err());
    }
}
