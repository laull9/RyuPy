//! 原frames真实MIR的合流反例；辅助域检查显式不冒充整程序通过。
use super::*;
use crate::trace_frame_tests::{SOURCE, build};
use crate::trace_lowering::LoweringOrigins;

fn rejected(program: &Program, origins: &LoweringOrigins, reason: &str) {
    assert!(validate::validate(program).is_ok(), "负例必须为有效MIR");
    let error = inspect(
        program,
        &origins.generators[0],
        &origins.generator_layouts[0],
    )
    .expect_err("实际坏MIR必须拒绝");
    assert!(error.contains(reason), "实际拒绝原因：{error}");
}

// 通过真实factory初值和resume恢复指令找参数槽，不能把factory LocalId当resume ID。
fn saved_parameter(program: &Program, origins: &LoweringOrigins) -> LocalId {
    let origin = &origins.generators[0];
    let layout = &origins.generator_layouts[0];
    crate::trace_frame::prepare(program, origin, layout)
        .expect("参数映射前先核真实factory初值与恢复模板");
    let factory = &program.functions[origin.factory];
    let parameter = factory.params[0];
    let source = layout
        .saved_locals
        .iter()
        .enumerate()
        .find_map(|(offset, local)| {
            let mut operand = &origin.initial_slots[offset + 4];
            for _ in 0..32 {
                let Operand::Local(id) = operand else {
                    return None;
                };
                if *id == parameter {
                    return Some((*local, offset + 4));
                }
                let definitions: Vec<_> = factory.body.blocks[factory.body.entry]
                    .stmts
                    .iter()
                    .filter_map(|statement| match statement {
                        Statement::Assign {
                            dest,
                            value: Rvalue::Use(value) | Rvalue::Box { value, .. },
                        } if dest == id => Some(value),
                        _ => None,
                    })
                    .collect();
                if definitions.len() != 1 {
                    return None;
                }
                operand = definitions[0];
            }
            None
        })
        .expect("实际factory参数必须有保存槽映射");
    let resume = &program.functions[origin.resume];
    assert!(
        resume.body.blocks[resume.body.entry]
            .stmts
            .iter()
            .any(|statement| {
                matches!(statement, Statement::Assign { dest, value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::ArrayGet), args,
        }} if *dest == source.0 && args.len() == 2
            && matches!(args[0], Operand::Local(frame) if frame == origin.frame_parameter)
            && matches!(args[1], Operand::Const(Constant::I64(slot)) if slot == source.1 as i64))
            }),
        "resume必须实际恢复所选参数槽"
    );
    source.0
}

// 在真实ready暂停全部spill之后造菱形；晚路径多一块，必须重处理已有输入。
fn diamond(program: &mut Program, origins: &LoweringOrigins, bad: bool, mixed: bool) {
    let origin = &origins.generators[0];
    let layout = &origins.generator_layouts[0];
    let source = saved_parameter(program, origins);
    let body = &mut program.functions[origin.resume].body;
    let exit = layout
        .exit_pcs
        .iter()
        .find(|(_, pc)| *pc != -1)
        .expect("真实暂停")
        .0;
    let cond = body.locals.alloc(LocalDecl {
        ty: MirType::Bool,
        name: None,
        is_cell: false,
    });
    let result = body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let terminator = std::mem::replace(
        &mut body.blocks[exit].terminator,
        Terminator::Goto(body.entry),
    );
    body.blocks[exit].stmts.push(Statement::Assign {
        dest: cond,
        value: Rvalue::DynTruthy {
            value: Operand::Const(Constant::None),
        },
    });
    body.blocks[exit].stmts.push(Statement::Assign {
        dest: result,
        value: Rvalue::Use(Operand::Const(Constant::None)),
    });
    let join = body.blocks.alloc(BasicBlock {
        stmts: Vec::new(),
        terminator: if mixed {
            Terminator::Return(Some(Operand::Local(result)))
        } else {
            terminator
        },
    });
    let good = body.blocks.alloc(BasicBlock {
        stmts: Vec::new(),
        terminator: Terminator::Goto(join),
    });
    let statements = if mixed {
        vec![Statement::Assign {
            dest: result,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::GeneratorRetry),
                args: Vec::new(),
            },
        }]
    } else if bad {
        vec![Statement::Assign {
            dest: source,
            value: Rvalue::Use(Operand::Const(Constant::None)),
        }]
    } else {
        Vec::new()
    };
    let changed = body.blocks.alloc(BasicBlock {
        stmts: statements,
        terminator: Terminator::Goto(join),
    });
    let delay = body.blocks.alloc(BasicBlock {
        stmts: Vec::new(),
        terminator: Terminator::Goto(changed),
    });
    body.blocks[exit].terminator = Terminator::Branch {
        cond: Operand::Local(cond),
        then_: good,
        else_: delay,
    };
}

