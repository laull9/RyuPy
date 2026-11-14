//! Linux ELF 静态闭包：优先冻结载荷，缺失的 libcrypt ABI 从本机 dpkg 包收集。
//! 不加载 ELF，不访问网络；来源、包版本和完整许可随产品冻结，随后递归审核并重写 rpath。

use anyhow::{Context, Result, ensure};
use serde_json::{Value, json};
use std::collections::{BTreeMap, BTreeSet};
use std::path::{Path, PathBuf};
use std::process::Command;

/// 工具调用集中在构建阶段，固定英文输出以稳定解析，不执行目标映像。
fn command(program: &str, args: &[&str]) -> Result<String> {
    let result = Command::new(program)
        .args(args)
        .env("LC_ALL", "C")
        .output()
        .with_context(|| format!("无法启动 Linux 闭包工具 {program}"))?;
    ensure!(
        result.status.success(),
        "{program} {:?} 失败：{}",
        args,
        String::from_utf8_lossy(&result.stderr)
    );
    Ok(String::from_utf8(result.stdout)?)
}

fn path_text(path: &Path) -> Result<&str> {
    path.to_str().context("Linux 原生库路径必须为 UTF-8")
}

/// 只允许没有路径成分的精确 SONAME，防止绝对 DT_NEEDED 绕过相对加载路径。
fn library_name(name: &str) -> Result<()> {
    ensure!(
        !name.is_empty()
            && !name.contains(['/', '\\', ':', '\n', '\r'])
            && name != "."
            && name != "..",
        "非法 ELF 库名：{name}"
    );
    Ok(())
}

fn bracket(line: &str) -> Result<&str> {
    let (_, tail) = line.split_once('[').context("readelf 动态字段缺少左括号")?;
    let (name, _) = tail.split_once(']').context("readelf 动态字段缺少右括号")?;
    library_name(name)?;
    Ok(name)
}

#[derive(Default)]
struct Elf {
    needed: Vec<String>,
    soname: Option<String>,
    requires: BTreeMap<String, BTreeSet<String>>,
    provides: BTreeSet<String>,
}

fn versions(text: &str, elf: &mut Elf) -> Result<()> {
    let mut definitions = false;
    let mut file = None;
    for line in text.lines() {
        if line.starts_with("Version definition section") {
            definitions = true;
        } else if line.starts_with("Version needs section") {
            definitions = false;
        }
        if let Some((_, tail)) = line.split_once("File: ") {
            let name = tail
                .split_whitespace()
                .next()
                .context("ELF 版本依赖缺少库名")?;
            library_name(name)?;
            file = Some(name.to_string());
        }
        if let Some((_, tail)) = line.split_once("Name: ") {
            let name = tail.split_whitespace().next().context("ELF 版本字段为空")?;
            if definitions {
                elf.provides.insert(name.to_string());
            } else if let Some(file) = &file {
                elf.requires
                    .entry(file.clone())
                    .or_default()
                    .insert(name.to_string());
            }
        }
    }
    Ok(())
}

fn inspect(path: &Path) -> Result<Elf> {
    // ELF 身份先于工具输出校验，避免错架构候选被缓存标签误导。
    use std::io::Read;
    let mut header = [0u8; 20];
    std::fs::File::open(path)?.read_exact(&mut header)?;
    ensure!(
        &header[..4] == b"\x7fELF"
            && header[4] == 2
            && header[5] == 1
            && u16::from_le_bytes([header[18], header[19]]) == 62,
        "Linux ELF 格式或架构错配：{}",
        path.display()
    );
    let path = path_text(path)?;
    // readelf 自己也必须成功解析完整 ELF 头；手写头部检查仅用于先行架构拒绝。
    command("readelf", &["-h", path])?;
    let mut elf = Elf::default();
    for line in command("readelf", &["-d", path])?.lines() {
        if line.contains("(NEEDED)") {
            elf.needed.push(bracket(line)?.to_string());
        } else if line.contains("(SONAME)") {
            ensure!(elf.soname.is_none(), "ELF 含多个 SONAME");
            elf.soname = Some(bracket(line)?.to_string());
        }
    }
    versions(&command("readelf", &["-V", path])?, &mut elf)?;
    Ok(elf)
}

/// 同一真实文件的符号链接别名合并；不同目录中的同名实现一律拒绝猜测。
fn payload_match(all: &[PathBuf], name: &str) -> Result<Option<PathBuf>> {
    let mut matches = BTreeMap::new();
    for path in all
        .iter()
        .filter(|p| p.file_name().and_then(|s| s.to_str()) == Some(name))
    {
        matches.entry(path.canonicalize()?).or_insert(path.clone());
    }
    ensure!(matches.len() <= 1, "Linux 载荷同名库歧义：{name}");
    Ok(matches.into_values().next())
}

