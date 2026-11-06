//! list.pop索引修复只改运行时入口：真实源码锁普通PIC、保存方法、展开及暂停路径不漂移。
//! 不手造MIR、不运行性能；原源码/MIR/CIL先落盘，失败目录保留供实证审阅。
use ryupy_mir::{
    Callee, Constant, FnId, Function, LocalId, MirType, Operand, Program, RuntimeFn, Rvalue,
    Statement,
};
use std::path::PathBuf;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT_ROOT: AtomicU64 = AtomicU64::new(0);
struct Root(PathBuf);
impl Drop for Root {
    fn drop(&mut self) {
        if !std::thread::panicking() {
            let _ = std::fs::remove_dir_all(&self.0);
        }
    }
}
fn root() -> Root {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let serial = NEXT_ROOT.fetch_add(1, Ordering::Relaxed);
    let path = std::env::temp_dir().join(format!(
        "ryupy-list-pop-index-structure-{}-{stamp}-{serial}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("list pop index structure evidence: {}", path.display());
    Root(path)
}
const SOURCE: &str = "
def direct_clear(values):
    return values.clear(0)
def direct_reverse(values):
    return values.reverse(0)
def direct_copy(values):
    return values.copy(0)
def direct_pop(values):
    return values.pop(0)
def touched(values, callback):
    return values.pop(callback())
def star_clear(values, items):
    return values.clear(*items)
def keyword_copy(values, callback):
    return values.copy(bad=callback())
def saved(values):
    method = values.pop
    return method(0)
def paused(holder):
    return holder.value.pop((yield 1))
def add(left: int, right: int) -> int:
    return left + right
def numeric_loop(count: int) -> int:
    total = 0
    for index in range(count):
        total = add(total, index)
    return total
def swap(left, right):
    left, right = right, left
    return left + right
";
fn function<'a>(program: &'a Program, name: &str) -> (FnId, &'a Function) {
    let found: Vec<_> = program
        .functions
        .iter()
        .filter(|(_, func)| func.name == name)
        .collect();
    assert_eq!(found.len(), 1, "{name}");
    found[0]
}
fn rvalues(function: &Function) -> impl Iterator<Item = &Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| block.stmts.iter())
        .filter_map(|stmt| match stmt {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
}
fn runtime_calls(function: &Function, expected: RuntimeFn) -> usize {
    rvalues(function).filter(|value| matches!(value, Rvalue::Call { callee: Callee::Runtime(actual), .. } if *actual == expected)).count()
}
fn local(operand: &Operand) -> LocalId {
    let Operand::Local(id) = operand else {
        panic!("要求冻结局部，不读可变全局: {operand:?}")
    };
    *id
}
fn assert_direct(function: &Function, method: &str, count: usize, callback: bool) {
    assert_eq!(runtime_calls(function, RuntimeFn::PrepareMethod), 0);
    assert_eq!(runtime_calls(function, RuntimeFn::IsModule), 1);
    assert_eq!(runtime_calls(function, RuntimeFn::CaptureModuleTarget), 1);
    assert_eq!(runtime_calls(function, RuntimeFn::CallCapturedMethod), 0);
    assert!(!rvalues(function).any(|value| matches!(
        value,
        Rvalue::MakeFunction { .. } | Rvalue::NewObject { .. }
    )));
    let mut found = 0;
    for (_, block) in function.body.blocks.iter() {
        let mut receiver = None;
        let mut flag = None;
        let mut captured = None;
        let mut argument = None;
        for (position, stmt) in block.stmts.iter().enumerate() {
            if let Statement::Assign { dest, value } = stmt {
                match value {
                    Rvalue::Use(Operand::Local(id)) if *id == function.params[0] => {
                        receiver = Some((position, *dest))
                    }
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::IsModule),
                        args,
                    } => {
                        let (before, value) = receiver.expect("receiver必须先快照");
                        assert!(before < position);
                        assert_eq!(local(&args[0]), value);
                        flag = Some((position, *dest));
                    }
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::CaptureModuleTarget),
                        args,
                    } => {
                        assert_eq!(local(&args[0]), receiver.unwrap().1);
                        assert!(
                            matches!(&args[1], Operand::Const(Constant::Str(name)) if name == method)
                        );
                        assert!(flag.unwrap().0 < position);
                        captured = Some((position, *dest));
                    }
                    Rvalue::DynCallValue { .. } if callback => argument = Some((position, *dest)),
                    Rvalue::DynCallMethod {
                        captured: Some(capture_flag),
                        obj,
                        name,
                        args,
                    } => {
                        assert_eq!(name, method);
                        assert_eq!(args.len(), count);
                        assert_eq!(local(capture_flag), flag.unwrap().1);
                        assert_eq!(local(obj), captured.unwrap().1);
                        assert!(captured.unwrap().0 < position);
                        if callback {
                            let (argument_position, result) = argument.expect("实际保留参数回调");
                            assert!(
                                captured.unwrap().0 < argument_position
                                    && argument_position < position
                            );
                            assert_eq!(local(&args[0]), result);
                        }
                        found += 1;
                    }
                    _ => {}
                }
            }
        }
    }
    assert_eq!(found, 1);
}
fn assert_saved_and_expansion(program: &Program) {
    let (_, saved) = function(program, "saved");
    assert_eq!(runtime_calls(saved, RuntimeFn::DynGetAttr), 1);
    assert_eq!(runtime_calls(saved, RuntimeFn::PrepareMethod), 0);
    assert_eq!(
        rvalues(saved)
            .filter(|value| matches!(value, Rvalue::DynCallValue { .. }))
            .count(),
        1
    );
    assert!(!rvalues(saved).any(|value| matches!(value, Rvalue::DynCallMethod { .. })));
    for name in ["star_clear", "keyword_copy"] {
        let (_, func) = function(program, name);
        assert_eq!(runtime_calls(func, RuntimeFn::CallCapturedMethod), 1);
        assert_eq!(runtime_calls(func, RuntimeFn::CaptureModuleTarget), 1);
        assert_eq!(runtime_calls(func, RuntimeFn::PrepareMethod), 0);
        assert!(!rvalues(func).any(|value| matches!(value, Rvalue::DynCallMethod { .. })));
    }
    assert_eq!(
        runtime_calls(function(program, "star_clear").1, RuntimeFn::StarArg),
        1
    );
    let (_, keyword) = function(program, "keyword_copy");
    assert_eq!(
        rvalues(keyword)
            .filter(|value| matches!(value, Rvalue::DynCallValue { .. }))
            .count(),
        1
    );
    assert!(rvalues(keyword).any(|value| matches!(value,
        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::CallCapturedMethod), args }
        if matches!(&args[4], Operand::Array(names) if matches!(names.as_slice(), [Operand::Const(Constant::Str(name))] if name == "bad")))));
}
fn assert_paused(program: &Program) {
    let bodies: Vec<_> = program
        .functions
        .iter()
        .filter(|(_, func)| func.name == "paused")
        .collect();
    assert_eq!(bodies.len(), 2);
    let factory = bodies
        .iter()
        .find(|(_, func)| rvalues(func).any(|value| matches!(value, Rvalue::MakeGenerator { .. })))
        .unwrap()
        .1;
    assert_eq!(runtime_calls(factory, RuntimeFn::PrepareMethod), 0);
    let resume = bodies
        .iter()
        .find(|(_, func)| runtime_calls(func, RuntimeFn::PrepareMethod) == 1)
        .unwrap()
        .1;
    assert_eq!(
        runtime_calls(resume, RuntimeFn::DynGetAttr),
        1,
        "holder.value只在第一次暂停前读取"
    );
    // 单位置参数保持固定arity路径；唯一实际调用必须使用暂停前保存的callee。
    assert_eq!(runtime_calls(resume, RuntimeFn::CallValue), 0);
    assert_eq!(runtime_calls(resume, RuntimeFn::CallCapturedMethod), 0);
    assert!(!rvalues(resume).any(|value| matches!(value, Rvalue::DynCallMethod { .. })));
    let calls: Vec<_> = rvalues(resume)
        .filter_map(|value| match value {
            Rvalue::DynCallValue { callee, args } => Some((callee, args)),
            _ => None,
        })
        .collect();
    assert_eq!(calls.len(), 1);
    let (callee, args) = calls[0];
    assert_eq!(args.len(), 1);
    let saved = local(callee);
    let argument = local(&args[0]);
    assert_eq!(resume.params.len(), 1);
    let frame = resume.params[0];
    assert_eq!(runtime_calls(resume, RuntimeFn::GeneratorInput), 1);
    // 恢复块先从同一frame取得send值，再将该值原样交给唯一调用。
    assert!(resume.body.blocks.iter().any(|(_, block)| {
        let input_at = block.stmts.iter().position(|stmt| matches!(stmt,
            Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::GeneratorInput), args } }
            if *dest == argument && matches!(args.as_slice(), [Operand::Local(id)] if *id == frame)));
        let call_at = block.stmts.iter().position(|stmt| matches!(stmt,
            Statement::Assign { value: Rvalue::DynCallValue { callee, args }, .. }
            if local(callee) == saved && matches!(args.as_slice(), [Operand::Local(id)] if *id == argument)));
        matches!((input_at, call_at), (Some(input), Some(call)) if input < call)
    }));
    let slot = i64::from(saved.into_raw().into_u32()) + 4;
    // 每次恢复先在入口reload同一frame槽，再进入状态分派及实际调用。
    assert!(resume.body.blocks[resume.body.entry].stmts.iter().any(|stmt| matches!(stmt,
        Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArrayGet), args } }
        if *dest == saved && matches!(args.as_slice(), [Operand::Local(id), Operand::Const(Constant::I64(index))] if *id == frame && *index == slot))));
    // 当前源唯一yield 1：同一返回块先读holder.value、绑定、保存，再spill实际callee。
    // 所有原locals都会spill，所以只有“存在ArraySet”不足以证明暂停前已经初始化。
    assert!(resume.body.blocks.iter().any(|(_, block)| {
        let ryupy_mir::Terminator::Return(Some(Operand::Local(yielded))) = &block.terminator else { return false };
        if !block.stmts.iter().any(|stmt| matches!(stmt,
            Statement::Assign { dest, value: Rvalue::Box { value: Operand::Const(Constant::I64(1)), .. } } if dest == yielded)) {
            return false;
        }
        let mut receiver = None;
        let mut prepared = None;
        let mut saved_at = None;
        let mut spilled_at = None;
        for (position, stmt) in block.stmts.iter().enumerate() {
            match stmt {
                Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::DynGetAttr), args } }
                    if matches!(&args[1], Operand::Const(Constant::Str(name)) if name == "value") => receiver = Some((position, *dest)),
                Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::PrepareMethod), args } }
                    if receiver.is_some_and(|(_, id)| matches!(&args[0], Operand::Local(actual) if *actual == id))
                    && matches!(&args[1], Operand::Const(Constant::Str(name)) if name == "pop") => prepared = Some((position, *dest)),
                Statement::Assign { dest, value: Rvalue::Use(Operand::Local(id)) }
                    if *dest == saved && prepared.is_some_and(|(_, prepared)| *id == prepared) => saved_at = Some(position),
                Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArraySet), args })
                    if matches!(args.as_slice(), [Operand::Local(actual_frame), Operand::Const(Constant::I64(index)), Operand::Local(id)] if *actual_frame == frame && *index == slot && *id == saved) => spilled_at = Some(position),
                _ => {}
            }
        }
        match (receiver, prepared, saved_at, spilled_at) {
            (Some((read, _)), Some((prepare, _)), Some(save), Some(spill)) => read < prepare && prepare < save && save < spill,
            _ => false,
        }
    }));
}
fn assert_static(program: &Program) {
    let (add_id, add) = function(program, "add");
    let (_, numeric) = function(program, "numeric_loop");
    for body in [add, numeric] {
        assert_eq!(body.ret, MirType::I64);
        assert!(
            body.params
                .iter()
                .all(|id| body.body.locals[*id].ty == MirType::I64)
        );
        assert!(!rvalues(body).any(|value| matches!(
            value,
            Rvalue::DynCallValue { .. }
                | Rvalue::DynCallMethod { .. }
                | Rvalue::Box { .. }
                | Rvalue::Unbox { .. }
        )));
    }
    assert!(rvalues(numeric).any(
        |value| matches!(value, Rvalue::Call { callee: Callee::Fn(id), .. } if *id == add_id)
    ));
    let (_, swap) = function(program, "swap");
    assert!(!rvalues(swap).any(|value| matches!(
        value,
        Rvalue::CallWithArgs {
            callee: RuntimeFn::MakeTuple,
            ..
        } | Rvalue::Call {
            callee: Callee::Runtime(
                RuntimeFn::GetItem
                    | RuntimeFn::UnpackCheck
                    | RuntimeFn::UnpackSequence
                    | RuntimeFn::UnpackEx
            ),
            ..
        }
    )));
}
fn method<'a>(cil: &'a str, name: &str) -> &'a str {
    cil.split("    .method ")
        .skip(1)
        .find(|text| text.lines().next().unwrap().contains(&format!(" {name}(")))
        .expect(name)
}

