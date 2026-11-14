// Phase2的动态值、异常、容器和类的逐层结构合同。
// ───────────────── M2.1：动态路径 ─────────────────

/// 动态函数：类型旁表只有静态函数有内容，MIR/CIL 里出现装箱与 `PyOps` 调用。
#[test]
fn snapshot_m2_1_dynamic() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_1_dynamic.py");

    let mut sess = Session::default();
    let types = dump(&mut sess, &case, Stage::Types).unwrap();
    assert!(types.contains("[Dynamic]: [Dynamic] -> Dynamic"), "{types}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("fn twice("), "{mir}");
    assert!(mir.contains("dyn.Add"), "{mir}");
    assert!(mir.contains("DynTruthy"), "{mir}");
    assert!(mir.contains("box<I64>"), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("PyPic.Binary"), "{cil}");
    assert!(cil.contains("PyOps.Truthy"), "{cil}");
    assert!(cil.contains("Builtins.WriteObject"), "{cil}");
}

/// 大整数：字面量以十进制字符串携带，超出 i64 的部分由运行时构造 BigInteger。
#[test]
fn snapshot_m2_1_bigint() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_1_bigint.py");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("DynFromDecimal"), "{mir}");
    assert!(mir.contains("9223372036854775808"), "{mir}");
    assert!(mir.contains("dyn.Add"), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("PyOps.FromDecimalString"), "{cil}");
    assert!(cil.contains("PyPic.Binary"), "{cil}");
    assert!(cil.contains("PyOps.Unary"), "{cil}");
}

/// M2.2：异常区域的 MIR 与 CIL。
///
/// 这份结构断言同时锁住三件事：受保护区间的块划分、`finally` 的状态机分派、CIL 异常表。
/// 稳定合同检查具体处理器类型与状态分派，块编号可随优化改变。
#[test]
fn snapshot_m2_2_try() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_2_finally.py");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("finally-guard handler->"), "{mir}");
    assert!(mir.contains("catch RuntimeError"), "{mir}");
    assert!(mir.contains("HandlerEnter"), "{mir}");
    assert!(mir.contains("HandlerLeave"), "{mir}");
    assert!(mir.contains("branch "), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("// EH: try ["), "{cil}");
    assert!(cil.contains("PyBaseException handler"), "{cil}");
    assert!(cil.contains("PyRuntimeError handler"), "{cil}");
    assert!(cil.contains("PyErrors.EnterHandler"), "{cil}");
    assert!(cil.contains("PyErrors.LeaveHandler"), "{cil}");
}

/// 错误输入：有限动态except只新增Name/Name起点Attribute/tuple；数字字面量仍明确拒绝。
#[test]
fn snapshot_try_unsupported_handler_type() {
    let scope = support::temp_scope::TempScope::new("phase2_contracts");
    let py_path = scope.path("ryupy_test_bad_except.py");
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
    assert!(rendered.contains("E0100"), "{rendered}");
    assert!(rendered.contains("except 1:"), "{rendered}");
    assert!(rendered.contains("3:"), "{rendered}");
}

/// M2.3：容器表达式统一降为 `Dynamic` 后的 MIR 与 CIL。
///
/// 这份结构断言同时锁住三件事：容器构造 / 下标读写降级成运行时调用、`object[]` 实参数组的
/// 真实落地方式、以及 `for` 在动态序列上的迭代协议。
#[test]
fn snapshot_m2_3_containers() {
    let scope = support::temp_scope::TempScope::new("phase2_contracts");
    let py_path = scope.path("ryupy_test_m2_3_containers.py");
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

    assert!(mir.contains("MakeList"), "{mir}");
    assert!(mir.contains("MakeDict"), "{mir}");
    assert!(mir.contains("SetItem"), "{mir}");
    assert!(mir.contains("IterStart"), "{mir}");
    assert!(mir.contains("IterNextOk"), "{mir}");
    assert!(mir.contains("IterValue"), "{mir}");
    assert!(mir.contains("Contains"), "{mir}");
    assert!(cil.contains("PyOps.MakeList"), "{cil}");
    assert!(cil.contains("PyOps.MakeDict"), "{cil}");
    assert!(cil.contains("PyOps.SetItem"), "{cil}");
    assert!(cil.contains("PyOps.IterStart"), "{cil}");
    assert!(cil.contains("PyOps.IterNextOk"), "{cil}");
    assert!(cil.contains("PyOps.Contains"), "{cil}");
}

/// M2.3：切片赋值与星号解包。
///
/// 这两条语句最容易在实现里被悄悄丢掉：切片赋值需要三段下标 + 长度校验，星号解包需要在
/// 运行时才知道 `*middle` 收多少个元素。结构断言保证它们确实生成了对应的运行时调用。
#[test]
fn snapshot_m2_3_slice_and_unpack() {
    let scope = support::temp_scope::TempScope::new("phase2_contracts");
    let py_path = scope.path("ryupy_test_m2_3_slice.py");
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
    assert!(mir.contains("UnpackEx"), "{mir}");
    assert!(mir.contains("MakeSlice"), "{mir}");
    assert!(mir.contains("SetItem"), "{mir}");
    assert!(mir.contains("GetItem"), "{mir}");
}

