//! 真正stub导出的结构门：类型/位置参数/关键字别名、机器范围和借用均来自同一验证计划。
use super::*;
use crate::native_binding::Platform;

fn plan(root: &crate::test_temp::Root, version: u32, exports: Value) -> BindingPlan {
    let mut header = vec![0; 64];
    header[..7].copy_from_slice(b"\x7fELF\x02\x01\x01");
    header[16] = 3;
    header[18] = 62;
    header[52] = 64;
    std::fs::write(root.join("library.so"), header).unwrap();
    let mut declaration = json!({"schema_version":version,"module":"portable_math","library":"native_math",
        "target":{"rid":"linux-x64","shared":"library.so"},"exports":exports});
    if version == 1 {
        let export = declaration["exports"][0].clone();
        declaration.as_object_mut().unwrap().remove("exports");
        declaration["export"] = export["name"].clone();
        declaration["symbol"] = export["symbol"].clone();
        declaration["signature"] = export["signature"].clone();
    }
    let path = root.join("binding.json");
    std::fs::write(&path, declaration.to_string()).unwrap();
    BindingPlan::read(&path, Platform::Linux, false).unwrap()
}

fn parsed(text: &str) -> ryupy_syntax::ParsedModule {
    let mut source = ryupy_base::SourceDb::default();
    let file = source.add("module.pyi", text);
    let (module, diagnostics) = ryupy_syntax::parse_for_version(
        file,
        text,
        ryupy_syntax::ast::PySourceType::Stub,
        ryupy_syntax::ast::PythonVersion::PY312,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}\n{text}");
    module
}

#[test]
fn legacy_versions_export_actual_fixed_posonly_shape() {
    for version in [1, 2] {
        let root = crate::test_temp::Root::new("binding-stub-legacy");
        let plan = plan(
            &root,
            version,
            json!([{"name":"sum_values","symbol":"add","signature":"cdecl_i32_i32_i64"}]),
        );
        let (text, contract) = render(&plan).unwrap();
        let module = parsed(&text);
        let functions: Vec<_> = module
            .module
            .body
            .iter()
            .filter_map(|s| {
                if let ryupy_syntax::ast::Stmt::FunctionDef(f) = s {
                    Some(f)
                } else {
                    None
                }
            })
            .collect();
        assert_eq!(functions.len(), 1);
        assert_eq!(functions[0].parameters.posonlyargs.len(), 2);
        assert!(functions[0].parameters.args.is_empty());
        assert_eq!(
            contract["exports"][0]["python"]["parameters"][0]["declared_name"],
            "left"
        );
        assert_eq!(
            contract["exports"][0]["machine"]["pinvoke_signature"],
            "static long (int, int)"
        );
        assert_eq!(
            contract["exports"][0]["python"]["parameters"][0]["abi"]["minimum"],
            "-2147483648"
        );
    }
}

#[test]
fn v3_keyword_names_aliases_and_borrowing_remain_parseable_and_auditable() {
    let root = crate::test_temp::Root::new("binding-stub-v3");
    let signature = json!({"calling_convention":"cdecl","parameters":[{"name":"for","type":"utf8","ownership":"borrowed_call","length":"usize"},{"name":"_p0","type":"bytes","ownership":"borrowed_call","length":"usize"},{"name":"__debug__","type":"u64"}],"returns":"bool8"});
    let plan = plan(
        &root,
        3,
        json!([
            {"name":"class","symbol":"read_values","signature":signature},
            {"name":"_ryupy_builtins","symbol":"read_values","signature":signature},
            {"name":"__debug__","symbol":"read_values","signature":signature}
        ]),
    );
    let (text, contract) = render(&plan).unwrap();
    parsed(&text);
    assert!(text.contains("import builtins as _ryupy_builtins_1"));
    assert!(text.contains("Literal[\"class\"]") && text.contains("Callable[["));
    let parameter = &contract["exports"][0]["python"]["parameters"][0];
    assert_eq!(parameter["declared_name"], "for");
    assert_eq!(parameter["stub_name"], "_p0_1");
    assert_eq!(parameter["abi"]["ownership"], "borrowed_call");
    assert_eq!(
        contract["exports"][0]["machine"]["parameters"]
            .as_array()
            .unwrap()
            .len(),
        5
    );
    assert_eq!(
        contract["exports"][0]["same_symbol_exports"]
            .as_array()
            .unwrap()
            .len(),
        2
    );
    assert_eq!(
        contract["exports"][0]["python"]["parameters"][2]["abi"]["maximum"],
        "18446744073709551615"
    );
    let bundle = root.join("bundle");
    std::fs::create_dir(&bundle).unwrap();
    plan.write_interfaces(&bundle).unwrap();
    assert!(bundle.join(plan.interface_paths().0).is_file());
    assert!(
        plan.write_interfaces(&bundle).is_err(),
        "重复生成必须拒绝覆盖"
    );
    assert_eq!(
        std::fs::read_to_string(bundle.join(plan.interface_paths().0)).unwrap(),
        text
    );
    assert!(
        plan.interface_artifact_role("_internal/interfaces/v1/unknown.pyi")
            .is_none()
    );
    assert!(
        plan.interface_artifact_role("_internal/interfaces/v1/portable_math.pyi.backup")
            .is_none()
    );
    assert_eq!(
        contract["exports"][0]["machine"]["parameters"][1]["declared_name"],
        "for"
    );
    assert_eq!(
        contract["exports"][0]["machine"]["parameters"][1]["expansion_index"],
        1
    );
}

#[test]
fn actual_getattr_export_and_keyword_hooks_remain_valid_stub_syntax() {
    let root = crate::test_temp::Root::new("binding-stub-hook");
    let mut plan = plan(
        &root,
        2,
        json!([
            {"name":"__getattr__","symbol":"add","signature":"cdecl_i32_i32_i64"},
            {"name":"class","symbol":"add","signature":"cdecl_i32_i32_i64"}
        ]),
    );
    parsed(&render(&plan).unwrap().0);
    // 路径采用包组件布局，未来放开dotted模块时无需另设导出准入。
    plan.module = "outer.inner".into();
    assert_eq!(
        plan.interface_paths().0,
        "_internal/interfaces/v1/outer/inner.pyi"
    );
}

#[test]
fn all_abi_types_have_python_and_machine_contracts() {
    use AbiType::*;
    for ty in [
        I8, U8, I16, U16, I32, U32, I64, U64, Isize, Usize, F32, F64, Bool8, Utf8, Bytes, Void,
    ] {
        assert!(!python_type(ty, "builtins").is_empty());
        assert!(type_contract(ty).is_object());
        assert_eq!(
            machine_parameters(ty).len(),
            if ty.is_buffer() { 2 } else { 1 }
        );
    }
    assert_eq!(python_type(Void, "builtins"), "None");
}
