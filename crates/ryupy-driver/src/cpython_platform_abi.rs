//! CPython 原生分发的底层 ABI 与机器架构检查：解析 Mach-O、ELF、PE 二进制头部，
//! 严格验证 64 位目标架构（arm64 / x86_64），拒绝错配、损坏或跨架构文件。

use anyhow::{Context, Result, bail, ensure};
use std::fs::File;
use std::io::Read;
use std::path::Path;

/// 二进制所声明的机器架构体系。
#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub(super) enum BinaryArch {
    Arm64,
    X86_64,
    Other(&'static str),
}

/// 检查给定库或可执行文件的机器架构是否与预期 RID 一致。
pub(super) fn check_architecture(path: &Path, expected_rid: &str) -> Result<()> {
    let mut file = File::open(path)
        .with_context(|| format!("无法打开二进制文件进行架构检查：{}", path.display()))?;
    let mut header = [0u8; 4096];
    let bytes_read = file
        .read(&mut header)
        .with_context(|| format!("无法读取二进制文件头部：{}", path.display()))?;
    ensure!(
        bytes_read >= 16,
        "文件头部过短，不是有效的原生二进制：{}",
        path.display()
    );

    let format_matches = match expected_rid {
        "linux-x64" => header.starts_with(b"\x7fELF"),
        "win-x64" => header.starts_with(b"MZ"),
        "osx-arm64" => !header.starts_with(b"MZ") && !header.starts_with(b"\x7fELF"),
        _ => false,
    };
    ensure!(
        format_matches,
        "原生二进制格式与目标RID错配：{expected_rid}，文件：{} (头部: {:02x?})",
        path.display(),
        &header[..bytes_read.min(8)]
    );
    let detected = detect_binary_arch(&header[..bytes_read])
        .with_context(|| format!("无法识别二进制架构：{}", path.display()))?;

    match expected_rid {
        "osx-arm64" => {
            ensure!(
                detected == BinaryArch::Arm64,
                "CPython 二进制架构错配：文件 {} 为 {:?}，预期为 osx-arm64 (Arm64)",
                path.display(),
                detected
            );
        }
        "linux-x64" | "win-x64" => {
            ensure!(
                detected == BinaryArch::X86_64,
                "CPython 二进制架构错配：文件 {} 为 {:?}，预期为 {} (X86_64)",
                path.display(),
                detected,
                expected_rid
            );
        }
        other => bail!("未知的目标 RID 架构校验规则：{other}"),
    }
    Ok(())
}

/// 解析 Mach-O / ELF / PE 文件头部并提取机器架构。
pub(super) fn detect_binary_arch(header: &[u8]) -> Result<BinaryArch> {
    if header.len() >= 4 && (&header[0..4] == b"\x7fELF") {
        return parse_elf_arch(header);
    }
    if header.len() >= 2 && &header[0..2] == b"MZ" {
        return parse_pe_arch(header);
    }
    if header.len() >= 4 {
        let magic = u32::from_ne_bytes([header[0], header[1], header[2], header[3]]);
        if magic == 0xFEEDFACF || magic == 0xCFFAEDFE || magic == 0xFEEDFACE || magic == 0xCEFAEDFE
        {
            return parse_macho_arch(header);
        }
        if magic == 0xCAFEBABE || magic == 0xBEBAFECA {
            return parse_fat_macho_arch(header);
        }
    }
    bail!(
        "未知格式的二进制魔数：{:02x?}",
        &header[..header.len().min(8)]
    )
}

/// 解析 ELF 头部（x86_64 / arm64 校验）。
fn parse_elf_arch(header: &[u8]) -> Result<BinaryArch> {
    ensure!(header.len() >= 20, "ELF 头部过短");
    let ei_class = header[4]; // 1 = 32-bit, 2 = 64-bit
    ensure!(
        ei_class == 2,
        "拒绝 32 位 ELF 二进制，CPython provider 要求 64 位"
    );

    let ei_data = header[5]; // 1 = Little Endian, 2 = Big Endian
    ensure!(ei_data == 1, "只支持小端序 ELF 二进制");

    let e_machine = u16::from_le_bytes([header[18], header[19]]);
    match e_machine {
        0x3E => Ok(BinaryArch::X86_64),
        0xB7 => Ok(BinaryArch::Arm64),
        _ => Ok(BinaryArch::Other("unsupported_elf_machine")),
    }
}

/// 解析 PE / COFF 头部（Windows x64 校验）。
fn parse_pe_arch(header: &[u8]) -> Result<BinaryArch> {
    ensure!(header.len() >= 0x40, "DOS 头部过短");
    let pe_offset =
        u32::from_le_bytes([header[0x3C], header[0x3D], header[0x3E], header[0x3F]]) as usize;
    ensure!(
        header.len() >= pe_offset + 24,
        "PE 签名与 COFF 头部超出读取范围"
    );
    ensure!(
        &header[pe_offset..pe_offset + 4] == b"PE\0\0",
        "无效的 PE 签名"
    );

    let machine = u16::from_le_bytes([header[pe_offset + 4], header[pe_offset + 5]]);
    match machine {
        0x8664 => Ok(BinaryArch::X86_64),
        0xAA64 => Ok(BinaryArch::Arm64),
        0x014C => {
            bail!("拒绝 32 位 PE 二进制，CPython provider 要求 64 位 (x86_64)");
        }
        _ => Ok(BinaryArch::Other("unsupported_pe_machine")),
    }
}

