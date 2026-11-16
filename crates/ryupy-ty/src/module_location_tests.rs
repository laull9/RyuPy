//! 合成模块元数据使用对象槽；实际构建的文件槽沿原Dynamic路径，普通字面量保精确类型。
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
                instance_capabilities: Default::default(),
                instance_globals: false,
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
                crate::Ty::OpaqueStr
            }
        );
        assert_eq!(ty("__name__"), crate::Ty::OpaqueStr);
        assert_eq!(ty("__package__"), crate::Ty::OpaqueStr);
        assert_eq!(ty("ordinary"), crate::Ty::Str);
        assert_eq!(ty("number"), crate::Ty::Int);
        let (final_types, errors) = crate::check_module(&module);
        assert!(errors.is_empty(), "{errors:?}");
        // 前置提示不是最终槽；合成元数据不得触发整main降级而抹掉其它槽的精确类型。
        assert_eq!(final_types.mode(module.main), crate::FuncMode::Static);
        for (id, var) in module.vars.iter() {
            assert_eq!(final_types.module_vars[id], types[id], "{}", var.name);
        }
    }
}
