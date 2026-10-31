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
