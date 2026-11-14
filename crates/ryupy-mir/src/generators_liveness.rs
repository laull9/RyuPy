//! 生成器跨挂起点局部变量的严格有界存活性分析。
//!
//! 针对 AOT 生成器的最窄安全优化：
//! 1. 严格门控：仅在非 product、无异常处理区域（EH regions 为空）的生成器中运行；
//! 2. 强制保活：所有用户命名局部、cell 共享局部、非 Dynamic 静态类型局部（I64/F64/Bool/Str）
//!    以及函数参数均 100% 强制保存恢复，绝对杜绝恢复端 Unbox 异常与反射/闭包观察漂移；
//! 3. 完备读取：复用 `crate::passes::dead_values::{read_value, read_operand}` 确保无遗漏；
//! 4. 有界收敛：严格迭代预算保护，超预算立即放弃并返回 `None` 回退全量保存。

use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

/// 检查函数体是否符合最窄生成器优化的准入条件。
pub(crate) fn is_candidate_for_optimization(body: &Body, product: bool) -> bool {
    // 门控：非 product 模式且无异常保护区域
    !product && body.regions.is_empty()
}

/// 收集终结符中引用的操作数。
pub(crate) fn collect_terminator_reads(term: &Terminator, reads: &mut FxHashSet<LocalId>) {
    match term {
        Terminator::Branch { cond, .. } => {
            crate::passes::dead_values::read_operand(cond, reads);
        }
        Terminator::Return(value) => {
            if let Some(v) = value {
                crate::passes::dead_values::read_operand(v, reads);
            }
        }
        Terminator::Raise { value, cause } => {
            for op in value.iter().chain(cause.iter()) {
                crate::passes::dead_values::read_operand(op, reads);
            }
        }
        Terminator::Suspend { value, .. } => {
            crate::passes::dead_values::read_operand(value, reads);
        }
        Terminator::Goto(_) | Terminator::Unreachable => {}
    }
}

/// 获取基本块的后继基本块列表。
pub(crate) fn terminator_successors(term: &Terminator) -> Vec<BlockId> {
    match term {
        Terminator::Goto(target) => vec![*target],
        Terminator::Branch { then_, else_, .. } => vec![*then_, *else_],
        Terminator::Suspend { resume, .. } => vec![*resume],
        Terminator::Return(_) | Terminator::Raise { .. } | Terminator::Unreachable => Vec::new(),
    }
}

/// 分析生成器体内每个 Suspend 挂起点在恢复边上的活跃局部变量集合。
///
/// 若函数不满足准入条件或分析超出迭代预算，返回 `None`（调用方回退全量保存）。
pub(crate) fn analyze_suspend_liveness(
    body: &Body,
    params: &[LocalId],
    product: bool,
) -> Option<FxHashMap<BlockId, FxHashSet<LocalId>>> {
    analyze_with_budget(
        body,
        params,
        product,
        body.blocks.len().saturating_mul(2).max(16),
    )
}

