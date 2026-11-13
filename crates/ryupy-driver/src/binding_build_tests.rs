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
    assert_eq!(v["compiler"]["codegen_report_version"], 2);
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
    let error = crate::build_configured(
        &mut Session::default(),
        &opts,
        Some(Path::new("missing-resource.json")),
        Some(&declaration),
    )
    .unwrap_err();
    assert!(error.to_string().contains("--resources 仅可与 --aot 同用"));
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

#[test]
fn multi_export_bundle_has_one_native_input_and_all_actual_references() {
    let (root, opts, declaration) = setup();
    let mut d: Value = serde_json::from_slice(&std::fs::read(&declaration).unwrap()).unwrap();
    for key in ["export", "signature", "symbol"] {
        d.as_object_mut().unwrap().remove(key);
    }
    d["schema_version"] = json!(2);
    d["exports"] = json!([
        {"name":"add", "symbol":"demo_add", "signature":crate::native_binding::SIGNATURE},
        {"name":"subtract", "symbol":"demo_subtract", "signature":crate::native_binding::SIGNATURE}
    ]);
    std::fs::write(&declaration, d.to_string()).unwrap();
    let artifacts =
        crate::build_configured(&mut Session::default(), &opts, None, Some(&declaration)).unwrap();
    let v: Value =
        serde_json::from_slice(&std::fs::read(artifacts.manifest.unwrap()).unwrap()).unwrap();
    assert_eq!(v["schema_version"], 4);
    assert_eq!(v["sources"].as_array().unwrap().len(), 1);
    assert_eq!(v["modules"].as_array().unwrap().len(), 2);
    assert_eq!(v["modules"][1]["exports"], json!(["add", "subtract"]));
    assert_eq!(
        v["modules"][1]["origin"]["binding_ids"],
        json!(["b0", "b1"])
    );
    assert_eq!(v["native_bindings"].as_array().unwrap().len(), 2);
    assert_eq!(v["build_inputs"].as_array().unwrap().len(), 3);
    assert_eq!(v["artifacts"].as_array().unwrap().len(), 4);
    assert_eq!(
        v["native_bindings"][0]["relative_file"],
        v["native_bindings"][1]["relative_file"]
    );
    assert_ne!(
        v["native_bindings"][0]["body"],
        v["native_bindings"][1]["body"]
    );
    let delegates: Vec<_> = v["emitted_delegate_references"]
        .as_array()
        .unwrap()
        .iter()
        .filter(|r| r["role"] == "declared_native_body")
        .collect();
    assert_eq!(delegates.len(), 2);
    std::fs::remove_dir_all(root).unwrap();
}

