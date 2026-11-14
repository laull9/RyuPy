//! Linux 闭包的结构、拒绝路径与真实 Linux 工具链测试；临时输入全部在仓库外清理。
use super::*;
use std::sync::atomic::{AtomicUsize, Ordering};

struct Workspace(PathBuf);
impl Workspace {
    fn new() -> Self {
        static NEXT: AtomicUsize = AtomicUsize::new(0);
        let path = std::env::temp_dir().join(format!(
            "ryupy-linux-closure-{}-{}",
            std::process::id(),
            NEXT.fetch_add(1, Ordering::Relaxed)
        ));
        std::fs::create_dir_all(&path).unwrap();
        Self(path)
    }
}
impl Drop for Workspace {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}

#[test]
fn linux_origin_edges_must_resolve_inside_frozen_root() {
    let work = Workspace::new();
    let lib = work.0.join("python/lib");
    std::fs::create_dir_all(&lib).unwrap();
    std::fs::write(lib.join("libpython3.12.so.1.0"), "手写目标").unwrap();
    let image = lib.join("libpython3.so");
    let root = work.0.join("python").canonicalize().unwrap();
    assert_eq!(
        origin_target(&image, "../lib/libpython3.12.so.1.0", &root).unwrap(),
        lib.join("libpython3.12.so.1.0").canonicalize().unwrap()
    );
    std::fs::write(work.0.join("outside.so"), "域外目标").unwrap();
    assert!(origin_target(&image, "../../outside.so", &root).is_err());
    assert!(origin_target(&image, "$PLATFORM/library.so", &root).is_err());
}

#[test]
fn linux_license_directory_references_do_not_hide_named_files() {
    let files = license_references("目录 /usr/share/common-licenses/\n 与目录 /usr/share/common-licenses/.\n许可 /usr/share/common-licenses/LGPL-2.1 和 /usr/share/common-licenses/GPL-2.").unwrap();
    assert_eq!(
        files,
        BTreeSet::from(["LGPL-2.1".to_string(), "GPL-2".to_string()])
    );
    assert!(license_references("/usr/share/common-licenses/../private").is_err());
}

#[test]
fn linux_version_requirements_are_scoped_to_library() {
    let mut elf = Elf::default();
    versions("Version definition section '.gnu.version_d' contains 2 entries:\n  Name: XCRYPT_2.0\n  Name: XCRYPT_4.3\nVersion needs section '.gnu.version_r' contains 2 entries:\n File: libcrypt.so.1 Cnt: 1\n Name: XCRYPT_2.0 Flags: none\n File: libc.so.6 Cnt: 1\n Name: GLIBC_2.2.5 Flags: none", &mut elf).unwrap();
    assert!(elf.provides.contains("XCRYPT_4.3"));
    assert_eq!(
        elf.requires["libcrypt.so.1"],
        BTreeSet::from(["XCRYPT_2.0".into()])
    );
    assert!(!elf.requires["libcrypt.so.1"].contains("GLIBC_2.2.5"));
    for name in ["", "../lib.so", "/usr/lib/lib.so", "lib.so\n", "a:b"] {
        assert!(library_name(name).is_err());
    }
    assert!(bracket("(NEEDED) [libcrypt.so.1]").is_ok());
    assert!(bracket("(NEEDED) [../libcrypt.so.1]").is_err());
}

#[test]
fn linux_cache_never_selects_i386_or_other_sonames() {
    let input = "libcrypt.so.1 (libc6,x86-64) => /lib/x86_64-linux-gnu/libcrypt.so.1\nlibcrypt.so.1 (libc6) => /lib/i386-linux-gnu/libcrypt.so.1\nlibcrypt.so.2 (libc6,x86-64) => /opt/libcrypt.so.2";
    assert_eq!(
        cache_candidates(input, "libcrypt.so.1").unwrap(),
        [PathBuf::from("/lib/x86_64-linux-gnu/libcrypt.so.1")]
    );
    assert!(
        cache_candidates(
            "libcrypt.so.1 (libc6,x86-64) => relative/lib.so",
            "libcrypt.so.1"
        )
        .is_err()
    );
}

#[test]
fn linux_payload_duplicates_and_missing_license_fail_closed() {
    let work = Workspace::new();
    let a = work.0.join("a/libcrypt.so.1");
    let b = work.0.join("b/libcrypt.so.1");
    for path in [&a, &b] {
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, b"handwritten distinct library input").unwrap();
    }
    assert_eq!(
        payload_match(std::slice::from_ref(&a), "libcrypt.so.1").unwrap(),
        Some(a.clone())
    );
    assert!(payload_match(&[a, b], "libcrypt.so.1").is_err());
    assert!(
        freeze_license("libcrypt1:amd64", "", &work.0)
            .unwrap_err()
            .to_string()
            .contains("许可证")
    );
    assert!(collect_system("libunknown.so.1", &work.0).is_err());
}

