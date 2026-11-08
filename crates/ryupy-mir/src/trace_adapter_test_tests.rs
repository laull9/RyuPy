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
        assert!(valid(
            runtime,
            args.clone(),
            MirType::Dynamic,
            !source,
            false
        ));
        assert!(!valid(runtime, vec![], MirType::Dynamic, !source, false));
        assert!(!valid(
            runtime,
            vec![depth.clone()],
            MirType::Dynamic,
            !source,
            false
        ));
        assert!(!valid(
            runtime,
            args.clone(),
            MirType::Dynamic,
            !source,
            true
        ));
        assert!(!valid(
            runtime,
            args.clone(),
            MirType::Dynamic,
            source,
            false
        ));
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
    // 先保存实际typed MIR及每个入口的归属，再做断言；CI --nocapture保留失败现场。
    println!(
        "S2a actual typed MIR/CFG:\n{}",
        crate::pretty::dump(&program)
    );
    for (function_id, function) in program.functions.iter() {
        for (index, region) in function.body.regions.iter().enumerate() {
            let catch = &function.body.blocks[region.handler];
            println!(
                "region function={function_id:?} index={index} kind={:?} trampoline={:?} caught={:?} target={:?}",
                region.kind, region.handler, region.exc_local, catch.terminator
            );
        }
    }
    assert!(validate::validate(&program).is_ok());
    let mut receivers = std::collections::BTreeMap::new();
    let mut value_error_entries = Vec::new();
    let mut unsupported_entries = Vec::new();
    for (function_id, function) in program.functions.iter() {
        for region in &function.body.regions {
            let catch = &function.body.blocks[region.handler];
            let caught = region.exc_local.expect("接收区域必须持真实异常局部");
            assert!(catch.stmts.iter().any(|s| matches!(
                s,
                Statement::Assign { dest, value: Rvalue::CatchException } if *dest == caught
            )));
            let Terminator::Goto(body_id) = catch.terminator else {
                panic!("预期真实catch入口跳到接收体")
            };
            let body = &function.body.blocks[body_id];
            let expected = if matches!(region.kind, RegionKind::FinallyGuard) {
                RuntimeFn::TraceAdapterForward
            } else {
                RuntimeFn::TraceAdapterHandle
            };
            let calls: Vec<_> = body
                .stmts
                .iter()
                .enumerate()
                .filter_map(|(i, s)| match runtime(s) {
                    Some((actual, args))
                        if matches!(
                            actual,
                            RuntimeFn::TraceAdapterHandle | RuntimeFn::TraceAdapterForward
                        ) =>
                    {
                        Some((i, actual, args))
                    }
                    _ => None,
                })
                .collect();
            println!(
                "receiver function={function_id:?} body={body_id:?} trampoline={:?} calls={calls:?}",
                region.handler
            );
            assert_eq!(calls.len(), 1, "每个真实body恰好一次接收");
            let (receive_index, actual, args) = calls[0];
            assert_eq!(actual, expected);
            assert!(matches!(&args[0], Operand::Local(id) if *id == caught));
            let Operand::Local(depth) = args[1] else {
                panic!("必须使用真实入口深度")
            };
            let key = (
                function_id.into_raw().into_u32(),
                body_id.into_raw().into_u32(),
            );
            let receiver = (actual, caught, depth);
            if let Some(previous) = receivers.insert(key, receiver) {
                assert_eq!(previous, receiver, "汇合入口必须共用同一异常槽和真实深度");
            }
            if let RegionKind::Catch { type_names } = &region.kind {
                let entry = (key, region.handler, caught, depth);
                if type_names.len() == 1 && type_names[0] == "ValueError" {
                    value_error_entries.push(entry);
                }
                if type_names.len() == 1 && type_names[0] == "io.UnsupportedOperation" {
                    unsupported_entries.push(entry);
                }
            }
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
    println!("unique receivers={receivers:?}");
    println!(
        "ValueError entries={value_error_entries:?}; io.UnsupportedOperation entries={unsupported_entries:?}"
    );
    assert_eq!(value_error_entries.len(), 1);
    assert_eq!(unsupported_entries.len(), 1);
    let (value_body, value_entry, value_caught, value_depth) = value_error_entries[0];
    let (io_body, io_entry, io_caught, io_depth) = unsupported_entries[0];
    assert_ne!(value_entry, io_entry, "typed EH必须保持独立trampoline");
    assert_eq!(value_body, io_body, "两个typed入口必须汇合到同一接收body");
    assert_eq!((value_caught, value_depth), (io_caught, io_depth));
    let handles = receivers
        .values()
        .filter(|(kind, _, _)| *kind == RuntimeFn::TraceAdapterHandle)
        .count();
    let forwards = receivers
        .values()
        .filter(|(kind, _, _)| *kind == RuntimeFn::TraceAdapterForward)
        .count();
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

#[test]
fn trace_adapter_native_typed_cfg_keeps_real_call_and_handle_before_pop() {
    for source in [
        include_str!("../../../tests/fixtures/trace_adapter_s2/native_local.py"),
        include_str!("../../../tests/fixtures/trace_adapter_s2/native_outer.py"),
    ] {
        let (program, diagnostics) = compile(source);
        assert!(diagnostics.is_empty(), "{diagnostics:?}");
        assert!(validate::validate(&program).is_ok());
        println!("S2a native typed MIR:\n{}", crate::pretty::dump(&program));
        let calls: Vec<_> = program
            .functions
            .iter()
            .flat_map(|(_, f)| f.body.blocks.iter())
            .flat_map(|(_, b)| b.stmts.iter())
            .filter_map(runtime)
            .map(|(kind, _)| kind)
            .collect();
        assert!(calls.contains(&RuntimeFn::FunctionLocalsValidate));
        assert!(!calls.contains(&RuntimeFn::TraceAdapterExplicit));
        let mut handles = 0;
        for (_, function) in program.functions.iter() {
            for region in &function.body.regions {
                if !matches!(region.kind, RegionKind::Catch { .. }) {
                    continue;
                }
                let caught = region.exc_local.expect("真实catch异常局部");
                let entry = &function.body.blocks[region.handler];
                assert!(entry.stmts.iter().any(|s| matches!(
                    s,
                    Statement::Assign { dest, value: Rvalue::CatchException } if *dest == caught
                )));
                let Terminator::Goto(body_id) = entry.terminator else {
                    panic!("静态catch必须进入真实接收体")
                };
                let body = &function.body.blocks[body_id];
                let handle = body
                    .stmts
                    .iter()
                    .position(|s| matches!(runtime(s), Some((RuntimeFn::TraceAdapterHandle, _))))
                    .expect("保留Handle");
                let (_, args) = runtime(&body.stmts[handle]).unwrap();
                assert!(matches!(&args[0], Operand::Local(id) if *id == caught));
                let pop = body
                    .stmts
                    .iter()
                    .position(|s| matches!(runtime(s), Some((RuntimeFn::TracePopTo, _))))
                    .expect("保留PopTo");
                let enter = body
                    .stmts
                    .iter()
                    .position(|s| matches!(runtime(s), Some((RuntimeFn::HandlerEnter, _))))
                    .expect("保留HandlerEnter");
                assert!(handle < pop && pop < enter);
                handles += 1;
            }
        }
        assert!(handles > 0);
    }
}
