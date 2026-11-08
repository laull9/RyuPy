//! 产品编译/独立结构/整产物封存的有限反例；这里只编译IR，不冒称JIT/NativeAOT或CP执行。
use crate::generator_product::{DeferredGenerators, Proof};
use crate::*;

const A: &str = include_str!("../../../tests/validation/generator_product/goldens/a_all_miss.py");
pub(super) const B: &str = include_str!("../../../tests/cases/generator_dynamic/frames/entry.py");
const HOT: &str =
    include_str!("../../../tests/validation/generator_product/goldens/hot_same_getter.py");
const C: &str =
    include_str!("../../../tests/validation/generator_product/goldens/c_nested_same_object.py");
const D: &str =
    include_str!("../../../tests/validation/generator_product/goldens/d_same_object_escape.py");
pub(super) struct Observer;
impl passes::PipelineObserver for Observer {
    fn observe(&mut self, _: passes::PipelineEvent) {}
}
pub(super) fn frontend(source: &str) -> (ryupy_hir::Module, ryupy_ty::ModuleTypes) {
    let mut sources = ryupy_base::SourceDb::default();
    let file = sources.add("product.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let info = ryupy_hir::ModuleInfo {
        name: "__main__".into(),
        file: "product.py".into(),
        resolve_file_at_runtime: true,
        ..Default::default()
    };
    let (hir, errors) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    (hir, types)
}
fn product(source: &str) -> Result<(Program, Vec<ModuleExport>, Proof), String> {
    let (hir, types) = frontend(source);
    generator_product::compile(&hir, &types, &passes::default_pipeline(), &mut Observer)
}
fn metadata(exports: Vec<ModuleExport>) -> UnitMeta {
    UnitMeta {
        instance_capabilities: Default::default(),
        execution: ModuleExecutionKind::LegacySingleton,
        environment_reason: "test-explicit-product".into(),
        location: Default::default(),
        file_backed_by_global: true,
        is_package: false,
        protected_exports: vec![],
        name: "__main__".into(),
        package: String::new(),
        file: "product.py".into(),
        exports,
    }
}
pub(super) fn linked(source: &str) -> LinkedProgram {
    let (program, exports, proof) = product(source).expect("产品实际编译");
    generator_product::link(vec![(program, metadata(exports))], 0, vec![proof])
        .expect("真实link验证")
}

#[test]
fn original_a_b_and_same_site_property_compile_through_final_link() {
    for source in [A, B, HOT] {
        let (hir, _) = frontend(source);
        assert!(
            trace_domain::analyze(&hir)
                .functions
                .values()
                .any(|summary| summary.errors.contains("RecursionError"))
        );
        let linked = linked(source);
        assert_eq!(generator_product::validate_emission(&linked), Ok(Some(1)));
        assert_eq!(linked.modules.len(), 1);
    }
}

#[test]
fn original_constructor_field_inputs_are_whole_product_rejections() {
    for source in [C, D] {
        assert!(product(source).is_err());
    }
}

#[test]
fn high_and_low_validation_are_distinct_and_dead_yield_keeps_real_generator_identity() {
    let (hir, types) = frontend(A);
    let mut plan = DeferredGenerators::default();
    let (mut high, _, diagnostics) = build::build_program_mode(&hir, &types, None, Some(&mut plan));
    assert!(diagnostics.is_empty());
    assert!(validate::validate_high(&high, plan.functions()).is_ok());
    assert!(validate::validate(&high).is_err());
    passes::run_pipeline_high(
        &mut high,
        &passes::default_pipeline(),
        &mut Observer,
        plan.functions(),
    )
    .unwrap();
    generators::lower_with_trace(&mut high, plan.functions(), &plan.exceptions, true);
    assert!(validate::validate(&high).is_ok());
    let dead = "def generate():\n    raise ValueError('before')\n    yield 'dead'\nit = generate()\nnext(it)\n";
    let value = linked(dead);
    assert!(
        value.functions.iter().any(
            |(_, f)| f
                .body
                .blocks
                .iter()
                .any(|(_, b)| b.stmts.iter().any(|s| matches!(
                    s,
                    Statement::Assign {
                        value: Rvalue::MakeGenerator { .. },
                        ..
                    }
                )))
        )
    );
}

