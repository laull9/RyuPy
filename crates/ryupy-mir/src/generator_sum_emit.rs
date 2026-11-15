//! 实验性genexpr→sum批量入口：复用经核验的同一字段状态机，只融合精确range消费。
//! 普通恢复入口不改；守卫失败恰好普通恢复一次，累计拒绝项保留原暂停PC交回sum。
use crate::generator_product::DeferredGenerators;
use crate::generator_sum_fields::Snapshot;
use crate::generator_sum_range_plan::{RangePlan, analyze_source};
use crate::generators::{call, local};
use crate::mir::*;

pub(super) struct Candidate {
    function: FnId,
    plan: RangePlan,
    snapshot: Snapshot,
}

// 仅同次build内部调用；候选不对外提供构造器或可变IR，也不跨link缓存。
pub(super) fn prepare(program: &Program, deferred: &DeferredGenerators) -> Vec<Candidate> {
    deferred
        .comprehension_inputs
        .iter()
        .filter_map(|&(function, input)| {
            let plan = analyze_source(program, deferred, function, input)?;
            if !plan.direct_yield(&program.functions[function]) {
                return None;
            }
            let snapshot = Snapshot::capture(program, function, plan.residents.clone());
            Some(Candidate { function, plan, snapshot })
        })
        .collect()
}

pub(super) fn finish(program: &mut Program, candidates: Vec<Candidate>) {
    for candidate in candidates {
        // 字段降级不能完整证明时保留普通结果，不安装能力，也不放宽旧fallback。
        if candidate.snapshot.verify(program).is_err() {
            continue;
        }
        if let Some((resume, mut batch)) = build_batch(program, &candidate) {
            batch.codegen_name.push_str("$sum_batch");
            let sum = program.functions.alloc(batch);
            let factory = &mut program.functions[candidate.function];
            for (_, block) in factory.body.blocks.iter_mut() {
                for statement in &mut block.stmts {
                    if let Statement::Assign {
                        value: Rvalue::MakeStateGenerator { resume: actual, sum_resume, .. },
                        ..
                    } = statement
                        && *actual == resume
                    {
                        *sum_resume = Some(sum);
                    }
                }
            }
        }
    }
}

fn build_batch(program: &Program, candidate: &Candidate) -> Option<(FnId, Function)> {
    let factory = &program.functions[candidate.function];
    let statements = &factory.body.blocks[factory.body.entry].stmts;
    let [
        Statement::Assign { value: Rvalue::NewObject { class, args }, .. },
        Statement::Assign { value: Rvalue::MakeStateGenerator { resume, .. }, .. },
    ] = statements.as_slice() else { return None; };
    // 工厂实参决定原输入字段，不依赖字段名字、顺序或源码range名字。
    let input_field = args.iter().position(|arg| *arg == Operand::Local(candidate.plan.guard_input))?;
    let mut batch = copy_resume(&program.functions[*resume]);
    let frame = *batch.params.first()?;
    let sum = local(&mut batch.body.locals, MirType::Dynamic);
    batch.params.push(sum);
    let ordinary_entry = batch.body.entry;
    let yield_block = candidate.plan.yield_block;
    let Terminator::Return(Some(value)) = batch.body.blocks[yield_block].terminator.clone() else {
        return None;
    };
    // 原yield块已经依次写所有驻留字段与PC；先发布后累计。失败时不改变current值。
    let return_item = batch.body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(Some(value.clone())),
    });
    let accepted = local(&mut batch.body.locals, MirType::Bool);
    batch.body.blocks[yield_block].stmts.push(call(
        accepted,
        RuntimeFn::GeneratorSumTryConsume,
        vec![Operand::Local(sum), value],
    ));
    batch.body.blocks[yield_block].terminator = Terminator::Branch {
        cond: Operand::Local(accepted),
        then_: ordinary_entry,
        else_: return_item,
    };
    let fallback_value = local(&mut batch.body.locals, MirType::Dynamic);
    let fallback = batch.body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: fallback_value,
            value: Rvalue::Call { callee: Callee::Fn(*resume), args: vec![Operand::Local(frame)] },
        }],
        terminator: Terminator::Return(Some(Operand::Local(fallback_value))),
    });
    let raw = local(&mut batch.body.locals, MirType::Dynamic);
    let payload = local(&mut batch.body.locals, MirType::Class(*class));
    let input = local(&mut batch.body.locals, MirType::Dynamic);
    let guarded = local(&mut batch.body.locals, MirType::Bool);
    batch.body.entry = batch.body.blocks.alloc(BasicBlock {
        stmts: vec![
            call(raw, RuntimeFn::GeneratorStateData, vec![Operand::Local(frame)]),
            Statement::Assign {
                dest: payload,
                value: Rvalue::Unbox { value: Operand::Local(raw), to: MirType::Class(*class) },
            },
            Statement::Assign {
                dest: input,
                value: Rvalue::GetField { obj: Operand::Local(payload), field: input_field as u32 },
            },
            call(guarded, RuntimeFn::GeneratorSumRangeGuard, vec![Operand::Local(input)]),
        ],
        terminator: Terminator::Branch {
            cond: Operand::Local(guarded),
            then_: ordinary_entry,
            else_: fallback,
        },
    });
    Some((*resume, batch))
}

// 只复制刚由字段lower产生的私有恢复体；不复制任意Python定义身份或产品证书。
fn copy_resume(source: &Function) -> Function {
    let mut body = crate::generators::empty_body();
    body.locals = la_arena::Arena::new();
    body.blocks = la_arena::Arena::new();
    for (_, local) in source.body.locals.iter() {
        body.locals.alloc(local.clone());
    }
    for (_, block) in source.body.blocks.iter() {
        body.blocks.alloc(block.clone());
    }
    body.entry = source.body.entry;
    Function {
        is_comprehension: false,
        module_state: None,
        module_state_source: None,
        instance_capabilities: source.instance_capabilities,
        definition: None,
        name: source.name.clone(),
        codegen_name: source.codegen_name.clone(),
        source_file: source.source_file.clone(),
        params: source.params.clone(),
        ret: MirType::Dynamic,
        body,
        param_meta: vec![],
        function_global: None,
        specialization_identity_global: None,
        defaults_global: None,
        signature_global: None,
        env: None,
        dynamic_origin: None,
    }
}

#[cfg(test)]
#[path = "generator_sum_emit_tests.rs"]
mod tests;
