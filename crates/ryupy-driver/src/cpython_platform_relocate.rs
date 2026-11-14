//! CPython 原生映像的重定位与依赖封闭性检查：
//! macOS 下维护 Mach-O install name 与签名；
//! Linux 下检查 ELF DT_NEEDED、rpath 与系统库闭包（支持版本化 .so.N 与全路径 rpath）；
//! Windows 下检查 PE 导入表与延迟加载表（Delay Import Table），防止外部未授权绝对依赖。

#![allow(dead_code)]

use anyhow::{Context, Result, bail, ensure};
use std::fs::File;
use std::io::Read;
use std::path::{Path, PathBuf};
use std::process::Command;

/// 判断文件是否为目标平台原生二进制动态库或扩展。
pub(super) fn is_native_image(path: &Path) -> bool {
    if !path.is_file() {
        return false;
    }
    if let Some(name) = path.file_name().and_then(|s| s.to_str()) {
        if name.ends_with(".dylib") || name.ends_with(".dll") || name.ends_with(".pyd") {
            return true;
        }
        // 捕获普通 .so 以及版本化共享库如 libpython3.12.so.1.0、libopenblas.so.3
        if name.ends_with(".so") || name.contains(".so.") {
            return true;
        }
    }
    // 结合头部魔数校验
    if let Ok(mut f) = File::open(path) {
        let mut magic = [0u8; 4];
        if f.read_exact(&mut magic).is_ok() {
            return &magic == b"\x7fELF"
                || &magic[0..2] == b"MZ"
                || magic == [0xFE, 0xED, 0xFA, 0xCF]
                || magic == [0xCF, 0xFA, 0xED, 0xFE]
                || magic == [0xCA, 0xFE, 0xBA, 0xBE];
        }
    }
    false
}

/// 递归收集指定根目录下的所有原生映像。
pub(super) fn collect_images(root: &Path, found: &mut Vec<PathBuf>) -> Result<()> {
    if !root.is_dir() {
        return Ok(());
    }
    for entry in std::fs::read_dir(root)? {
        let path = entry?.path();
        if path.is_dir() {
            collect_images(&path, found)?;
        } else if is_native_image(&path) {
            found.push(path);
        }
    }
    Ok(())
}

/// 执行外部命令并确保退出码成功，附带标准错误输出。
fn run_command(program: &str, args: &[&std::ffi::OsStr]) -> Result<()> {
    let output = Command::new(program)
        .args(args)
        .output()
        .with_context(|| format!("无法启动外部命令：{program}"))?;
    ensure!(
        output.status.success(),
        "{program} 执行失败 (退出码 {:?})：{}",
        output.status.code(),
        String::from_utf8_lossy(&output.stderr)
    );
    Ok(())
}

/// 计算 target 相对于 base 的相对路径（用于 @loader_path / $ORIGIN）。
pub(super) fn relative_path(base: &Path, target: &Path) -> Result<String> {
    let base: Vec<_> = base.components().collect();
    let target: Vec<_> = target.components().collect();
    let shared = base
        .iter()
        .zip(&target)
        .take_while(|(left, right)| left == right)
        .count();
    ensure!(shared > 0, "原生依赖与目标映像不属于同一文件系统根");
    let mut result = PathBuf::new();
    for _ in shared..base.len() {
        result.push("..");
    }
    for component in &target[shared..] {
        result.push(component.as_os_str());
    }
    Ok(result.to_str().context("原生库相对路径不是 UTF-8")?.into())
}

