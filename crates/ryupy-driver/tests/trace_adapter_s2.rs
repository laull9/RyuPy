//! 隔离probe沿真实driver dump入口覆盖parse/HIR/types/MIR/CIL；实际PE执行由同轮host完成。
#![cfg(feature = "trace-adapter-test")]
use ryupy_driver::{Session, Stage};

#[test]
fn trace_adapter_s2_real_driver_stages_and_rejections() {
    let root = std::path::Path::new(env!("CARGO_MANIFEST_DIR"))
        .join("../../tests/fixtures/trace_adapter_s2");
    for name in ["explicit_inner", "explicit_module", "recursive", "reused"] {
        for stage in [Stage::Hir, Stage::Types, Stage::Mir, Stage::Cil] {
            let mut session = Session::default();
            ryupy_driver::dump(&mut session, &root.join(format!("{name}.py")), stage)
                .expect("四正例必须通过真实driver全部阶段");
        }
    }
    for name in [
        "bare_reject",
        "cause_reject",
        "generator_reject",
        "nested_handler_reject",
    ] {
        let mut session = Session::default();
        assert!(
            ryupy_driver::dump(&mut session, &root.join(format!("{name}.py")), Stage::Mir).is_err()
        );
        assert!(session.diagnostics.has_errors());
    }
}
