//! 生产资格事实的正负输入，不运行 Python，不更新 traceback golden。
//! 这里只检查 HIR 事实；通过不代表最终 MIR 证书或生产 traceback 已通过。
use crate::trace_domain::{self, Atom, Primitive, Values};

const ORIGINAL: &str = include_str!("../../../tests/cases/generator_dynamic/frames/entry.py");

fn facts(source: &str, production_metadata: bool) -> trace_domain::Facts {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("unrelated_name.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        resolve_file_at_runtime: production_metadata,
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    trace_domain::analyze(&hir)
}

fn accepted_facts(source: &str) -> trace_domain::Facts {
    let result = facts(source, true);
    assert!(result.converged);
    assert!(result.refusals.is_empty(), "{:?}", result.refusals);
    result
}

#[test]
fn original_frames_has_closed_value_and_error_flow() {
    let result = accepted_facts(ORIGINAL);
    assert!(result.functions.values().any(|summary| {
        summary.yields.atoms() == [Atom::Primitive(Primitive::String)]
            && summary.errors.contains("ValueError")
            && summary.errors.contains("TypeError")
    }));
    assert!(result.functions.values().any(|summary| {
        summary.returns.atoms().is_empty() && summary.errors.contains("TypeError")
    }));
}

#[test]
fn source_names_and_module_aliases_do_not_grant_capabilities() {
    let renamed = ORIGINAL
        .replace("Target", "Container")
        .replace("generate", "produce")
        .replace("error", "problem")
        .replace("target failed", "different text");
    accepted_facts(&renamed);
    let aliases = ORIGINAL
        .replace(
            "it = generate(Target())",
            "g = generate\nc = Target\nn = next\np = print\nit = g(c())",
        )
        .replace("print(next(it))", "p(n(it))")
        .replace("\nnext(it)", "\nn(it)");
    accepted_facts(&aliases);
}

#[test]
fn ordinary_parameter_and_return_wrappers_preserve_generator_identity() {
    let source = ORIGINAL.replace(
        "it = generate(Target())",
        "def wrap(f, value):\n    return f(value)\n\nit = wrap(generate, Target())",
    );
    accepted_facts(&source);
}

#[test]
fn unsupported_definitions_are_not_skipped_as_unused() {
    for extra in [
        "\ndef unused(callback):\n    callback()\n",
        "\ndef unused(value=next(it)):\n    return value\n",
        "\ndef unused():\n    raise\n",
        "\nTarget.changed = 1\n",
        "\ndef unused():\n    import sys\n",
    ] {
        let result = facts(&format!("{ORIGINAL}{extra}"), true);
        assert!(!result.refusals.is_empty(), "{extra}");
    }
}

#[test]
fn unknown_shadow_and_non_generator_inputs_are_rejected() {
    for source in [
        ORIGINAL.replace("next(it)", "next(Target())"),
        ORIGINAL.replace("target.error", "target.missing"),
        ORIGINAL.replace("@property", "@unknown"),
        ORIGINAL.replace(
            "it = generate(Target())",
            "next = Target\nit = generate(Target())",
        ),
    ] {
        let result = facts(&source, true);
        assert!(!result.refusals.is_empty());
    }
}

#[test]
fn budget_overflow_is_absorbing_and_bottom_is_distinct() {
    let mut values = Values::default();
    for index in 0..=trace_domain::SET_LIMIT {
        let function = la_arena::Idx::from_raw((index as u32).into());
        values.join(&Values::one(Atom::Function(function)));
    }
    assert!(values.is_unknown());
    assert!(!values.join(&Values::one(Atom::Primitive(Primitive::None))));
    assert!(!Values::default().is_unknown());
}
