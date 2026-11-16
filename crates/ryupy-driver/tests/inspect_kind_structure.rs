//! 真实HIR种类到MIR定义的来源门；canonical的实际PE冷发布由codegen独立测试保护。
use ryupy_mir::{Program, PythonFunctionKind};

fn compile(profile: bool) -> Program {
    let source =
        "def numeric(value: int) -> int:\n    return value + 1\ndef generated():\n    yield 1\n";
    let source = if cfg!(feature = "native-coroutines") {
        format!("{source}async def coroutine():\n    return 1\n")
    } else {
        source.to_string()
    };
    let source = source.as_str();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("kinds.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let info = ryupy_hir::ModuleInfo {
        introspection_profile: profile,
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty());
    let (mut program, _, diagnostics) = ryupy_mir::build::build_program_with_generic(&hir, &types);
    assert!(diagnostics.is_empty());
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    program
}
#[test]
fn kind_is_definition_data_and_canonical_registration_follows_actual_creation() {
    for profile in [false, true] {
        let program = compile(profile);
        for (_, function) in program
            .functions
            .iter()
            .filter(|(_, f)| f.signature_global.is_some())
        {
            let kind = function.definition.as_ref().unwrap().introspection_kind;
            let expected = match function.name.as_str() {
                "numeric" => Some(PythonFunctionKind::OrdinaryModule),
                "generated" => Some(PythonFunctionKind::Generator),
                "coroutine" => Some(PythonFunctionKind::Coroutine),
                name => panic!("unexpected public function {name}"),
            };
            assert_eq!(kind, if profile { expected } else { None });
        }
        // 同一canonical只由真实MakeFunction发布；伴生体保同一kind但没有独立signature。
        let private: Vec<_> = program
            .functions
            .iter()
            .filter(|(_, f)| matches!(f.core, ryupy_mir::FunctionCore::GenericCompanion { .. }))
            .collect();
        for (_, function) in private {
            assert!(function.signature_global.is_none());
        }
    }
}