#[test]
fn resources_keep_real_source_locations_and_binding_origins_for_both_versions() {
    // 编译/发射/冻结均走真实路径；仅最终 native 用占位文件，真实 ILC 由专项 runner 验收。
    for version in [1, 2] {
        for with_data in [false, true] {
            let (root, opts, declaration) = setup();
            std::fs::create_dir(root.join("pkg")).unwrap();
            std::fs::write(root.join("pkg/__init__.py"), "# 真实包\n").unwrap();
            std::fs::write(root.join("pkg/data.txt"), "随包数据\n").unwrap();
            std::fs::write(&opts.input, "import pkg\nimport declared_demo\n").unwrap();
            let resources = root.join("resources.json");
            std::fs::write(
                &resources,
                json!({"schema_version":1,"modules":[{
                    "module":"pkg", "files":if with_data {vec!["data.txt"]} else {vec![]}
                }]})
                .to_string(),
            )
            .unwrap();
            let mut plan =
                BindingPlan::read(&declaration, Platform::current().unwrap(), false).unwrap();
            // 清单结构测试复用已经验证的输入记录；不启动链接器，不把占位 native 当执行证据。
            plan.declaration_version = version;
            plan.aot = true;
            plan.input.digest.role = "native_static_archive".into();
            if version == 2 {
                plan.exports.push(crate::native_binding::BindingExport {
                    name: "subtract".into(),
                    symbol: "demo_subtract".into(),
                    signature: crate::native_binding::SIGNATURE.into(),
                });
            }
            let mut session = Session {
                fixed_binding_build: true,
                ..Session::default()
            };
            let (mut linked, sources, resource_plan) = crate::compile_linked_locations(
                &mut session,
                &opts.input,
                &[],
                true,
                Some(&resources),
                true,
            )
            .unwrap();
            let resource_plan = resource_plan.unwrap();
            let location = &resource_plan.locations[0];
            let source_id = location.module_id;
            assert_eq!(sources[source_id as usize].module_name, "pkg");
            let source_module = linked
                .modules
                .iter()
                .find(|(id, _)| id.into_raw().into_u32() == source_id)
                .unwrap()
                .1;
            assert!(matches!(
                &source_module.location,
                ryupy_mir::ModuleLocation::ExecutableRelative(path) if path == &location.relative_file
            ));
            plan.check_modules(&linked).unwrap();
            let originals = ryupy_codegen_cil::SourceModules::capture(&linked).unwrap();
            let bindings = declare(&mut linked, &plan);
            let emitted = fixed_bindings::emit_source_with_report(
                &linked,
                &ryupy_codegen_cil::EmitOptions {
                    assembly_name: "main".into(),
                    entry_source_path: None,
                },
                &bindings,
                &BindingDeployment {
                    relative_file: plan.platform.relative_file().into(),
                },
                &originals,
            )
            .unwrap();
            let context = BindingManifest::new(
                emitted.report,
                sources,
                emitted.bindings,
                linked.entry.into_raw().into_u32(),
                &emitted.bytes,
                &session,
                &plan,
            )
            .unwrap();
            let dll = root.join("main.dll");
            std::fs::write(&dll, emitted.bytes).unwrap();
            crate::runtime::copy_runtime_to(&root).unwrap();
            let mut inputs =
                crate::manifest::input_digests(&dll, &root.join("RyuPy.Runtime.dll")).unwrap();
            inputs.push(plan.input.digest.clone());
            let staged = resource_plan.stage(&root).unwrap();
            let publish = root.join("publish");
            std::fs::create_dir(&publish).unwrap();
            let native = publish.join("app");
            std::fs::write(&native, b"native placeholder").unwrap();
            staged.publish(&publish).unwrap();
            let mut artifacts = crate::manifest_files::collect(&publish, &native).unwrap();
            staged.mark_artifacts(&mut artifacts).unwrap();
            let base = context
                .render(&plan, Some("10.0.403"), &artifacts, &inputs)
                .unwrap();
            let base: Value = serde_json::from_slice(&base).unwrap();
            let bytes =
                crate::resources::extend_manifest(&serde_json::to_vec(&base).unwrap(), &staged)
                    .unwrap();
            let combined: Value = serde_json::from_slice(&bytes).unwrap();
            assert_eq!(combined["schema_version"], version + 4);
            for key in [
                "native_bindings",
                "modules",
                "sources",
                "build_inputs",
                "emitted_delegate_references",
            ] {
                assert_eq!(combined[key], base[key], "{key}");
            }
            assert_eq!(combined["scope"]["resources_included"], with_data);
            assert_eq!(combined["scope"]["declared_resources_complete"], true);
            assert_eq!(combined["module_locations"][0]["module_id"], source_id);
            assert_eq!(
                combined["resources"].as_array().unwrap().len(),
                usize::from(with_data)
            );
            if with_data {
                assert_eq!(
                    combined["resources"][0]["consumer_module_ids"],
                    json!([source_id])
                );
                assert_eq!(
                    std::fs::read(publish.join(staged.records[0].relative_path.clone())).unwrap(),
                    "随包数据\n".as_bytes()
                );
            }
            assert_ne!(combined["native_bindings"][0]["module_id"], source_id);
            std::fs::remove_dir_all(root).unwrap();
        }
    }
}

