//! 既有重定位映像集合的目标架构收敛：有界读取fat表与真实内层头，仅转换唯一已证明的arm64 slice。
//! 证据不足时保留原部署文件；源安装不变，转换沿临时文件原子提交，后续仍走原ABI/加载身份/签名验收。
use anyhow::{Context, Result, ensure};
use serde::Serialize;
use serde_json::{Value, json};
use std::fs::{File, OpenOptions};
use std::io::{Read, Seek, SeekFrom, Write};
use std::path::Path;
use std::sync::atomic::{AtomicU64, Ordering};
const ARM64: u32 = 0x0100000c;
static NEXT: AtomicU64 = AtomicU64::new(0);
#[derive(Clone, Copy, Debug, Serialize)]
struct Slice {
    cpu_type: u32,
    subtype: u32,
    offset: u64,
    bytes: u64,
    alignment: u32,
}
#[derive(Default)]
struct Proof {
    format: &'static str,
    slices: Vec<Slice>,
    selected: Option<Slice>,
    filetype: Option<u32>,
    reason: &'static str,
}
fn read_at(
    file: &mut (impl Read + Seek),
    length: u64,
    offset: u64,
    count: usize,
) -> Result<Vec<u8>> {
    ensure!(
        offset
            .checked_add(count as u64)
            .is_some_and(|end| end <= length),
        "Mach-O头或slice越界"
    );
    file.seek(SeekFrom::Start(offset))?;
    let mut bytes = vec![0; count];
    file.read_exact(&mut bytes)?;
    Ok(bytes)
}
fn number(bytes: &[u8], at: usize, little: bool) -> u32 {
    let b = [bytes[at], bytes[at + 1], bytes[at + 2], bytes[at + 3]];
    if little {
        u32::from_le_bytes(b)
    } else {
        u32::from_be_bytes(b)
    }
}
fn wide_number(bytes: &[u8], at: usize, little: bool) -> u64 {
    let mut b = [0; 8];
    b.copy_from_slice(&bytes[at..at + 8]);
    if little {
        u64::from_le_bytes(b)
    } else {
        u64::from_be_bytes(b)
    }
}
fn inspect(file: &mut (impl Read + Seek), length: u64) -> Result<Proof> {
    let header = read_at(file, length, 0, 8)?;
    let (wide, little) = match &header[..4] {
        [0xca, 0xfe, 0xba, 0xbe] => (false, false),
        [0xbe, 0xba, 0xfe, 0xca] => (false, true),
        [0xca, 0xfe, 0xba, 0xbf] => (true, false),
        [0xbf, 0xba, 0xfe, 0xca] => (true, true),
        _ => {
            return Ok(Proof {
                format: "not_universal",
                reason: "not_universal",
                ..Proof::default()
            });
        }
    };
    let count = number(&header, 4, little) as u64;
    let row = if wide { 32 } else { 20 };
    // 仅限制可选优化的头部预算，超额保原；既有ABI检查继续决定是否可加载。
    ensure!(
        count > 0 && count <= 1024,
        "fat表超出可选收敛的有界读取预算"
    );
    let table_bytes = count.checked_mul(row).context("fat表长度溢出")?;
    let table_end = 8u64.checked_add(table_bytes).context("fat表偏移溢出")?;
    let table = read_at(file, length, 8, table_bytes as usize)?;
    let mut slices = Vec::new();
    for index in 0..count as usize {
        let at = index * row as usize;
        let slice = Slice {
            cpu_type: number(&table, at, little),
            subtype: number(&table, at + 4, little),
            offset: if wide {
                wide_number(&table, at + 8, little)
            } else {
                number(&table, at + 8, little) as u64
            },
            bytes: if wide {
                wide_number(&table, at + 16, little)
            } else {
                number(&table, at + 12, little) as u64
            },
            alignment: number(&table, at + if wide { 24 } else { 16 }, little),
        };
        let align = 1u64
            .checked_shl(slice.alignment)
            .context("fat slice alignment越界")?;
        ensure!(
            slice.bytes >= 32
                && slice.offset >= table_end
                && slice.offset % align == 0
                && slice
                    .offset
                    .checked_add(slice.bytes)
                    .is_some_and(|end| end <= length),
            "fat slice范围或alignment损坏"
        );
        slices.push(slice);
    }
    let mut ranges = slices.clone();
    ranges.sort_by_key(|slice| slice.offset);
    ensure!(
        ranges
            .windows(2)
            .all(|pair| pair[0].offset + pair[0].bytes <= pair[1].offset),
        "fat slice范围重叠"
    );
    let mut proof = Proof {
        format: if wide { "fat64" } else { "fat32" },
        slices,
        ..Proof::default()
    };
    let candidates: Vec<_> = proof
        .slices
        .iter()
        .filter(|s| s.cpu_type == ARM64 && s.subtype == 0)
        .copied()
        .collect();
    if candidates.len() != 1 {
        proof.reason = if candidates.len() > 1 {
            "ambiguous_target_subtype"
        } else if proof.slices.iter().any(|s| s.cpu_type == ARM64) {
            "target_subtype_not_proven"
        } else {
            "target_architecture_absent"
        };
        return Ok(proof);
    }
    let slice = candidates[0];
    let inner = read_at(file, length, slice.offset, 32)?;
    ensure!(
        inner[..4] == [0xcf, 0xfa, 0xed, 0xfe]
            && number(&inner, 4, true) == slice.cpu_type
            && number(&inner, 8, true) == slice.subtype,
        "fat表与实际目标Mach-O64头不符"
    );
    ensure!(
        32u64 + number(&inner, 20, true) as u64 <= slice.bytes,
        "目标Mach-O加载命令越过slice"
    );
    let kind = number(&inner, 12, true);
    proof.filetype = Some(kind);
    if !matches!(kind, 2 | 6 | 8) {
        proof.reason = "nonloadable_macho_preserved";
        return Ok(proof);
    }
    proof.selected = Some(slice);
    proof.reason = "unique_arm64_all_loadable_slice";
    Ok(proof)
}
/// 调用者只能传入原重定位已拥有的映像；本函数不扫描或扩大资源集合。
pub(super) fn converge(path: &Path, root: &Path, rid: &str) -> Result<Value> {
    let mut file = File::open(path)?;
    let metadata = file.metadata()?;
    let original = metadata.len();
    let relative = path.strip_prefix(root)?.to_string_lossy();
    if rid != "osx-arm64" {
        return Ok(
            json!({"path":relative,"action":"preserved","reason":"target_rid_not_supported"}),
        );
    }
    let proof = match inspect(&mut file, original) {
        Ok(proof) => proof,
        Err(error) => {
            return Ok(
                json!({"path":relative,"action":"preserved","reason":"slice_evidence_incomplete",
            "detail":format!("{error:#}"),"source_bytes":original,"output_bytes":original}),
            );
        }
    };
    let mut record = json!({"path":relative,"action":"preserved","reason":proof.reason,"target_rid":rid,
        "source_container":proof.format,"source_fat_table":proof.slices,"source_bytes":original,"output_bytes":original,
        "verified_target_filetype":proof.filetype,"source_authority":"original_installed_payload_metadata_unchanged"});
    if let Some(slice) = proof.selected {
        replace_slice(&mut file, path, slice, &metadata.permissions())?;
        record["action"] = json!("selected_target_slice");
        record["selected_slice"] = json!(slice);
        record["output_bytes"] = json!(slice.bytes);
        record["output_architecture"] = json!("arm64_all");
        record["signature"] = json!("existing_relocation_adhoc_resign_preserving_entitlements");
    }
    Ok(record)
}
fn replace_slice(
    file: &mut File,
    path: &Path,
    slice: Slice,
    permissions: &std::fs::Permissions,
) -> Result<()> {
    let parent = path.parent().context("部署映像缺少父目录")?;
    let temp = parent.join(format!(
        ".ryupy-target-slice-{}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    copy_slice_to(file, path, &temp, slice, permissions)
}
fn copy_slice_to(file: &mut File, path: &Path, temp: &Path, slice: Slice, permissions: &std::fs::Permissions) -> Result<()> {
    // create_new成功后才取得清理权限；碰撞文件属于别人，绝不能在失败清理中删除。
    let mut output = OpenOptions::new().write(true).create_new(true).open(temp)?;
    let result = (|| {
        file.seek(SeekFrom::Start(slice.offset))?;
        let copied = std::io::copy(&mut file.take(slice.bytes), &mut output)?;
        ensure!(copied == slice.bytes, "目标slice复制不完整");
        output.flush()?;
        std::fs::set_permissions(temp, permissions.clone())?;
        drop(output);
        std::fs::rename(temp, path)?;
        Ok(())
    })();
    if result.is_err() {
        let _ = std::fs::remove_file(temp);
    }
    result
}
#[cfg(test)]
#[path = "cpython_macho_slice_tests.rs"]
mod tests;
