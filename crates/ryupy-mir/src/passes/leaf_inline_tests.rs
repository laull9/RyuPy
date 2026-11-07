//! 叶摘要、调用点预算与不可变身份槽的结构回归；运行时语义另由真实源码差分覆盖。
use super::{ProgramPass, leaf_inline::LeafInline};
use crate::mir::*;
use la_arena::Arena;

fn local(f: &mut Function, ty: MirType) -> LocalId {
    f.body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
}
fn get(id: LocalId) -> Operand {
    Operand::Local(id)
}
fn call(runtime: RuntimeFn, args: Vec<Operand>) -> Rvalue {
    Rvalue::Call {
        callee: Callee::Runtime(runtime),
        args,
    }
}
fn empty(name: &str) -> Function {
    let mut blocks = Arena::default();
    let entry = blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });
    Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: name.into(),
        codegen_name: name.into(),
        source_file: "leaf.py".into(),
        definition: None,
        params: vec![],
        ret: MirType::Dynamic,
        body: Body {
            source_handler_contracts: Vec::new(),
            locals: Arena::default(),
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
    }
}
fn sample(calls: usize) -> (Program, FnId, FnId) {
    let mut globals = Arena::default();
    let mut slot = || {
        globals.alloc(GlobalDecl {
            storage: crate::GlobalStorage::LegacyStatic,
            name: "slot".into(),
            ty: MirType::Dynamic,
        })
    };
    let fg = slot();
    let defaults = slot();
    let sig = slot();
    let alias = slot();
    let caller_sig = slot();
    let mut leaf = empty("twice");
    leaf.function_global = Some(fg);
    leaf.defaults_global = Some(defaults);
    leaf.signature_global = Some(sig);
    let param = local(&mut leaf, MirType::Dynamic);
    leaf.params.push(param);
    leaf.param_meta.push(ParamMeta {
        name: "value".into(),
        kind: ParamKind::PositionalOrKeyword,
        has_default: false,
        ty: MirType::Dynamic,
    });
    let depth = local(&mut leaf, MirType::I64);
    let result = local(&mut leaf, MirType::Dynamic);
    let b = &mut leaf.body.blocks[leaf.body.entry];
    b.stmts = vec![
        assign(
            depth,
            call(
                RuntimeFn::TracePush,
                vec![
                    Operand::Const(Constant::Str("leaf.py".into())),
                    Operand::Const(Constant::Str("twice".into())),
                ],
            ),
        ),
        Statement::Line(2),
        assign(
            result,
            Rvalue::DynBinary {
                op: DynOp::Add,
                lhs: get(param),
                rhs: get(param),
                inplace: false,
            },
        ),
        Statement::Eval(call(RuntimeFn::TracePopTo, vec![get(depth)])),
    ];
    b.terminator = Terminator::Return(Some(get(result)));
    let mut caller = empty("invoke");
    caller.signature_global = Some(caller_sig);
    let arg = local(&mut caller, MirType::Dynamic);
    caller.params.push(arg);
    let callee = local(&mut caller, MirType::Dynamic);
    let value = local(&mut caller, MirType::Dynamic);
    let mut statements = vec![
        Statement::SetGlobal {
            dest: alias,
            value: Operand::Global(fg),
        },
        assign(
            callee,
            call(
                RuntimeFn::LoadName,
                vec![
                    Operand::Global(alias),
                    Operand::Const(Constant::Str("alias".into())),
                ],
            ),
        ),
    ];
    statements.push(assign(
        arg,
        Rvalue::Box {
            value: Operand::Const(Constant::I64(7)),
            from: MirType::I64,
        },
    ));
    for _ in 0..calls {
        let result = local(&mut caller, MirType::Dynamic);
        statements.push(assign(
            result,
            Rvalue::DynCallValue {
                callee: get(callee),
                args: vec![get(arg)],
            },
        ));
    }
    statements.push(assign(value, Rvalue::Use(get(arg))));
    caller.body.blocks[caller.body.entry].stmts = statements;
    caller.body.blocks[caller.body.entry].terminator = Terminator::Return(Some(get(value)));
    let mut functions = Arena::default();
    let leaf = functions.alloc(leaf);
    let caller = functions.alloc(caller);
    (
        Program {
            functions,
            globals,
            classes: Arena::default(),
            entry: caller,
            source_name: "leaf.py".into(),
        },
        leaf,
        caller,
    )
}
fn wraps(f: &Function) -> usize {
    f.body
        .blocks
        .iter()
        .flat_map(|(_, b)| &b.stmts)
        .filter(|s| {
            matches!(
                s,
                Statement::Assign {
                    value: Rvalue::Binary {
                        op: BinOp::WrappingAdd,
                        ..
                    },
                    ..
                }
            )
        })
        .count()
}
#[test]
fn guard_uses_new_immutable_anchor_and_keeps_original_call() {
    let (mut p, leaf, caller) = sample(1);
    LeafInline.run(&mut p);
    let anchor = p.functions[leaf].specialization_identity_global.unwrap();
    assert_ne!(Some(anchor), p.functions[leaf].function_global);
    assert_eq!(wraps(&p.functions[caller]), 1);
    assert!(
        p.functions[caller]
            .body
            .blocks
            .iter()
            .flat_map(|(_, b)| &b.stmts)
            .any(|s| matches!(
                s,
                Statement::Assign {
                    value: Rvalue::DynCallValue { .. },
                    ..
                }
            ))
    );
    crate::validate::validate(&p).unwrap();
}
#[test]
fn default_and_non_leaf_bodies_do_not_get_guessed() {
    for change in 0..5 {
        let (mut p, leaf, caller) = sample(1);
        let f = &mut p.functions[leaf];
        match change {
            0 => f.param_meta[0].has_default = true,
            1 => f.param_meta[0].kind = ParamKind::KeywordOnly,
            2 => f.body.locals[f.params[0]].is_cell = true,
            3 => f.env = Some(f.params[0]),
            _ => f.body.blocks[f.body.entry].stmts.insert(
                1,
                Statement::Eval(call(
                    RuntimeFn::TracePopTo,
                    vec![Operand::Const(Constant::I64(0))],
                )),
            ),
        }
        LeafInline.run(&mut p);
        assert_eq!(wraps(&p.functions[caller]), 0);
    }
}
#[test]
fn per_function_budget_is_four_and_fallback_is_not_revisited() {
    let (mut p, leaf, caller) = sample(6);
    LeafInline.run(&mut p);
    assert_eq!(wraps(&p.functions[caller]), 4);
    assert!(p.functions[leaf].specialization_identity_global.is_some());
    crate::validate::validate(&p).unwrap();
}
#[test]
fn identity_slots_cannot_be_written_reused_or_exported() {
    for variant in 0..4 {
        let (mut p, leaf, caller) = sample(1);
        LeafInline.run(&mut p);
        let anchor = p.functions[leaf].specialization_identity_global.unwrap();
        match variant {
            0 => {
                let f = &mut p.functions[caller];
                f.body.blocks[f.body.entry]
                    .stmts
                    .push(Statement::SetGlobal {
                        dest: anchor,
                        value: Operand::Const(Constant::Null),
                    });
            }
            1 => {
                p.functions[leaf].specialization_identity_global = p.functions[leaf].function_global
            }
            2 => p.functions[leaf].defaults_global = None,
            _ => p.globals[anchor].ty = MirType::I64,
        }
        assert!(
            crate::validate::validate(&p)
                .unwrap_err()
                .iter()
                .any(|e| e.contains("身份锚点"))
        );
    }
    let (mut p, leaf, _) = sample(1);
    LeafInline.run(&mut p);
    let anchor = p.functions[leaf].specialization_identity_global.unwrap();
    let linked = crate::link(
        vec![(
            p,
            crate::UnitMeta {
                instance_capabilities: Default::default(),
                execution: crate::ModuleExecutionKind::LegacySingleton,
                environment_reason: "existing-contract".into(),
                location: Default::default(),
                file_backed_by_global: false,
                is_package: false,
                name: "test".into(),
                package: "".into(),
                file: "test.py".into(),
                protected_exports: vec![],
                exports: vec![ModuleExport {
                    name: "leak".into(),
                    value: anchor,
                    guard: ExportGuard::ValueSentinel,
                }],
            },
        )],
        0,
    );
    // 此fixture没有来源槽，唯一导出就是上面的真实特化锚点，不能误中其它守门。
    assert!(
        linked
            .functions
            .iter()
            .all(|(_, f)| f.dynamic_origin.is_none())
    );
    assert_eq!(
        crate::validate::validate_linked_exports(&linked).unwrap_err(),
        vec!["模块 `test` 不能导出私有函数身份/来源槽".to_string()]
    );
}

