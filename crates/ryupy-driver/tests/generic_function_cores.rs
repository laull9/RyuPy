//! 双核心真实源码发射门；锁原typed CIL/完整实参thunk，冷Binder分支及唯一函数构造。
use ryupy_mir::*;
const SOURCE: &str = "def leaf(value: int) -> int:\n    return value\ndef forward(value: int) -> int:\n    return leaf(value)\nprint(forward(3))\n";
fn linked(generic: bool) -> LinkedProgram {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("generic.py", SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            file: "generic.py".into(),
            ..Default::default()
        },
        SOURCE,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (mut program, exports, diagnostics) = if generic {
        build::build_program_with_generic(&hir, &types)
    } else {
        build_program(&hir, &types)
    };
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
    link(
        vec![(
            program,
            UnitMeta {
                instance_capabilities: Default::default(),
                execution: ModuleExecutionKind::LegacySingleton,
                environment_reason: "generic-core-test".into(),
                location: ModuleLocation::LegacyDiagnostic,
                file_backed_by_global: true,
                is_package: false,
                protected_exports: vec![],
                name: "__main__".into(),
                package: String::new(),
                file: "generic.py".into(),
                exports,
            },
        )],
        0,
    )
}
fn method<'a>(cil: &'a str, name: &str) -> &'a str {
    let mut candidates = cil
        .split("    .method ")
        .skip(1)
        .filter(|m| m.lines().next().unwrap().contains(&format!(" {name}(")));
    let result = candidates
        .next()
        .unwrap_or_else(|| panic!("missing {name}: {cil}"));
    assert!(candidates.next().is_none());
    result
}
fn dump(program: &LinkedProgram) -> String {
    ryupy_codegen_cil::dump_source_cil(
        program,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "generic_core_structure".into(),
            entry_source_path: None,
        },
    )
    .unwrap()
}
#[test]
fn complete_typed_abi_and_thunk_prefix_are_identical_with_generic_companion() {
    let ordinary = dump(&linked(false));
    let program = linked(true);
    source_provenance::require_linked(&program).unwrap();
    let cil = dump(&program);
    for name in ["m0_leaf", "m0_forward"] {
        assert_eq!(
            method(&ordinary, name),
            method(&cil, name),
            "typed CIL必须保持: {name}"
        );
    }
    for name in ["__thunk_0", "__thunk_1"] {
        let before = method(&ordinary, name).split_once("Return").unwrap().0;
        let after = method(&cil, name).split_once("Return").unwrap().0;
        assert_eq!(before, after, "完整位置入口不得加冷分派检查");
        assert!(!after.contains("Generic"));
        let thunk = method(&cil, name);
        assert!(thunk.find("PyBinder.Bind").unwrap() < thunk.find("$generic$").unwrap());
        assert!(
            thunk.contains("PyCast.ToInt64"),
            "完整供参慢路原typed边界保留"
        );
    }
    assert_eq!(
        cil.matches("NewObject(void [RyuPy.Runtime]RyuPy.Runtime.PyFunction..ctor")
            .count(),
        ordinary
            .matches("NewObject(void [RyuPy.Runtime]RyuPy.Runtime.PyFunction..ctor")
            .count()
    );
    assert_eq!(cil.matches(".RegisterGenericFunction(").count(), 2);
    assert_eq!(
        ordinary
            .matches("NewObject(void [RyuPy.Runtime]RyuPy.Runtime.PyFunction..ctor")
            .count(),
        2
    );
    let generic_name = program
        .functions
        .iter()
        .find_map(|(_, function)| {
            if function.name != "forward" {
                return None;
            }
            match function.core {
                FunctionCore::GenericCompanion { .. } => Some(function.codegen_name.as_str()),
                _ => None,
            }
        })
        .unwrap();
    let generic = method(&cil, generic_name);
    assert!(
        generic
            .lines()
            .next()
            .unwrap()
            .starts_with("private static [System.Runtime]System.Object "),
        "{}", generic.lines().next().unwrap()
    );
    assert!(generic.contains(".CallValueGeneric("));
    assert!(!generic.contains("PyCast.ToInt64"));
    assert!(!generic.contains(".m0_leaf("));
    let bytes = ryupy_codegen_cil::emit_source_assembly(
        &program,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "generic_core_structure".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    assert!(bytes.len() > 512);
}
