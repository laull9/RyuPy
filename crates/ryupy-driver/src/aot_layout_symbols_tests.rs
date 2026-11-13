//! 符号交付纯测试：成熟 ELF writer 构造独立身份，不依赖宿主 SDK 或冻结二进制。
use super::{files, symbols};
use crate::manifest_files;
use object::{Architecture, BinaryFormat, Endianness, SectionKind};
use serde_json::{Value, json};
use std::path::{Path, PathBuf};
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
const HOST: &str = "RyuPyAotHost_test";

pub(super) fn elf(id: &[u8], debug: bool, link: Option<(&str, u32)>) -> Vec<u8> {
    let mut object =
        object::write::Object::new(BinaryFormat::Elf, Architecture::X86_64, Endianness::Little);
    let note = object.add_section(
        Vec::new(),
        b".note.gnu.build-id".to_vec(),
        SectionKind::Note,
    );
    let mut data = Vec::new();
    for value in [4_u32, id.len() as u32, 3] {
        data.extend(value.to_le_bytes());
    }
    data.extend(b"GNU\0");
    data.extend(id);
    while data.len() % 4 != 0 {
        data.push(0);
    }
    object.append_section_data(note, &data, 4);
    if debug {
        let section = object.add_section(Vec::new(), b".debug_info".to_vec(), SectionKind::Debug);
        object.append_section_data(section, b"synthetic-debug-info", 1);
    }
    if let Some((name, crc)) = link {
        let section =
            object.add_section(Vec::new(), b".gnu_debuglink".to_vec(), SectionKind::Other);
        let mut data = name.as_bytes().to_vec();
        data.push(0);
        while data.len() % 4 != 0 {
            data.push(0);
        }
        data.extend(crc.to_le_bytes());
        object.append_section_data(section, &data, 4);
    }
    let mut bytes = object.write().unwrap();
    // 测试只需要可解析 ELF 的身份节，不执行该 synthetic 镜像。
    bytes[16..18].copy_from_slice(&3_u16.to_le_bytes());
    bytes
}
struct Case {
    root: PathBuf,
    source: PathBuf,
    candidate: PathBuf,
}
impl Case {
    fn new(native: &[u8], debug: Option<&[u8]>) -> Self {
        let root = std::env::temp_dir().join(format!(
            "ryupy-symbols-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        let source = root.join("publish");
        let candidate = root.join("candidate");
        std::fs::create_dir_all(&source).unwrap();
        std::fs::write(source.join(HOST), native).unwrap();
        if let Some(bytes) = debug {
            std::fs::write(source.join(format!("{HOST}.dbg")), bytes).unwrap();
        }
        std::fs::write(source.join("unknown.dbg"), b"must keep").unwrap();
        std::fs::create_dir(source.join("resources")).unwrap();
        std::fs::write(source.join("resources/user.dbg"), b"user resource").unwrap();
        let entries = manifest_files::collect(&source, &source.join(HOST)).unwrap();
        let manifest = json!({"kind":"ryupy-native-aot-artifact","schema_version":1,"artifacts":entries,"preserve_unknown":true});
        manifest_files::write_atomic(&source, &serde_json::to_vec(&manifest).unwrap()).unwrap();
        files::copy_tree(&source, &candidate).unwrap();
        Self {
            root,
            source,
            candidate,
        }
    }
    fn run(&self) -> Value {
        let record = symbols::separate(&self.root, &self.source, &self.candidate, HOST)
            .unwrap()
            .unwrap();
        serde_json::from_slice(&std::fs::read(record).unwrap()).unwrap()
    }
}
impl Drop for Case {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.root);
    }
}
fn pair() -> (Vec<u8>, Vec<u8>) {
    let debug = elf(b"same-id!", true, None);
    let native = elf(
        b"same-id!",
        false,
        Some((&format!("{HOST}.dbg"), crc32fast::hash(&debug))),
    );
    (native, debug)
}
#[test]
fn matched_symbols_only_leave_dist_and_original_tree_is_unchanged() {
    let (native, debug) = pair();
    let c = Case::new(&native, Some(&debug));
    let old = files::snapshot(&c.source, false).unwrap();
    let r = c.run();
    assert_eq!(r["status"], "separated");
    assert_eq!(r["reason"], "verified_debuglink_crc_and_build_id");
    assert_eq!(r["native_sha256"], manifest_files::hash_bytes(&native));
    assert_eq!(r["symbols_sha256"], manifest_files::hash_bytes(&debug));
    assert_eq!(r["native_relative_path"], format!("publish/{HOST}"));
    assert_eq!(r["symbols_relative_path"], format!("publish/{HOST}.dbg"));
    assert_eq!(files::snapshot(&c.source, false).unwrap(), old);
    assert_eq!(std::fs::read(c.candidate.join(HOST)).unwrap(), native);
    assert!(!c.candidate.join(format!("{HOST}.dbg")).exists());
    assert_eq!(
        std::fs::read(c.candidate.join("unknown.dbg")).unwrap(),
        b"must keep"
    );
    assert_eq!(
        std::fs::read(c.candidate.join("resources/user.dbg")).unwrap(),
        b"user resource"
    );
    let m = files::verify_manifest(&c.candidate).unwrap();
    assert_eq!(m["preserve_unknown"], true);
    assert_eq!(m["artifacts"].as_array().unwrap().len(), 3);
}
#[test]
fn unknown_or_mismatched_identity_preserves_complete_dist() {
    let (native, debug) = pair();
    let wrong_crc = elf(b"same-id!", false, Some((&format!("{HOST}.dbg"), 0)));
    let wrong_id = elf(
        b"other-id",
        false,
        Some((&format!("{HOST}.dbg"), crc32fast::hash(&debug))),
    );
    let wrong_name = elf(
        b"same-id!",
        false,
        Some(("../other.dbg", crc32fast::hash(&debug))),
    );
    let no_debug = elf(b"same-id!", false, None);
    let no_info = elf(b"same-id!", false, None);
    let no_info_native = elf(
        b"same-id!",
        false,
        Some((&format!("{HOST}.dbg"), crc32fast::hash(&no_info))),
    );
    for (n, d, reason) in [
        (native.as_slice(), None, "symbols_absent"),
        (
            wrong_crc.as_slice(),
            Some(debug.as_slice()),
            "debuglink_crc_mismatch",
        ),
        (
            wrong_id.as_slice(),
            Some(debug.as_slice()),
            "build_id_mismatch",
        ),
        (
            wrong_name.as_slice(),
            Some(debug.as_slice()),
            "debuglink_basename_mismatch",
        ),
        (
            no_debug.as_slice(),
            Some(debug.as_slice()),
            "debuglink_absent",
        ),
        (
            b"not elf",
            Some(debug.as_slice()),
            "native_parse_unrecognized",
        ),
        (
            no_info_native.as_slice(),
            Some(no_info.as_slice()),
            "debug_info_absent",
        ),
    ] {
        let c = Case::new(n, d);
        let before = files::snapshot(&c.candidate, false).unwrap();
        let r = c.run();
        assert_eq!(r["status"], "retained");
        assert_eq!(r["reason"], reason);
        assert_eq!(files::snapshot(&c.candidate, false).unwrap(), before);
    }
}
#[test]
fn changed_copy_link_and_record_collision_fail_without_touching_source() {
    let (n, d) = pair();
    for mode in 0..3 {
        let c = Case::new(&n, Some(&d));
        let source = files::snapshot(&c.source, false).unwrap();
        match mode {
            0 => std::fs::write(c.candidate.join(HOST), b"tampered").unwrap(),
            1 => {
                std::fs::remove_file(c.candidate.join(format!("{HOST}.dbg"))).unwrap();
                std::os::unix::fs::symlink(
                    c.source.join(format!("{HOST}.dbg")),
                    c.candidate.join(format!("{HOST}.dbg")),
                )
                .unwrap();
            }
            _ => {
                std::fs::write(c.root.join("debug-symbols.json"), b"owned by someone else").unwrap()
            }
        }
        assert!(symbols::separate(&c.root, &c.source, &c.candidate, HOST).is_err());
        assert_eq!(files::snapshot(&c.source, false).unwrap(), source);
    }
}
#[test]
fn source_alias_or_escape_cannot_become_a_mutation_target() {
    let (n, d) = pair();
    let c = Case::new(&n, Some(&d));
    let source = files::snapshot(&c.source, false).unwrap();
    assert!(symbols::separate(&c.root, &c.source, &c.source, HOST).is_err());
    assert!(symbols::separate(&c.root, &c.source, &c.candidate, "../outside").is_err());
    assert!(symbols::separate(Path::new("/unowned"), &c.source, &c.candidate, HOST).is_err());
    assert_eq!(files::snapshot(&c.source, false).unwrap(), source);
}