#[test]
fn recursion_check_is_only_on_success_and_has_void_zero_argument_abi() {
    let (mut p, _, caller) = sample(1);
    LeafInline.run(&mut p);
    let f = &p.functions[caller];
    let checks:Vec<_>=f.body.blocks.iter().filter(|(_,b)|b.stmts.iter().any(|s|matches!(s,Statement::Eval(Rvalue::Call{callee:Callee::Runtime(RuntimeFn::TraceCheckDepth),args})if args.is_empty()))).collect();
    assert_eq!(checks.len(), 1);
    assert!(matches!(
        checks[0].1.stmts.get(1),
        Some(Statement::Assign {
            value: Rvalue::Box { .. },
            ..
        })
    ));
    let bb = checks[0].0;
    p.functions[caller].body.blocks[bb].stmts[0] = Statement::Eval(call(
        RuntimeFn::TraceCheckDepth,
        vec![Operand::Const(Constant::I64(0))],
    ));
    assert!(
        crate::validate::validate(&p)
            .unwrap_err()
            .iter()
            .any(|e| e.contains("递归深度检查ABI"))
    );
}

#[test]
fn unknown_or_boolean_arguments_keep_original_call_without_anchor() {
    for boolean in [false, true] {
        let (mut p, leaf, caller) = sample(1);
        let f = &mut p.functions[caller];
        let arg = f.params[0];
        if boolean {
            for stmt in &mut f.body.blocks[f.body.entry].stmts {
                if let Statement::Assign { dest, value } = stmt
                    && *dest == arg
                {
                    *value = Rvalue::Box {
                        value: Operand::Const(Constant::Bool(true)),
                        from: MirType::Bool,
                    };
                }
            }
        } else {
            f.body.blocks[f.body.entry]
                .stmts
                .retain(|s| !matches!(s,Statement::Assign{dest,..}if *dest==arg));
        }
        LeafInline.run(&mut p);
        assert_eq!(wraps(&p.functions[caller]), 0);
        assert!(p.functions[leaf].specialization_identity_global.is_none());
    }
}

