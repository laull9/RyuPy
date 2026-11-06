//! 实例模块不能从常量赋值推断可逃逸字典中的静态槽位。
use crate::{FuncMode, Ty};

#[test]
fn instance_globals_are_dynamic_before_any_inference() {
    let source = "number = 42\ntext = 'hello'\ncopy = number\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("instance.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    for instance_globals in [false, true] {
        let (module, diagnostics) = ryupy_hir::lower_module(
            &parsed,
            &ryupy_hir::ModuleInfo {
                instance_globals,
                ..ryupy_hir::ModuleInfo::default()
            },
            source,
        );
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        let initial = crate::initial_mode::initial_mode(&module, &module.functions[module.main]);
        assert_eq!(
            initial,
            if instance_globals {
                FuncMode::Dynamic
            } else {
                FuncMode::Static
            }
        );
        let slots = crate::module_vars::infer_module_var_types(&module);
        if instance_globals {
            assert!(module.vars.iter().all(|(id, _)| slots[id] == Ty::Dynamic));
        } else {
            let (number, _) = module
                .vars
                .iter()
                .find(|(_, var)| var.name.as_str() == "number")
                .unwrap();
            assert_eq!(slots[number], Ty::Int);
        }
        let (types, diagnostics) = crate::check_module(&module);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        if instance_globals {
            assert!(
                types
                    .modes
                    .iter()
                    .all(|(_, mode)| *mode == FuncMode::Dynamic)
            );
            assert!(types.module_vars.iter().all(|(_, ty)| *ty == Ty::Dynamic));
        }
    }
}
