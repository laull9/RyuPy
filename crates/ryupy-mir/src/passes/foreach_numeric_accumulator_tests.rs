//! 通用迭代holder合同红测：每个独立变异先验证输入，再验证无改写和输出。
//! 手工夹具逐角色复现真实SimplifyCfg输出；另以真实源构建覆盖实际降级/元数据。
use super::{ForEachNumericAccumulator, MirPass};
use crate::mir::*;
use la_arena::{Arena, RawIdx};

fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
}
fn call(r: RuntimeFn, args: Vec<Operand>) -> Rvalue {
    Rvalue::Call {
        callee: Callee::Runtime(r),
        args,
    }
}
fn local(id: LocalId) -> Operand {
    Operand::Local(id)
}
fn sample() -> (Program, FnId, [BlockId; 6], [LocalId; 9]) {
    let mut locals = Arena::default();
    let ids: Vec<_> = (0..9)
        .map(|i| {
            locals.alloc(LocalDecl {
                ty: if i == 4 {
                    MirType::Bool
                } else if i == 8 {
                    MirType::I64
                } else {
                    MirType::Dynamic
                },
                name: match i {
                    0 => Some("xs".into()),
                    1 => Some("total".into()),
                    2 => Some("item".into()),
                    7 => Some("seed".into()),
                    _ => None,
                },
                is_cell: false,
            })
        })
        .collect();
    let [xs, total, target, iterator, ok, current, tmp, seed, depth] = ids.try_into().unwrap();
    let result = locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let mut blocks = Arena::default();
    let ids: Vec<_> = (0..6)
        .map(|_| {
            blocks.alloc(BasicBlock {
                stmts: vec![],
                terminator: Terminator::Unreachable,
            })
        })
        .collect();
    let [p, h, b, x, e, unused] = ids.try_into().unwrap();
    blocks[p].stmts = vec![
        assign(
            depth,
            call(
                RuntimeFn::TracePush,
                vec![
                    Operand::Const(Constant::Str("kernel.py".into())),
                    Operand::Const(Constant::Str("kernel".into())),
                ],
            ),
        ),
        assign(total, Rvalue::Use(local(seed))),
        assign(iterator, call(RuntimeFn::IterStart, vec![local(xs)])),
    ];
    blocks[p].terminator = Terminator::Goto(h);
    blocks[h].stmts = vec![assign(
        ok,
        call(RuntimeFn::IterNextOk, vec![local(iterator)]),
    )];
    blocks[h].terminator = Terminator::Branch {
        cond: local(ok),
        then_: b,
        else_: x,
    };
    blocks[b].stmts = vec![
        assign(current, call(RuntimeFn::IterValue, vec![local(iterator)])),
        assign(target, Rvalue::Use(local(current))),
        Statement::Line(5),
        assign(
            tmp,
            Rvalue::DynBinary {
                op: DynOp::Add,
                lhs: local(total),
                rhs: local(target),
                inplace: true,
            },
        ),
        assign(total, Rvalue::Use(local(tmp))),
    ];
    blocks[b].terminator = Terminator::Goto(h);
    blocks[x].stmts = vec![
        Statement::Line(6),
        assign(result, Rvalue::Use(local(total))),
    ];
    blocks[x].terminator = Terminator::Goto(e);
    blocks[e].stmts = vec![Statement::Eval(call(
        RuntimeFn::TracePopTo,
        vec![local(depth)],
    ))];
    blocks[e].terminator = Terminator::Return(Some(local(result)));
    let mut functions = Arena::default();
    let function = functions.alloc(Function {
        module_state: None,
        instance_capabilities: Default::default(),
        module_state_source: None,
        name: "kernel".into(),
        codegen_name: "kernel".into(),
        source_file: "kernel.py".into(),
        definition: None,
        params: vec![xs, seed],
        ret: MirType::Dynamic,
        body: Body {
            direct_globals_contracts: Vec::new(),
            source_handler_contracts: Vec::new(),
            locals,
            blocks,
            entry: p,
            regions: vec![],
        },
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: Some(GlobalId::from_raw(RawIdx::from(0))),
        env: None,
        dynamic_origin: None,
    });
    (
        Program {
            functions,
            classes: Arena::default(),
            globals: Arena::default(),
            entry: function,
            source_name: "kernel.py".into(),
        },
        function,
        [p, h, b, x, e, unused],
        [xs, total, target, iterator, ok, current, tmp, seed, depth],
    )
}
fn validate(p: &Program, label: &str) {
    assert!(
        crate::validate::validate(p).is_ok(),
        "{label}: {:?}",
        crate::validate::validate(p)
    );
}
fn holders(p: &Program) -> usize {
    crate::pretty::dump(p)
        .matches("NumericAccumulatorCreate")
        .count()
}
#[test]
fn real_shape_preserves_protocol_and_only_replaces_update() {
    let (mut p, id, [entry, _, body, exit, _, _], _) = sample();
    validate(&p, "输入");
    let before = crate::pretty::dump(&p);
    ForEachNumericAccumulator.run(&mut p.functions[id]);
    validate(&p, "输出");
    let after = crate::pretty::dump(&p);
    assert_eq!(holders(&p), 1);
    for name in [
        "IterStart",
        "IterNextOk",
        "IterValue",
        "TracePush",
        "TracePopTo",
    ] {
        assert_eq!(
            before.matches(name).count(),
            after.matches(name).count(),
            "{name}"
        );
    }
    assert!(matches!(
        p.functions[id].body.blocks[entry].stmts.last(),
        Some(Statement::Assign {
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::NumericAccumulatorCreate),
                ..
            },
            ..
        })
    ));
    assert!(matches!(
        p.functions[id].body.blocks[exit].stmts.first(),
        Some(Statement::Assign {
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::NumericAccumulatorValue),
                ..
            },
            ..
        })
    ));
    assert!(matches!(
        &p.functions[id].body.blocks[body].stmts[1],
        Statement::Assign {
            value: Rvalue::Use(_),
            ..
        }
    ));
    ForEachNumericAccumulator.run(&mut p.functions[id]);
    assert_eq!(holders(&p), 1, "单候选且重复运行不重写");
}

