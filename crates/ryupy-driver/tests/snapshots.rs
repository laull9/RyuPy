//! 既有语言竖切的 IR 与诊断快照；宿主临时路径不属于语义断言。

use ryupy_driver::{Session, Stage};
mod support;
use support::dump;
#[path = "snapshots_cil_contract/mod.rs"]
mod cil_features;

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
    cil_features::check_and_reject_mutation("m1_1", &cil);
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
    cil_features::check_and_reject_mutation("m1_2_control_flow", &cil);
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
    cil_features::check_and_reject_mutation("m1_3_fib", &cil);
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
    cil_features::check_and_reject_mutation("m1_4_mandelbrot", &cil);
}

#[test]
fn snapshot_m1_5_dataclass() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase1/m1_5_dataclass.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m1_5_dataclass_hir", hir);

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    insta::assert_snapshot!("m1_5_dataclass_types", types);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m1_5_dataclass_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    cil_features::check_and_reject_mutation("m1_5_dataclass", &cil);
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

// ───────────────── M2.1：动态路径 ─────────────────

/// 动态函数：类型旁表只有静态函数有内容，MIR/CIL 里出现装箱与 `PyOps` 调用。
#[test]
fn snapshot_m2_1_dynamic() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_1_dynamic.py");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    insta::assert_snapshot!("m2_1_dynamic_types", types);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m2_1_dynamic_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    cil_features::check_and_reject_mutation("m2_1_dynamic", &cil);
}

/// 大整数：字面量以十进制字符串携带，超出 i64 的部分由运行时构造 BigInteger。
#[test]
fn snapshot_m2_1_bigint() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_1_bigint.py");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m2_1_bigint_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m2_1_bigint_cil", cil);
}

/// M2.2：异常区域的 MIR 与 CIL。
///
/// 这份快照同时锁住三件事：受保护区间的块划分、`finally` 的状态机分派、CIL 异常表。
/// 改动图结构时先看这里的 diff，再决定是不是预期变化。
#[test]
fn snapshot_m2_2_try() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_2_finally.py");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m2_2_try_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    cil_features::check_and_reject_mutation("m2_2_try", &cil);
}

/// 错误输入：有限动态except只新增Name/Name起点Attribute/tuple；数字字面量仍明确拒绝。
#[test]
fn snapshot_try_unsupported_handler_type() {
    let py_path = std::env::temp_dir().join("ryupy_test_bad_except.py");
    std::fs::write(&py_path, "try:\n    print(1)\nexcept 1:\n    print(2)\n").unwrap();

    let mut sess = Session::default();
    let res = dump(&mut sess, &py_path, Stage::Mir);
    let _ = std::fs::remove_file(&py_path);

    assert!(res.is_err(), "非类型名的 except 应被拒绝");
    // 仅规范化宿主临时目录，保留源码行列与完整诊断内容。
    let rendered = sess.render_diagnostics(false).replace(
        py_path.to_str().unwrap(),
        py_path.file_name().unwrap().to_str().unwrap(),
    );
    assert!(
        rendered.contains("E0100"),
        "期望 E0100 诊断，实际：{rendered}"
    );
    insta::assert_snapshot!("m2_2_unsupported_handler_type", rendered);
}

/// M2.3：容器表达式统一降为 `Dynamic` 后的 MIR 与 CIL。
///
/// 这份快照同时锁住三件事：容器构造 / 下标读写降级成运行时调用、`object[]` 实参数组的
/// 真实落地方式、以及 `for` 在动态序列上的迭代协议。改动态降级策略时先看这里的 diff。
#[test]
fn snapshot_m2_3_containers() {
    let py_path = std::env::temp_dir().join("ryupy_test_m2_3_containers.py");
    std::fs::write(
        &py_path,
        "def mix():\n    items = [1, 2]\n    items[0] = len(items)\n    lookup = {\"a\": 1}\n    lookup[\"a\"] = 2\n    total = 0\n    for value in items:\n        total = total + value\n    return total in items\n",
    )
    .unwrap();

    let mut sess = Session::default();
    let mir = dump(&mut sess, &py_path, Stage::Mir).expect("容器用例应能编译");
    let mut sess = Session::default();
    let cil = dump(&mut sess, &py_path, Stage::Cil).expect("容器用例应能出 CIL");
    let _ = std::fs::remove_file(&py_path);

    insta::assert_snapshot!("m2_3_containers_mir", mir);
    cil_features::check_and_reject_mutation("m2_3_containers", &cil);
}

/// M2.3：切片赋值与星号解包。
///
/// 这两条语句最容易在实现里被悄悄丢掉：切片赋值需要三段下标 + 长度校验，星号解包需要在
/// 运行时才知道 `*middle` 收多少个元素。快照保证它们确实生成了对应的运行时调用。
#[test]
fn snapshot_m2_3_slice_and_unpack() {
    let py_path = std::env::temp_dir().join("ryupy_test_m2_3_slice.py");
    std::fs::write(
        &py_path,
        "def reshape(values):\n    head, *tail = values\n    values[1:3] = [8, 9]\n    return values[::-1]\n",
    )
    .unwrap();

    let mut sess = Session::default();
    let mir = dump(&mut sess, &py_path, Stage::Mir).expect("切片 / 解包应能编译");
    let _ = std::fs::remove_file(&py_path);

    assert!(mir.contains("SetItem"), "期望切片赋值走运行时调用：{mir}");
    assert!(
        mir.contains("UnpackEx"),
        "期望星号解包走完整迭代准备：{mir}"
    );
    insta::assert_snapshot!("m2_3_slice_unpack_mir", mir);
}

