//! 真实 driver 编译/部署结构回归，合成库头不执行，不冒充真实 native 成功。
use super::*;
use serde_json::{Value, json};
fn setup() -> (PathBuf, BuildOptions, PathBuf) {
    let root = owned_root(&std::env::temp_dir()).unwrap();
    let input = root.join("main.py");
    std::fs::write(&input, "import declared_demo\nprint('ready')\n").unwrap();
    let platform = Platform::current().unwrap();
    let mut h = vec![0u8; 512];
    match platform {
        Platform::Linux => {
            h[..7].copy_from_slice(b"\x7fELF\x02\x01\x01");
            h[16] = 3;
            h[18] = 62;
            h[52] = 64;
        }
        Platform::Mac => {
            h[..4].copy_from_slice(&0xfeedfacfu32.to_le_bytes());
            h[4..8].copy_from_slice(&0x0100000cu32.to_le_bytes());
            h[12] = 6;
        }
        Platform::Windows => {
            h[..2].copy_from_slice(b"MZ");
            h[60] = 64;
            h[64..68].copy_from_slice(b"PE\0\0");
            h[68..70].copy_from_slice(&0x8664u16.to_le_bytes());
            h[84..86].copy_from_slice(&240u16.to_le_bytes());
            h[86..88].copy_from_slice(&0x2000u16.to_le_bytes());
            h[88..90].copy_from_slice(&0x20bu16.to_le_bytes());
            h[196] = 16;
        }
    }
    std::fs::write(root.join("input.bin"), h).unwrap();
    let declaration = root.join("binding.json");
    std::fs::write(&declaration,serde_json::to_vec(&json!({"schema_version":1,"module":"declared_demo","export":"add","signature":"cdecl_i32_i32_i64","library":"ryupy_cabi_probe","symbol":"ryupy_demo_add_i32","target":{"rid":platform.rid(),"shared":"input.bin"}})).unwrap()).unwrap();
    let opts = BuildOptions {
        input,
        out_dir: root.join("out"),
        aot: false,
        search_paths: vec![],
    };
    (root, opts, declaration)
}
#[test]
fn jit_bundle_owned_and_actual_source_schema() {
    let (root, opts, declaration) = setup();
    let first =
        crate::build_configured(&mut Session::default(), &opts, None, Some(&declaration)).unwrap();
    let original = std::fs::read(first.manifest.as_ref().unwrap()).unwrap();
    let second =
        crate::build_configured(&mut Session::default(), &opts, None, Some(&declaration)).unwrap();
    assert_ne!(first.dll, second.dll);
    assert_eq!(std::fs::read(first.manifest.unwrap()).unwrap(), original);
    assert!(first.native_exe.is_none());
    let v: Value = serde_json::from_slice(&original).unwrap();
    assert_eq!(v["schema_version"], 3);
    assert_eq!(v["sources"].as_array().unwrap().len(), 1);
    assert_eq!(v["modules"].as_array().unwrap().len(), 2);
    assert_eq!(v["modules"][1]["origin"]["kind"], "declared_native_binding");
    assert!(v["toolchain"]["sdk_version"].is_null());
    assert_eq!(
        v["native_bindings"][0]["pinvoke"]["calling_convention"],
        "Cdecl"
    );
    assert_eq!(v["build_inputs"].as_array().unwrap().len(), 3);
    assert_eq!(v["artifacts"].as_array().unwrap().len(), 4);
    std::fs::remove_dir_all(root).unwrap();
}
#[test]
fn combinations_and_source_collision_fail_before_writes() {
    let (root, opts, declaration) = setup();
    assert!(
        crate::build_configured(
            &mut Session::default(),
            &opts,
            Some(Path::new("missing-resource.json")),
            Some(&declaration)
        )
        .is_err()
    );
    assert!(!opts.out_dir.exists());
    std::fs::write(root.join("declared_demo.py"), "x = 1\n").unwrap();
    let e = crate::build_configured(&mut Session::default(), &opts, None, Some(&declaration))
        .unwrap_err();
    assert!(e.to_string().contains("冲突"), "{e:?}");
    assert!(!opts.out_dir.exists());
    std::fs::remove_dir_all(root).unwrap();
}
#[test]
fn portable_output_keeps_unicode_and_rejects_device_alias() {
    for n in [
        "NUL",
        "con.foo",
        "COM1",
        "LPT²",
        "CON .foo",
        "CLOCK$",
        "trailing.",
        "trailing ",
        "bad?name",
    ] {
        assert!(validate_name(n).is_err(), "{n}");
    }
    for n in ["中文 & spaces", "ordinary.foo", "COM10"] {
        validate_name(n).unwrap();
    }
}
