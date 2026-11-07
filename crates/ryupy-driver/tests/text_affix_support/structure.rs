//! 单个真实源码结构门：固定arity PIC、保存/暂停callee和opaque边界；不运行第五个应用。
//! source、阶段诊断、MIR和CIL先留存；不手造MIR，不以结构断言冒充helper零分配或实测提速。
use ryupy_mir::{
    Callee, Constant, FnId, Function, LinkedProgram, LocalId, MirType, Operand, RuntimeFn, Rvalue,
    Statement,
};

const SOURCE: &str = r#"
def exact_start():
    return 'alpha'.startswith('a')
def exact_end():
    return 'alpha'.endswith('a')
def window(value: str, prefix, start, end):
    return value.startswith(prefix, start, end)
def touched(value, callback):
    return value.startswith(callback(), 0, 3)
def ordinary(value, argument):
    return value.select(argument)
def saved(value):
    method = value.endswith
    return method('c', 0, 3)
def paused(holder):
    return holder.value.startswith((yield 'prefix'), 0, 3)
def add(left: int, right: int) -> int:
    return left + right
def numeric_loop(count: int) -> int:
    total = 0
    for index in range(count):
        total = add(total, index)
    return total
"#;

fn function<'a>(program: &'a LinkedProgram, name: &str) -> (FnId, &'a Function) {
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

fn calls(function: &Function, expected: RuntimeFn) -> usize {
    rvalues(function)
        .filter(|value| {
            matches!(value,
        Rvalue::Call { callee: Callee::Runtime(actual), .. } if *actual == expected)
        })
        .count()
}

fn local(operand: &Operand) -> LocalId {
    let Operand::Local(id) = operand else {
        panic!("要求已冻结局部: {operand:?}")
    };
    *id
}

fn direct(function: &Function, name: &str, arity: usize) {
    assert_eq!(calls(function, RuntimeFn::PrepareMethod), 0);
    assert_eq!(calls(function, RuntimeFn::IsModule), 1);
    assert_eq!(calls(function, RuntimeFn::CaptureModuleTarget), 1);
    assert_eq!(calls(function, RuntimeFn::CallCapturedMethod), 0);
    let methods: Vec<_> = rvalues(function)
        .filter_map(|value| match value {
            Rvalue::DynCallMethod {
                captured,
                name,
                args,
                ..
            } => Some((captured, name, args)),
            _ => None,
        })
        .collect();
    assert_eq!(methods.len(), 1);
    assert!(methods[0].0.is_some());
    assert_eq!(methods[0].1, name);
    assert_eq!(methods[0].2.len(), arity);
    assert!(!rvalues(function).any(|value| matches!(
        value,
        Rvalue::MakeFunction { .. } | Rvalue::NewObject { .. }
    )));
}

fn saved_and_opaque(program: &LinkedProgram) {
    let (_, saved) = function(program, "saved");
    assert_eq!(calls(saved, RuntimeFn::DynGetAttr), 1);
    assert_eq!(calls(saved, RuntimeFn::PrepareMethod), 0);
    let values: Vec<_> = rvalues(saved)
        .filter_map(|value| match value {
            Rvalue::DynCallValue { args, .. } => Some(args),
            _ => None,
        })
        .collect();
    assert_eq!(values.len(), 1);
    assert_eq!(values[0].len(), 3);
    assert!(!rvalues(saved).any(|value| matches!(value, Rvalue::DynCallMethod { .. })));
    let (_, window) = function(program, "window");
    assert_eq!(window.body.locals[window.params[0]].ty, MirType::Dynamic);
    assert_eq!(calls(window, RuntimeFn::CastToStr), 0);
}

// 参数包含真实用户回调，逐局部核对捕获目标与标志先于回调并被最后调用原样使用。
fn capture_before_argument(function: &Function) {
    let mut found = 0;
    for (_, block) in function.body.blocks.iter() {
        let mut receiver = None;
        let mut flag = None;
        let mut captured = None;
        let mut argument = None;
        for (position, statement) in block.stmts.iter().enumerate() {
            if let Statement::Assign { dest, value } = statement {
                match value {
                    Rvalue::Use(Operand::Local(id)) if *id == function.params[0] => {
                        receiver = Some((position, *dest));
                    }
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::IsModule),
                        args,
                    } => {
                        let (before, target) = receiver.expect("receiver必须先冻结");
                        assert!(before < position);
                        assert_eq!(local(&args[0]), target);
                        flag = Some((position, *dest));
                    }
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::CaptureModuleTarget),
                        args,
                    } => {
                        assert_eq!(local(&args[0]), receiver.unwrap().1);
                        assert!(
                            matches!(&args[1], Operand::Const(Constant::Str(name)) if name == "startswith")
                        );
                        assert!(flag.unwrap().0 < position);
                        captured = Some((position, *dest));
                    }
                    Rvalue::DynCallValue { .. } => argument = Some((position, *dest)),
                    Rvalue::DynCallMethod {
                        captured: Some(actual_flag),
                        obj,
                        name,
                        args,
                    } => {
                        assert_eq!(name, "startswith");
                        assert_eq!(args.len(), 3);
                        assert_eq!(local(actual_flag), flag.unwrap().1);
                        assert_eq!(local(obj), captured.unwrap().1);
                        let (argument_at, value) = argument.expect("保留真实参数回调");
                        assert!(captured.unwrap().0 < argument_at && argument_at < position);
                        assert_eq!(local(&args[0]), value);
                        found += 1;
                    }
                    _ => {}
                }
            }
        }
    }
    assert_eq!(found, 1);
}