#[test]
fn actual_mir_late_spill_write_is_rejected() {
    let (mut program, origins) = build(SOURCE);
    diamond(&mut program, &origins, true, false);
    rejected(&program, &origins, "当前版本");
}

#[test]
fn actual_mir_mixed_return_is_rejected() {
    let (mut program, origins) = build(SOURCE);
    diamond(&mut program, &origins, false, true);
    rejected(&program, &origins, "Return 混合");
}

#[test]
#[ignore = "旧反向frame证明实验已知失败，独立门保留原断言；不属于产品准入"]
fn actual_mir_diamond_reaches_fixed_point() {
    let (mut program, origins) = build(SOURCE);
    diamond(&mut program, &origins, false, false);
    assert!(validate::validate(&program).is_ok());
    let proof = inspect(
        &program,
        &origins.generators[0],
        &origins.generator_layouts[0],
    )
    .expect("实际多前驱合流应在原预算内闭合");
    assert!(proof.diagnostics.equal_joins > 0);
    assert!(!proof.complete_production_certificate);
}

#[test]
fn actual_mir_nested_frame_escape_is_rejected() {
    let (mut program, origins) = build(SOURCE);
    let origin = &origins.generators[0];
    let body = &mut program.functions[origin.resume].body;
    body.blocks[body.entry]
        .stmts
        .push(Statement::Eval(Rvalue::Use(Operand::Array(vec![
            Operand::Array(vec![Operand::Local(origin.frame_parameter)]),
        ]))));
    rejected(&program, &origins, "frame");
}

pub(super) fn seed_engine<'a>(
    program: &'a Program,
    origins: &'a LoweringOrigins,
) -> (Engine<'a>, State) {
    assert!(validate::validate(program).is_ok());
    let origin = &origins.generators[0];
    let layout = &origins.generator_layouts[0];
    let prepared = crate::trace_frame::prepare(program, origin, layout).expect("真实前置检查");
    let mut budget = Budget {
        diagnostics: Diagnostics::default(),
    };
    let lookup = LayoutIndex::build(prepared.body, layout, &mut budget).expect("有界三表");
    let mut engine = Engine {
        body: prepared.body,
        origin,
        layout,
        pcs: prepared.pcs,
        lookup,
        indices: HashMap::new(),
        nodes: Vec::new(),
        queue: VecDeque::new(),
        budget,
        reads: 0,
        retries: 0,
    };
    let state = domain::restart(
        engine.body,
        origin,
        layout.initial_pc,
        prepared
            .slots
            .into_iter()
            .map(|value| Values::single(value, &mut engine.budget))
            .collect::<Result<Vec<_>, _>>()
            .expect("初槽有界"),
        &mut engine.budget,
    )
    .expect("真实factory播种");
    (engine, state)
}

#[test]
fn actual_ir_seed_first_arrival_and_late_must_join() {
    let (program, origins) = build(SOURCE);
    let (mut engine, mut state) = seed_engine(&program, &origins);
    assert!(engine.queue.is_empty());
    assert!(engine.nodes.is_empty());
    let source = engine.layout.saved_locals[0];
    state.stored[index(source) + 4] = true;
    engine.enqueue(state.clone()).expect("首到直装");
    assert!(engine.nodes[0].input.stored[index(source) + 4]);
    engine.queue.pop_front();
    engine.nodes[0].dirty = false;
    state.stored[index(source) + 4] = false;
    state.sources[index(source)].clear();
    engine.enqueue(state).expect("晚到交must");
    assert!(!engine.nodes[0].input.stored[index(source) + 4]);
    assert!(engine.nodes[0].input.sources[index(source)].is_empty());
    assert!(engine.nodes[0].dirty);
    assert_eq!(engine.queue.len(), 1);
}

