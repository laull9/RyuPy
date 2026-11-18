//! 真实签名授权回归：强制重签而非保留原签名，完整plist语义由平台工具与plistlib回读。
use super::*;

struct Root(PathBuf);
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
fn changed_dylib_and_forced_bundle_resign_restore_complete_entitlements() {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root =
        Root(std::env::temp_dir().join(format!("ryupy-signature-{}-{stamp}", std::process::id())));
    std::fs::create_dir(&root.0).unwrap();
    let source = root.0.join("source.c");
    std::fs::write(&source, "int fixture(void) { return 7; }\n").unwrap();
    let plist = root.0.join("source-entitlements.plist");
    run(&["/usr/bin/python3".as_ref(), "-c".as_ref(),
        "import plistlib,sys; plistlib.dump({'org.ryupy.fixture':{'items':['one','two'], 'number':7}, 'com.apple.security.cs.disable-library-validation':True},open(sys.argv[1],'wb'))".as_ref(), plist.as_os_str()]);
    let version = Command::new("/usr/bin/sw_vers")
        .arg("-productVersion")
        .output()
        .unwrap();
    let force = String::from_utf8(version.stdout)
        .unwrap()
        .split('.')
        .next()
        .unwrap()
        .parse::<u32>()
        .unwrap()
        >= 15;
    for kind in ["dylib", "bundle"] {
        let directory = root.0.join(kind);
        std::fs::create_dir(&directory).unwrap();
        let image = directory.join("sample.so");
        run(&[
            "clang".as_ref(),
            "-arch".as_ref(),
            "arm64".as_ref(),
            if kind == "dylib" {
                "-dynamiclib".as_ref()
            } else {
                "-bundle".as_ref()
            },
            source.as_os_str(),
            "-o".as_ref(),
            image.as_os_str(),
        ]);
        let mut signing = vec![
            "codesign".as_ref(),
            "--force".as_ref(),
            "--sign".as_ref(),
            "-".as_ref(),
            "--identifier".as_ref(),
            "org.ryupy.signature.fixture".as_ref(),
            "--options".as_ref(),
            "runtime".as_ref(),
            "--entitlements".as_ref(),
            plist.as_os_str(),
        ];
        if force {
            signing.push("--force-library-entitlements".as_ref());
        }
        signing.push(image.as_os_str());
        run(&signing);
        let before = snapshot(&image).unwrap();
        assert!(before.xml.as_ref().is_some_and(|xml| !xml.is_empty()));
        if kind == "dylib" {
            run(&[
                "install_name_tool".as_ref(),
                "-id".as_ref(),
                "@rpath/relocated.so".as_ref(),
                image.as_os_str(),
            ]);
        }
        assert_eq!(
            finish(&image, false, true, force, &before).unwrap(),
            "adhoc_resign_preserving_entitlements"
        );
        run(&[
            "codesign".as_ref(),
            "--verify".as_ref(),
            "--strict".as_ref(),
            image.as_os_str(),
        ]);
        run(&["/usr/bin/python3".as_ref(), "-c".as_ref(),
            "import plistlib,subprocess,sys; actual=subprocess.check_output(['codesign','-d','--entitlements','-','--xml',sys.argv[1]],stderr=subprocess.DEVNULL); assert plistlib.loads(actual)==plistlib.load(open(sys.argv[2],'rb'))".as_ref(),
            image.as_os_str(), plist.as_os_str()]);
        let metadata = Command::new("codesign")
            .arg("-dvv")
            .arg(&image)
            .output()
            .unwrap();
        let metadata = String::from_utf8(metadata.stderr).unwrap();
        assert!(metadata.contains("Identifier=org.ryupy.signature.fixture"));
        assert!(metadata.contains("(adhoc,runtime)"));
        assert!(std::fs::read_dir(&directory).unwrap().all(|item| {
            !item
                .unwrap()
                .file_name()
                .to_string_lossy()
                .starts_with(".ryupy-signature-")
        }));
    }
}

#[test]
fn failed_signing_also_removes_owned_entitlement_file() {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = Root(std::env::temp_dir().join(format!(
        "ryupy-signature-failure-{}-{stamp}",
        std::process::id()
    )));
    std::fs::create_dir(&root.0).unwrap();
    let missing = root.0.join("missing-image");
    let original = Entitlements {
        xml: Some(b"<?xml version=\"1.0\"?><plist version=\"1.0\"><dict/></plist>".to_vec()),
        signed: true,
    };
    assert!(finish(&missing, false, false, false, &original).is_err());
    assert!(std::fs::read_dir(&root.0).unwrap().next().is_none());
}
