//! 既有语言竖切的 IR 与诊断结构合同；各用例直接检查对应运算、类型和控制流。

use ryupy_driver::{Session, Stage};
mod support;
use support::dump;

#[test]
fn snapshot_m1_0() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_0_print_int.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(hir.contains("print((1 + 2))"), "{hir}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("Add(const 1_i64, const 2_i64)"), "{mir}");
    assert!(mir.contains("RuntimeFn::WriteI64"), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("AddOverflow"), "{cil}");
    assert!(cil.contains("Builtins.WriteI64(object, long)"), "{cil}");
}

#[test]
fn snapshot_m1_1() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_1_functions.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(hir.contains("def main_calc(x: int) -> int:"), "{hir}");
    assert!(hir.contains("helper(x) * 2"), "{hir}");
    assert!(hir.contains("def helper(n: int) -> int:"), "{hir}");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    assert!(types.contains("[Static]: [Int] -> Int"), "{types}");
    assert!(types.contains("local Idx::<Local>(1): Int"), "{types}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("fn main_calc(_0: i64) -> i64"), "{mir}");
    assert!(mir.contains(" = helper("), "{mir}");
    assert!(mir.contains("Mul("), "{mir}");
    assert!(mir.contains("Add("), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("m0_main_calc(long)"), "{cil}");
    assert!(cil.contains("m0_helper(long)"), "{cil}");
    assert!(cil.contains("MultiplyOverflow"), "{cil}");
    assert!(cil.contains("AddOverflow"), "{cil}");
}

#[test]
fn catches_unassigned_local_read() {
    let scope = support::temp_scope::TempScope::new("snapshots");
    let py_path = scope.path("ryupy_test_unbound.py");
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
    assert!(hir.contains("7 // 2"), "{hir}");
    assert!(hir.contains("Neg(7) % 3"), "{hir}");
    assert!(hir.contains("2 ** 10"), "{hir}");
    assert!(hir.contains("1 << 10"), "{hir}");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    assert!(types.contains("[Static]: [] -> None"), "{types}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("FloorDivI64"), "{mir}");
    assert!(mir.contains("ModI64"), "{mir}");
    assert!(mir.contains("PowI64"), "{mir}");
    assert!(mir.contains("TrueDivF64"), "{mir}");
    assert!(mir.contains("ShlI64"), "{mir}");
    assert!(mir.contains("ShrI64"), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("PyMath.FloorDivI64"), "{cil}");
    assert!(cil.contains("PyMath.ModI64"), "{cil}");
    assert!(cil.contains("PyMath.PowI64"), "{cil}");
    assert!(cil.contains("PyMath.TrueDivF64"), "{cil}");
    assert!(cil.contains("PyMath.ShiftLeftI64"), "{cil}");
}

#[test]
fn snapshot_m1_2_control_flow() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_2_control_flow.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(hir.contains("if (n Lt 0):"), "{hir}");
    assert!(hir.contains("while (n NotEq 1):"), "{hir}");
    assert!(hir.contains("n // 2"), "{hir}");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    assert!(types.contains("[Static]: [Int] -> Int"), "{types}");
    assert!(types.contains("[Static]: [Int, Int] -> Int"), "{types}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("fn classify(_0: i64) -> i64"), "{mir}");
    assert!(mir.contains("branch "), "{mir}");
    assert!(mir.contains("RuntimeFn::FloorDivI64"), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("m0_classify(long)"), "{cil}");
    assert!(cil.contains("BranchTruthy"), "{cil}");
    assert!(cil.contains("PyMath.FloorDivI64"), "{cil}");
}

#[test]
fn snapshot_m1_3_for_range() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_3_for_range.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(hir.contains("range(0, 10)"), "{hir}");
    assert!(hir.contains("range(10, 0, Neg(3))"), "{hir}");
    assert!(hir.contains("range(5, 5)"), "{hir}");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    assert!(types.contains("ModuleVar>(3): Int"), "{types}");
    assert!(types.contains("ModuleVar>(4): Int"), "{types}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("const -3_i64"), "{mir}");
    assert!(mir.contains("branch "), "{mir}");
    assert!(mir.contains("Add("), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("BranchTruthy"), "{cil}");
    assert!(cil.contains("AddOverflow"), "{cil}");
    assert!(cil.contains("Builtins.WriteI64"), "{cil}");
}

#[test]
fn snapshot_m1_3_fib() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_3_fib.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(hir.contains("fib_rec((n - 1)) + fib_rec((n - 2))"), "{hir}");
    assert!(hir.contains("for _ in range(0, n)"), "{hir}");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    assert!(types.contains("[Static]: [Int] -> Int"), "{types}");
    assert!(types.contains("local Idx::<Local>(4): Int"), "{types}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("fn fib_rec(_0: i64) -> i64"), "{mir}");
    assert!(mir.contains(" = fib_rec("), "{mir}");
    assert!(mir.contains("fn fib_iter(_0: i64) -> i64"), "{mir}");
    assert!(mir.contains("branch "), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("m0_fib_rec(long)"), "{cil}");
    assert!(cil.contains("m0_fib_iter(long)"), "{cil}");
    assert!(cil.contains("AddOverflow"), "{cil}");
}

#[test]
fn snapshot_m1_4_floats() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_4_floats.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(hir.contains("0.1 + 0.2"), "{hir}");
    assert!(hir.contains("7.5 // 2"), "{hir}");
    assert!(hir.contains("2.0 ** 0.5"), "{hir}");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    assert!(types.contains("[Static]: [] -> None"), "{types}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains(": f64;"), "{mir}");
    assert!(mir.contains("FloorDivF64"), "{mir}");
    assert!(mir.contains("ModF64"), "{mir}");
    assert!(mir.contains("PowF64"), "{mir}");
    assert!(mir.contains("F64ToI64"), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("Add"), "{cil}");
    assert!(cil.contains("Multiply"), "{cil}");
    assert!(cil.contains("PyMath.FloorDivF64"), "{cil}");
    assert!(cil.contains("PyMath.F64ToI64"), "{cil}");
    assert!(cil.contains("Builtins.WriteF64"), "{cil}");
}

#[test]
fn snapshot_m1_4_mandelbrot() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_4_mandelbrot.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(
        hir.contains("def mandel(cr: float, ci: float, max_iter: int) -> int:"),
        "{hir}"
    );
    assert!(hir.contains("while (n Lt max_iter)"), "{hir}");
    assert!(hir.contains("zr * zr"), "{hir}");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    assert!(
        types.contains("[Static]: [Float, Float, Int] -> Int"),
        "{types}"
    );
    assert!(types.contains("local Idx::<Local>(6): Float"), "{types}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(
        mir.contains("fn mandel(_0: f64, _1: f64, _2: i64) -> i64"),
        "{mir}"
    );
    assert!(mir.contains("Mul("), "{mir}");
    assert!(mir.contains("branch "), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("m0_mandel(double, double, long)"), "{cil}");
    assert!(cil.contains("Multiply"), "{cil}");
    assert!(cil.contains("BranchTruthy"), "{cil}");
}