#[test]
fn reconstructed_mutations_are_valid_inputs_and_matcher_rejects() {
    let names = [
        "wrong_next_iterator",
        "wrong_current_iterator",
        "duplicated_next",
        "extra_current",
        "missing_current",
        "current_after_rhs",
        "body_extra_predecessor",
        "header_extra_predecessor",
        "exit_extra_predecessor",
        "entry_backedge",
        "target_overwrites_total",
        "hidden_operand_array_read",
        "second_total_write",
        "update_temp_escape",
        "cell_total",
        "missing_signature",
        "nested_environment",
        "inplace_false",
        "subtract_not_add",
        "unbound_initial",
    ];
    for (variant, name) in names.into_iter().enumerate() {
        let (mut p, id, [entry, h, b, x, _, unused], [xs, total, _, iterator, _, _, tmp, seed, _]) =
            sample();
        let f = &mut p.functions[id];
        match variant {
            0 | 1 => {
                let (block, index) = if variant == 0 { (h, 0) } else { (b, 0) };
                if let Statement::Assign {
                    value: Rvalue::Call { args, .. },
                    ..
                } = &mut f.body.blocks[block].stmts[index]
                {
                    args[0] = local(xs);
                }
            }
            2 => f.body.blocks[h].stmts.push(Statement::Eval(call(
                RuntimeFn::IterNextOk,
                vec![local(iterator)],
            ))),
            3 => f.body.blocks[b].stmts.insert(
                2,
                Statement::Eval(call(RuntimeFn::IterValue, vec![local(iterator)])),
            ),
            4 => {
                f.body.blocks[b].stmts.drain(..2);
            }
            5 => {
                // 真正RHS回调先于Current；即使validator允许，matcher也必须拒绝重排后的形状。
                let rhs = f.body.locals.alloc(LocalDecl {
                    ty: MirType::Dynamic,
                    name: None,
                    is_cell: false,
                });
                if let Statement::Assign {
                    value: Rvalue::DynBinary { rhs: operand, .. },
                    ..
                } = &mut f.body.blocks[b].stmts[3]
                {
                    *operand = local(rhs);
                }
                f.body.blocks[b].stmts.insert(
                    0,
                    assign(
                        rhs,
                        Rvalue::DynCallValue {
                            callee: local(seed),
                            args: vec![],
                        },
                    ),
                );
            }
            6..=8 => f.body.blocks[unused].terminator = Terminator::Goto([b, h, x][variant - 6]),
            9 => f.body.blocks[b].terminator = Terminator::Goto(entry),
            10 => {
                if let Statement::Assign { dest, .. } = &mut f.body.blocks[b].stmts[1] {
                    *dest = total;
                }
            }
            11 => f.body.blocks[b].stmts.insert(
                2,
                Statement::Eval(Rvalue::Use(Operand::Array(vec![Operand::Array(vec![
                    local(total),
                ])]))),
            ),
            12 => f.body.blocks[b]
                .stmts
                .insert(2, assign(total, Rvalue::Use(local(seed)))),
            13 => f.body.blocks[x]
                .stmts
                .insert(0, Statement::Eval(Rvalue::Use(local(tmp)))),
            14 => f.body.locals[total].is_cell = true,
            15 => f.signature_global = None,
            16 => {
                let env = f.body.locals.alloc(LocalDecl {
                    ty: MirType::Dynamic,
                    name: None,
                    is_cell: false,
                });
                f.env = Some(env);
                f.params.push(env);
            }
            17 | 18 => {
                if let Statement::Assign {
                    value: Rvalue::DynBinary { op, inplace, .. },
                    ..
                } = &mut f.body.blocks[b].stmts[3]
                {
                    if variant == 17 {
                        *inplace = false;
                    } else {
                        *op = DynOp::Sub;
                    }
                }
            }
            19 => {
                f.body.blocks[entry].stmts.remove(1);
            }
            _ => unreachable!(),
        }
        validate(&p, &format!("变异输入 {name}"));
        let before = crate::pretty::dump(&p);
        ForEachNumericAccumulator.run(&mut p.functions[id]);
        validate(&p, &format!("变异输出 {name}"));
        assert_eq!(before, crate::pretty::dump(&p), "matcher须原样拒绝 {name}");
    }
}

