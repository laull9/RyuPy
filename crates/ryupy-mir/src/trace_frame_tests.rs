//! 原 frames 的真实 build 输出与同构损坏；不执行 Python 或产品，不颁总证书。
use crate::trace_lowering::LoweringOrigins;
use crate::*;

pub(super) const SOURCE: &str = include_str!("../../../tests/cases/generator_dynamic/frames/entry.py");

pub(super) fn build(source: &str) -> (Program, LoweringOrigins) {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("frame_proof_input.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics, origins) = trace_lowering::build_for_audit(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert_eq!(origins.generators.len(), 1);
    assert_eq!(origins.generator_layouts.len(), 1);
    (program, origins)
}

fn rejected(program: &Program, origins: &LoweringOrigins) {
    assert!(
        trace_frame_dataflow::inspect(
            program,
            &origins.generators[0],
            &origins.generator_layouts[0]
        )
        .is_err()
    );
}

fn rejected_because(program: &Program, origins: &LoweringOrigins, reason: &str) {
    assert!(
        validate::validate(program).is_ok(),
        "负例必须是结构有效 MIR"
    );
    let error = trace_frame_dataflow::inspect(
        program,
        &origins.generators[0],
        &origins.generator_layouts[0],
    )
    .expect_err("有效 MIR 仍必须命中特定 frame 规则");
    assert!(error.contains(reason), "错误原因不符：{error}");
}

#[test]
fn original_frames_has_pc_partitioned_retry_frame_obligations() {
    let (program, origins) = build(SOURCE);
    let analysis = trace_frame_dataflow::inspect(
        &program,
        &origins.generators[0],
        &origins.generator_layouts[0],
    );
    let result = analysis.expect("原 frames 的私有 frame 义务应闭合");
    println!(
        "frame obligations: accesses={} phase_keys={} caught_reads={} retry_edges={}",
        result.frame_accesses, result.diagnostics.phase_keys, result.exception_reads, result.retry_edges
    );
    println!("original diagnostics: {:?}", result.diagnostics);
    assert!(result.frame_accesses > 0);
    assert!(result.exception_reads > 0);
    assert!(result.retry_edges > 0);
    assert!(result.diagnostics.phase_keys <= trace_frame_dataflow::STATE_BUDGET);
    assert!(!result.complete_production_certificate);
    assert!(!result.required_runtime_contracts.is_empty());
}

#[test]
fn renamed_generator_and_factory_alias_keep_frame_proof() {
    let source = SOURCE
        .replace("Target", "Receiver")
        .replace("generate", "produce")
        .replace(
            "it = produce(Receiver())",
            "factory = produce\nit = factory(Receiver())",
        );
    let (program, origins) = build(&source);
    let result = trace_frame_dataflow::inspect(
        &program,
        &origins.generators[0],
        &origins.generator_layouts[0],
    )
    .expect("重命名/alias 正例必须闭合，失败保留实际热点");
    println!("renamed diagnostics: {:?}", result.diagnostics);
}

fn retry_block(body: &mut Body) -> &mut BasicBlock {
    body.blocks
        .iter_mut()
        .find_map(|(_, block)| {
            block
                .stmts
                .iter()
                .any(|stmt| {
                    matches!(
                        stmt,
                        Statement::Assign {
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::GeneratorRetry),
                                ..
                            },
                            ..
                        }
                    )
                })
                .then_some(block)
        })
        .expect("真实 lowering 应含 Retry")
}

#[test]
fn retry_with_unknown_pc_is_rejected() {
    let (mut program, origins) = build(SOURCE);
    let block = retry_block(&mut program.functions[origins.generators[0].resume].body);
    let mut changed = false;
    for stmt in &mut block.stmts {
        if let Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ArraySet),
            args,
        }) = stmt
            && matches!(args[1], Operand::Const(Constant::I64(0)))
        {
            args[2] = Operand::Const(Constant::I64(999999));
            changed = true;
        }
    }
    assert!(changed);
    rejected(&program, &origins);
}