fn prepared(program: &LinkedProgram) {
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
    assert_eq!(calls(factory, RuntimeFn::PrepareMethod), 0);
    let resume = bodies
        .iter()
        .find(|(_, func)| calls(func, RuntimeFn::PrepareMethod) == 1)
        .unwrap()
        .1;
    assert_eq!(
        calls(resume, RuntimeFn::DynGetAttr),
        1,
        "holder.value只读一次"
    );
    assert_eq!(calls(resume, RuntimeFn::CallValue), 0);
    assert_eq!(calls(resume, RuntimeFn::CallCapturedMethod), 0);
    assert!(!rvalues(resume).any(|value| matches!(value, Rvalue::DynCallMethod { .. })));
    let values: Vec<_> = rvalues(resume)
        .filter_map(|value| match value {
            Rvalue::DynCallValue { callee, args } => Some((callee, args)),
            _ => None,
        })
        .collect();
    assert_eq!(values.len(), 1);
    let (callee, args) = values[0];
    assert_eq!(args.len(), 3);
    let saved = local(callee);
    let input = local(&args[0]);
    assert_eq!(resume.params.len(), 1);
    let frame = resume.params[0];
    let slot = i64::from(saved.into_raw().into_u32()) + 4;
    assert_eq!(calls(resume, RuntimeFn::GeneratorInput), 1);
    assert!(resume.body.blocks.iter().any(|(_, block)| {
        let input_at = block.stmts.iter().position(|stmt| matches!(stmt,
            Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::GeneratorInput), args } }
            if *dest == input && matches!(args.as_slice(), [Operand::Local(id)] if *id == frame)));
        let call_at = block.stmts.iter().position(|stmt| matches!(stmt,
            Statement::Assign { value: Rvalue::DynCallValue { callee, args }, .. }
            if local(callee) == saved && local(&args[0]) == input));
        matches!((input_at, call_at), (Some(before), Some(after)) if before < after)
    }));
    assert!(resume.body.blocks[resume.body.entry].stmts.iter().any(|stmt| matches!(stmt,
        Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArrayGet), args } }
        if *dest == saved && matches!(args.as_slice(), [Operand::Local(id), Operand::Const(Constant::I64(index))] if *id == frame && *index == slot))));
    // 不能只证明“有spill”：绑定结果必须先写入真实saved局部，再于首次yield前spill。
    assert!(resume.body.blocks.iter().any(|(_, block)| {
        if !matches!(&block.terminator, ryupy_mir::Terminator::Return(Some(_))) {
            return false;
        }
        let mut receiver = None;
        let mut prepared = None;
        let mut saved_at = None;
        let mut spilled_at = None;
        for (position, statement) in block.stmts.iter().enumerate() {
            match statement {
                Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::DynGetAttr), args } }
                    if matches!(&args[1], Operand::Const(Constant::Str(name)) if name == "value") => receiver = Some((position, *dest)),
                Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::PrepareMethod), args } }
                    if receiver.is_some_and(|(_, value)| matches!(&args[0], Operand::Local(actual) if *actual == value))
                    && matches!(&args[1], Operand::Const(Constant::Str(name)) if name == "startswith") => prepared = Some((position, *dest)),
                Statement::Assign { dest, value: Rvalue::Use(Operand::Local(id)) }
                    if *dest == saved && prepared.is_some_and(|(_, target)| *id == target) => saved_at = Some(position),
                Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArraySet), args })
                    if matches!(args.as_slice(), [Operand::Local(actual), Operand::Const(Constant::I64(index)), Operand::Local(id)] if *actual == frame && *index == slot && *id == saved) => spilled_at = Some(position),
                _ => {}
            }
        }
        matches!((receiver, prepared, saved_at, spilled_at), (Some((read, _)), Some((prepare, _)), Some(save), Some(spill)) if read < prepare && prepare < save && save < spill)
    }));
}