#[cfg(all(target_os = "linux", target_arch = "x86_64"))]
#[test]
fn binding_resource_tree_rejects_output_overlap_before_sdk_and_preserves_dist() {
    // 合成archive仅满足读取准入；不执行它，也不将占位旧dist称为Native验证。
    for previous in [false, true] {
        let (root, mut opts, declaration) = setup();
        opts.aot = true;
        opts.out_dir = root.clone();
        let mut object = vec![0u8; 32];
        object[..6].copy_from_slice(b"\x7fELF\x02\x01");
        object[16] = 1;
        object[18] = 62;
        let mut archive = b"!<arch>\n".to_vec();
        archive.extend(
            format!(
                "{:<16}{:<12}{:<6}{:<6}{:<8}{:<10}`\n",
                "file.o/",
                0,
                0,
                0,
                644,
                object.len()
            )
            .bytes(),
        );
        archive.extend(object);
        std::fs::write(root.join("input.a"), archive).unwrap();
        let mut value: Value =
            serde_json::from_slice(&std::fs::read(&declaration).unwrap()).unwrap();
        value["target"]["archive"] = "input.a".into();
        std::fs::write(&declaration, serde_json::to_vec(&value).unwrap()).unwrap();
        let old = previous.then(|| crate::aot_layout::build(&opts, |inner, _| {
            let dll = inner.out_dir.join("main.dll");
            std::fs::write(&dll, b"managed-old")?;
            let publish = inner.out_dir.join("old-publish");
            std::fs::create_dir(&publish)?;
            let native = publish.join("old-native");
            std::fs::write(&native, b"old-native")?;
            use std::os::unix::fs::PermissionsExt;
            std::fs::set_permissions(&native, std::fs::Permissions::from_mode(0o755))?;
            let data = json!({"schema_version":1,"kind":"ryupy-native-aot-artifact","artifacts":[{
                "relative_path":"old-native","role":"native_executable","size":10,
                "sha256":crate::manifest_files::hash_bytes(b"old-native")} ]});
            let manifest = crate::manifest_files::write_atomic(&publish, &serde_json::to_vec(&data)?)?;
            Ok(Artifacts { dll, native_exe: Some(native), manifest: Some(manifest), debug_symbols: None })
        }).unwrap());
        let old_manifest = old
            .as_ref()
            .map(|a| std::fs::read(a.manifest.as_ref().unwrap()).unwrap());
        let resources = root.join("resources.json");
        std::fs::write(&resources, br#"{"schema_version":1,"modules":[{"module":"__main__","trees":[{"directory":".","suffixes":[".json"]}]}]}"#).unwrap();
        let mut session = Session::default();
        let error =
            crate::build_configured(&mut session, &opts, Some(&resources), Some(&declaration))
                .unwrap_err();
        let message = format!("{error:#}");
        assert!(message.contains("生成输出重叠"), "{message}");
        assert!(session.diagnostics.is_empty());
        assert!(!session.fixed_binding_build);
        let attempts: Vec<_> = std::fs::read_dir(root.join("main.build"))
            .unwrap()
            .map(|entry| entry.unwrap().path())
            .filter(|path| path.join("build-error.log").is_file())
            .collect();
        assert_eq!(attempts.len(), 1);
        let files: Vec<_> = std::fs::read_dir(&attempts[0])
            .unwrap()
            .map(|entry| entry.unwrap().file_name())
            .collect();
        assert_eq!(
            files,
            [std::ffi::OsString::from("build-error.log")],
            "不能发射托管字节或启动SDK"
        );
        if let Some(old) = old {
            assert_eq!(
                std::fs::read(old.native_exe.unwrap()).unwrap(),
                b"old-native"
            );
            assert_eq!(
                std::fs::read(old.manifest.unwrap()).unwrap(),
                old_manifest.unwrap()
            );
            assert_eq!(
                std::fs::read_dir(root.join("main.dist")).unwrap().count(),
                2
            );
        } else {
            assert!(!root.join("main.dist").exists());
        }
        std::fs::remove_dir_all(root).unwrap();
    }
}
