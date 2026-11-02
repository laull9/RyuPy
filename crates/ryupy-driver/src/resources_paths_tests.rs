//! 资源目标的ASCII政策与真实宿主路径分开验证；不把字符串检查冒充文件身份验证。
use super::{Kind, Targets, checked_source, portable};
use std::path::PathBuf;

struct OwnedRoot(PathBuf);
impl Drop for OwnedRoot {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
fn root() -> OwnedRoot {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-resources-中文 & host-{}-{stamp}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    OwnedRoot(path)
}

#[test]
fn portable_target_accepts_ascii_but_rejects_aliases_devices_and_unicode() {
    for valid in [
        "data.txt",
        "pkg/data file & value.txt",
        "_ryupy_resources/v1/modules/pkg/data.bin",
        "COM10",
        "LPT0.txt",
    ] {
        portable(valid).unwrap();
    }
    for invalid in [
        "",
        "/data",
        "data/",
        "a//b",
        ".",
        "a/../b",
        "a/./b",
        "C:/file",
        "a\\b",
        "//server/share",
        "file:stream",
        "a\0b",
        "a\nb",
        "a\u{7f}b",
        "file.",
        "file ",
        "CON",
        "con.txt",
        "PrN.json",
        "AUX",
        "NUL.bin",
        "COM1.dat",
        "lpt9",
        "CONIN$",
        "CONOUT$.txt",
        "CLOCK$",
        "a?b",
        "a*b",
        "a<b",
        "a>b",
        "a|b",
        "a\"b",
        "中文.txt",
        "caf\u{e9}.txt",
        "cafe\u{301}.txt",
    ] {
        assert!(portable(invalid).is_err(), "accepted {invalid:?}");
    }
    assert!(portable(&"x".repeat(256)).is_err());
    assert!(portable(&"a".repeat(255)).is_ok());
    let long = vec!["x".repeat(250); 5].join("/");
    assert!(portable(&long).is_err());
}

#[test]
fn case_and_file_directory_collisions_are_rejected_in_both_orders() {
    for (first, second) in [("Pkg/data.txt", "pkg/data.txt"), ("a", "a/b"), ("a/b", "a")] {
        let mut targets = Targets::default();
        targets.insert(first, Kind::Resource).unwrap();
        assert!(
            targets.insert(second, Kind::Resource).is_err(),
            "{first} -> {second}"
        );
    }
    let mut targets = Targets::default();
    targets.insert("pkg/a.txt", Kind::Resource).unwrap();
    targets.insert("pkg/b.txt", Kind::Resource).unwrap();
    // 同路径去重只允许登记；是否同一实际源必须由上层stage核实。
    targets.insert("pkg/a.txt", Kind::Resource).unwrap();
}

#[test]
fn virtual_anchors_and_reserved_publish_names_cannot_be_resources() {
    for kind in [Kind::Anchor, Kind::Reserved, Kind::Published] {
        let mut targets = Targets::default();
        targets.insert("pkg/module.py", kind).unwrap();
        assert!(targets.insert("pkg/module.py", Kind::Resource).is_err());
        assert!(
            targets
                .insert("pkg/module.py/data", Kind::Resource)
                .is_err()
        );
    }
    let mut targets = Targets::default();
    targets.insert("pkg/module.py", Kind::Anchor).unwrap();
    targets.insert("pkg/module.py", Kind::Anchor).unwrap();
    assert!(targets.insert("pkg", Kind::Resource).is_err());
}

#[test]
fn source_checks_preserve_unicode_host_root_and_reject_nonfiles() {
    let root = root();
    std::fs::create_dir(root.0.join("pkg")).unwrap();
    std::fs::write(root.0.join("pkg/data.txt"), b"data").unwrap();
    assert_eq!(
        checked_source(&root.0, "pkg/data.txt").unwrap(),
        root.0.join("pkg/data.txt")
    );
    assert!(checked_source(&root.0, "pkg").is_err());
    assert!(checked_source(&root.0, "missing").is_err());
    assert!(checked_source(&root.0, "pkg/data.txt/child").is_err());
    assert!(checked_source(&root.0, "../escape").is_err());
    assert!(checked_source(&root.0.join("pkg/data.txt"), "child").is_err());
}

#[test]
fn publish_enumeration_uses_relative_targets_not_unicode_host_components() {
    let root = root();
    std::fs::create_dir(root.0.join("nested")).unwrap();
    std::fs::write(root.0.join("native.bin"), b"native").unwrap();
    std::fs::write(root.0.join("nested/data.bin"), b"data").unwrap();
    let mut targets = Targets::default();
    targets.include_publish(&root.0).unwrap();
    assert!(targets.insert("native.bin", Kind::Resource).is_err());
    assert!(targets.insert("nested", Kind::Resource).is_err());
    assert!(targets.insert("NATIVE.BIN", Kind::Resource).is_err());
}

#[cfg(unix)]
#[test]
fn unix_real_symlink_source_and_publish_entries_are_not_followed() {
    use std::os::unix::fs::symlink;
    let root = root();
    std::fs::write(root.0.join("actual.bin"), b"data").unwrap();
    symlink(root.0.join("actual.bin"), root.0.join("link.bin")).unwrap();
    assert!(checked_source(&root.0, "link.bin").is_err());
    let mut targets = Targets::default();
    assert!(targets.include_publish(&root.0).is_err());
}