/// macOS Mach-O 映像重定位与代码签名（保持 macOS 原有严格行为不变）。
pub(super) fn relocate_macos(output: &Path, shim_name: &str, system_deps: &[&str]) -> Result<()> {
    let mut all = Vec::new();
    collect_images(&output.join("python"), &mut all)?;
    all.push(output.join(shim_name));

    for image in &all {
        super::platform_abi::check_architecture(image, super::rid()?)?;
        let report = Command::new("otool")
            .arg("-L")
            .arg(image)
            .output()
            .with_context(|| format!("无法运行 otool -L 检查：{}", image.display()))?;
        ensure!(
            report.status.success(),
            "CPython 原生依赖无法读取：{}",
            image.display()
        );
        let text = String::from_utf8(report.stdout)?;
        for line in text.lines().skip(1) {
            let Some(dependency) = line.trim().split(" (compatibility version ").next() else {
                continue;
            };
            if dependency.is_empty()
                || system_deps
                    .iter()
                    .any(|prefix| dependency.starts_with(prefix))
            {
                continue;
            }
            let name = Path::new(dependency)
                .file_name()
                .context("Mach-O 依赖缺少文件名")?;
            let matches: Vec<_> = all
                .iter()
                .filter(|candidate| candidate.file_name() == Some(name))
                .collect();
            ensure!(
                matches.len() == 1,
                "CPython 原生依赖未唯一收集：{} → {dependency}",
                image.display()
            );
            let target = matches[0];
            if target == image {
                continue;
            }
            let relative = relative_path(image.parent().context("原生映像缺少父目录")?, target)?;
            let replacement = format!("@loader_path/{relative}");
            if dependency != replacement {
                run_command(
                    "install_name_tool",
                    &[
                        "-change".as_ref(),
                        dependency.as_ref(),
                        replacement.as_ref(),
                        image.as_os_str(),
                    ],
                )?;
            }
        }
        if image
            .extension()
            .is_some_and(|extension| extension == "dylib")
        {
            let id = format!("@rpath/{}", image.file_name().unwrap().to_string_lossy());
            run_command(
                "install_name_tool",
                &["-id".as_ref(), id.as_ref(), image.as_os_str()],
            )?;
        }
        run_command(
            "codesign",
            &[
                "--force".as_ref(),
                "--sign".as_ref(),
                "-".as_ref(),
                image.as_os_str(),
            ],
        )?;
    }
    Ok(())
}

/// Linux ELF 共享库依赖与 rpath 封闭性校验及重定位。
pub(super) fn relocate_linux(output: &Path, shim_name: &str, system_deps: &[&str]) -> Result<()> {
    let mut all = Vec::new();
    collect_images(&output.join("python"), &mut all)?;
    all.push(output.join(shim_name));

    // 检查必需审计工具 readelf
    let readelf_out = Command::new("readelf")
        .arg("-h")
        .arg(output.join(shim_name))
        .output()
        .context("缺少 Linux 平台 ELF 审计工具 readelf，无法验证依赖闭包安全")?;
    ensure!(
        readelf_out.status.success(),
        "readelf 运行失败，无法解析 ELF 依赖"
    );

    // 检查重定位工具 patchelf
    let patchelf_avail = Command::new("patchelf").arg("--version").output();
    let patchelf_ok = patchelf_avail
        .as_ref()
        .map(|o| o.status.success())
        .unwrap_or(false);
    ensure!(
        patchelf_ok,
        "缺少 patchelf 工具，无法重定位 Linux 原生共享库 rpath"
    );

    for image in &all {
        super::platform_abi::check_architecture(image, super::rid()?)?;
        let report = Command::new("readelf")
            .arg("-d")
            .arg(image)
            .output()
            .with_context(|| format!("readelf 无法读取动态依赖节：{}", image.display()))?;
        ensure!(
            report.status.success(),
            "readelf 读取失败 (code {:?})：{}",
            report.status.code(),
            image.display()
        );
        let text = String::from_utf8_lossy(&report.stdout);
        let mut needed_deps = Vec::new();
        for line in text.lines() {
            if line.contains("(NEEDED)") {
                if let Some(dep) = extract_needed_library(line) {
                    needed_deps.push(dep);
                }
            }
        }

        let mut required_dirs = std::collections::BTreeSet::new();
        for dep in &needed_deps {
            if system_deps.iter().any(|sys| dep == sys) {
                continue;
            }
            let matches: Vec<_> = all
                .iter()
                .filter(|c| c.file_name().and_then(|f| f.to_str()) == Some(dep))
                .collect();
            ensure!(
                matches.len() == 1,
                "Linux CPython 原生依赖未封闭：{} 依赖 {}，分发内匹配项数量为 {}（必须为 1）",
                image.display(),
                dep,
                matches.len()
            );
            if let Some(parent) = matches[0].parent() {
                required_dirs.insert(parent);
            }
        }

        // rpath 必须包含 $ORIGIN 以及指向每个已解析非系统依赖所在目录的相对路径
        let image_parent = image.parent().context("原生库缺少父目录")?;
        let mut rpaths = vec!["$ORIGIN".to_string()];
        for dir in required_dirs {
            if dir != image_parent {
                let rel = relative_path(image_parent, dir)?;
                rpaths.push(format!("$ORIGIN/{rel}"));
            }
        }
        let rpath_str = rpaths.join(":");
        run_command(
            "patchelf",
            &[
                "--set-rpath".as_ref(),
                rpath_str.as_ref(),
                image.as_os_str(),
            ],
        )?;
    }
    Ok(())
}