#[test]
fn actual_ir_seed_raise_prepare_and_phase_split() {
    let (program, origins) = build(SOURCE);
    let (mut engine, mut state) = seed_engine(&program, &origins);
    let local = engine.layout.saved_locals[0];
    state.locals[index(local)] =
        Values::checked(vec![Value::Exception, Value::None], &mut engine.budget).unwrap();
    let value = Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::RaisePrepare),
        args: vec![Operand::Local(local)],
    };
    let output = eval::evaluate(&value, &mut state, &mut engine).expect("逐原子RaisePrepare");
    assert_eq!(output.as_slice(), &[Value::Exception, Value::Opaque]);
    let pcs: Vec<_> = engine
        .layout
        .pc_targets
        .iter()
        .take(2)
        .map(|(pc, _)| Value::Integer(*pc))
        .collect();
    state.slots[0] = Values::checked(pcs.clone(), &mut engine.budget).unwrap();
    engine.enqueue(state.clone()).expect("所有合法PC分发");
    assert_eq!(engine.nodes.len(), 2);
    for pc in pcs {
        assert!(
            engine
                .nodes
                .iter()
                .any(|node| node.input.slots[0] == [pc.clone()])
        );
    }
    state.slots[0] = Values::single(Value::Integer(999999), &mut engine.budget).unwrap();
    assert!(
        engine
            .enqueue(state)
            .expect_err("未知PC不能筛掉")
            .contains("未知 PC")
    );
}

#[test]
fn actual_ir_seed_growth_and_inner_work_are_bounded() {
    let (program, origins) = build(SOURCE);
    let (mut engine, state) = seed_engine(&program, &origins);
    engine.enqueue(state.clone()).expect("首到收费");
    let first = engine.budget.diagnostics.cumulative_content;
    engine.enqueue(state.clone()).expect("equal不加内容费");
    assert_eq!(engine.budget.diagnostics.cumulative_content, first);
    let mut changed = state;
    changed.locals[0]
        .insert(Value::Exception, &mut engine.budget)
        .unwrap();
    engine.enqueue(changed.clone()).expect("排队中增长也收费");
    assert!(engine.budget.diagnostics.cumulative_content > first);
    engine.budget.diagnostics.cumulative_content = VALUE_CELL_BUDGET;
    changed.locals[0]
        .insert(Value::None, &mut engine.budget)
        .unwrap();
    assert!(
        engine
            .enqueue(changed)
            .expect_err("不能回收费用")
            .contains("内容预算")
    );
    let (mut engine, mut state) = seed_engine(&program, &origins);
    state.locals[0] =
        Values::checked((0..32).map(Value::Integer).collect(), &mut engine.budget).unwrap();
    state.locals[1] =
        Values::checked((0..32).map(Value::Integer).collect(), &mut engine.budget).unwrap();
    engine.budget.diagnostics.work_units = STEP_BUDGET - 100;
    let value = Rvalue::Binary {
        op: BinOp::Eq,
        lhs: Operand::Local(engine.layout.saved_locals[0]),
        rhs: Operand::Local(engine.layout.saved_locals[1]),
    };
    assert!(
        eval::evaluate(&value, &mut state, &mut engine)
            .expect_err("内层枚举也计work")
            .contains("工作预算")
    );
    // 两种空集合分支直接测试生产使用的成员原语，不借先行其它费用挡住。
    let mut budget = Budget {
        diagnostics: Diagnostics::default(),
    };
    let mut empty = Vec::new();
    budget.diagnostics.work_units = STEP_BUDGET;
    assert!(
        domain::add(&mut empty, Value::None, &mut budget)
            .expect_err("空Value首次新增仍须成员处理费")
            .contains("工作预算")
    );
    assert!(empty.is_empty());
    budget.diagnostics.work_units = 0;
    assert!(domain::add(&mut empty, Value::None, &mut budget).expect("余量足够时正常新增"));
    assert_eq!(empty, vec![Value::None]);
    assert_eq!(budget.diagnostics.work_units, 2);
    let left = [engine.layout.saved_locals[0]];
    budget.diagnostics.work_units = STEP_BUDGET;
    assert!(
        domain::intersect_sources(&left, &[], &mut budget)
            .expect_err("非空R与空R交仍须成员处理费")
            .contains("工作预算")
    );
    budget.diagnostics.work_units = 0;
    assert!(
        domain::intersect_sources(&left, &[], &mut budget)
            .expect("余量足够时删除旧must成员")
            .is_empty()
    );
    assert_eq!(budget.diagnostics.work_units, 1);
}