fn numeric(program: &LinkedProgram) {
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
                | Rvalue::DynBinary { .. }
                | Rvalue::Box { .. }
                | Rvalue::Unbox { .. }
        )));
    }
    assert!(rvalues(numeric).any(|value| matches!(value,
        Rvalue::Call { callee: Callee::Fn(id), .. } if *id == add_id)));
}

fn method<'a>(cil: &'a str, name: &str) -> &'a str {
    cil.split("    .method ")
        .skip(1)
        .find(|text| text.lines().next().unwrap().contains(&format!(" {name}(")))
        .expect(name)
}

#[test]
fn text_affix_keeps_exact_ordinary_saved_and_prepared_shapes() {
    let root = super::support::create_root("structure");
    std::fs::write(root.join("structure.py"), SOURCE).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("structure.py", SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    std::fs::write(root.join("parse.diagnostics"), format!("{diagnostics:?}\n")).unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), SOURCE);
    std::fs::write(root.join("hir.diagnostics"), format!("{diagnostics:?}\n")).unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    std::fs::write(root.join("types.diagnostics"), format!("{diagnostics:?}\n")).unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (mut program, exports, diagnostics) = ryupy_mir::build_program(&hir, &types);
    std::fs::write(root.join("mir.diagnostics"), format!("{diagnostics:?}\n")).unwrap();
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let optimized =
        ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline());
    std::fs::write(root.join("pipeline.result"), format!("{optimized:?}\n")).unwrap();
    std::fs::write(
        root.join("structure.mir"),
        ryupy_mir::pretty::dump(&program),
    )
    .unwrap();
    optimized.unwrap();
    // 链接/CIL也先留证，再断言MIR形状，避免结构失败时缺少后端证据。
    let linked = ryupy_mir::link(
        vec![(
            program,
            ryupy_mir::UnitMeta {
                instance_capabilities: Default::default(),
                execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
                environment_reason: "existing-contract".into(),
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
    let generated = ryupy_codegen_cil::dump_cil(
        &linked,
        &ryupy_codegen_cil::EmitOptions {
            assembly_name: "text_affix_structure".into(),
            entry_source_path: None,
        },
    );
    std::fs::write(
        root.join("cil.result"),
        format!("{:?}\n", generated.as_ref().map(|_| ())),
    )
    .unwrap();
    let cil = generated.unwrap();
    std::fs::write(root.join("structure.cil"), &cil).unwrap();
    // 链接只给codegen_name加m0前缀，Python函数名不变；检查真实linked对象而不改写它。
    for (name, member, arity) in [
        ("exact_start", "startswith", 1),
        ("exact_end", "endswith", 1),
        ("window", "startswith", 3),
        ("touched", "startswith", 3),
        ("ordinary", "select", 1),
    ] {
        let body = method(&cil, &format!("m0_{name}"));
        assert!(
            body.contains(&format!("PyPic.CallCapturedMethod{arity}(")),
            "{body}"
        );
        assert!(
            !body.contains("PrepareMethod(") && !body.contains("NewArray("),
            "{body}"
        );
        let (_, function) = function(&linked, name);
        direct(function, member, arity);
    }
    capture_before_argument(function(&linked, "touched").1);
    saved_and_opaque(&linked);
    prepared(&linked);
    numeric(&linked);
    assert!(method(&cil, "m0_saved").contains("CallValue3("));
    assert!(
        method(&cil, "m0_add")
            .lines()
            .next()
            .unwrap()
            .contains("long m0_add(long, long)")
    );
}