/// 从 readelf -d 的输出行中提取 NEEDED 库名称。
fn extract_needed_library(line: &str) -> Option<String> {
    let start = line.find('[')? + 1;
    let end = line.find(']')?;
    if start < end {
        Some(line[start..end].to_string())
    } else {
        None
    }
}

/// Windows PE 依赖封闭性校验：读取 PE 导入表及延迟导入表，确保外部依赖仅限 Windows 系统 DLL 白名单。
pub(super) fn relocate_windows(output: &Path, shim_name: &str, system_deps: &[&str]) -> Result<()> {
    let mut all = Vec::new();
    collect_images(&output.join("python"), &mut all)?;
    all.push(output.join(shim_name));

    for image in &all {
        super::platform_abi::check_architecture(image, super::rid()?)?;
        let imported = read_pe_imports(image)
            .with_context(|| format!("无法读取 Windows PE 导入表：{}", image.display()))?;
        for dep in imported {
            let dep_lower = dep.to_ascii_lowercase();
            if system_deps.iter().any(|sys| {
                if sys.ends_with('-') {
                    dep_lower.starts_with(&sys.to_ascii_lowercase())
                } else {
                    dep.eq_ignore_ascii_case(sys)
                }
            }) {
                continue;
            }
            let matches: Vec<_> = all
                .iter()
                .filter(|c| {
                    c.file_name()
                        .and_then(|f| f.to_str())
                        .is_some_and(|name| name.eq_ignore_ascii_case(&dep))
                })
                .collect();
            ensure!(
                matches.len() == 1,
                "Windows CPython 原生依赖未封闭：{} 依赖 {}，但不在系统白名单或随包分发中",
                image.display(),
                dep
            );
        }
    }
    Ok(())
}

