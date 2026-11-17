//! 仅由实际native父模块注册的macOS生产重定位门；不能随fake Linux RID的源码include重复注册。
//! 保留真实clang/codesign/loader与原断言，纯结构和签名工具测试仍留在各自独立模块。
use super::platform_relocate::relocate_macos_with_components;
use std::path::{Path, PathBuf};
use std::process::Command;

struct Root(PathBuf);
impl Root {
    fn new() -> Self {
        let stamp = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos();
        let path = std::env::temp_dir().join(format!(
            "ryupy-native-production-{}-{stamp}",
            std::process::id()
        ));
        std::fs::create_dir(&path).unwrap();
        Self(path.canonicalize().unwrap())
    }
}
impl Drop for Root {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.0);
    }
}
fn run(args: &[&std::ffi::OsStr]) {
    let output = Command::new(args[0]).args(&args[1..]).output().unwrap();
    assert!(
        output.status.success(),
        "{}",
        String::from_utf8_lossy(&output.stderr)
    );
}

#[test]
fn real_native_closure_preserves_valid_images_and_isolates_duplicate_ids() {
    use std::process::Command;
    let f = Root::new();
    let root = f.0.join("product");
    let inputs =
        Path::new(env!("CARGO_MANIFEST_DIR")).join("../../tests/general_projects/macho_relocation");
    let generated = Command::new("/usr/bin/python3")
        .args(["-B", "-c", "import sys,shutil; from pathlib import Path; sys.path.insert(0,sys.argv[1]); from fixtures import prepare; root=Path(sys.argv[2]); loader,rows=prepare(root/'python',fat=True); shutil.move(loader,root/'loader')"])
        .arg(inputs).arg(&root).output().unwrap();
    assert!(
        generated.status.success(),
        "{}",
        String::from_utf8_lossy(&generated.stderr)
    );
    // 另一个未改加载边的真实映像主动移除签名，必须由平台验签失败触发重签。
    let unsigned = root.join("python/independent/copper/consumer.dylib");
    assert!(
        Command::new("codesign")
            .arg("--remove-signature")
            .arg(&unsigned)
            .status()
            .unwrap()
            .success()
    );
    let audit =
        relocate_macos_with_components(&root, "loader", &["/usr/lib/", "/System/Library/"], &[])
            .unwrap();
    let files = audit["files"].as_array().unwrap();
    let independent: Vec<_> = files
        .iter()
        .filter(|row| row["path"].as_str().unwrap().contains("/independent/"))
        .collect();
    assert_eq!(independent.len(), 4);
    assert_eq!(
        independent
            .iter()
            .filter(|row| row["signature"] == "preserved_valid_original")
            .count(),
        3,
        "{independent:?}"
    );
    assert!(
        independent
            .iter()
            .any(|row| row["action"] == "selected_target_slice"
                && row["signature"] == "preserved_valid_original")
    );
    assert!(
        Command::new("codesign")
            .args(["--verify", "--strict"])
            .arg(&unsigned)
            .status()
            .unwrap()
            .success()
    );
    let duplicates: Vec<_> = files
        .iter()
        .filter(|row| {
            row["path"].as_str().unwrap().contains("/duplicate/")
                && row["path"].as_str().unwrap().ends_with("consumer.dylib")
        })
        .collect();
    assert_eq!(duplicates.len(), 2);
    assert!(
        duplicates
            .iter()
            .all(|row| row["rewritten_dependency_edges"] == 1)
    );
    // 构建根实际搬迁后，在同一进程同时持有两个版本，检验动态加载器最终选择。
    let moved = f.0.join("已搬迁 产品");
    std::fs::rename(&root, &moved).unwrap();
    for (paths, expected) in [
        (
            vec![
                "python/independent/copper/consumer.dylib",
                "python/independent/quartz/nested/consumer.dylib",
            ],
            "11\n29\n",
        ),
        (
            vec![
                "python/duplicate/first/consumer.dylib",
                "python/duplicate/second/deep/consumer.dylib",
            ],
            "41\n83\n",
        ),
    ] {
        let output = Command::new(moved.join("loader"))
            .args(paths.iter().map(|path| moved.join(path)))
            .env_remove("DYLD_LIBRARY_PATH")
            .env_remove("DYLD_FALLBACK_LIBRARY_PATH")
            .current_dir(&f.0)
            .output()
            .unwrap();
        assert!(
            output.status.success(),
            "{}",
            String::from_utf8_lossy(&output.stderr)
        );
        assert_eq!(String::from_utf8(output.stdout).unwrap(), expected);
    }
}