#[test]
fn snapshot_m1_5_dataclass() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_5_dataclass.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(hir.contains("class Body:"), "{hir}");
    assert!(hir.contains("x: float"), "{hir}");
    assert!(hir.contains("class Vec2:"), "{hir}");
    assert!(hir.contains("x: int"), "{hir}");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    assert!(
        types.contains("[Dynamic]: [Dynamic, Dynamic] -> Dynamic"),
        "{types}"
    );

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("DeclareDataclass"), "{mir}");
    assert!(mir.contains("ApplyDataclass"), "{mir}");
    assert!(mir.contains("DynGetAttr"), "{mir}");
    assert!(mir.contains("SetAttr"), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("PyOps.DeclareDataclass"), "{cil}");
    assert!(cil.contains("PyOps.ApplyDataclass"), "{cil}");
    assert!(cil.contains("PyPic.GetAttr"), "{cil}");
    assert!(cil.contains("PyPic.SetAttr"), "{cil}");
}

#[test]
fn catches_range_zero_step() {
    let scope = support::temp_scope::TempScope::new("snapshots");
    let py_path = scope.path("ryupy_test_zero_step.py");
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
    // 编译输出与输入都属于本次外部临时根，断言失败也由Drop清理。
    struct TempRoot(std::path::PathBuf);
    impl Drop for TempRoot {
        fn drop(&mut self) {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let root = TempRoot(
        std::env::temp_dir().join(format!("ryupy-fib-overflow-{}-{stamp}", std::process::id())),
    );
    std::fs::create_dir(&root.0).unwrap();
    let py_path = root.0.join("ryupy_test_fib_ovf.py");
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
    let out_dir = root.0.join("output");
    let opts = ryupy_driver::BuildOptions {
        input: py_path.clone(),
        out_dir,
        aot: false,
        search_paths: Vec::new(),
    };
    let artifacts = ryupy_driver::build(&mut sess, &opts).unwrap();
    let res = ryupy_driver::dotnet::run_dll(&artifacts.dll).unwrap();
    let _ = std::fs::remove_file(&py_path);

    assert!(!res.status.success(), "fib_iter(100) 应当因算术溢出失败");
    let stderr = String::from_utf8_lossy(&res.stderr);
    // 溢出要表现成 Python 的 OverflowError（带 traceback），
    // 不能泄漏 .NET 的 "Unhandled exception. System.OverflowException" 与 CLR 调用栈
    assert!(
        stderr.contains("OverflowError") && !stderr.contains("System.OverflowException"),
        "期望 Python 风格的 OverflowError，实际 stderr: {stderr}"
    );
}

// Phase2合同保留原测试函数名，拆分源码避免单文件超过600行。
include!("support/phase2_contracts.rs");
