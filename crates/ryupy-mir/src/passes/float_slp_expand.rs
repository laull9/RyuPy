//! 两lane SLP 展开：ISA与精确double双重守卫、显式Vector128运算、每lane独立最终盒。
//! 原动态链仍按lane0完整执行再lane1；原容器和第三尾项留在同一continuation，不移动副作用。
use super::float_slp::{Plan, Source};
use crate::mir::*;
use rustc_hash::FxHashMap;

pub(super) fn expand(f: &mut Function, plan: Plan, scalar: bool) {
    let start = plan.lanes[0].start;
    let finish = plan.lanes[1].finish;
    let tail = f.body.blocks[plan.block].stmts.split_off(finish + 1);
    let original = f.body.blocks[plan.block].stmts.split_off(start);
    let next = std::mem::replace(
        &mut f.body.blocks[plan.block].terminator,
        Terminator::Unreachable,
    );
    let continuation = block(f, tail, next);
    // 独立slow_final仍唯一定义；Use分隔两lane，后续FloatChain可优化标量回退而不重包SIMD。
    let slow_finals = [local(f, MirType::Dynamic), local(f, MirType::Dynamic)];
    let mut slow = Vec::new();
    for (mut statement, position) in original.into_iter().zip(start..=finish) {
        for (lane, temporary) in plan.lanes.iter().zip(slow_finals) {
            if position == lane.finish {
                if let Statement::Assign { dest, .. } = &mut statement {
                    *dest = temporary;
                }
            }
        }
        slow.push(statement);
        for (lane, temporary) in plan.lanes.iter().zip(slow_finals) {
            if position == lane.finish {
                slow.push(assign(lane.final_dest, Rvalue::Use(get(temporary))));
            }
        }
    }
    let fallback = block(f, slow, Terminator::Goto(continuation));
    let mut raw = FxHashMap::default();
    let mut snapshots = Vec::new();
    let mut fast = Vec::new();
    for input in &plan.inputs {
        let snapshot = local(f, MirType::Dynamic);
        let value = local(f, MirType::F64);
        f.body.blocks[plan.block]
            .stmts
            .push(assign(snapshot, Rvalue::Use(get(*input))));
        fast.push(assign(
            value,
            Rvalue::UnboxExactF64 {
                value: get(snapshot),
            },
        ));
        raw.insert(*input, value);
        snapshots.push(snapshot);
    }
    fast.extend(if scalar {
        scalar_body(f, &plan, &raw)
    } else {
        vector_body(f, &plan, &raw)
    });
    let mut success = block(f, fast, Terminator::Goto(continuation));
    for snapshot in snapshots.into_iter().rev() {
        let guard = local(f, MirType::Bool);
        success = block(
            f,
            vec![assign(
                guard,
                Rvalue::IsBoxedF64 {
                    value: get(snapshot),
                },
            )],
            branch(guard, success, fallback),
        );
    }
    let hardware = local(f, MirType::Bool);
    let hardware_block = block(
        f,
        vec![assign(hardware, Rvalue::IsVector128Supported)],
        branch(hardware, success, fallback),
    );
    f.body.blocks[plan.block].terminator = Terminator::Goto(hardware_block);
}
/// 对照臂保原lane次序及每一步舍入，ISA/type入口与packed臂完全共用。
fn scalar_body(f: &mut Function, plan: &Plan, raw: &FxHashMap<LocalId, LocalId>) -> Vec<Statement> {
    let mut statements = Vec::new();
    for lane in &plan.lanes {
        let mut results = Vec::new();
        for operation in &lane.operations {
            let source = |source: Source| match source {
                Source::Input(index) => raw[&lane.inputs[index]],
                Source::Step(index) => results[index],
            };
            let result = local(f, MirType::F64);
            statements.push(assign(
                result,
                Rvalue::Binary {
                    op: operation.op,
                    lhs: get(source(operation.lhs)),
                    rhs: get(source(operation.rhs)),
                },
            ));
            results.push(result);
        }
        statements.push(assign(
            lane.final_dest,
            Rvalue::Box {
                value: get(results[results.len() - 1]),
                from: MirType::F64,
            },
        ));
    }
    statements
}
fn vector_body(f: &mut Function, plan: &Plan, raw: &FxHashMap<LocalId, LocalId>) -> Vec<Statement> {
    let mut statements = Vec::new();
    let mut packed = Vec::new();
    for (low, high) in plan.lanes[0].inputs.iter().zip(&plan.lanes[1].inputs) {
        let vector = local(f, MirType::V128F64);
        statements.push(assign(
            vector,
            Rvalue::VectorPackF64 {
                low: get(raw[low]),
                high: get(raw[high]),
            },
        ));
        packed.push(vector);
    }
    let mut results = Vec::new();
    for operation in &plan.lanes[0].operations {
        let source = |source: Source| match source {
            Source::Input(index) => packed[index],
            Source::Step(index) => results[index],
        };
        let vector = local(f, MirType::V128F64);
        statements.push(assign(
            vector,
            Rvalue::VectorBinaryF64 {
                op: operation.op,
                lhs: get(source(operation.lhs)),
                rhs: get(source(operation.rhs)),
            },
        ));
        results.push(vector);
    }
    let result = results[results.len() - 1];
    for (index, lane) in plan.lanes.iter().enumerate() {
        let value = local(f, MirType::F64);
        statements.push(assign(
            value,
            Rvalue::VectorLaneF64 {
                value: get(result),
                lane: index as u8,
            },
        ));
        statements.push(assign(
            lane.final_dest,
            Rvalue::Box {
                value: get(value),
                from: MirType::F64,
            },
        ));
    }
    statements
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
