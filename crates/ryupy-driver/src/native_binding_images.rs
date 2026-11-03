//! 有界校验受信库/归档的有限目标形状；不是 ABI、符号存在或完整静态闭包认证。
use super::Platform;
use anyhow::{Context, Result, ensure};
use std::io::{Read, Seek, SeekFrom};
fn at(f: &mut (impl Read + Seek), size: u64, offset: u64, n: usize) -> Result<Vec<u8>> {
    ensure!(
        offset.checked_add(n as u64).is_some_and(|end| end <= size),
        "原生头越界"
    );
    f.seek(SeekFrom::Start(offset))?;
    let mut bytes = vec![0; n];
    f.read_exact(&mut bytes)?;
    Ok(bytes)
}
fn u16le(b: &[u8], n: usize) -> u16 {
    u16::from_le_bytes([b[n], b[n + 1]])
}
fn u32le(b: &[u8], n: usize) -> u32 {
    u32::from_le_bytes([b[n], b[n + 1], b[n + 2], b[n + 3]])
}
pub(super) fn shared(f: &mut (impl Read + Seek), size: u64, platform: Platform) -> Result<()> {
    match platform {
        Platform::Linux => {
            let b = at(f, size, 0, 64)?;
            ensure!(
                &b[..4] == b"\x7fELF"
                    && b[4] == 2
                    && b[5] == 1
                    && b[6] == 1
                    && u16le(&b, 16) == 3
                    && u16le(&b, 18) == 62
                    && u16le(&b, 52) == 64,
                "需要 ELF64 x64 ET_DYN"
            );
        }
        Platform::Mac => {
            let b = at(f, size, 0, 32)?;
            ensure!(
                u32le(&b, 0) == 0xfeedfacf && u32le(&b, 4) == 0x0100000c && u32le(&b, 12) == 6,
                "需要薄 arm64 MH_DYLIB"
            );
        }
        Platform::Windows => {
            let dos = at(f, size, 0, 64)?;
            ensure!(&dos[..2] == b"MZ", "共享输入缺MZ");
            let offset = u32le(&dos, 60) as u64;
            let h = at(f, size, offset, 24)?;
            ensure!(
                &h[..4] == b"PE\0\0" && u16le(&h, 4) == 0x8664 && u16le(&h, 22) & 0x2000 != 0,
                "需要 AMD64 PE DLL"
            );
            let o = at(f, size, offset + 24, u16le(&h, 20) as usize)?;
            ensure!(o.len() >= 112 && u16le(&o, 0) == 0x20b, "需要PE32+");
            if u32le(&o, 108) > 14 {
                ensure!(
                    o.len() >= 232 && u32le(&o, 224) == 0 && u32le(&o, 228) == 0,
                    "不能含CLR目录"
                );
            }
        }
    }
    Ok(())
}
pub(super) fn archive(f: &mut (impl Read + Seek), size: u64, platform: Platform) -> Result<()> {
    ensure!(
        at(f, size, 0, 8)? == b"!<arch>\n",
        "只支持普通归档，不支持thin archive"
    );
    let mut offset = 8u64;
    let mut members = 0;
    let mut objects = 0;
    while offset < size {
        members += 1;
        ensure!(members <= 4096, "归档成员超出预算");
        let h = at(f, size, offset, 60)?;
        ensure!(&h[58..] == b"`\n", "归档头损坏");
        let n = std::str::from_utf8(&h[48..58])?.trim();
        ensure!(
            !n.is_empty() && n.bytes().all(|b| b.is_ascii_digit()),
            "归档长度不是无符号十进制"
        );
        let length: u64 = n.parse().context("归档长度溢出")?;
        let start = offset.checked_add(60).context("归档偏移溢出")?;
        let end = start.checked_add(length).context("归档长度溢出")?;
        ensure!(end <= size, "归档成员越界");
        let name = std::str::from_utf8(&h[..16])?.trim();
        let mut object = start;
        let mut object_len = length;
        let mut metadata = matches!(name, "/" | "//" | "/SYM64/") || name.starts_with("__.SYMDEF");
        if let Some(text) = name.strip_prefix("#1/") {
            let count: u64 = text.parse().context("BSD名称长度无效")?;
            ensure!(count <= length && count <= 4096, "BSD名称超额");
            let long = at(f, size, start, count as usize)?;
            metadata = long.starts_with(b"__.SYMDEF");
            object += count;
            object_len -= count;
        }
        if !metadata {
            let head = at(f, end, object, object_len.min(32) as usize)?;
            let valid = match platform {
                Platform::Linux => {
                    head.len() >= 20
                        && &head[..4] == b"\x7fELF"
                        && head[4] == 2
                        && head[5] == 1
                        && u16le(&head, 16) == 1
                        && u16le(&head, 18) == 62
                }
                Platform::Mac => {
                    head.len() >= 16
                        && u32le(&head, 0) == 0xfeedfacf
                        && u32le(&head, 4) == 0x0100000c
                        && u32le(&head, 12) == 1
                }
                Platform::Windows => {
                    head.len() >= 20 && u16le(&head, 0) == 0x8664 && u16le(&head, 16) == 0
                }
            };
            ensure!(valid, "不支持的目标对象或明显导入库成员");
            objects += 1;
        }
        let next = end.checked_add(length % 2).context("归档padding溢出")?;
        ensure!(next > offset && next <= size, "归档没有前进或padding缺失");
        offset = next;
    }
    ensure!(objects > 0, "归档没有受支持目标对象");
    Ok(())
}