/// 只读系统链接器缓存；同 ABI 的多个真实来源也必须显式消除歧义。
fn cache_candidates(text: &str, name: &str) -> Result<Vec<PathBuf>> {
    let mut paths = BTreeSet::new();
    for line in text.lines() {
        if line.split_whitespace().next() != Some(name) {
            continue;
        }
        let (description, path) = line.split_once(" => ").context("ldconfig 缓存行格式错误")?;
        // 缓存经常同时列出 i386 ABI，它不能成为 x64 候选。
        if !description.contains("x86-64") {
            continue;
        }
        let path = PathBuf::from(path.trim());
        ensure!(path.is_absolute(), "ldconfig 来源必须为绝对路径");
        paths.insert(path);
    }
    Ok(paths.into_iter().collect())
}

fn package_owner(source: &Path) -> Result<String> {
    // usrmerge 下缓存路径和包数据库的路径可能分别是 /lib 与 /usr/lib。
    let real = source.canonicalize()?;
    let mut spellings = BTreeSet::from([source.to_path_buf(), real.clone()]);
    if let Ok(tail) = real.strip_prefix("/usr/lib") {
        spellings.insert(Path::new("/lib").join(tail));
    }
    let mut owners = BTreeSet::new();
    for spelling in spellings {
        if let Ok(text) = command("dpkg-query", &["-S", path_text(&spelling)?]) {
            for line in text.lines() {
                if let Some((owner, owned)) = line.split_once(": ") {
                    if Path::new(owned) == spelling {
                        owners.insert(owner.to_string());
                    }
                }
            }
        }
    }
    ensure!(
        owners.len() == 1,
        "系统库包归属缺失或歧义：{}",
        source.display()
    );
    owners.into_iter().next().context("缺少 OS 包")
}

/// 冻结 Debian copyright 及其引用的 common-licenses 文本；缺失文本立即终止。
fn freeze_license(package: &str, files: &str, python: &Path) -> Result<Vec<String>> {
    let base = package.split(':').next().context("OS 包名为空")?;
    ensure!(
        base.chars()
            .all(|c| c.is_ascii_alphanumeric() || "+-.".contains(c)),
        "非法 OS 包名"
    );
    let copyright = PathBuf::from(format!("/usr/share/doc/{base}/copyright"));
    ensure!(
        files.lines().any(|p| Path::new(p) == copyright),
        "OS 包未登记许可证：{package}"
    );
    let text = std::fs::read_to_string(&copyright).context("无法读取 OS 包 copyright")?;
    ensure!(
        text.contains("License:") && !text.trim().is_empty(),
        "OS 包缺少可识别许可证：{package}"
    );
    let relative = PathBuf::from("licenses/native").join(base);
    let destination = python.join(&relative);
    std::fs::create_dir_all(&destination)?;
    std::fs::copy(&copyright, destination.join("copyright"))?;
    let mut result = vec![relative.join("copyright").to_string_lossy().into_owned()];
    // Debian copyright 中的外部许可引用有句号/引号等标点；仅接受公共许可目录的直接文件。
    for tail in text.split("/usr/share/common-licenses/").skip(1) {
        let name: String = tail
            .chars()
            .take_while(|c| c.is_ascii_alphanumeric() || "-_.+".contains(*c))
            .collect();
        let name = name.trim_end_matches('.');
        ensure!(!name.is_empty() && name != "..", "公共许可证引用无效");
        let source = Path::new("/usr/share/common-licenses").join(name);
        ensure!(source.is_file(), "公共许可证缺失：{}", source.display());
        std::fs::copy(&source, destination.join(name))?;
        let frozen = relative.join(name).to_string_lossy().into_owned();
        if !result.contains(&frozen) {
            result.push(frozen);
        }
    }
    Ok(result)
}

