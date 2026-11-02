//! 只实际构建的公开__file__槽保对象身份；不扩大其余字符串/整数槽策略。
#[test]
fn runtime_module_file_is_dynamic_without_demoting_unrelated_module_slots() {
    let source = "ordinary = 'hello'\nnumber = 42\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("unit.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty());
    for enabled in [false, true] {
        let (module, errors) = ryupy_hir::lower_module(
            &parsed,
            &ryupy_hir::ModuleInfo {
                resolve_file_at_runtime: enabled,
                name: "pkg.unit".into(),
                package: "pkg".into(),
                file: "unit.py".into(),
            },
            source,
        );
        assert!(errors.is_empty(), "{errors:?}");
        let types = crate::module_vars::infer_module_var_types(&module);
        let ty = |name: &str| {
            let (id, _) = module
                .vars
                .iter()
                .find(|(_, var)| var.name.as_str() == name)
                .unwrap();
            types[id].clone()
        };
        assert_eq!(
            ty("__file__"),
            if enabled {
                crate::Ty::Dynamic
            } else {
                crate::Ty::Str
            }
        );
        assert_eq!(ty("ordinary"), crate::Ty::Str);
        assert_eq!(ty("number"), crate::Ty::Int);
        let (_, errors) = crate::check_module(&module);
        assert!(errors.is_empty(), "{errors:?}");
    }
}