#[test]
fn unknown_capability_system_exit_bare_and_protocol_injection_are_rejected() {
    for source in [
        A.replace("raise ValueError('original')", "raise SystemExit"),
        A.replace("generate(TypeError)", "generate(SystemExit)"),
        A.replace("except kind:", "except:"),
        A.replace("raise ValueError('original')", "raise StopIteration"),
        A.replace("raise ValueError('original')", "raise RecursionError"),
        A.replace("next(it)\n", "it.send(None)\n"),
        A.replace("next(it)\n", "it.close()\n"),
        A.replace("next(it)\n", "it.throw(TypeError)\n"),
    ] {
        assert!(product(&source).is_err(), "{source}");
    }
}

#[test]
fn explicit_or_possible_none_exception_argument_rejects_through_aliases() {
    let parameter = A
        .replace("def generate(kind):", "def generate(kind, message):")
        .replace("ValueError('original')", "ValueError(message)")
        .replace("generate(TypeError)", "generate(TypeError, None)");
    let returned = format!("def message():\n    return None\n{}", A)
        .replace("def generate(kind):", "def generate(kind, text):")
        .replace("ValueError('original')", "ValueError(text)")
        .replace(
            "it = generate(TypeError)",
            "text = message()\nit = generate(TypeError, text)",
        );
    let mixed = parameter.replace(
        "it = generate(TypeError, None)",
        "first = generate(TypeError, 'valid')\nit = generate(TypeError, None)",
    );
    let callee_alias = A
        .replace("def generate(kind):", "def generate(kind, error):")
        .replace("ValueError('original')", "error(None)")
        .replace(
            "it = generate(TypeError)",
            "error = ValueError\nit = generate(TypeError, error)",
        );
    for source in [
        A.replace("ValueError('original')", "ValueError(None)"),
        parameter,
        returned,
        mixed,
        callee_alias,
    ] {
        let error = product(&source).expect_err("可能None的单参不能进入产品");
        assert!(error.contains("产品异常单参构造可能为None"), "{error}");
    }
    for source in [
        A.replace("ValueError('original')", "ValueError()"),
        A.replace("ValueError('original')", "ValueError"),
    ] {
        assert_eq!(
            generator_product::validate_emission(&linked(&source)),
            Ok(Some(1))
        );
    }
}

#[test]
fn sealed_link_rejects_changed_actual_source_global_owner_and_abi() {
    for mutation in 0..4 {
        let mut program = linked(B);
        let init = program.modules[program.entry].init;
        let entry = program.functions[init].body.entry;
        match mutation {
            0 => program.functions[init].body.blocks[entry]
                .stmts
                .push(Statement::Line(999)),
            1 => {
                program.modules[program.entry].exports[0].value =
                    la_arena::Idx::from_raw(999_999.into())
            }
            2 => program.functions[init].ret = MirType::Dynamic,
            _ => program.modules[program.entry].init = la_arena::Idx::from_raw(999_999.into()),
        }
        assert!(generator_product::validate_emission(&program).is_err());
    }
}

#[test]
fn absent_certificate_cannot_enable_v1_and_prelink_proof_cannot_cover_new_code() {
    let mut program = linked(A);
    program.trace_product = None;
    assert!(generator_product::validate_emission(&program).is_err());
    let (mut program, exports, proof) = product(B).unwrap();
    let entry = program.functions[program.entry].body.entry;
    program.functions[program.entry].body.blocks[entry]
        .stmts
        .push(Statement::Line(1234));
    assert!(generator_product::link(vec![(program, metadata(exports))], 0, vec![proof]).is_err());
}
