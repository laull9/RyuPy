//! 循环浮点状态展开：原对象缓存/原生值/模式分离，跨迭代只在可观察边界物化。
//!
//! mode && total==CLR null 表示快路产生了尚未装箱的新值；零轮与慢路返回的原盒保持身份。
//! 原动态链与写回整体搬入慢路，任何用户异常发生前都不提前提交下一轮状态。
use super::float_chain::Plan as Chain;
use super::float_loop::Plan;
use crate::mir::*;
use rustc_hash::FxHashMap;

pub(super) fn expand(
    f: &mut Function,
    chain: &Chain,
    plan: Plan,
    island: Option<super::numeric_list::Plan>,
) {
    if let Some(numeric) = &island {
        super::numeric_list_expand::rewrite_factory(f, numeric);
    }
    let raw_total = local(f, MirType::F64);
    let mode = local(f, MirType::Bool);
    f.body.locals[raw_total].name = Some("$float_loop_raw".into());
    f.body.locals[mode].name = Some("$float_loop_mode".into());
    let is_null = local(f, MirType::Bool);
    let dirty = local(f, MirType::Bool);
    let tail = f.body.blocks[chain.block]
        .stmts
        .split_off(chain.start + chain.steps.len() + 1);
    let original = f.body.blocks[chain.block].stmts.split_off(chain.start);
    let next = std::mem::replace(
        &mut f.body.blocks[chain.block].terminator,
        Terminator::Unreachable,
    );
    let continuation = block(f, tail, next);
    let reload = block(
        f,
        vec![unbox(raw_total, plan.total)],
        Terminator::Goto(continuation),
    );
    let mut slow_statements = original;
    slow_statements.push(assign(
        mode,
        Rvalue::IsBoxedF64 {
            value: get(plan.total),
        },
    ));
    let slow = block(f, slow_statements, branch(mode, reload, continuation));
    let materialize = block(
        f,
        vec![boxed(plan.total, raw_total)],
        Terminator::Goto(slow),
    );
    let fallback = block(
        f,
        dirty_guard(plan.total, mode, is_null, dirty),
        branch(dirty, materialize, slow),
    );

    let mut raw = FxHashMap::default();
    raw.insert(plan.total, raw_total);
    let mut fast_statements = Vec::new();
    let mut unboxes = Vec::new();
    let mut boxed_inputs = Vec::new();
    let mut snapshots = Vec::new();
    for &input in chain.inputs.iter().filter(|&&input| input != plan.total) {
        let snapshot = local(f, MirType::Dynamic);
        boxed_inputs.push(assign(snapshot, Rvalue::Use(get(input))));
        let native = local(f, MirType::F64);
        raw.insert(input, native);
        snapshots.push(snapshot);
        unboxes.push(unbox(native, snapshot));
    }
    let mut last_result = raw_total;
    for step in &chain.steps {
        let result = local(f, MirType::F64);
        fast_statements.push(assign(
            result,
            Rvalue::Binary {
                op: step.op,
                lhs: get(raw[&step.lhs]),
                rhs: get(raw[&step.rhs]),
            },
        ));
        raw.insert(step.dest, result);
        last_result = result;
    }
    fast_statements.extend([
        assign(raw_total, Rvalue::Use(get(last_result))),
        assign(plan.total, Rvalue::Use(Operand::Const(Constant::Null))),
    ]);
    let (arithmetic, mut fast) = if island.is_some() {
        let arithmetic = block(f, fast_statements, Terminator::Goto(continuation));
        let unbox_inputs = block(f, unboxes, Terminator::Goto(arithmetic));
        (arithmetic, unbox_inputs)
    } else {
        // 未命中岛时保原FloatLoop的单一快块与块数，不为实验改变既有生成形状。
        unboxes.extend(fast_statements);
        let fast = block(f, unboxes, Terminator::Goto(continuation));
        (fast, fast)
    };
    for snapshot in snapshots.into_iter().rev() {
        let check = local(f, MirType::Bool);
        fast = block(
            f,
            vec![assign(
                check,
                Rvalue::IsBoxedF64 {
                    value: get(snapshot),
                },
            )],
            branch(check, fast, fallback),
        );
    }
    if let Some(numeric) = &island {
        let original_prefix = f.body.blocks[chain.block]
            .stmts
            .split_off(numeric.prefix_start);
        let terminator = super::numeric_list_expand::read_path(
            f,
            numeric,
            original_prefix,
            boxed_inputs,
            super::numeric_list_expand::ReadTargets {
                mode,
                ordinary_guards: fast,
                dynamic: fallback,
                arithmetic,
                raw: &raw,
            },
        );
        f.body.blocks[chain.block].terminator = terminator;
    } else {
        f.body.blocks[chain.block].stmts.extend(boxed_inputs);
        f.body.blocks[chain.block].terminator = branch(mode, fast, fallback);
    }

    // 每次外层重入都重置模式和raw；保留pre中原Box/Use写入的对象，不复制初始盒。
    let initial_reload = block(
        f,
        vec![unbox(raw_total, plan.total)],
        Terminator::Goto(plan.header),
    );
    f.body.blocks[plan.preheader].stmts.extend([
        assign(raw_total, Rvalue::Use(Operand::Const(Constant::F64(0.0)))),
        assign(
            mode,
            Rvalue::IsBoxedF64 {
                value: get(plan.total),
            },
        ),
    ]);
    f.body.blocks[plan.preheader].terminator = branch(mode, initial_reload, plan.header);

    // 唯一正常出口只物化dirty值；慢路产生的共享盒与零轮初值都直接沿用。
    let exit_stmts = std::mem::take(&mut f.body.blocks[plan.exit].stmts);
    let exit_term = std::mem::replace(
        &mut f.body.blocks[plan.exit].terminator,
        Terminator::Unreachable,
    );
    let (final_exit, exit_box) = if let Some(numeric) = &island {
        super::numeric_list_expand::exit_paths(f, numeric, raw_total, exit_stmts, exit_term)
    } else {
        let final_exit = block(f, exit_stmts, exit_term);
        let exit_box = block(
            f,
            vec![boxed(plan.total, raw_total)],
            Terminator::Goto(final_exit),
        );
        (final_exit, exit_box)
    };
    f.body.blocks[plan.exit].stmts = dirty_guard(plan.total, mode, is_null, dirty);
    f.body.blocks[plan.exit].terminator = branch(dirty, exit_box, final_exit);
}
fn dirty_guard(total: LocalId, mode: LocalId, is_null: LocalId, dirty: LocalId) -> Vec<Statement> {
    vec![
        assign(
            is_null,
            Rvalue::Binary {
                op: BinOp::Eq,
                lhs: get(total),
                rhs: Operand::Const(Constant::Null),
            },
        ),
        assign(
            dirty,
            Rvalue::Binary {
                op: BinOp::BitAnd,
                lhs: get(mode),
                rhs: get(is_null),
            },
        ),
    ]
}
fn unbox(dest: LocalId, source: LocalId) -> Statement {
    assign(dest, Rvalue::UnboxExactF64 { value: get(source) })
}
fn boxed(dest: LocalId, source: LocalId) -> Statement {
    assign(
        dest,
        Rvalue::Box {
            value: get(source),
            from: MirType::F64,
        },
    )
}
fn local(f: &mut Function, ty: MirType) -> LocalId {
    f.body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn block(f: &mut Function, stmts: Vec<Statement>, terminator: Terminator) -> BlockId {
    f.body.blocks.alloc(BasicBlock { stmts, terminator })
}
fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
}
fn get(local: LocalId) -> Operand {
    Operand::Local(local)
}
fn branch(cond: LocalId, then_: BlockId, else_: BlockId) -> Terminator {
    Terminator::Branch {
        cond: get(cond),
        then_,
        else_,
    }
}
