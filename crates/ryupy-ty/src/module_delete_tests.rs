//! 删除仅检查绑定，不读取值；纯数值槽保持静态，builtin回退仅影响对应槽。
use crate::{FuncMode, Ty};

fn lower(source: &str) -> ryupy_hir::Module {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("delete.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let (module, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    module
}

#[test]
fn deleting_unknown_or_typed_global_does_not_demote_main() {
    let module = lower("x = 1\ny = 2.0\nflag = False\ndel x, missing\nx = 3\n");
    let (types, diagnostics) = crate::check_module(&module);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert_eq!(types.modes[module.main], FuncMode::Static);
    for (name, expected) in [("x", Ty::Int), ("y", Ty::Float), ("flag", Ty::Bool)] {
        let id = module
            .vars
            .iter()
            .find(|(_, v)| v.name.as_str() == name)
            .unwrap()
            .0;
        assert_eq!(types.module_vars[id], expected);
    }
}

#[test]
fn deleted_builtin_fallback_is_dynamic_before_the_first_inference_round() {
    let module = lower("x = 1\nlen = 2\ndel len\n");
    let types = crate::module_vars::infer_module_var_types(&module);
    for (name, expected) in [("x", Ty::Int), ("len", Ty::Dynamic)] {
        let id = module
            .vars
            .iter()
            .find(|(_, v)| v.name.as_str() == name)
            .unwrap()
            .0;
        assert_eq!(types[id], expected);
    }
}
