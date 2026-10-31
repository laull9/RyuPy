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

#[test]
fn snapshot_m1_2_arith() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_2_arith.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m1_2_arith_hir", hir);

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    insta::assert_snapshot!("m1_2_arith_types", types);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m1_2_arith_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m1_2_arith_cil", cil);
}

#[test]
fn snapshot_m1_2_control_flow() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_2_control_flow.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m1_2_control_flow_hir", hir);

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    insta::assert_snapshot!("m1_2_control_flow_types", types);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m1_2_control_flow_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m1_2_control_flow_cil", cil);
}

#[test]
fn snapshot_m1_3_for_range() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_3_for_range.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m1_3_for_range_hir", hir);

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    insta::assert_snapshot!("m1_3_for_range_types", types);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m1_3_for_range_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m1_3_for_range_cil", cil);
}

#[test]
fn snapshot_m1_3_fib() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_3_fib.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m1_3_fib_hir", hir);

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    insta::assert_snapshot!("m1_3_fib_types", types);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m1_3_fib_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m1_3_fib_cil", cil);
}

#[test]
fn snapshot_m1_4_floats() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_4_floats.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m1_4_floats_hir", hir);

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    insta::assert_snapshot!("m1_4_floats_types", types);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m1_4_floats_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m1_4_floats_cil", cil);
}

#[test]
fn snapshot_m1_4_mandelbrot() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_4_mandelbrot.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m1_4_mandelbrot_hir", hir);

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    insta::assert_snapshot!("m1_4_mandelbrot_types", types);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m1_4_mandelbrot_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m1_4_mandelbrot_cil", cil);
}

#[test]
fn catches_range_zero_step() {
    let py_path = std::env::temp_dir().join("ryupy_test_zero_step.py");
    std::fs::write(&py_path, "for i in range(0, 10, 0):\n    print(i)\n").unwrap();

    let mut sess = Session::default();
    let res = dump(&mut sess, &py_path, Stage::Mir);
    let _ = std::fs::remove_file(&py_path);

    assert!(res.is_err());
    assert!(
        sess.diagnostics.iter().any(|d| d.code == Some("E0302")),
        "expected E0302 diagnostic, got: {:?}",
        sess.diagnostics
    );
}

#[test]
fn fib_iter_100_overflows() {
    let root = ryupy_driver::runtime::workspace_root();
    let py_path = std::env::temp_dir().join("ryupy_test_fib_ovf.py");
    let src = r#"
def fib_iter(n: int) -> int:
    a = 0
    b = 1
    for _ in range(n):
        t = a + b
        a = b
        b = t
    return a

print(fib_iter(100))
"#;
    std::fs::write(&py_path, src).unwrap();

    let mut sess = Session::default();
    let out_dir = root.join("target/test_ovf");
    let opts = ryupy_driver::BuildOptions {
        input: py_path.clone(),
        out_dir,
        aot: false,
    };
    let artifacts = ryupy_driver::build(&mut sess, &opts).unwrap();
    let res = ryupy_driver::dotnet::run_dll(&artifacts.dll).unwrap();
    let _ = std::fs::remove_file(&py_path);

    assert!(!res.status.success(), "fib_iter(100) 应当因算术溢出失败");
    let stderr = String::from_utf8_lossy(&res.stderr);
    assert!(
        stderr.contains("OverflowException"),
        "期望包含 OverflowException，实际 stderr: {stderr}"
    );
}