/// 根据OptionalHeader和节表解析普通/延迟导入；每次RVA映射均限制在文件实际原始节范围。
pub(super) fn read_pe_imports(path: &Path) -> Result<Vec<String>> {
    parse_pe_imports(&std::fs::read(path)?)
}
fn parse_pe_imports(data: &[u8]) -> Result<Vec<String>> {
    let u16_at = |offset: usize| -> Result<usize> {
        let bytes = data.get(offset..offset + 2).context("PE字段截断")?;
        Ok(u16::from_le_bytes(bytes.try_into()?) as usize)
    };
    let u32_at = |offset: usize| -> Result<usize> {
        let bytes = data.get(offset..offset + 4).context("PE字段截断")?;
        Ok(u32::from_le_bytes(bytes.try_into()?) as usize)
    };
    ensure!(data.get(..2) == Some(b"MZ"), "无效的PE/DOS签名");
    let pe = u32_at(0x3c)?;
    ensure!(data.get(pe..pe + 4) == Some(b"PE\0\0"), "无效的PE签名");
    ensure!(u16_at(pe + 4)? == 0x8664, "PE机器架构必须为AMD64");
    let sections = u16_at(pe + 6)?;
    let optional_size = u16_at(pe + 20)?;
    let optional = pe + 24;
    ensure!(optional_size >= 112, "PE OptionalHeader截断");
    let table = optional + optional_size;
    ensure!(table + sections * 40 <= data.len(), "PE节表截断");
    ensure!(u16_at(optional)? == 0x20b, "仅支持PE32+映像");
    let directories = u32_at(optional + 108)?;
    ensure!(
        directories <= (optional_size - 112) / 8,
        "PE数据目录越过OptionalHeader"
    );
    // 返回映射起点和剩余原始节长度，防止表跨越到不相邻节或虚拟零填充区域。
    let map = |rva: usize| -> Result<(usize, usize)> {
        for index in 0..sections {
            let section = table + index * 40;
            let address = u32_at(section + 12)?;
            let raw_size = u32_at(section + 16)?;
            let raw = u32_at(section + 20)?;
            if rva >= address && rva - address < raw_size {
                ensure!(raw + raw_size <= data.len(), "PE原始节越界或截断");
                let delta = rva - address;
                return Ok((raw + delta, raw_size - delta));
            }
        }
        bail!("PE RVA {rva:#x} 未能匹配到任何有效节范围")
    };
    let mut imports = Vec::new();
    for (index, stride, name_field) in [(1, 20, 12), (13, 32, 4)] {
        if directories <= index {
            continue;
        }
        let directory = optional + 112 + index * 8;
        let rva = u32_at(directory)?;
        let size = u32_at(directory + 4)?;
        if rva == 0 && size == 0 {
            continue;
        }
        ensure!(rva != 0 && size >= stride, "PE导入目录地址/大小无效");
        let (offset, available) = map(rva)?;
        ensure!(size <= available, "PE导入目录越界");
        let mut terminated = false;
        for descriptor in (offset..offset + size).step_by(stride) {
            ensure!(descriptor + stride <= offset + size, "PE导入描述符截断");
            if data[descriptor..descriptor + stride]
                .iter()
                .all(|byte| *byte == 0)
            {
                terminated = true;
                break;
            }
            if index == 13 {
                ensure!(u32_at(descriptor)? == 1, "PE延迟导入仅支持RVA属性");
            }
            let (name, remaining) = map(u32_at(descriptor + name_field)?)?;
            let bytes = &data[name..name + remaining];
            let end = bytes
                .iter()
                .position(|byte| *byte == 0)
                .context("PE导入名缺少终结符")?;
            ensure!(
                end > 0 && end < 260 && bytes[..end].is_ascii(),
                "PE导入名格式错误"
            );
            let name = std::str::from_utf8(&bytes[..end])?.to_string();
            ensure!(!name.contains(['/', '\\', ':']), "PE导入名不能携带路径");
            if !imports.contains(&name) {
                imports.push(name);
            }
        }
        ensure!(terminated, "PE导入描述符缺少全零终结符");
    }
    Ok(imports)
}

#[cfg(test)]
mod tests {
    use super::*;