/// 动态属性访问：M2.2 起编译成 `PyObject.GetAttr` 运行时查找。
///
/// 早期版本在编译期直接拒绝，但 Python 属性是运行时概念——类型未知不代表一定失败。
/// 找不到属性时由运行时抛 `AttributeError`，与 CPython 一致。
#[test]
fn snapshot_dynamic_attribute_access() {
    let py_path = std::env::temp_dir().join("ryupy_test_dynamic_attr.py");
    std::fs::write(
        &py_path,
        "def read(obj):\n    return obj.value\n\nprint(read(1))\n",
    )
    .unwrap();

    let mut sess = Session::default();
    let mir = dump(&mut sess, &py_path, Stage::Mir).expect("动态属性访问应能编译");
    let _ = std::fs::remove_file(&py_path);

    assert!(
        mir.contains("DynGetAttr"),
        "期望生成动态属性读取调用，实际：{mir}"
    );
    insta::assert_snapshot!("m2_2_dynamic_attribute_mir", mir);
}

/// M2.5：普通 class 的 HIR / MIR / CIL 结构快照。
///
/// 重点检查类体顺序、类对象槽位、方法函数对象与 `SetAttr` / `Decorate` 调用。
#[test]
fn snapshot_m2_5_classes() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_5_classes.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m2_5_classes_hir", hir);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m2_5_classes_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    cil_features::check_and_reject_mutation("m2_5_classes", &cil);
}

/// M2.6：用户异常类会生成 CLR 子类与工厂委托，CIL 快照防止继承链退化。
#[test]
fn snapshot_m2_6_user_errors_cil() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_6_user_errors.py");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    insta::assert_snapshot!("m2_6_user_errors_cil", cil);
}

/// 普通Legacy多继承已准入；快照记录合法HIR形状，不再期待旧拒绝；基类顺序由MI运行合同覆盖。
#[test]
fn snapshot_multiple_inheritance_hir() {
    let py_path = std::env::temp_dir().join("ryupy_test_multi_inherit.py");
    std::fs::write(
        &py_path,
        "class A:\n    pass\n\nclass B:\n    pass\n\nclass C(A, B):\n    pass\n",
    )
    .unwrap();
    let mut sess = Session::default();
    let hir = dump(&mut sess, &py_path, Stage::Hir).unwrap();
    let _ = std::fs::remove_file(&py_path);
    assert!(!sess.render_diagnostics(false).contains("E0100"));
    insta::assert_snapshot!("m2_6_multiple_inheritance", hir);
}

/// M2.8：`with` 去糖成 try/except/finally 后的 HIR / MIR / CIL。
///
/// HIR 快照锁住展开顺序：固定类型 `__enter__` → `__exit__` → 调用进入 → `handled` 标记 →
/// try/handler/finally。MIR与手写CIL结构/变异合同锁住状态机和异常捕获，不保存生成CIL。
#[test]
fn snapshot_m2_8_with() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_8_with.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    insta::assert_snapshot!("m2_8_with_hir", hir);

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    insta::assert_snapshot!("m2_8_with_mir", mir);

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    cil_features::check_and_reject_mutation("m2_8_with", &cil);
}

/// 错误输入：`async with` 明确拒绝，而不是静默当成同步上下文。
#[test]
fn snapshot_m2_8_async_with_diagnostic() {
    let py_path = std::env::temp_dir().join("ryupy_test_async_with.py");
    std::fs::write(
        &py_path,
        "async def work(manager):\n    async with manager:\n        print(1)\n",
    )
    .unwrap();

    let mut sess = Session::default();
    let res = dump(&mut sess, &py_path, Stage::Mir);
    let _ = std::fs::remove_file(&py_path);

    assert!(res.is_err(), "async with 应被拒绝");
    // 仅规范化宿主临时目录，保留源码行列与完整诊断内容。
    let rendered = sess.render_diagnostics(false).replace(
        py_path.to_str().unwrap(),
        py_path.file_name().unwrap().to_str().unwrap(),
    );
    assert!(
        rendered.contains("E0100") && rendered.contains("async with"),
        "期望 async with 的 E0100 诊断，实际：{rendered}"
    );
    insta::assert_snapshot!("m2_8_async_with", rendered);
}

/// M2.9：动态运算 / 调用的 PIC 调用点编号与运行时入口。
///
/// 这里锁住三件事：每个 `DynBinary` 有独立 site 编号、`CallValue` / 捕获方法分流
/// 把 site 作为第一个实参、CIL 引用的是 `PyPic` 而不是通用 `PyOps` 入口。
#[test]
fn snapshot_m2_9_pic_cil() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_9_pic.py");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).expect("PIC 用例应能出 CIL");

    assert!(cil.contains("PyPic.Binary"), "期望二元运算走 PIC：{cil}");
    assert!(
        cil.contains("PyPic.CallCapturedMethod0(int, bool, object, string)"),
        "期望捕获方法调用保留带site的PIC分流入口：{cil}"
    );
    assert!(cil.contains("PyOps.IsModule(object)"));
    assert!(cil.contains("PyOps.CaptureModuleTarget(object, string)"));
    assert!(!cil.contains("CaptureModuleMethod"));
    assert!(cil.contains("PyPic.CallValue"), "期望动态调用走 PIC：{cil}");
    cil_features::check_and_reject_mutation("m2_9_pic", &cil);
}