#[cfg(unix)]
#[test]
fn linux_payload_symlink_alias_is_one_identity() {
    let work = Workspace::new();
    let a = work.0.join("a/libcrypt.so.1");
    let b = work.0.join("b/libcrypt.so.1");
    std::fs::create_dir_all(a.parent().unwrap()).unwrap();
    std::fs::create_dir_all(b.parent().unwrap()).unwrap();
    std::fs::write(&a, b"handwritten input").unwrap();
    std::os::unix::fs::symlink(&a, &b).unwrap();
    assert!(payload_match(&[a, b], "libcrypt.so.1").unwrap().is_some());
}

/// CI Linux 上直接走系统缓存、dpkg、许可冻结和 patchelf，缺工具时失败而非跳过。
#[cfg(target_os = "linux")]
#[test]
fn linux_real_libcrypt_is_frozen_with_license_and_rpath() {
    let work = Workspace::new();
    std::fs::create_dir_all(work.0.join("python/lib")).unwrap();
    let source = work.0.join("consumer.c");
    std::fs::write(&source, "extern char *crypt(const char *, const char *);\nchar *probe(void) { return crypt(\"test\", \"xx\"); }\n").unwrap();
    let shim = work.0.join("shim.so");
    command(
        "cc",
        &[
            "-shared",
            "-fPIC",
            path_text(&source).unwrap(),
            "-Wl,--no-as-needed",
            "-l:libcrypt.so.1",
            "-o",
            path_text(&shim).unwrap(),
        ],
    )
    .unwrap();
    // 模拟PBS libpython3.so的$ORIGIN加载边，并确保真实工具走规范化重定位。
    let forwarding = work.0.join("python/lib/forward.so");
    std::fs::copy(&shim, &forwarding).unwrap();
    command(
        "patchelf",
        &[
            "--replace-needed",
            "libcrypt.so.1",
            "$ORIGIN/../../shim.so",
            path_text(&forwarding).unwrap(),
        ],
    )
    .unwrap();
    // 此边越过python根，产品必须拒绝；随后移除手写负例再验证真实系统库冻结。
    assert!(relocate(&work.0, "shim.so", &["libc.so.6", "ld-linux-x86-64.so.2"]).is_err());
    std::fs::remove_file(&forwarding).unwrap();
    let core = ["libc.so.6", "ld-linux-x86-64.so.2"];
    relocate(&work.0, "shim.so", &core).unwrap();
    let report: Value =
        serde_json::from_slice(&std::fs::read(work.0.join("python/native-sources.json")).unwrap())
            .unwrap();
    let lib = &report["libraries"][0];
    assert_eq!(lib["soname"], "libcrypt.so.1");
    assert!(!lib["version"].as_str().unwrap().is_empty());
    for license in lib["licenses"].as_array().unwrap() {
        assert!(
            work.0
                .join("python")
                .join(license.as_str().unwrap())
                .is_file()
        );
    }
    let target = work.0.join("python/lib/libcrypt.so.1");
    assert!(target.is_file());
    assert_eq!(
        inspect(&target).unwrap().soname.as_deref(),
        Some("libcrypt.so.1")
    );
    assert!(
        command("patchelf", &["--print-rpath", path_text(&shim).unwrap()])
            .unwrap()
            .contains("$ORIGIN/python/lib")
    );
    // 冻结库错架构与版本不满足必须拒绝，不能回到系统缓存补救已存在载荷。
    let mut bytes = std::fs::read(&target).unwrap();
    bytes[18..20].copy_from_slice(&183u16.to_le_bytes());
    std::fs::write(&target, bytes).unwrap();
    assert!(relocate(&work.0, "shim.so", &core).is_err());
}

/// 真实zlib加载边使用同一系统包冻结流程，许可证和rpath必须进入产品。
#[cfg(target_os = "linux")]
#[test]
fn linux_real_zlib_is_frozen_with_license_and_rpath() {
    let work = Workspace::new();
    std::fs::create_dir_all(work.0.join("python/lib")).unwrap();
    let source = work.0.join("consumer.c");
    std::fs::write(&source, "extern const char *zlibVersion(void);\nconst char *probe(void) { return zlibVersion(); }\n").unwrap();
    let shim = work.0.join("shim.so");
    command(
        "cc",
        &[
            "-shared",
            "-fPIC",
            path_text(&source).unwrap(),
            "-Wl,--no-as-needed",
            "-l:libz.so.1",
            "-o",
            path_text(&shim).unwrap(),
        ],
    )
    .unwrap();
    relocate(&work.0, "shim.so", &["libc.so.6", "ld-linux-x86-64.so.2"]).unwrap();
    let report: Value =
        serde_json::from_slice(&std::fs::read(work.0.join("python/native-sources.json")).unwrap())
            .unwrap();
    let library = report["libraries"]
        .as_array()
        .unwrap()
        .iter()
        .find(|entry| entry["soname"] == "libz.so.1")
        .unwrap();
    assert!(work.0.join("python/lib/libz.so.1").is_file());
    assert!(library["package"].as_str().unwrap().starts_with("zlib1g"));
    assert!(!library["version"].as_str().unwrap().is_empty());
    for license in library["licenses"].as_array().unwrap() {
        assert!(
            work.0
                .join("python")
                .join(license.as_str().unwrap())
                .is_file()
        );
    }
    assert!(
        command("patchelf", &["--print-rpath", path_text(&shim).unwrap()])
            .unwrap()
            .contains("$ORIGIN/python/lib")
    );
}