#[test]
fn retry_without_caught_write_is_rejected() {
    let (mut program, origins) = build(SOURCE);
    let block = retry_block(&mut program.functions[origins.generators[0].resume].body);
    let before = block.stmts.len();
    block.stmts.retain(|stmt| {
        !matches!(stmt,
        Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArraySet), args })
            if matches!(args[1], Operand::Const(Constant::I64(3))))
    });
    assert_eq!(block.stmts.len() + 1, before);
    rejected(&program, &origins);
}

#[test]
fn wrong_restore_slot_and_frame_alias_are_rejected() {
    for alias in [false, true] {
        let (mut program, origins) = build(SOURCE);
        let origin = &origins.generators[0];
        let body = &mut program.functions[origin.resume].body;
        let block = &mut body.blocks[body.entry];
        let mut changed = false;
        for stmt in &mut block.stmts {
            if let Statement::Assign {
                dest,
                value:
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ArrayGet),
                        args,
                    },
            } = stmt
                && matches!(args[1], Operand::Const(Constant::I64(4)))
            {
                if alias {
                    args[0] = Operand::Local(*dest);
                } else {
                    args[1] = Operand::Const(Constant::I64(5));
                }
                changed = true;
                break;
            }
        }
        assert!(changed);
        rejected(&program, &origins);
    }
}

#[test]
fn missing_spill_and_wrong_dispatch_map_are_rejected() {
    let (mut program, mut origins) = build(SOURCE);
    let block = retry_block(&mut program.functions[origins.generators[0].resume].body);
    let before = block.stmts.len();
    block.stmts.retain(|stmt| {
        !matches!(stmt,
        Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArraySet), args })
            if matches!(args[1], Operand::Const(Constant::I64(4))))
    });
    assert_eq!(block.stmts.len() + 1, before);
    rejected(&program, &origins);
    let (program, fresh) = build(SOURCE);
    origins = fresh;
    let targets = &mut origins.generator_layouts[0].pc_targets;
    assert!(targets.len() >= 2);
    let first = targets[0].1;
    targets[0].1 = targets[1].1;
    targets[1].1 = first;
    rejected(&program, &origins);
}

#[test]
fn factory_result_overwrite_after_make_generator_is_rejected() {
    let (mut program, origins) = build(SOURCE);
    let factory = &mut program.functions[origins.generators[0].factory];
    let block = &mut factory.body.blocks[factory.body.entry];
    let Terminator::Return(Some(Operand::Local(result))) = &block.terminator else {
        panic!("真实 factory 应返回生成器局部");
    };
    block.stmts.push(Statement::Assign {
        dest: *result,
        value: Rvalue::Use(Operand::Const(Constant::None)),
    });
    rejected_because(&program, &origins, "MakeGenerator");
}

#[test]
fn stale_copy_and_post_spill_local_write_are_rejected() {
    for stale_copy in [true, false] {
        let (mut program, origins) = build(SOURCE);
        let origin = &origins.generators[0];
        let source = program.functions[origin.factory].params[0];
        let body = &mut program.functions[origin.resume].body;
        let temporary = body.locals.alloc(LocalDecl {
            ty: MirType::Dynamic,
            name: None,
            is_cell: false,
        });
        let slot = source.into_raw().into_u32() as i64 + 4;
        let block = retry_block(body);
        let position = block
            .stmts
            .iter()
            .position(|stmt| {
                matches!(stmt,
            Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArraySet), args })
                if matches!(args[1], Operand::Const(Constant::I64(value)) if value == slot))
            })
            .expect("真实 Retry 保存普通参数");
        let overwrite = Statement::Assign {
            dest: source,
            value: Rvalue::Use(Operand::Const(Constant::None)),
        };
        if stale_copy {
            let Statement::Eval(Rvalue::Call { args, .. }) = &mut block.stmts[position] else {
                unreachable!();
            };
            args[2] = Operand::Local(temporary);
            block.stmts.insert(position, overwrite);
            block.stmts.insert(
                position,
                Statement::Assign {
                    dest: temporary,
                    value: Rvalue::Use(Operand::Local(source)),
                },
            );
        } else {
            block.stmts.insert(position + 1, overwrite);
        }
        rejected_because(&program, &origins, "版本");
    }
}

