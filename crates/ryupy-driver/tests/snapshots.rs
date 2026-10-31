use ryupy_driver::{Session, Stage, dump};

#[test]
fn snapshot_m1_0() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_0_print_int.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m1_0_hir", hir);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m1_0_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m1_0_cil", cil);
}

#[test]
fn snapshot_m1_1() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_1_functions.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m1_1_hir", hir);

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    insta::assert_snapshot!("m1_1_types", types);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m1_1_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m1_1_cil", cil);
}

#[test]
fn catches_unassigned_local_read() {
    let py_path = std::env::temp_dir().join("ryupy_test_unbound.py");
    std::fs::write(&py_path, "def foo() -> int:\n    x = x + 1\n    return x\n").unwrap();

    let mut sess = Session::default();
    let res = dump(&mut sess, &py_path, Stage::Mir);
    let _ = std::fs::remove_file(&py_path);

    assert!(res.is_err());
    assert!(
        sess.diagnostics.iter().any(|d| d.code == Some("E0301")),
        "expected E0301 diagnostic, got: {:?}",
        sess.diagnostics
    );
}
