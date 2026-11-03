//! 首批纯结构回归：合成库头不执行，不当成真实 C ABI 验证。
use super::*;
use std::io::Cursor;
use std::sync::atomic::{AtomicU64, Ordering};
static NEXT: AtomicU64 = AtomicU64::new(0);
fn root() -> PathBuf {
    let time = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let p = std::env::temp_dir().join(format!(
        "binding-input-{}-{time}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    std::fs::create_dir(&p).unwrap();
    p.canonicalize().unwrap()
}
fn elf() -> Vec<u8> {
    let mut b = vec![0; 64];
    b[..7].copy_from_slice(b"\x7fELF\x02\x01\x01");
    b[16] = 3;
    b[18] = 62;
    b[52] = 64;
    b
}
fn declaration() -> serde_json::Value {
    serde_json::json!({"schema_version":1,"module":"demo_native","export":"add","signature":SIGNATURE,"library":"demo_c","symbol":"add_i32","target":{"rid":"linux-x64","shared":"库 & file.so","archive":"unread.a"}})
}
#[test]
fn strict_declaration_selected_only() {
    let r = root();
    std::fs::write(r.join("库 & file.so"), elf()).unwrap();
    let p = r.join("b.json");
    let good = declaration();
    std::fs::write(&p, good.to_string()).unwrap();
    let plan = BindingPlan::read(&p, Platform::Linux, false).unwrap();
    assert_eq!(plan.module, "demo_native");
    assert_eq!(plan.input.digest.size, 64);
    for (k, v) in [
        ("unknown", serde_json::json!(1)),
        ("schema_version", serde_json::json!(2)),
        ("module", serde_json::json!("a.b")),
        ("export", serde_json::json!("__file__")),
        ("signature", serde_json::json!("arbitrary")),
    ] {
        let mut bad = good.clone();
        bad[k] = v;
        std::fs::write(&p, bad.to_string()).unwrap();
        assert!(BindingPlan::read(&p, Platform::Linux, false).is_err());
    }
    std::fs::remove_dir_all(r).unwrap();
}
#[test]
fn snapshot_survives_source_removal_and_rejects_change() {
    let r = root();
    let p = r.join("i.so");
    std::fs::write(&p, elf()).unwrap();
    let input = files::NativeInput::open(&p, Platform::Linux, false).unwrap();
    let target = r.join("s.so");
    let frozen = input.freeze(&target).unwrap();
    assert!(input.freeze(&target).is_err());
    drop(input);
    std::fs::remove_file(p).unwrap();
    frozen.verify().unwrap();
    std::fs::write(&target, b"changed").unwrap();
    assert!(frozen.verify().is_err());
    std::fs::remove_dir_all(r).unwrap();
}
#[test]
fn source_change_and_escape_rejected() {
    let r = root();
    let p = r.join("i.so");
    std::fs::write(&p, elf()).unwrap();
    let input = files::NativeInput::open(&p, Platform::Linux, false).unwrap();
    let mut changed = elf();
    changed[63] = 1;
    std::fs::write(&p, changed).unwrap();
    assert!(input.freeze(&r.join("s.so")).is_err());
    for p in ["../x", "/x", "a/../x", "a\\x", "C:x", ""] {
        assert!(files::checked_relative(&r, p).is_err());
    }
    std::fs::remove_dir_all(r).unwrap();
}
#[test]
fn bounded_input_and_declaration() {
    let r = root();
    let p = r.join("large");
    let f = std::fs::File::create(&p).unwrap();
    f.set_len(INPUT_LIMIT + 1).unwrap();
    drop(f);
    assert!(files::NativeInput::open(&p, Platform::Linux, false).is_err());
    assert!(files::read_small(&p, DECLARATION_LIMIT).is_err());
    std::fs::remove_dir_all(r).unwrap();
}
#[test]
fn library_shape_not_executable() {
    let mut b = elf();
    images::shared(&mut Cursor::new(&b), 64, Platform::Linux).unwrap();
    b[16] = 2;
    assert!(images::shared(&mut Cursor::new(&b), 64, Platform::Linux).is_err());
    let mut mac = vec![0; 32];
    mac[..4].copy_from_slice(&0xfeedfacfu32.to_le_bytes());
    mac[4..8].copy_from_slice(&0x0100000cu32.to_le_bytes());
    mac[12] = 6;
    images::shared(&mut Cursor::new(&mac), 32, Platform::Mac).unwrap();
    mac[12] = 2;
    assert!(images::shared(&mut Cursor::new(&mac), 32, Platform::Mac).is_err());
}
fn archive(obj: &[u8]) -> Vec<u8> {
    let mut b = b"!<arch>\n".to_vec();
    b.extend(
        format!(
            "{:<16}{:<12}{:<6}{:<6}{:<8}{:<10}`\n",
            "file.o/",
            0,
            0,
            0,
            644,
            obj.len()
        )
        .bytes(),
    );
    b.extend(obj);
    if obj.len() % 2 == 1 {
        b.push(b'\n')
    }
    b
}
#[test]
fn archive_count_length_and_import_rejected() {
    let mut obj = elf();
    obj[16] = 1;
    let good = archive(&obj);
    images::archive(&mut Cursor::new(&good), good.len() as u64, Platform::Linux).unwrap();
    for bad in [b"!<thin>\n".to_vec(), good[..good.len() - 1].to_vec()] {
        assert!(
            images::archive(&mut Cursor::new(&bad), bad.len() as u64, Platform::Linux).is_err()
        );
    }
    let import = archive(&[
        0, 0, 255, 255, 0, 0, 100, 134, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
    ]);
    assert!(
        images::archive(
            &mut Cursor::new(&import),
            import.len() as u64,
            Platform::Windows
        )
        .is_err()
    );
    let mut many = b"!<arch>\n".to_vec();
    for _ in 0..4096 {
        many.extend_from_slice(&good[8..]);
    }
    images::archive(&mut Cursor::new(&many), many.len() as u64, Platform::Linux).unwrap();
    many.extend_from_slice(&good[8..]);
    assert!(images::archive(&mut Cursor::new(&many), many.len() as u64, Platform::Linux).is_err());
    let mut neg = good.clone();
    neg[56..66].copy_from_slice(b"-1        ");
    assert!(images::archive(&mut Cursor::new(&neg), neg.len() as u64, Platform::Linux).is_err());
}
#[cfg(unix)]
#[test]
fn symlink_rejected() {
    let r = root();
    std::fs::write(r.join("plain"), elf()).unwrap();
    std::os::unix::fs::symlink(r.join("plain"), r.join("link")).unwrap();
    assert!(files::NativeInput::open(&r.join("link"), Platform::Linux, false).is_err());
    std::fs::remove_dir_all(r).unwrap();
}