fn collect_system(name: &str, python: &Path) -> Result<(PathBuf, Value)> {
    // 本次只授权标准库 _crypt 所需 ABI；其余边必须通过冻结 payload 显式提供。
    ensure!(
        name == "libcrypt.so.1",
        "Linux 未收集原生依赖 {name}，请通过显式 payload 提供"
    );
    let cache = command("/sbin/ldconfig", &["-p"])?;
    let mut candidates = BTreeMap::new();
    for path in cache_candidates(&cache, name)? {
        candidates.entry(path.canonicalize()?).or_insert(path);
    }
    ensure!(
        candidates.len() == 1,
        "系统链接器缓存中 {name} 缺失或同名歧义"
    );
    let (real, source) = candidates.into_iter().next().context("缺少系统库")?;
    let elf = inspect(&real)?;
    ensure!(
        elf.soname.as_deref() == Some(name),
        "系统库 SONAME 与依赖版本错配：{name}"
    );
    let package = package_owner(&source)?;
    let metadata = command(
        "dpkg-query",
        &[
            "-W",
            "-f=${binary:Package}\t${Version}\t${Architecture}\t${db:Status-Status}",
            &package,
        ],
    )?;
    let fields: Vec<_> = metadata.split('\t').collect();
    ensure!(
        fields.len() == 4
            && fields[0] == package
            && !fields[1].is_empty()
            && fields[2] == "amd64"
            && fields[3] == "installed",
        "OS 包版本/架构/安装状态不符：{metadata}"
    );
    let files = command("dpkg-query", &["-L", &package])?;
    ensure!(
        files
            .lines()
            .any(|p| Path::new(p).canonicalize().ok().as_ref() == Some(&real)),
        "OS 包文件清单未拥有原生库：{package}"
    );
    let licenses = freeze_license(&package, &files, python)?;
    let target = python.join("lib").join(name);
    std::fs::create_dir_all(target.parent().context("库目标缺少父目录")?)?;
    ensure!(!target.exists(), "系统库目标已存在：{}", target.display());
    std::fs::copy(&real, &target)?;
    Ok((
        target,
        json!({"soname": name, "source": source, "resolved_source": real,
        "package": package, "version": fields[1], "architecture": fields[2],
        "resolver": "/sbin/ldconfig -p + dpkg-query", "destination": format!("lib/{name}"),
        "licenses": licenses}),
    ))
}

/// 收集期间使用增长队列：新引入的库与原载荷接受相同传递依赖和架构审核。
pub(super) fn relocate(output: &Path, shim: &str, core: &[&str]) -> Result<()> {
    command("patchelf", &["--version"])?;
    let python = output.join("python");
    let mut all = Vec::new();
    super::collect_images(&python, &mut all)?;
    all.push(output.join(shim));
    all.sort();
    let frozen_root = python.canonicalize()?;
    for image in &all {
        ensure!(
            image == &output.join(shim) || image.canonicalize()?.starts_with(&frozen_root),
            "Linux 冻结载荷链接越界：{}",
            image.display()
        );
    }
    let mut reports = Vec::new();
    let mut index = 0;
    while index < all.len() {
        let image = all[index].clone();
        let elf = inspect(&image)?;
        for name in &elf.needed {
            if core.contains(&name.as_str()) {
                continue;
            }
            if payload_match(&all, name)?.is_none() {
                let (path, report) = collect_system(name, &python)?;
                all.push(path);
                reports.push(report);
            }
        }
        index += 1;
    }
    // 收集完成后再审核版本和修改 rpath，确保每条非核心边都落到唯一冻结库。
    for image in &all {
        let elf = inspect(image)?;
        let parent = image.parent().context("ELF 缺少父目录")?;
        let mut paths = BTreeSet::from(["$ORIGIN".to_string()]);
        for name in &elf.needed {
            if core.contains(&name.as_str()) {
                continue;
            }
            let target = payload_match(&all, name)?.context("Linux 传递闭包缺库")?;
            let target_elf = inspect(&target)?;
            if let Some(soname) = target_elf.soname {
                ensure!(soname == *name, "ELF SONAME 版本错配：{name} 与 {soname}");
            }
            if let Some(required) = elf.requires.get(name) {
                ensure!(
                    required.is_subset(&target_elf.provides),
                    "ELF 符号版本不满足：{} → {name}，要求 {:?}",
                    image.display(),
                    required
                );
            }
            let relative =
                super::relative_path(parent, target.parent().context("依赖库缺少父目录")?)?;
            ensure!(
                !relative.contains([':', '$', '\n', '\r']),
                "Linux 相对 rpath 目录含加载器特殊字符：{relative}"
            );
            if !relative.is_empty() {
                paths.insert(format!("$ORIGIN/{relative}"));
            }
        }
        let rpath = paths.into_iter().collect::<Vec<_>>().join(":");
        command("patchelf", &["--set-rpath", &rpath, path_text(image)?])?;
        let actual = command("patchelf", &["--print-rpath", path_text(image)?])?;
        ensure!(
            actual.trim() == rpath,
            "ELF rpath 重写审核失败：{}",
            image.display()
        );
        inspect(image)?;
    }
    // 置于 python 下，使既有 files 清单和事务发布自动覆盖来源及许可。
    std::fs::write(
        python.join("native-sources.json"),
        serde_json::to_vec_pretty(&json!({
            "schema": 1, "policy": "payload-first; dpkg-libcrypt-abi1-only", "libraries": reports
        }))?,
    )?;
    Ok(())
}

#[cfg(test)]
#[path = "cpython_platform_linux_tests.rs"]
mod tests;