#[test]
fn clearing_pending_caught_without_read_is_rejected() {
    let (mut program, origins) = build(SOURCE);
    let body = &mut program.functions[origins.generators[0].resume].body;
    let mut changed = false;
    for (_, block) in body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign { value, .. } = statement
                && matches!(value, Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::ArrayGet), args
                } if matches!(args[1], Operand::Const(Constant::I64(3))))
            {
                *value = Rvalue::Use(Operand::Const(Constant::None));
                changed = true;
                break;
            }
        }
        if changed {
            break;
        }
    }
    assert!(changed);
    rejected_because(&program, &origins, "caught");
}

#[test]
fn caught_read_into_wrong_handler_local_is_rejected() {
    let (mut program, origins) = build(SOURCE);
    let origin = &origins.generators[0];
    let body = &mut program.functions[origin.resume].body;
    let wrong = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let (block, position, _) = origins.generator_layouts[0].catch_reads[0];
    let Statement::Assign { dest, .. } = &mut body.blocks[block].stmts[position] else {
        panic!("真实 catch 替换点必须是赋值");
    };
    *dest = wrong;
    rejected_because(&program, &origins, "caught");
}

#[test]
fn repeated_actual_match_block_converges_without_epoch_growth() {
    let (mut program, origins) = build(SOURCE);
    let body = &mut program.functions[origins.generators[0].resume].body;
    let (id, block) = body
        .blocks
        .iter_mut()
        .find(|(_, block)| {
            block.stmts.iter().any(|stmt| {
                matches!(
                    stmt,
                    Statement::Assign {
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ExceptionMatch),
                            ..
                        },
                        ..
                    }
                )
            })
        })
        .expect("原 frames 必须存在真实 matcher");
    let Terminator::Branch { else_, .. } = &mut block.terminator else {
        panic!("真实 matcher 必须条件分支");
    };
    // 同一保护区内回接，保留另一个出口；原指令、异常边、slot与旁证均不改。
    *else_ = id;
    assert!(validate::validate(&program).is_ok());
    let result = trace_frame_dataflow::inspect(
        &program,
        &origins.generators[0],
        &origins.generator_layouts[0],
    )
    .expect("真实 MIR 同一静态写复访须在原预算内收敛");
    println!("loop diagnostics: {:?}", result.diagnostics);
    assert!(result.diagnostics.self_edges > 0);
    assert!(result.diagnostics.equal_joins > 0);
    assert!(result.diagnostics.cumulative_content <= trace_frame_dataflow::VALUE_CELL_BUDGET);
}

#[test]
fn old_copy_never_revives_after_repeated_current_writes() {
    for writes in [2, 4, 8] {
        let (mut program, origins) = build(SOURCE);
        let origin = &origins.generators[0];
        let source = program.functions[origin.factory].params[0];
        let body = &mut program.functions[origin.resume].body;
        let temporary = body.locals.alloc(LocalDecl {
            ty: MirType::Dynamic,
            name: None,
            is_cell: false,
        });
        let slot = source.into_raw().into_u32() as i64 + 4;
        let block = retry_block(body);
        let position = block
            .stmts
            .iter()
            .position(|stmt| {
                matches!(stmt,
            Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArraySet), args })
                if matches!(args[1], Operand::Const(Constant::I64(value)) if value == slot))
            })
            .expect("真实 Retry 必须保存参数");
        let Statement::Eval(Rvalue::Call { args, .. }) = &mut block.stmts[position] else {
            unreachable!();
        };
        args[2] = Operand::Local(temporary);
        let mut prefix = vec![Statement::Assign {
            dest: temporary,
            value: Rvalue::Use(Operand::Local(source)),
        }];
        for _ in 0..writes {
            prefix.push(Statement::Assign {
                dest: source,
                value: Rvalue::Use(Operand::Const(Constant::None)),
            });
        }
        block.stmts.splice(position..position, prefix);
        // 即使偶数次写回同一抽象Value，也不能让旧copy因小epoch碰撞恢复资格。
        rejected_because(&program, &origins, "版本");
    }
}
