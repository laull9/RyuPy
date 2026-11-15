//! 显式实验入口：守卫失败回原四语句，保留此前capture和具名tuple。默认流水线不调用。
use crate::mir::*;
use crate::records_increment_plan::{Plan, plans};
use rustc_hash::FxHashSet;

pub fn apply_records_increment_experiment(program: &mut Program) -> usize {
    let mut changed = 0;
    for (_, f) in program.functions.iter_mut() {
        let reachable = reachable(f);
        // 单次预先取计划，逆序拆分保证原块内更早索引不变；不再次优化慢路。
        let mut candidates = plans(f);
        candidates.retain(|p| reachable.contains(&p.block));
        for plan in candidates.into_iter().rev() {
            expand(f, plan);
            changed += 1;
        }
    }
    changed
}

fn reachable(f: &Function) -> FxHashSet<BlockId> {
    let mut seen = FxHashSet::default();
    let mut pending = vec![f.body.entry];
    while let Some(id) = pending.pop() {
        if !seen.insert(id) {
            continue;
        }
        match f.body.blocks[id].terminator {
            Terminator::Goto(next) => pending.push(next),
            Terminator::Branch { then_, else_, .. } => pending.extend([then_, else_]),
            _ => {}
        }
    }
    seen
}

fn expand(f: &mut Function, plan: Plan) {
    let tail = f.body.blocks[plan.block].stmts.split_off(plan.start + 4);
    let original = f.body.blocks[plan.block].stmts.split_off(plan.start);
    let terminator = std::mem::replace(
        &mut f.body.blocks[plan.block].terminator,
        Terminator::Unreachable,
    );
    let continuation = f.body.blocks.alloc(BasicBlock {
        stmts: tail,
        terminator,
    });
    let fallback = f.body.blocks.alloc(BasicBlock {
        stmts: original,
        terminator: Terminator::Goto(continuation),
    });
    let condition = f.body.locals.alloc(LocalDecl {
        ty: MirType::Bool,
        name: None,
        is_cell: false,
    });
    f.body.blocks[plan.block].stmts.push(Statement::Assign {
        dest: condition,
        value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::RecordsTryIncrementMaterialized),
            args: vec![Operand::Local(plan.receiver), Operand::Local(plan.key)],
        },
    });
    f.body.blocks[plan.block].terminator = Terminator::Branch {
        cond: Operand::Local(condition),
        then_: continuation,
        else_: fallback,
    };
}

/// 显式程序级实验pass；默认pipeline不注册。
#[cfg(feature = "records-increment-experiment")]
pub struct RecordsIncrementExperiment;
#[cfg(feature = "records-increment-experiment")]
impl crate::passes::ProgramPass for RecordsIncrementExperiment {
    fn name(&self) -> &'static str {
        "records_increment_experiment"
    }
    fn run(&self, program: &mut Program) {
        apply_records_increment_experiment(program);
    }
}
