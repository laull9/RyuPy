//! 公开运行时函数不能靠标注拒Python对象；静态数值helper仍保原参数/返回类型。
use crate::{FuncMode, ModuleTypes, Ty, check_module};
use ryupy_hir::{FuncId, Module};

fn check(source: &str) -> (Module, ModuleTypes) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("module_bindings.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (module, diagnostics) =
        ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = check_module(&module);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    (module, types)
}

fn functions(module: &Module, name: &str) -> Vec<FuncId> {
    module
        .functions
        .iter()
        .filter(|(_, func)| func.name.as_str() == name)
        .map(|(id, _)| id)
        .collect()
}

#[test]
fn module_function_binding_runtime_entries_are_dynamic_even_with_full_annotations() {
    let (module, types) = check(
        "f = 0\ndef f(value: int) -> int:\n    return value\ndef repeated(value: int) -> int:\n    return value\nsaved = repeated\ndef repeated(value: int) -> int:\n    return value\ndef keep(value):\n    return value\n@keep\ndef decorated(value: int) -> int:\n    return value\n",
    );
    for name in ["f", "repeated", "decorated"] {
        for id in functions(&module, name) {
            assert_eq!(types.modes[id], FuncMode::Dynamic);
            assert_eq!(types.sigs[id].params, vec![Ty::Dynamic]);
            assert_eq!(types.sigs[id].ret, Ty::Dynamic);
        }
    }
    assert_eq!(functions(&module, "repeated").len(), 2);
    assert_eq!(types.modes[module.main], FuncMode::Dynamic);
    assert!(
        module
            .vars
            .iter()
            .all(|(id, _)| types.module_vars[id] == Ty::Dynamic),
        "明确承认混合模块全部模块槽装箱，不能声称只有碰撞槽付代价"
    );
}

#[test]
fn module_function_binding_does_not_demote_unrelated_numeric_function_bodies() {
    let (module, types) = check(
        "f = 0\ndef f(value: int) -> int:\n    return value\ndef add(left: int, right: int) -> int:\n    return left + right\ndef numeric_loop(count: int) -> int:\n    total = 0\n    for index in range(count):\n        total = add(total, index)\n    return total\ndef recursive(value: int) -> int:\n    if value <= 1:\n        return 1\n    return value * recursive(value - 1)\n",
    );
    for name in ["add", "numeric_loop", "recursive"] {
        let id = functions(&module, name)[0];
        assert_eq!(types.modes[id], FuncMode::Static, "{name}");
        assert!(types.sigs[id].params.iter().all(|ty| *ty == Ty::Int));
        assert_eq!(types.sigs[id].ret, Ty::Int);
    }
    assert_eq!(types.modes[functions(&module, "f")[0]], FuncMode::Dynamic);
}
