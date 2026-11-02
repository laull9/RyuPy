//! 同宿主 Native AOT 平台合同与有界文件头校验；不推断交叉编译、不加载或执行产物。
use std::io::{Read, Seek, SeekFrom};
use std::path::Path;

use anyhow::{Context, Result, bail, ensure};

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(super) enum HostPlatform {
    LinuxX64,
    MacArm64,
    WindowsX64,
}

impl HostPlatform {
    pub(super) fn current() -> Result<Self> {
        let env = if cfg!(target_env = "gnu") {
            "gnu"
        } else if cfg!(target_env = "msvc") {
            "msvc"
        } else {
            ""
        };
        Self::from_target(std::env::consts::OS, std::env::consts::ARCH, env)
    }

    fn from_target(os: &str, arch: &str, env: &str) -> Result<Self> {
        match (os, arch, env) {
            ("linux", "x86_64", "gnu") => Ok(Self::LinuxX64),
            ("macos", "aarch64", "") => Ok(Self::MacArm64),
            ("windows", "x86_64", "msvc") => Ok(Self::WindowsX64),
            _ => bail!(
                "Native AOT 不支持此宿主 {os}/{arch}/{env}，仅允许 Linux x64 glibc、macOS arm64、Windows x64 MSVC 同宿主发布"
            ),
        }
    }

    pub(super) fn rid(self) -> &'static str {
        match self {
            Self::LinuxX64 => "linux-x64",
            Self::MacArm64 => "osx-arm64",
            Self::WindowsX64 => "win-x64",
        }
    }

    pub(super) fn executable_name(self, host: &str) -> String {
        match self {
            Self::WindowsX64 => format!("{host}.exe"),
            _ => host.to_owned(),
        }
    }

    pub(super) fn validate_image(self, path: &Path) -> Result<()> {
        // Linux保留已验生产合同；原ELF/依赖审计继续由真实smoke承担。
        if self == Self::LinuxX64 {
            return Ok(());
        }
        let mut file = std::fs::File::open(path).context("无法读取本次 AOT 产物文件头")?;
        self.validate_header(&mut file)
    }

    fn validate_header(self, reader: &mut (impl Read + Seek)) -> Result<()> {
        match self {
            Self::LinuxX64 => Ok(()),
            Self::MacArm64 => {
                let mut header = [0; 32];
                reader
                    .read_exact(&mut header)
                    .context("Mach-O 文件头被截断")?;
                ensure!(
                    u32_at(&header, 0) == 0xfeedfacf,
                    "AOT 产物不是 thin Mach-O64"
                );
                ensure!(u32_at(&header, 4) == 0x0100000c, "Mach-O 产物不是 arm64");
                ensure!(u32_at(&header, 12) == 2, "Mach-O 产物不是 MH_EXECUTE");
                Ok(())
            }
            Self::WindowsX64 => validate_pe(reader),
        }
    }
}

fn validate_pe(reader: &mut (impl Read + Seek)) -> Result<()> {
    let length = reader.seek(SeekFrom::End(0))?;
    reader.seek(SeekFrom::Start(0))?;
    let mut dos = [0; 64];
    reader.read_exact(&mut dos).context("PE DOS 文件头被截断")?;
    ensure!(&dos[..2] == b"MZ", "AOT 产物缺少 MZ 签名");
    let offset = u64::from(u32_at(&dos, 60));
    // SDK常规输出远小于此边界；不按不可信offset分配或扫描整个文件。
    ensure!(
        (64..=1_048_576).contains(&offset),
        "PE 文件头偏移超出受支持范围"
    );
    ensure!(
        offset.checked_add(24).is_some_and(|end| end <= length),
        "PE 文件头越界"
    );
    reader.seek(SeekFrom::Start(offset))?;
    let mut coff = [0; 24];
    reader.read_exact(&mut coff)?;
    ensure!(&coff[..4] == b"PE\0\0", "AOT 产物缺少 PE 签名");
    ensure!(u16_at(&coff, 4) == 0x8664, "PE 产物不是 AMD64");
    let flags = u16_at(&coff, 22);
    ensure!(
        flags & 2 != 0 && flags & 0x2000 == 0,
        "PE 产物不是独立可执行映像"
    );
    let size = usize::from(u16_at(&coff, 20));
    ensure!(
        (112..=4096).contains(&size),
        "PE optional header 大小超出受支持范围"
    );
    ensure!(
        offset
            .checked_add(24 + size as u64)
            .is_some_and(|end| end <= length),
        "PE optional header 越界"
    );
    let mut optional = vec![0; size];
    reader.read_exact(&mut optional)?;
    ensure!(u16_at(&optional, 0) == 0x20b, "PE 产物不是 PE32+");
    let count = u64::from(u32_at(&optional, 108));
    ensure!(
        count
            .checked_mul(8)
            .and_then(|n| n.checked_add(112))
            .is_some_and(|end| end <= size as u64),
        "PE data directories 越界"
    );
    if count > 14 {
        ensure!(
            u32_at(&optional, 224) == 0 && u32_at(&optional, 228) == 0,
            "AOT 产物不能含 CLR runtime header"
        );
    }
    Ok(())
}

// 调用点先核固定头长度/目录长度，再读取固定偏移，禁止畸形文件造成切片panic。
fn u16_at(bytes: &[u8], offset: usize) -> u16 {
    u16::from_le_bytes([bytes[offset], bytes[offset + 1]])
}
fn u32_at(bytes: &[u8], offset: usize) -> u32 {
    u32::from_le_bytes([
        bytes[offset],
        bytes[offset + 1],
        bytes[offset + 2],
        bytes[offset + 3],
    ])
}

#[cfg(test)]
#[path = "aot_platform_tests.rs"]
mod tests;