fn compile(source: &str, instance: bool) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("foreach.py", source);
    let (parsed, d) = ryupy_syntax::parse(file, source);
    assert!(d.is_empty(), "{d:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_globals: instance,
        ..Default::default()
    };
    let (hir, d) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(d.is_empty(), "{d:?}");
    let (ty, d) = ryupy_ty::check_module(&hir);
    assert!(d.is_empty(), "{d:?}");
    let (mut p, _, d) = crate::build_program(&hir, &ty);
    assert!(d.is_empty(), "{d:?}");
    validate(&p, "源构建");
    for (_, f) in p.functions.iter_mut() {
        super::SimplifyCfg.run(f);
    }
    validate(&p, "SimplifyCfg");
    p
}
fn apply(p: &mut Program) {
    for (_, f) in p.functions.iter_mut() {
        ForEachNumericAccumulator.run(f);
    }
    validate(p, "pass输出");
}
#[test]
fn real_sources_retain_generic_calls_and_accept_seed_identity() {
    for source in [
        "TOKEN=7\ndef kernel(n):\n    total=0\n    for item in range(n):\n        total+=TOKEN\n    return total\n",
        "def kernel(xs, seed):\n    total=seed\n    for item in xs:\n        total+=item\n    return total\n",
        "def kernel(xs, seed):\n    total=seed\n    for item in xs:\n        total+=callback(item)\n    return total\n",
    ] {
        let mut p = compile(source, true);
        apply(&mut p);
        assert_eq!(holders(&p), 1, "{}", crate::pretty::dump(&p));
    }
}
#[test]
fn source_observers_complex_cfg_and_unbound_seed_reject() {
    for source in [
        "def kernel(xs):\n    total=0\n    def get():\n        return total\n    for item in xs:\n        total+=item\n    return total\n",
        "def kernel(xs):\n    total=0\n    try:\n        for item in xs:\n            total+=item\n    finally:\n        callback()\n    return total\n",
        "def kernel(xs):\n    total=0\n    for item in xs:\n        if item:\n            break\n        total+=item\n    return total\n",
        "def kernel(xs):\n    total=0\n    for item in xs:\n        total+=item\n    else:\n        callback()\n    return total\n",
        "def kernel(xs):\n    total=0\n    for item in xs:\n        yield item\n        total+=item\n    return total\n",
        "def kernel(xs):\n    total=0\n    for item in xs:\n        total+=total\n    return total\n",
        "def kernel(xs):\n    total=0\n    for total in xs:\n        total+=1\n    return total\n",
        "def kernel(xs):\n    global item\n    total=0\n    for item in xs:\n        total+=item\n    return total\n",
        "def kernel(xs):\n    total=0\n    for item in xs:\n        total+=item\n        callback()\n    return total\n",
        "def kernel(xs):\n    if xs:\n        total=0\n    for item in xs:\n        total+=item\n    return total\n",
        "def kernel(xs):\n    total=0\n    for a,b in xs:\n        total+=a\n    return total\n",
        "def kernel(xs):\n    total=0\n    for a,*b in xs:\n        total+=a\n    return total\n",
        // first_parameter隐式读取total：表面零实参不能隐去frame依赖。
        "def kernel(total, xs):\n    total=total\n    for item in xs:\n        total+=callback()\n    return total\n",
    ] {
        let mut p = compile(source, true);
        let before = crate::pretty::dump(&p);
        apply(&mut p);
        assert_eq!(before, crate::pretty::dump(&p), "{source}");
    }
    // 删除局部在实例HIR明确NI；另用合法legacy MIR证明matcher拒绝，分开记账。
    let source = "def kernel(xs):\n    total=0\n    del total\n    for item in xs:\n        total+=callback()\n    return total\n";
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("del.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty());
    let info = ryupy_hir::ModuleInfo {
        instance_globals: true,
        ..Default::default()
    };
    let (_, diagnostics) = ryupy_hir::lower_module(&parsed, &info, source);
    assert!(
        diagnostics
            .iter()
            .any(|d| d.message.contains("非模块/global裸名del目标")),
        "{diagnostics:?}"
    );
    let mut p = compile(source, false);
    let before = crate::pretty::dump(&p);
    assert!(before.contains("MakeUnbound") && before.contains("LoadChecked"));
    apply(&mut p);
    assert_eq!(before, crate::pretty::dump(&p));
    // locals未进入实例HIR支持域；从合法legacy源单独验证整函数观察门。
    let mut p = compile(
        "def kernel(xs):\n    total=0\n    snapshot=locals()\n    for item in xs:\n        total+=item\n    return total\n",
        false,
    );
    let before = crate::pretty::dump(&p);
    apply(&mut p);
    assert_eq!(before, crate::pretty::dump(&p));
}
#[test]
fn seed_final_state_and_epilogue_tampering_reject() {
    for variant in 0..9 {
        let (mut p, id, [entry, _, body, _, end, _], [_, total, _, _, _, _, tmp, _, depth]) =
            sample();
        let f = &mut p.functions[id];
        match variant {
            0 => f.body.blocks[entry]
                .stmts
                .push(assign(total, Rvalue::Use(Operand::Const(Constant::Null)))),
            1 => f.body.blocks[entry]
                .stmts
                .push(assign(total, Rvalue::Use(local(tmp)))),
            2 => f.body.blocks[body].stmts.insert(
                2,
                Statement::Eval(call(
                    RuntimeFn::FunctionLocalsSync,
                    vec![
                        local(total),
                        Operand::Const(Constant::Str("total".into())),
                        local(total),
                    ],
                )),
            ),
            3 => f.body.blocks[end]
                .stmts
                .push(Statement::Eval(Rvalue::Use(local(total)))),
            4 => f.body.blocks[end].stmts.push(Statement::Eval(call(
                RuntimeFn::TracePopTo,
                vec![local(depth)],
            ))),
            5 => f.body.blocks[entry]
                .stmts
                .push(assign(total, call(RuntimeFn::MakeUnbound, vec![]))),
            6 => f.body.blocks[entry].stmts.push(assign(
                total,
                call(
                    RuntimeFn::LoadChecked,
                    vec![local(total), Operand::Const(Constant::Str("total".into()))],
                ),
            )),
            // 合法Dynamic数组仍超出种子来源合同；不能以“元素已绑定”放宽。
            7 | 8 => f.body.blocks[entry].stmts.push(assign(
                total,
                Rvalue::Use(if variant == 7 {
                    Operand::Array(vec![])
                } else {
                    Operand::Array(vec![Operand::Array(vec![Operand::Const(Constant::I64(1))])])
                }),
            )),
            _ => unreachable!(),
        }
        validate(&p, "附加红测输入");
        let before = crate::pretty::dump(&p);
        apply(&mut p);
        assert_eq!(before, crate::pretty::dump(&p), "{variant}");
    }
}
#[test]
fn old_wrappers_default_off_and_new_pass_precedes_dead_values() {
    for pipeline in [
        super::default_pipeline(),
        super::pipeline_with_direct_accumulator(false),
        super::pipeline_with_options(false, true),
        super::pipeline_with_slp_options(false, false, true, false),
        super::pipeline_with_foreach_options(true, false, false, false, true),
    ] {
        assert!(
            !pipeline
                .iter()
                .any(|p| p.name() == "foreach_numeric_accumulator_experimental")
        );
    }
    let pipeline = super::pipeline_with_foreach_options(false, false, false, false, true);
    let names: Vec<_> = pipeline.iter().map(|p| p.name()).collect();
    assert_eq!(
        &names[..3],
        &[
            "simplify_cfg",
            "foreach_numeric_accumulator_experimental",
            "dead_values"
        ]
    );
    assert_eq!(
        names
            .iter()
            .filter(|&&n| n == "foreach_numeric_accumulator_experimental")
            .count(),
        1
    );
    let (mut p, _, _, _) = sample();
    super::run_pipeline(&mut p, &pipeline).unwrap();
    assert_eq!(holders(&p), 1);
}