    // 手写最小PE输入，只包含产品解析需要的头部、节和两类导入表。
    fn pe_input() -> Vec<u8> {
        let mut data = vec![0u8; 0x600];
        data[..2].copy_from_slice(b"MZ");
        data[0x3c..0x40].copy_from_slice(&0x80u32.to_le_bytes());
        data[0x80..0x84].copy_from_slice(b"PE\0\0");
        data[0x84..0x86].copy_from_slice(&0x8664u16.to_le_bytes());
        data[0x86..0x88].copy_from_slice(&1u16.to_le_bytes());
        data[0x94..0x96].copy_from_slice(&0xf0u16.to_le_bytes());
        data[0x98..0x9a].copy_from_slice(&0x20bu16.to_le_bytes());
        data[0x104..0x108].copy_from_slice(&16u32.to_le_bytes());
        for (index, rva, size) in [(1, 0x1000u32, 40u32), (13, 0x1040, 64)] {
            let field = 0x108 + index * 8;
            data[field..field + 4].copy_from_slice(&rva.to_le_bytes());
            data[field + 4..field + 8].copy_from_slice(&size.to_le_bytes());
        }
        for (field, value) in [(0x194, 0x1000u32), (0x198, 0x400), (0x19c, 0x200)] {
            data[field..field + 4].copy_from_slice(&value.to_le_bytes());
        }
        data[0x20c..0x210].copy_from_slice(&0x1100u32.to_le_bytes());
        data[0x240..0x244].copy_from_slice(&1u32.to_le_bytes());
        data[0x244..0x248].copy_from_slice(&0x1120u32.to_le_bytes());
        data[0x300..0x30b].copy_from_slice(b"python.dll\0");
        data[0x320..0x32d].copy_from_slice(b"optional.dll\0");
        data
    }
    #[test]
    fn pe_regular_and_delayed_imports_are_both_collected() {
        assert_eq!(
            parse_pe_imports(&pe_input()).unwrap(),
            ["python.dll", "optional.dll"]
        );
    }
    #[test]
    fn pe_truncation_and_missing_descriptor_terminator_are_rejected() {
        let data = pe_input();
        for length in [0, 63, 0x98, 0x108, 0x19f, 0x220, 0x5ff] {
            assert!(
                parse_pe_imports(&data[..length]).is_err(),
                "截断长度{length}"
            );
        }
        let mut no_terminator = data.clone();
        no_terminator[0x114..0x118].copy_from_slice(&20u32.to_le_bytes());
        assert!(
            parse_pe_imports(&no_terminator)
                .unwrap_err()
                .to_string()
                .contains("终结符")
        );
        let mut no_raw_bytes = data;
        no_raw_bytes[0x198..0x19c].copy_from_slice(&16u32.to_le_bytes());
        assert!(parse_pe_imports(&no_raw_bytes).is_err());
    }

    #[test]
    fn test_corrupted_pe_rva_diagnostic_error() {
        let mut data = vec![0u8; 512];
        data[0..2].copy_from_slice(b"MZ");
        data[0x3C..0x40].copy_from_slice(&0x80u32.to_le_bytes());
        data[0x80..0x84].copy_from_slice(b"PE\0\0");
        data[0x84..0x86].copy_from_slice(&0x8664u16.to_le_bytes()); // x64
        data[0x94..0x96].copy_from_slice(&0xF0u16.to_le_bytes()); // opt header size
        let opt = 0x80 + 24;
        data[opt..opt + 2].copy_from_slice(&0x20Bu16.to_le_bytes()); // PE32+
        data[opt + 108..opt + 112].copy_from_slice(&16u32.to_le_bytes());
        // 设置 Import Directory RVA 为非法越界地址 0x999999
        let import_dir = opt + 112 + 8;
        data[import_dir..import_dir + 4].copy_from_slice(&0x999999u32.to_le_bytes());

        data[import_dir + 4..import_dir + 8].copy_from_slice(&40u32.to_le_bytes());
        let pe_file =
            std::env::temp_dir().join(format!("ryupy_corrupted_pe_{}.dll", std::process::id()));
        std::fs::write(&pe_file, &data).unwrap();

        let result = read_pe_imports(&pe_file);
        assert!(result.is_err());
        let err_msg = result.unwrap_err().to_string();
        assert!(err_msg.contains("未能匹配到任何有效节") || err_msg.contains("越界"));

        let _ = std::fs::remove_file(&pe_file);
    }
}