#[test]
fn list_pop_index_fix_keeps_direct_saved_and_prepared_call_paths() {
    let root = root();
    std::fs::write(root.0.join("structure.py"), SOURCE).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("structure.py", SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (mut program, exports, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    std::fs::write(
        root.0.join("structure.mir"),
        ryupy_mir::pretty::dump(&program),
    )
    .unwrap();
    for (name, member, arity) in [
        ("direct_clear", "clear", 1),
        ("direct_reverse", "reverse", 1),
        ("direct_copy", "copy", 1),
        ("direct_pop", "pop", 1),
    ] {
        assert_direct(function(&program, name).1, member, arity, false);
    }
    assert_direct(function(&program, "touched").1, "pop", 1, true);
    assert_saved_and_expansion(&program);
    assert_paused(&program);
    assert_static(&program);
    let linked = ryupy_mir::link(
        vec![(
            program,
            ryupy_mir::UnitMeta {
                execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
                location: ryupy_mir::ModuleLocation::LegacyDiagnostic,
                file_backed_by_global: false,
                is_package: false,
                protected_exports: vec![],
                name: "__main__".into(),
                package: String::new(),
                file: "structure.py".into(),
                exports,
            },
        )],
        0,
    );
    let cil = ryupy_codegen_cil::dump_cil(
        &linked,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "list_pop_index_structure".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    std::fs::write(root.0.join("structure.cil"), &cil).unwrap();
    for (name, arity) in [
        ("direct_clear", 1),
        ("direct_reverse", 1),
        ("direct_copy", 1),
        ("direct_pop", 1),
        ("touched", 1),
    ] {
        let body = method(&cil, &format!("m0_{name}"));
        assert!(
            body.contains(&format!("PyPic.CallCapturedMethod{arity}(")),
            "{body}"
        );
        assert!(
            !body.contains("PrepareMethod(") && !body.contains("NewArray("),
            "普通调用不能为新检查改成绑定包装/实参数组"
        );
    }
    assert!(method(&cil, "m0_star_clear").contains("PyOps.CallCapturedMethod("));
    assert!(method(&cil, "m0_keyword_copy").contains("PyOps.CallCapturedMethod("));
    assert!(method(&cil, "m0_saved").contains("CallValue1("));
    assert!(
        method(&cil, "m0_add")
            .lines()
            .next()
            .unwrap()
            .contains("long m0_add(long, long)")
    );
}