/// 动态属性访问：M2.2 起编译成 `PyObject.GetAttr` 运行时查找。
///
/// 早期版本在编译期直接拒绝，但 Python 属性是运行时概念——属性由运行时协议解析。
/// 找不到属性时由运行时抛 `AttributeError`，与 CPython 一致。
#[test]
fn snapshot_dynamic_attribute_access() {
    let scope = support::temp_scope::TempScope::new("phase2_contracts");
    let py_path = scope.path("ryupy_test_dynamic_attr.py");
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
    assert!(mir.contains("DynGetAttr"), "{mir}");
    assert!(mir.contains("const \"value\""), "{mir}");
}

/// M2.5：普通 class 的 HIR / MIR / CIL 结构结构断言。
///
/// 重点检查类体顺序、类对象槽位、方法函数对象与 `SetAttr` / `Decorate` 调用。
#[test]
fn snapshot_m2_5_classes() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_5_classes.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(hir.contains("class Box:"), "{hir}");
    assert!(hir.contains("def __init__(self, value):"), "{hir}");
    assert!(hir.contains("self.value = value"), "{hir}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("MakeClass"), "{mir}");
    assert!(mir.contains("FunctionModule"), "{mir}");
    assert!(mir.contains("FinalizeOrdinaryClass"), "{mir}");
    assert!(mir.contains("Decorate"), "{mir}");
    assert!(mir.contains("SetAttr"), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("PyOps.MakeClass"), "{cil}");
    assert!(cil.contains("PyOps.FinalizeOrdinaryClass"), "{cil}");
    assert!(cil.contains("PyOps.Decorate"), "{cil}");
    assert!(cil.contains("PyPic.SetAttr"), "{cil}");
}

/// M2.6：用户异常类会生成 CLR 子类与工厂委托，CIL 结构断言防止继承链退化。
#[test]
fn snapshot_m2_6_user_errors_cil() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_6_user_errors.py");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("PyOps.MakeClass"), "{cil}");
    assert!(cil.contains("PyErrors.PrepareRaise"), "{cil}");
    assert!(cil.contains("PyValueError handler"), "{cil}");
    assert!(cil.contains("PyErrors.EnterHandler"), "{cil}");
}

/// 普通Legacy多继承已准入；结构断言记录合法HIR形状，不再期待旧拒绝；基类顺序由MI运行合同覆盖。
#[test]
fn snapshot_multiple_inheritance_hir() {
    let scope = support::temp_scope::TempScope::new("phase2_contracts");
    let py_path = scope.path("ryupy_test_multi_inherit.py");
    std::fs::write(
        &py_path,
        "class A:\n    pass\n\nclass B:\n    pass\n\nclass C(A, B):\n    pass\n",
    )
    .unwrap();
    let mut sess = Session::default();
    let hir = dump(&mut sess, &py_path, Stage::Hir).unwrap();
    let _ = std::fs::remove_file(&py_path);
    assert!(!sess.render_diagnostics(false).contains("E0100"));
    assert!(hir.contains("class A:"), "{hir}");
    assert!(hir.contains("class B:"), "{hir}");
    assert!(hir.contains("class C:"), "{hir}");
}

/// M2.8：`with` 去糖成 try/except/finally 后的 HIR / MIR / CIL。
///
/// HIR 结构断言锁住展开顺序：取 `__exit__` → `__enter__` → 绑定 → `handled` 标记 →
/// try/handler/finally。MIR / CIL 结构断言锁住 `finally` 状态机与 `BaseException` 捕获。
#[test]
fn snapshot_m2_8_with() {
    let root = ryupy_driver::runtime::workspace_root();
    let case = root.join("tests/e2e/phase2/m2_8_with.py");

    let mut sess = Session::default();
    let hir = dump(&mut sess, &case, Stage::Hir).unwrap();
    assert!(hir.contains("context_method"), "{hir}");
    assert!(hir.contains("__exit__"), "{hir}");
    assert!(hir.contains("__enter__"), "{hir}");
    assert!(hir.contains("finally:"), "{hir}");

    let mut sess = Session::default();
    let mir = dump(&mut sess, &case, Stage::Mir).unwrap();
    assert!(mir.contains("ContextMethod"), "{mir}");
    assert!(mir.contains("finally-guard handler->"), "{mir}");
    assert!(mir.contains("Reraise"), "{mir}");
    assert!(mir.contains("HandlerEnter"), "{mir}");

    let mut sess = Session::default();
    let cil = dump(&mut sess, &case, Stage::Cil).unwrap();
    assert!(cil.contains("PyOps.ContextMethod"), "{cil}");
    assert!(cil.contains("PyErrors.Reraise"), "{cil}");
    assert!(cil.contains("PyBaseException handler"), "{cil}");
}

/// 错误输入：`async with` 明确拒绝，而不是静默当成同步上下文。
#[test]
fn snapshot_m2_8_async_with_diagnostic() {
    let scope = support::temp_scope::TempScope::new("phase2_contracts");
    let py_path = scope.path("ryupy_test_async_with.py");
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
    assert!(rendered.contains("E0100"), "{rendered}");
    assert!(rendered.contains("async with manager:"), "{rendered}");
    assert!(rendered.contains("2:"), "{rendered}");
}

/// M2.9：动态运算 / 调用的 PIC 调用点编号与运行时入口。
///
/// 这里锁住三件事：每个 `dyn.Add` 有独立 site 编号、`CallValue` / 捕获方法分流
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
    assert!(cil.contains("PyPic.Binary"), "{cil}");
    assert!(cil.contains("PyPic.CallValue"), "{cil}");
    assert!(cil.contains("PyPic.CallCapturedMethod0"), "{cil}");
}