#[test]
fn pipeline_experiments_are_independent_and_default_leaf_is_off() {
    for direct in [false, true] {
        for leaf in [false, true] {
            let passes = super::pipeline_with_options(direct, leaf);
            assert_eq!(
                passes.iter().any(|p| p.name() == "guarded_leaf_inline"),
                leaf
            );
            assert_eq!(
                passes
                    .iter()
                    .any(|p| p.name() == "direct_numeric_accumulator_experimental"),
                direct
            );
            assert_eq!(
                passes.iter().any(|p| p.name() == "numeric_accumulator"),
                !direct
            );
        }
    }
    assert!(
        !super::default_pipeline()
            .iter()
            .any(|p| p.name() == "guarded_leaf_inline")
    );
    assert!(
        !super::pipeline_with_direct_accumulator(true)
            .iter()
            .any(|p| p.name() == "guarded_leaf_inline")
    );
}

/// 尾owner只是隐藏ABI，不改变Python实参个数；失败路仍保留原DynCallValue。
#[test]
fn dynamic_owner_keeps_leaf_arity_and_original_fallback() {
    let (mut p, leaf, caller) = sample(1);
    let f = &mut p.functions[leaf];
    let owner = local(f, MirType::Dynamic);
    f.params.push(owner);
    f.dynamic_origin = Some(DynamicFunctionOrigin {
        owner,
        source_global: None,
    });
    if let Statement::Assign {
        value: Rvalue::Call { callee, args },
        ..
    } = &mut f.body.blocks[f.body.entry].stmts[0]
    {
        *callee = Callee::Runtime(RuntimeFn::TracePushWithOrigin);
        args.push(get(owner));
    }
    LeafInline.run(&mut p);
    assert_eq!(wraps(&p.functions[caller]), 1);
    assert!(p.functions[caller].body.blocks.iter().any(|(_, b)| b.stmts.iter().any(|stmt|
        matches!(stmt, Statement::Assign {value: Rvalue::DynCallValue {args, ..}, ..} if args.len() == 1))));
    crate::validate::validate(&p).unwrap();
}

#[test]
fn dynamic_owner_is_not_an_arithmetic_parameter() {
    let (mut p, leaf, caller) = sample(1);
    let f = &mut p.functions[leaf];
    let owner = local(f, MirType::Dynamic);
    f.params.push(owner);
    f.dynamic_origin = Some(DynamicFunctionOrigin {
        owner,
        source_global: None,
    });
    for stmt in &mut f.body.blocks[f.body.entry].stmts {
        match stmt {
            Statement::Assign {
                value: Rvalue::Call { callee, args },
                ..
            } => {
                *callee = Callee::Runtime(RuntimeFn::TracePushWithOrigin);
                args.push(get(owner));
            }
            Statement::Assign {
                value: Rvalue::DynBinary { lhs, .. },
                ..
            } => *lhs = get(owner),
            _ => {}
        }
    }
    LeafInline.run(&mut p);
    assert_eq!(wraps(&p.functions[caller]), 0);
}