/// 显式预算入口同时支持失败闭合测试；预算不足时放弃改写。
pub(crate) fn analyze_with_budget(
    body: &Body,
    params: &[LocalId],
    product: bool,
    max_iterations: usize,
) -> Option<FxHashMap<BlockId, FxHashSet<LocalId>>> {
    if !is_candidate_for_optimization(body, product) {
        return None;
    }

    let block_count = body.blocks.len();
    if block_count == 0 {
        return Some(FxHashMap::default());
    }

    // 1. 计算每个基本块的 Def 和 upward-exposed Use 集合
    let mut block_def: FxHashMap<BlockId, FxHashSet<LocalId>> = FxHashMap::default();
    let mut block_use: FxHashMap<BlockId, FxHashSet<LocalId>> = FxHashMap::default();

    for (block_id, block) in body.blocks.iter() {
        let mut defs = FxHashSet::default();
        let mut uses = FxHashSet::default();

        for stmt in &block.stmts {
            match stmt {
                Statement::Assign { dest, value } => {
                    let mut r_reads = FxHashSet::default();
                    crate::passes::dead_values::read_value(value, &mut r_reads);
                    for u in r_reads {
                        if !defs.contains(&u) {
                            uses.insert(u);
                        }
                    }
                    defs.insert(*dest);
                }
                Statement::Eval(value) => {
                    let mut r_reads = FxHashSet::default();
                    crate::passes::dead_values::read_value(value, &mut r_reads);
                    for u in r_reads {
                        if !defs.contains(&u) {
                            uses.insert(u);
                        }
                    }
                }
                Statement::SetField { obj, value, .. } => {
                    let mut r_reads = FxHashSet::default();
                    crate::passes::dead_values::read_operand(obj, &mut r_reads);
                    crate::passes::dead_values::read_operand(value, &mut r_reads);
                    for u in r_reads {
                        if !defs.contains(&u) {
                            uses.insert(u);
                        }
                    }
                }
                Statement::SetGlobal { value, .. } => {
                    let mut r_reads = FxHashSet::default();
                    crate::passes::dead_values::read_operand(value, &mut r_reads);
                    for u in r_reads {
                        if !defs.contains(&u) {
                            uses.insert(u);
                        }
                    }
                }
                Statement::Line(_) => {}
            }
        }

        let mut term_reads = FxHashSet::default();
        collect_terminator_reads(&block.terminator, &mut term_reads);
        for u in term_reads {
            if !defs.contains(&u) {
                uses.insert(u);
            }
        }

        block_def.insert(block_id, defs);
        block_use.insert(block_id, uses);
    }

    // 2. 后向数据流分析计算 LiveIn 和 LiveOut
    let mut live_in: FxHashMap<BlockId, FxHashSet<LocalId>> = FxHashMap::default();
    let mut live_out: FxHashMap<BlockId, FxHashSet<LocalId>> = FxHashMap::default();

    for (block_id, _) in body.blocks.iter() {
        live_in.insert(block_id, FxHashSet::default());
        live_out.insert(block_id, FxHashSet::default());
    }

    // 预算设定：严格基于基本块数量的有界上界
    let mut changed = true;
    let mut iterations = 0;

    while changed {
        if iterations >= max_iterations {
            // 超出预算未达到不动点收敛，必须返回 None 保证不使用不完整活跃集合！
            return None;
        }
        iterations += 1;
        changed = false;

        for (block_id, block) in body.blocks.iter() {
            let succs = terminator_successors(&block.terminator);
            let mut new_out = FxHashSet::default();
            for s in succs {
                if let Some(in_s) = live_in.get(&s) {
                    new_out.extend(in_s.iter().copied());
                }
            }

            let defs = &block_def[&block_id];
            let uses = &block_use[&block_id];

            let mut new_in = uses.clone();
            for var in &new_out {
                if !defs.contains(var) {
                    new_in.insert(*var);
                }
            }

            if new_in != live_in[&block_id] {
                live_in.insert(block_id, new_in);
                changed = true;
            }
            if new_out != live_out[&block_id] {
                live_out.insert(block_id, new_out);
                changed = true;
            }
        }
    }

    // 3. 强制保活集合：
    // - 所有用户命名局部（避免破坏 locals 反射与调试观察）；
    // - 所有 cell 共享局部（避免破坏闭包状态）；
    // - 所有非 Dynamic 的静态类型局部（I64/F64/Bool/Str 等，避免恢复端 Unbox 抛出异常）；
    // - 所有形参（进入函数时已由调用约定或工厂槽初始化）；
    let mut must_retain = FxHashSet::default();
    for (id, decl) in body.locals.iter() {
        if decl.name.is_some() || decl.is_cell || decl.ty != MirType::Dynamic {
            must_retain.insert(id);
        }
    }
    for param in params {
        must_retain.insert(*param);
    }

    // 4. 为每个 Suspend 挂起点计算跨暂停必须保存的局部变量
    let mut result = FxHashMap::default();
    for (block_id, block) in body.blocks.iter() {
        if let Terminator::Suspend { resume, input, .. } = block.terminator {
            let mut suspend_live = live_in.get(&resume).cloned().unwrap_or_default();
            // 在 resume 恢复入口，input 变量会被 GeneratorInput 定义覆写；
            // 若 input 无需跨挂起保留旧值且未被 must_retain 强制要求，可安全忽略。
            if let Some(in_var) = input {
                if !must_retain.contains(&in_var) {
                    suspend_live.remove(&in_var);
                }
            }
            // 强制合并所有必须保活的槽位
            suspend_live.extend(must_retain.iter().copied());
            result.insert(block_id, suspend_live);
        }
    }

    Some(result)
}