/// 子测试进程独占 PATH，避免并行测试期间修改主进程环境；工具只模拟静态输出。
#[cfg(unix)]
#[test]
fn linux_mock_tools_audit_transitive_closure() {
    use std::os::unix::fs::PermissionsExt;
    const ROOT_ENV: &str = "RYUPY_LINUX_CLOSURE_TEST_ROOT";
    if let Ok(root) = std::env::var(ROOT_ENV) {
        let scenario = std::env::var("RYUPY_LINUX_CLOSURE_TEST_CASE").unwrap();
        let result = relocate(Path::new(&root), "shim.so", &["libc.so.6"]);
        if scenario == "ok" {
            result.unwrap();
            let rpath = std::fs::read_to_string(Path::new(&root).join("shim.so.rpath")).unwrap();
            assert!(rpath.contains("$ORIGIN/python/lib"));
            assert!(
                Path::new(&root)
                    .join("python/native-sources.json")
                    .is_file()
            );
        } else {
            let error = format!("{:#}", result.unwrap_err());
            let expected = match scenario.as_str() {
                "version" => "符号版本不满足",
                "missing" => "显式 payload",
                "arch" => "架构错配",
                "duplicate" => "同名库歧义",
                _ => panic!("未知测试场景"),
            };
            assert!(error.contains(expected), "{scenario}: {error}");
        }
        return;
    }
    for scenario in ["ok", "version", "missing", "arch", "duplicate"] {
        let work = Workspace::new();
        let bin = work.0.join("bin");
        std::fs::create_dir_all(&bin).unwrap();
        std::fs::create_dir_all(work.0.join("python/lib")).unwrap();
        let mut elf = [0u8; 64];
        elf[..4].copy_from_slice(b"\x7fELF");
        elf[4] = 2;
        elf[5] = 1;
        elf[18] = 62;
        std::fs::write(work.0.join("shim.so"), elf).unwrap();
        if scenario != "missing" {
            if scenario == "arch" {
                elf[18] = 183;
            }
            std::fs::write(work.0.join("python/lib/libhelper.so"), elf).unwrap();
        }
        if scenario == "duplicate" {
            std::fs::create_dir_all(work.0.join("python/other")).unwrap();
            std::fs::write(work.0.join("python/other/libhelper.so"), elf).unwrap();
        }
        let scripts = [
            (
                "readelf",
                r##"#!/bin/sh
case "$1" in
-h) exit 0;;
-d)
case "$2" in
*/shim.so) echo '(NEEDED) [libhelper.so]';;
*) echo '(SONAME) [libhelper.so]'; echo '(NEEDED) [libc.so.6]';;
esac;;
-V)
case "$2" in
*/shim.so)
echo "Version needs section '.gnu.version_r' contains 1 entry:"
echo ' File: libhelper.so Cnt: 1'
if [ "$RYUPY_LINUX_CLOSURE_TEST_CASE" = version ]; then echo ' Name: ABI_2'; else echo ' Name: ABI_1'; fi;;
*) echo "Version definition section '.gnu.version_d' contains 1 entry:"; echo ' Name: ABI_1';;
esac;;
*) exit 1;;
esac
"##,
            ),
            (
                "patchelf",
                r##"#!/bin/sh
case "$1" in
--version) echo 'test patchelf';;
--set-rpath) echo "$2" > "$3.rpath";;
--print-rpath) /bin/cat "$2.rpath";;
*) exit 1;;
esac
"##,
            ),
        ];
        for (name, text) in scripts {
            let script = bin.join(name);
            std::fs::write(&script, text).unwrap();
            std::fs::set_permissions(&script, std::fs::Permissions::from_mode(0o755)).unwrap();
        }
        let result = Command::new(std::env::current_exe().unwrap())
            .args([
                "--exact",
                std::thread::current().name().unwrap(),
                "--nocapture",
            ])
            .env("PATH", &bin)
            .env(ROOT_ENV, &work.0)
            .env("RYUPY_LINUX_CLOSURE_TEST_CASE", scenario)
            .output()
            .unwrap();
        assert!(
            result.status.success(),
            "{scenario}: {} {}",
            String::from_utf8_lossy(&result.stdout),
            String::from_utf8_lossy(&result.stderr)
        );
    }
}
