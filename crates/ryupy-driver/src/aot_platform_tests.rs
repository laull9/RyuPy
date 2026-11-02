//! 平台/产物头纯测试：所有平台可运行，不调用SDK、不执行生成文件。
use super::*;
use std::io::Cursor;

fn put16(bytes: &mut [u8], offset: usize, value: u16) {
    bytes[offset..offset + 2].copy_from_slice(&value.to_le_bytes());
}
fn put32(bytes: &mut [u8], offset: usize, value: u32) {
    bytes[offset..offset + 4].copy_from_slice(&value.to_le_bytes());
}
fn macho() -> Vec<u8> {
    let mut bytes = vec![0; 32];
    put32(&mut bytes, 0, 0xfeedfacf);
    put32(&mut bytes, 4, 0x0100000c);
    put32(&mut bytes, 12, 2);
    bytes
}
fn pe() -> Vec<u8> {
    let mut bytes = vec![0; 64 + 24 + 240];
    bytes[..2].copy_from_slice(b"MZ");
    put32(&mut bytes, 60, 64);
    bytes[64..68].copy_from_slice(b"PE\0\0");
    put16(&mut bytes, 68, 0x8664);
    put16(&mut bytes, 84, 240);
    put16(&mut bytes, 86, 2);
    put16(&mut bytes, 88, 0x20b);
    put32(&mut bytes, 88 + 108, 16);
    bytes
}
fn check(platform: HostPlatform, bytes: &[u8]) -> Result<()> {
    platform.validate_header(&mut Cursor::new(bytes))
}

#[test]
fn only_explicit_host_tuples_select_rid_and_executable_suffix() {
    for (os, arch, env, platform, rid, name) in [
        (
            "linux",
            "x86_64",
            "gnu",
            HostPlatform::LinuxX64,
            "linux-x64",
            "Host",
        ),
        (
            "macos",
            "aarch64",
            "",
            HostPlatform::MacArm64,
            "osx-arm64",
            "Host",
        ),
        (
            "windows",
            "x86_64",
            "msvc",
            HostPlatform::WindowsX64,
            "win-x64",
            "Host.exe",
        ),
    ] {
        assert_eq!(HostPlatform::from_target(os, arch, env).unwrap(), platform);
        assert_eq!(platform.rid(), rid);
        assert_eq!(platform.executable_name("Host"), name);
    }
    for tuple in [
        ("linux", "aarch64", "gnu"),
        ("linux", "x86_64", "musl"),
        ("macos", "x86_64", ""),
        ("windows", "aarch64", "msvc"),
        ("windows", "x86_64", "gnu"),
        ("unknown", "x86_64", ""),
    ] {
        assert!(HostPlatform::from_target(tuple.0, tuple.1, tuple.2).is_err());
    }
}

#[test]
fn macho_requires_thin_arm64_executable_and_complete_header() {
    let valid = macho();
    assert!(check(HostPlatform::MacArm64, &valid).is_ok());
    for length in 0..32 {
        assert!(check(HostPlatform::MacArm64, &valid[..length]).is_err());
    }
    for (offset, wrong) in [(0, 0xcafebabe), (0, 0xfeedface), (4, 0x01000007), (12, 6)] {
        let mut bytes = valid.clone();
        put32(&mut bytes, offset, wrong);
        assert!(check(HostPlatform::MacArm64, &bytes).is_err());
    }
}

#[test]
fn pe_requires_amd64_native_executable_not_managed_dll_or_pe32() {
    let valid = pe();
    assert!(check(HostPlatform::WindowsX64, &valid).is_ok());
    for (offset, wrong) in [
        (0, 0),
        (64, 0),
        (68, 0xaa64),
        (86, 0x2002),
        (86, 0),
        (88, 0x10b),
    ] {
        let mut bytes = valid.clone();
        put16(&mut bytes, offset, wrong);
        assert!(check(HostPlatform::WindowsX64, &bytes).is_err());
    }
    for offset in [88 + 224, 88 + 228] {
        let mut bytes = valid.clone();
        put32(&mut bytes, offset, 1);
        assert!(check(HostPlatform::WindowsX64, &bytes).is_err());
    }
}

#[test]
fn pe_offsets_lengths_and_directory_counts_fail_without_panics() {
    let valid = pe();
    for length in 0..valid.len() {
        assert!(check(HostPlatform::WindowsX64, &valid[..length]).is_err());
    }
    for offset in [0, 63, 1_048_577, u32::MAX] {
        let mut bytes = valid.clone();
        put32(&mut bytes, 60, offset);
        assert!(check(HostPlatform::WindowsX64, &bytes).is_err());
    }
    for size in [0, 111, 4097, u16::MAX] {
        let mut bytes = valid.clone();
        put16(&mut bytes, 84, size);
        assert!(check(HostPlatform::WindowsX64, &bytes).is_err());
    }
    let mut bytes = valid;
    put32(&mut bytes, 88 + 108, u32::MAX);
    assert!(check(HostPlatform::WindowsX64, &bytes).is_err());
}

#[test]
fn pe_without_clr_directory_is_valid_but_declared_missing_entry_is_not() {
    let mut bytes = pe();
    put16(&mut bytes, 84, 112);
    put32(&mut bytes, 88 + 108, 0);
    bytes.truncate(88 + 112);
    assert!(check(HostPlatform::WindowsX64, &bytes).is_ok());
    put32(&mut bytes, 88 + 108, 15);
    assert!(check(HostPlatform::WindowsX64, &bytes).is_err());
}

#[test]
fn legacy_linux_header_policy_does_not_gain_unreviewed_restrictions() {
    assert!(check(HostPlatform::LinuxX64, &[]).is_ok());
    // 文件非空/普通文件/执行位仍在publish中检查；Linux ELF另由真实smoke审计。
}
