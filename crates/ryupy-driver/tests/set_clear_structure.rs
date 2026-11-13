//! set.clear 只修运行时：真实源码经过正常前中后端，锁 direct/PIC、保存和展开路径。
//! 这是一个结构检查，不运行 Python/SDK/JIT，不计入三个应用的差分通过数。
use ryupy_mir::{
    Callee, Constant, Function, LocalId, Operand, Program, RuntimeFn, Rvalue, Statement,
};
use std::path::PathBuf;

// 每次独占目录；成功失败均保原 source/MIR/CIL，不能因断言失败丢现场。
fn root() -> PathBuf {
    let stamp = std::time::SystemTime::now()
        .duration_since(std::time::UNIX_EPOCH)
        .unwrap()
        .as_nanos();
    let path = std::env::temp_dir().join(format!(
        "ryupy-set-clear-structure-{}-{stamp}",
        std::process::id()
    ));
    std::fs::create_dir(&path).unwrap();
    eprintln!("set clear structure evidence: {}", path.display());
    path
}

const SOURCE: &str = "
def direct(values):
    return values.clear()
def touched(values, callback):
    return values.clear(callback())
def saved(values):
    method = values.clear
    return method()
def starred(values, items):
    return values.clear(*items)
def keyword(values, callback):
    return values.clear(bad=callback())
";

fn function<'a>(program: &'a Program, name: &str) -> &'a Function {
    let found: Vec<_> = program
        .functions
        .iter()
        .filter(|(_, func)| func.name == name)
        .collect();
    assert_eq!(found.len(), 1, "{name}");
    found[0].1
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
    rvalues(function)
        .filter(|value| {
            matches!(value,
            Rvalue::Call { callee: Callee::Runtime(actual), .. } if *actual == expected)
        })
        .count()
}

fn local(operand: &Operand) -> LocalId {
    let Operand::Local(id) = operand else {
        panic!("要求冻结局部，不读可变全局: {operand:?}")
    };
    *id
}

fn assert_direct(function: &Function, count: usize, callback: bool) {
    assert_eq!(runtime_calls(function, RuntimeFn::PrepareMethod), 0);
    assert_eq!(runtime_calls(function, RuntimeFn::IsModule), 1);
    assert_eq!(runtime_calls(function, RuntimeFn::CaptureModuleTarget), 1);
    assert_eq!(runtime_calls(function, RuntimeFn::CallCapturedMethod), 0);
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
                        let (before, value) = receiver.expect("接收者必须先快照");
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
                            matches!(&args[1], Operand::Const(Constant::Str(name)) if name == "clear")
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
                        assert_eq!(name, "clear");
                        assert_eq!(args.len(), count);
                        assert_eq!(local(capture_flag), flag.unwrap().1);
                        assert_eq!(local(obj), captured.unwrap().1);
                        assert!(captured.unwrap().0 < position);
                        if callback {
                            let (argument_position, result) = argument.expect("必须保留参数回调");
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

fn method<'a>(cil: &'a str, name: &str) -> &'a str {
    cil.split("    .method ")
        .skip(1)
        .find(|text| text.lines().next().unwrap().contains(&format!(" {name}(")))
        .expect(name)
}

#[test]
fn set_clear_keeps_real_direct_saved_and_expanded_call_paths() {
    let root = root();
    std::fs::write(root.join("structure.py"), SOURCE).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("structure.py", SOURCE);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    // 源码型结构门使用与真实link一致的模块身份；不能以空name冒充Known来源。
    let info = ryupy_hir::ModuleInfo {
        name: "__main__".into(),
        file: "structure.py".into(),
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, SOURCE);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (mut program, exports, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    ryupy_mir::passes::run_pipeline(&mut program, &ryupy_mir::passes::default_pipeline()).unwrap();
    std::fs::write(
        root.join("structure.mir"),
        ryupy_mir::pretty::dump(&program),
    )
    .unwrap();
    assert_direct(function(&program, "direct"), 0, false);
    assert_direct(function(&program, "touched"), 1, true);
    let saved = function(&program, "saved");
    assert_eq!(runtime_calls(saved, RuntimeFn::DynGetAttr), 1);
    assert_eq!(runtime_calls(saved, RuntimeFn::PrepareMethod), 0);
    assert_eq!(
        rvalues(saved)
            .filter(|value| matches!(value, Rvalue::DynCallValue { .. }))
            .count(),
        1
    );
    assert!(!rvalues(saved).any(|value| matches!(value, Rvalue::DynCallMethod { .. })));
    for name in ["starred", "keyword"] {
        let body = function(&program, name);
        assert_eq!(runtime_calls(body, RuntimeFn::CallCapturedMethod), 1);
        assert_eq!(runtime_calls(body, RuntimeFn::CaptureModuleTarget), 1);
        assert_eq!(runtime_calls(body, RuntimeFn::PrepareMethod), 0);
        assert!(!rvalues(body).any(|value| matches!(value, Rvalue::DynCallMethod { .. })));
    }
    assert_eq!(
        runtime_calls(function(&program, "starred"), RuntimeFn::StarArg),
        1
    );
    let linked = ryupy_mir::link(
        vec![(
            program,
            ryupy_mir::UnitMeta {
                instance_capabilities: Default::default(),
                execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
                environment_reason: "existing-contract".into(),
                location: ryupy_mir::ModuleLocation::LegacyDiagnostic,
                file_backed_by_global: true,
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
            assembly_name: "set_clear_structure".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    std::fs::write(root.join("structure.cil"), &cil).unwrap();
    let symbol = |name: &str| {
        let found: Vec<_> = linked
            .functions
            .iter()
            .filter(|(_, f)| f.name == name)
            .collect();
        assert_eq!(found.len(), 1);
        found[0].1.codegen_name.as_str()
    };
    for (name, count) in [("direct", 0), ("touched", 1)] {
        let body = method(&cil, symbol(name));
        assert!(
            body.contains(&format!("PyPic.CallCapturedMethod{count}(")),
            "{body}"
        );
        assert!(
            !body.contains("PrepareMethod(") && !body.contains("NewArray("),
            "普通调用不能为清空检查创建绑定包装/实参数组"
        );
    }
    assert!(method(&cil, symbol("saved")).contains("CallValue0("));
    assert!(method(&cil, symbol("starred")).contains("PyOps.CallCapturedMethod("));
    assert!(method(&cil, symbol("keyword")).contains("PyOps.CallCapturedMethod("));
}
