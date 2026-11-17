//! 通用目标slice收敛的结构门；手写小型头部模拟fat容器，不生成或提交二进制golden。
use super::*;
use std::io::Cursor;
use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
struct Temp(PathBuf);
impl Drop for Temp {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
fn temp() -> Temp {
    let root = std::env::temp_dir().join(format!(
        "ryupy-macho-slice-{}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&root).unwrap();
    Temp(root)
}
fn thin(cpu: u32, subtype: u32, kind: u32) -> Vec<u8> {
    [0xfeedfacf, cpu, subtype, kind, 0, 0, 0, 0]
        .into_iter()
        .flat_map(u32::to_le_bytes)
        .collect()
}
fn fat(wide: bool, little: bool, arches: &[(u32, u32, u32)]) -> Vec<u8> {
    let mut result = vec![0; 256 + arches.len() * 64];
    let order = |value: u32| {
        if little {
            value.to_le_bytes()
        } else {
            value.to_be_bytes()
        }
    };
    result[..4].copy_from_slice(&order(if wide { 0xcafebabf } else { 0xcafebabe }));
    result[4..8].copy_from_slice(&order(arches.len() as u32));
    for (index, &(cpu, subtype, kind)) in arches.iter().enumerate() {
        let at = 8 + index * if wide { 32 } else { 20 };
        let offset = 256 + index * 64;
        result[at..at + 4].copy_from_slice(&order(cpu));
        result[at + 4..at + 8].copy_from_slice(&order(subtype));
        if wide {
            let encode = |value: u64| {
                if little {
                    value.to_le_bytes()
                } else {
                    value.to_be_bytes()
                }
            };
            result[at + 8..at + 16].copy_from_slice(&encode(offset as u64));
            result[at + 16..at + 24].copy_from_slice(&encode(32));
            result[at + 24..at + 28].copy_from_slice(&order(6));
        } else {
            result[at + 8..at + 12].copy_from_slice(&order(offset as u32));
            result[at + 12..at + 16].copy_from_slice(&order(32));
            result[at + 16..at + 20].copy_from_slice(&order(6));
        }
        result[offset..offset + 32].copy_from_slice(&thin(cpu, subtype, kind));
    }
    result
}
#[test]
fn fat32_fat64_both_byte_orders_select_exact_header_and_range() {
    for wide in [false, true] {
        for little in [false, true] {
            for kind in [2, 6, 8] {
                let bytes = fat(wide, little, &[(0x1000007, 3, kind), (ARM64, 0, kind)]);
                let proof = inspect(&mut Cursor::new(&bytes), bytes.len() as u64).unwrap();
                let selected = proof.selected.unwrap();
                assert_eq!(
                    (selected.offset, selected.bytes, selected.subtype),
                    (320, 32, 0)
                );
                assert_eq!(proof.filetype, Some(kind));
            }
        }
    }
}
#[test]
fn nonloadable_unknown_subtype_and_ambiguous_targets_preserve_original() {
    for arches in [
        vec![(ARM64, 0, 1)],
        vec![(ARM64, 0, 10)],
        vec![(ARM64, 2, 6)],
        vec![(ARM64, 0, 6), (ARM64, 0, 6)],
    ] {
        let root = temp();
        let path = root.0.join("owned-image");
        let original = fat(false, false, &arches);
        std::fs::write(&path, &original).unwrap();
        let report = converge(&path, &root.0, "osx-arm64").unwrap();
        assert_eq!(report["action"], "preserved");
        assert!(!report["reason"].as_str().unwrap().is_empty());
        assert_eq!(std::fs::read(path).unwrap(), original);
    }
}
#[test]
fn invalid_table_range_alignment_overlap_and_inner_header_never_convert() {
    let original = fat(false, false, &[(ARM64, 0, 6), (0x1000007, 3, 6)]);
    let mut damaged = Vec::new();
    for (at, value) in [(16, 0xfffffff0u32), (20, 0xfffffff0), (24, 64), (36, 256)] {
        let mut bytes = original.clone();
        bytes[at..at + 4].copy_from_slice(&value.to_be_bytes());
        damaged.push(bytes);
    }
    let mut mismatch = original.clone();
    mismatch[260..264].copy_from_slice(&0x1000007u32.to_le_bytes());
    damaged.push(mismatch);
    let mut truncated = original.clone();
    truncated.truncate(20);
    damaged.push(truncated);
    for bytes in damaged {
        assert!(inspect(&mut Cursor::new(&bytes), bytes.len() as u64).is_err());
        let root = temp();
        let path = root.0.join("owned-image");
        std::fs::write(&path, &bytes).unwrap();
        let record = converge(&path, &root.0, "osx-arm64").unwrap();
        assert_eq!(record["action"], "preserved");
        assert_eq!(std::fs::read(path).unwrap(), bytes);
    }
}
#[test]
fn selected_slice_is_atomic_mode_preserving_and_missing_target_keeps_existing_abi_error() {
    let root = temp();
    let path = root.0.join("owned-image");
    std::fs::write(
        &path,
        fat(false, false, &[(0x1000007, 3, 6), (ARM64, 0, 6)]),
    )
    .unwrap();
    #[cfg(unix)]
    {
        use std::os::unix::fs::PermissionsExt;
        std::fs::set_permissions(&path, std::fs::Permissions::from_mode(0o751)).unwrap();
    }
    let report = converge(&path, &root.0, "osx-arm64").unwrap();
    assert_eq!(report["action"], "selected_target_slice");
    assert_eq!(std::fs::read(&path).unwrap(), thin(ARM64, 0, 6));
    #[cfg(unix)]
    {
        use std::os::unix::fs::PermissionsExt;
        assert_eq!(
            std::fs::metadata(&path).unwrap().permissions().mode() & 0o777,
            0o751
        );
    }
    std::fs::write(&path, fat(false, false, &[(0x1000007, 3, 6)])).unwrap();
    let report = converge(&path, &root.0, "osx-arm64").unwrap();
    assert_eq!(report["reason"], "target_architecture_absent");
    assert!(super::super::super::platform_abi::check_architecture(&path, "osx-arm64").is_err());
    assert_eq!(std::fs::read_dir(&root.0).unwrap().count(), 1);
}

#[test]
fn occupied_temporary_file_is_never_removed_or_replaced() {
    let root=temp();let source=root.0.join("source");let occupied=root.0.join("occupied");
    std::fs::write(&source,thin(ARM64,0,6)).unwrap();std::fs::write(&occupied,"existing-owner").unwrap();
    let mut file=File::open(&source).unwrap();let permissions=file.metadata().unwrap().permissions();
    let slice=Slice {cpu_type:ARM64,subtype:0,offset:0,bytes:32,alignment:0};
    assert!(copy_slice_to(&mut file,&source,&occupied,slice,&permissions).is_err());
    assert_eq!(std::fs::read_to_string(&occupied).unwrap(),"existing-owner");
    assert_eq!(std::fs::read(&source).unwrap(),thin(ARM64,0,6));
}
