//! records开关经过实际build/dump共用流水线；未编译feature或固定绑定路径必须前置拒绝。
use super::*;
#[cfg(feature = "records-increment-experiment")]
use ryupy_mir::{Callee, RuntimeFn, Rvalue, Statement};

#[test]
fn records_driver_feature_off_rejects_before_source_read_and_fixed_binding_stays_out() {
    assert!(!Session::default().experimental_records_increment);
    let mut session = Session { experimental_records_increment: true, ..Default::default() };
    if !cfg!(feature = "records-increment-experiment") {
        let error = dump(&mut session, std::path::Path::new("missing-records.py"), Stage::Mir).unwrap_err();
        assert!(error.to_string().contains("构建feature"));
    }
    session.fixed_binding_build = true;
    assert!(accumulator_options::validate(&session).is_err());
}

#[cfg(feature = "records-increment-experiment")]
#[test]
fn records_real_compile_and_dump_agree_on_explicit_guard_selection() {
    let tree = test_temp::Root::new("records-driver");
    let input = tree.join("entry.py");
    std::fs::write(&input, "def step(counts, key):\n    counts[key] = counts.get(key, 0) + 1\n    return key\nd = {}\nstep(d, ('ann', 'buy'))\n").unwrap();
    for enabled in [false, true] {
        let mut session = Session { experimental_records_increment: enabled, ..Default::default() };
        let (linked, _, _) = compile_linked(&mut session, &input, &[], false, None)
            .unwrap_or_else(|e| panic!("{e}: {}", session.render_diagnostics(false)));
        let guards = linked.functions.iter().flat_map(|(_, f)| f.body.blocks.iter())
            .flat_map(|(_, b)| &b.stmts).filter(|s| matches!(s, Statement::Assign {
                value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::RecordsTryIncrementMaterialized), .. }, ..
            })).count();
        assert_eq!(guards, usize::from(enabled));
        let text = dump(&mut session, &input, Stage::Mir).unwrap();
        assert_eq!(text.matches("RecordsTryIncrementMaterialized").count(), usize::from(enabled));
    }
}