#[test]
fn all_thin_production_relocation_keeps_signed_identity_runtime_and_entitlements() {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = Root(std::env::temp_dir().join(format!(
        "ryupy-thin-signature-{}-{stamp}",
        std::process::id()
    )));
    let python = root.0.join("python");
    std::fs::create_dir_all(&python).unwrap();
    let source = root.0.join("library.c");
    std::fs::write(&source, "int fixture(void) { return 7; }\n").unwrap();
    let library = python.join("libfixture.dylib");
    run(&[
        "clang".as_ref(),
        "-arch".as_ref(),
        "arm64".as_ref(),
        "-dynamiclib".as_ref(),
        source.as_os_str(),
        "-Wl,-install_name,/build/libfixture.dylib".as_ref(),
        "-o".as_ref(),
        library.as_os_str(),
    ]);
    std::fs::write(
        &source,
        "extern int fixture(void); int main(void) { return fixture()==7 ? 0 : 1; }\n",
    )
    .unwrap();
    let entry = root.0.join("entry");
    run(&[
        "clang".as_ref(),
        "-arch".as_ref(),
        "arm64".as_ref(),
        source.as_os_str(),
        library.as_os_str(),
        "-o".as_ref(),
        entry.as_os_str(),
    ]);
    let plist = root.0.join("original.plist");
    run(&["/usr/bin/python3".as_ref(), "-c".as_ref(),
        "import plistlib,sys; plistlib.dump({'org.ryupy.fixture':{'values':[1,2]},'com.apple.security.cs.disable-library-validation':True},open(sys.argv[1],'wb'))".as_ref(), plist.as_os_str()]);
    // 源签名由平台按自身版本选择工具选项；生产函数随后独立决定宿主能力。
    run(&["/usr/bin/python3".as_ref(), "-c".as_ref(),
        "import subprocess,platform,sys; cmd=['codesign','--force','--sign','-','--identifier','org.ryupy.thin.fixture','--options','runtime','--entitlements',sys.argv[2]]; cmd+=['--force-library-entitlements'] if int(platform.mac_ver()[0].split('.')[0])>=15 else []; subprocess.run(cmd+[sys.argv[1]],check=True)".as_ref(), library.as_os_str(), plist.as_os_str()]);
    let audit =
        relocate_macos_with_components(&root.0, "entry", &["/usr/lib/", "/System/Library/"], &[])
            .unwrap();
    let files = audit["files"].as_array().unwrap();
    assert!(files.iter().all(|row| row["action"] == "preserved"));
    assert!(
        files
            .iter()
            .any(|row| row["rewritten_dependency_edges"] == 1)
    );
    assert!(files.iter().any(
        |row| row["path"] == "python/libfixture.dylib" && row["load_commands_changed"] == true
    ));
    run(&[
        "codesign".as_ref(),
        "--verify".as_ref(),
        "--strict".as_ref(),
        library.as_os_str(),
    ]);
    run(&["/usr/bin/python3".as_ref(), "-c".as_ref(),
        "import plistlib,subprocess,sys; actual=subprocess.check_output(['codesign','-d','--entitlements','-','--xml',sys.argv[1]],stderr=subprocess.DEVNULL); assert plistlib.loads(actual)==plistlib.load(open(sys.argv[2],'rb')); metadata=subprocess.run(['codesign','-dvv',sys.argv[1]],capture_output=True,text=True,check=True).stderr; assert 'Identifier=org.ryupy.thin.fixture' in metadata and '(adhoc,runtime)' in metadata".as_ref(), library.as_os_str(), plist.as_os_str()]);
    run(&[entry.as_os_str()]);
    assert!(std::fs::read_dir(&python).unwrap().all(|item| {
        !item
            .unwrap()
            .file_name()
            .to_string_lossy()
            .starts_with(".ryupy-signature-")
    }));
}
