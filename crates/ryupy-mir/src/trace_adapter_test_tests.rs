//! S2a真实前端到typed CFG顺序与固定ABI拒绝测试；不靠文本grep代替控制流身份。
use crate::*;
use la_arena::Arena;

fn valid(runtime: RuntimeFn, args: Vec<Operand>, ret: MirType, eval: bool, array: bool) -> bool {
    let mut locals = Arena::default();
    let dest = locals.alloc(LocalDecl {
        ty: ret,
        name: None,
        is_cell: false,
    });
    let value = if array {
        Rvalue::CallWithArgs {
            callee: runtime,
            prefix: args,
            args: vec![],
        }
    } else {
        Rvalue::Call {
            callee: Callee::Runtime(runtime),
            args,
        }
    };
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![if eval {
            Statement::Eval(value)
        } else {
            Statement::Assign { dest, value }
        }],
        terminator: Terminator::Return(None),
    });
    let mut functions = Arena::default();
    let entry = functions.alloc(Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "module_abi".into(),
        codegen_name: "module_abi".into(),
        source_file: "module_abi.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Unit,
        body: Body {
            source_handler_contracts: Vec::new(),
            locals,
            blocks,
            entry,
            regions: vec![],
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    });
    crate::validate::validate(&Program {
        functions,
        classes: Arena::default(),
        globals: Arena::default(),
        entry,
        source_name: "module_abi.py".into(),
    })
    .is_ok()
}

#[test]
fn trace_adapter_abi_rejects_wrong_arguments_results_and_array_calls() {
    let object = Operand::Const(Constant::Null);
    let depth = Operand::Const(Constant::I64(1));
    for runtime in [
        RuntimeFn::TraceAdapterExplicit,
        RuntimeFn::TraceAdapterHandle,
        RuntimeFn::TraceAdapterForward,
    ] {
        let source = runtime == RuntimeFn::TraceAdapterExplicit;
        let args = if source {
            vec![object.clone()]
        } else {
            vec![object.clone(), depth.clone()]
        };
        assert!(valid(runtime, args.clone(), MirType::Dynamic, !source, false));
        assert!(!valid(runtime, vec![], MirType::Dynamic, !source, false));
        assert!(!valid(
            runtime,
            vec![depth.clone()],
            MirType::Dynamic,
            !source,
            false
        ));
        assert!(!valid(runtime, args.clone(), MirType::Dynamic, !source, true));
        assert!(!valid(runtime, args.clone(), MirType::Dynamic, source, false));
        if source {
            assert!(!valid(runtime, args, MirType::I64, false, false));
        } else {
            assert!(!valid(
                runtime,
                vec![object.clone(), object.clone()],
                MirType::Dynamic,
                true,
                false
            ));
        }
    }
}

fn compile(source: &str) -> (Program, Vec<ryupy_base::Diagnostic>) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("trace_adapter.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) =
        ryupy_hir::lower_module(&parsed, &ryupy_hir::ModuleInfo::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    (program, diagnostics)
}

fn runtime(statement: &Statement) -> Option<(RuntimeFn, &[Operand])> {
    match statement {
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(runtime),
            args,
        })
        | Statement::Assign {
            value:
                Rvalue::Call {
                    callee: Callee::Runtime(runtime),
                    args,
                },
            ..
        } => Some((*runtime, args)),
        _ => None,
    }
}

#[test]
fn trace_adapter_real_typed_cfg_receives_before_pop_with_exact_caught_local() {
    let source = include_str!("../../../tests/fixtures/trace_adapter_s2/explicit_inner.py");
    let (program, diagnostics) = compile(source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(validate::validate(&program).is_ok());
    let mut handles = 0;
    let mut forwards = 0;
    for (_, function) in program.functions.iter() {
        for region in &function.body.regions {
            let catch = &function.body.blocks[region.handler];
            let caught = region.exc_local.expect("接收区域必须持真实异常局部");
            assert!(catch.stmts.iter().any(|s| matches!(
                s,
                Statement::Assign { dest, value: Rvalue::CatchException } if *dest == caught
            )));
            let Terminator::Goto(body) = catch.terminator else {
                panic!("预期真实catch入口跳到接收体")
            };
            let body = &function.body.blocks[body];
            let expected = if matches!(region.kind, RegionKind::FinallyGuard) {
                forwards += 1;
                RuntimeFn::TraceAdapterForward
            } else {
                handles += 1;
                RuntimeFn::TraceAdapterHandle
            };
            let (receive_index, depth) = body
                .stmts
                .iter()
                .enumerate()
                .find_map(|(i, s)| match runtime(s) {
                    Some((actual, args)) if actual == expected => {
                        assert!(matches!(&args[0], Operand::Local(id) if *id == caught));
                        let Operand::Local(depth) = args[1] else {
                            panic!("必须使用真实入口深度")
                        };
                        Some((i, depth))
                    }
                    _ => None,
                })
                .expect("真实catch必须接到适配器");
            let pop_index = body
                .stmts
                .iter()
                .position(|s| matches!(runtime(s), Some((RuntimeFn::TracePopTo, _))))
                .expect("保留原PopTo");
            let enter_index = body
                .stmts
                .iter()
                .position(|s| matches!(runtime(s), Some((RuntimeFn::HandlerEnter, _))))
                .expect("保留原上下文登记");
            assert!(receive_index < pop_index && pop_index < enter_index);
            assert!(
                function
                    .body
                    .blocks
                    .iter()
                    .flat_map(|(_, b)| &b.stmts)
                    .any(|s| {
                        matches!(s, Statement::Assign { dest, .. } if *dest == depth)
                            && matches!(
                                runtime(s),
                                Some((RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin, _))
                            )
                    })
            );
        }
    }
    assert_eq!(handles, 2);
    assert_eq!(forwards, 2);
}

#[test]
fn trace_adapter_admission_rejects_unimplemented_semantics() {
    for source in [
        include_str!("../../../tests/fixtures/trace_adapter_s2/bare_reject.py"),
        include_str!("../../../tests/fixtures/trace_adapter_s2/cause_reject.py"),
        include_str!("../../../tests/fixtures/trace_adapter_s2/generator_reject.py"),
        include_str!("../../../tests/fixtures/trace_adapter_s2/nested_handler_reject.py"),
        "try:\n    pass\nfinally:\n    pass\n",
    ] {
        let (_, diagnostics) = compile(source);
        assert!(!diagnostics.is_empty());
    }
}
