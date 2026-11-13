//! 固定签名的单库绑定声明：兼容v1单导出，v2有界多导出，严格拒绝未知字段。
use anyhow::{Context, Result, ensure};
use serde::Deserialize;
use std::path::{Path, PathBuf};
#[path = "native_binding_files.rs"]
pub(crate) mod files;
#[path = "native_binding_images.rs"]
mod images;
pub(crate) const INPUT_LIMIT: u64 = 64 * 1024 * 1024;
const DECLARATION_LIMIT: u64 = 64 * 1024;
pub(crate) const SIGNATURE: &str = "cdecl_i32_i32_i64";
pub(crate) const EXPORT_LIMIT: usize = 64;
#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Declaration {
    schema_version: u32,
    module: String,
    export: Option<String>,
    signature: Option<String>,
    library: String,
    symbol: Option<String>,
    exports: Option<Vec<BindingExport>>,
    target: Target,
}
/// 一个公开Python函数；允许不同名字显式绑定同一C符号，各自保有函数身份。
#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
pub(crate) struct BindingExport {
    pub name: String,
    pub symbol: String,
    pub signature: String,
}
#[derive(Deserialize)]
#[serde(deny_unknown_fields)]
struct Target {
    rid: String,
    shared: Option<String>,
    archive: Option<String>,
}
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(crate) enum Platform {
    Linux,
    Mac,
    Windows,
}
impl Platform {
    pub(crate) fn current() -> Result<Self> {
        match (std::env::consts::OS, std::env::consts::ARCH) {
            ("linux", "x86_64") if cfg!(target_env = "gnu") => Ok(Self::Linux),
            ("macos", "aarch64") => Ok(Self::Mac),
            ("windows", "x86_64") if cfg!(target_env = "msvc") => Ok(Self::Windows),
            _ => anyhow::bail!(
                "原生绑定只支持 Linux x64 glibc、macOS arm64、Windows x64 MSVC 同宿主"
            ),
        }
    }
    pub(crate) fn rid(self) -> &'static str {
        match self {
            Self::Linux => "linux-x64",
            Self::Mac => "osx-arm64",
            Self::Windows => "win-x64",
        }
    }
    pub(crate) fn native_format(self) -> &'static str {
        match self {
            Self::Linux => "ELF",
            Self::Mac => "Mach-O",
            Self::Windows => "PE",
        }
    }
    pub(crate) fn architecture(self) -> &'static str {
        match self {
            Self::Mac => "arm64",
            _ => "x64",
        }
    }
    pub(crate) fn relative_file(self) -> &'static str {
        match self {
            Self::Linux => "_internal/native/v1/b0/library.so",
            Self::Mac => "_internal/native/v1/b0/library.dylib",
            Self::Windows => "_internal/native/v1/b0/library.dll",
        }
    }
}
pub(crate) struct BindingPlan {
    pub declaration_version: u32,
    pub module: String,
    pub exports: Vec<BindingExport>,
    pub library: String,
    pub declaration_sha256: String,
    pub platform: Platform,
    pub aot: bool,
    pub input: files::NativeInput,
}
impl BindingPlan {
    pub(crate) fn read(path: &Path, platform: Platform, aot: bool) -> Result<Self> {
        let bytes = files::read_small(path, DECLARATION_LIMIT)?;
        let d: Declaration = serde_json::from_slice(&bytes).context("无效原生绑定声明")?;
        // 两种声明只在此归一化；混合字段拒绝，避免新版配置被旧字段静默截断。
        let exports = match d.schema_version {
            1 => {
                ensure!(d.exports.is_none(), "版本1不能包含exports");
                vec![BindingExport {
                    name: d.export.context("版本1缺少export")?,
                    symbol: d.symbol.context("版本1缺少symbol")?,
                    signature: d.signature.context("版本1缺少signature")?,
                }]
            }
            2 => {
                ensure!(
                    d.export.is_none() && d.symbol.is_none() && d.signature.is_none(),
                    "版本2不能包含版本1单导出字段"
                );
                d.exports.context("版本2缺少exports")?
            }
            _ => anyhow::bail!("只支持版本1或版本2原生绑定声明"),
        };
        ensure!(
            (1..=EXPORT_LIMIT).contains(&exports.len()),
            "原生导出数量必须为1至64"
        );
        for value in [&d.module, &d.library] {
            ensure!(identifier(value), "绑定名称必须是有界ASCII标识符");
        }
        ensure!(d.module != "__main__", "绑定不能替代入口");
        let mut names = std::collections::HashSet::new();
        for e in &exports {
            ensure!(
                identifier(&e.name) && identifier(&e.symbol),
                "绑定名称必须是有界ASCII标识符"
            );
            ensure!(e.signature == SIGNATURE, "只支持固定cdecl_i32_i32_i64签名");
            ensure!(
                !matches!(e.name.as_str(), "__name__" | "__package__" | "__file__"),
                "绑定不能替代保留元数据"
            );
            ensure!(names.insert(&e.name), "原生导出名称重复");
        }
        ensure!(d.target.rid == platform.rid(), "绑定RID必须精确匹配宿主");
        // 只消费本次模式所选输入，不给另一条未读取的路径背书。
        let selected = (if aot {
            d.target.archive
        } else {
            d.target.shared
        })
        .context("缺少本次模式所选原生输入")?;
        let path = absolute(path)?;
        let root = path.parent().context("声明没有父目录")?;
        let source = files::checked_relative(root, &selected)?;
        let input = files::NativeInput::open(&source, platform, aot)?;
        Ok(Self {
            declaration_version: d.schema_version,
            module: d.module,
            exports,
            library: d.library,
            declaration_sha256: crate::manifest_files::hash_bytes(&bytes),
            platform,
            aot,
            input,
        })
    }
    pub(crate) fn check_modules(&self, p: &ryupy_mir::LinkedProgram) -> Result<()> {
        for (_, m) in p.modules.iter() {
            ensure!(
                m.name != self.module
                    && !m.name.starts_with(&format!("{}.", self.module))
                    && !self.module.starts_with(&format!("{}.", m.name)),
                "原生绑定与实际源码模块冲突"
            );
        }
        Ok(())
    }
}
fn identifier(s: &str) -> bool {
    let mut b = s.bytes();
    s.len() <= 128
        && matches!(b.next(), Some(b'a'..=b'z' | b'A'..=b'Z' | b'_'))
        && b.all(|v| v.is_ascii_alphanumeric() || v == b'_')
}
fn absolute(p: &Path) -> Result<PathBuf> {
    Ok(if p.is_absolute() {
        p.to_owned()
    } else {
        std::env::current_dir()?.join(p)
    })
}
#[cfg(test)]
#[path = "native_binding_tests.rs"]
mod tests;