/// 解析单架构 Mach-O 头部（macOS arm64 / x86_64）。
fn parse_macho_arch(header: &[u8]) -> Result<BinaryArch> {
    ensure!(header.len() >= 8, "Mach-O 头部过短");
    let cputype = u32::from_ne_bytes([header[4], header[5], header[6], header[7]]);
    match cputype {
        0x0100000C => Ok(BinaryArch::Arm64),
        0x01000007 => Ok(BinaryArch::X86_64),
        _ => Ok(BinaryArch::Other("unsupported_macho_cputype")),
    }
}

/// 解析 Universal / Fat Mach-O 头部。
fn parse_fat_macho_arch(header: &[u8]) -> Result<BinaryArch> {
    ensure!(header.len() >= 8, "Fat Mach-O 头部过短");
    let nfat_arch = u32::from_be_bytes([header[4], header[5], header[6], header[7]]) as usize;
    let mut has_arm64 = false;
    let mut has_x86_64 = false;
    for i in 0..nfat_arch {
        let offset = 8 + i * 20;
        if header.len() < offset + 4 {
            break;
        }
        let cputype = u32::from_be_bytes([
            header[offset],
            header[offset + 1],
            header[offset + 2],
            header[offset + 3],
        ]);
        if cputype == 0x0100000C {
            has_arm64 = true;
        } else if cputype == 0x01000007 {
            has_x86_64 = true;
        }
    }
    if has_arm64 {
        Ok(BinaryArch::Arm64)
    } else if has_x86_64 {
        Ok(BinaryArch::X86_64)
    } else {
        Ok(BinaryArch::Other("unsupported_fat_cputype"))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn test_elf_x86_64_detection() {
        let mut header = vec![0u8; 64];
        header[0..4].copy_from_slice(b"\x7fELF");
        header[4] = 2; // 64-bit
        header[5] = 1; // little endian
        header[18] = 0x3E; // EM_X86_64
        header[19] = 0x00;
        assert_eq!(detect_binary_arch(&header).unwrap(), BinaryArch::X86_64);
    }

    #[test]
    fn test_pe_x64_detection() {
        let mut header = vec![0u8; 256];
        header[0..2].copy_from_slice(b"MZ");
        header[0x3C] = 0x80; // PE offset at 128
        header[0x80..0x84].copy_from_slice(b"PE\0\0");
        header[0x84] = 0x64; // 0x8664 AMD64
        header[0x85] = 0x86;
        assert_eq!(detect_binary_arch(&header).unwrap(), BinaryArch::X86_64);
    }

    #[test]
    fn test_macho_arm64_detection() {
        let mut header = vec![0u8; 32];
        let magic: u32 = 0xFEEDFACF;
        header[0..4].copy_from_slice(&magic.to_ne_bytes());
        let cputype: u32 = 0x0100000C; // ARM64
        header[4..8].copy_from_slice(&cputype.to_ne_bytes());
        assert_eq!(detect_binary_arch(&header).unwrap(), BinaryArch::Arm64);
    }

    #[test]
    fn test_elf_32bit_rejected() {
        let mut header = vec![0u8; 64];
        header[0..4].copy_from_slice(b"\x7fELF");
        header[4] = 1; // 32-bit
        assert!(detect_binary_arch(&header).is_err());
    }

    #[test]
    fn test_pe_32bit_rejected() {
        let mut header = vec![0u8; 256];
        header[0..2].copy_from_slice(b"MZ");
        header[0x3C] = 0x80;
        header[0x80..0x84].copy_from_slice(b"PE\0\0");
        header[0x84] = 0x4C; // 0x014C i386
        header[0x85] = 0x01;
        assert!(detect_binary_arch(&header).is_err());
    }

    #[test]
    fn test_unknown_magic_rejected() {
        let header = b"INVALID_BINARY_MAGIC";
        assert!(detect_binary_arch(header).is_err());
    }

    #[test]
    fn test_check_architecture_positive_and_negative_matches() {
        let temp_path =
            std::env::temp_dir().join(format!("ryupy_test_abi_match_{}.so", std::process::id()));

        // 构造合法的 ELF x86_64 二进制文件
        let mut elf_data = vec![0u8; 64];
        elf_data[0..4].copy_from_slice(b"\x7fELF");
        elf_data[4] = 2; // 64-bit
        elf_data[5] = 1; // Little endian
        elf_data[18] = 0x3E; // EM_X86_64
        std::fs::write(&temp_path, &elf_data).unwrap();

        // 正例：对 linux-x64 和 win-x64（均为 x86_64）应通过
        assert!(check_architecture(&temp_path, "linux-x64").is_ok());

        // 负例：对 osx-arm64（要求 Arm64）应明确报错拒绝
        let err = check_architecture(&temp_path, "osx-arm64").unwrap_err();
        assert!(err.to_string().contains("错配"));

        let _ = std::fs::remove_file(&temp_path);
    }

    #[test]
    fn test_check_architecture_truncated_file() {
        let short_path =
            std::env::temp_dir().join(format!("ryupy_test_short_{}.bin", std::process::id()));
        std::fs::write(&short_path, b"short").unwrap();

        let err = check_architecture(&short_path, "linux-x64").unwrap_err();
        assert!(err.to_string().contains("头部过短"));

        let _ = std::fs::remove_file(&short_path);
    }
}
