//! 直接globals的局部源码/callee快照合同：验证实际指令，不凭能力或位置标签授权。
use crate::*;
use rustc_hash::FxHashSet;

#[derive(Debug, Clone)]
pub struct DirectGlobalsContract {
    /// 原函数内部HIR身份；link不跨函数解释或重新分配该对局部ID。
    pub source_call: u32,
    pub source_callee: u32,
    pub context: LocalId,
    pub snapshot: LocalId,
    pub result: LocalId,
    /// 真实callee求值后的值；不是再次读取名字的描述。
    pub callee_value: Operand,
}

pub(crate) fn validate_functions(functions: &la_arena::Arena<Function>, errors: &mut Vec<String>) {
    for (_, function) in functions.iter() {
        let facts = &function.body.direct_globals_contracts;
        let mut sources = FxHashSet::default();
        let mut snapshots = FxHashSet::default();
        let mut results = FxHashSet::default();
        let helper_count = function.body.blocks.iter().flat_map(|(_, b)| &b.stmts)
            .filter(|s| matches!(s, Statement::Assign { value, .. } | Statement::Eval(value)
                if matches!(value, Rvalue::Call { callee: Callee::Runtime(RuntimeFn::InstanceDirectGlobals), .. })))
            .count();
        if helper_count != facts.len() {
            errors.push(format!(
                "函数 `{}` 的直接globals调用与源码证明不是一一对应",
                function.name
            ));
        }
        for fact in facts {
            let slots = [fact.snapshot, fact.result, fact.context];
            let declared = slots.iter().all(|slot| {
                function
                    .body
                    .locals
                    .iter()
                    .any(|(id, d)| id == *slot && d.ty == MirType::Dynamic && !d.is_cell)
            });
            let distinct = fact.snapshot != fact.result
                && fact.snapshot != fact.context
                && fact.result != fact.context;
            let mut uses = FxHashSet::default();
            crate::passes::dead_values::read_operand(&fact.callee_value, &mut uses);
            let valid_source = !uses.contains(&fact.snapshot) && !uses.contains(&fact.result);
            let source_unique = sources.insert(fact.source_call);
            let snapshot_unique = snapshots.insert(fact.snapshot);
            let result_unique = results.insert(fact.result);
            let mut matched = 0;
            let mut snapshot_writes = 0;
            let mut result_writes = 0;
            let mut snapshot_reads = 0;
            for (_, block) in function.body.blocks.iter() {
                for (index, stmt) in block.stmts.iter().enumerate() {
                    if let Statement::Assign { dest, .. } = stmt {
                        snapshot_writes += usize::from(*dest == fact.snapshot);
                        result_writes += usize::from(*dest == fact.result);
                    }
                    let mut read = FxHashSet::default();
                    read_statement(stmt, &mut read);
                    snapshot_reads += usize::from(read.contains(&fact.snapshot));
                    if matches!(stmt, Statement::Assign { dest, value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::InstanceDirectGlobals), args } }
                        if *dest == fact.result && args == &[Operand::Local(fact.context), Operand::Local(fact.snapshot)])
                        && index > 0
                        && matches!(&block.stmts[index - 1], Statement::Assign { dest, value: Rvalue::Use(value) }
                            if *dest == fact.snapshot && value == &fact.callee_value)
                    {
                        matched += 1;
                    }
                }
                let mut read = FxHashSet::default();
                read_terminator(&block.terminator, &mut read);
                snapshot_reads += usize::from(read.contains(&fact.snapshot));
                if let Terminator::Suspend {
                    input: Some(input), ..
                } = &block.terminator
                {
                    snapshot_writes += usize::from(*input == fact.snapshot);
                    result_writes += usize::from(*input == fact.result);
                }
            }
            let externally_written = function
                .params
                .iter()
                .copied()
                .chain(function.body.regions.iter().filter_map(|r| r.exc_local))
                .any(|slot| slot == fact.snapshot || slot == fact.result);
            if !function.instance_capabilities.direct_globals
                || function.module_state.is_none()
                || !declared
                || !distinct
                || !valid_source
                || !source_unique
                || !snapshot_unique
                || !result_unique
                || externally_written
                || matched != 1
                || snapshot_writes != 1
                || result_writes != 1
                || snapshot_reads != 1
            {
                errors.push(format!(
                    "函数 `{}` 的直接globals源码点{}:{}与实际callee快照/调用不一致",
                    function.name, fact.source_call, fact.source_callee
                ));
            }
        }
    }
}

fn read_statement(statement: &Statement, reads: &mut FxHashSet<LocalId>) {
    use crate::passes::dead_values::{read_operand, read_value};
    match statement {
        Statement::Assign { value, .. } | Statement::Eval(value) => read_value(value, reads),
        Statement::SetField { obj, value, .. } => {
            read_operand(obj, reads);
            read_operand(value, reads);
        }
        Statement::SetGlobal { value, .. } => read_operand(value, reads),
        Statement::Line(_) => {}
    }
}
fn read_terminator(terminator: &Terminator, reads: &mut FxHashSet<LocalId>) {
    use crate::passes::dead_values::read_operand;
    match terminator {
        Terminator::Branch { cond, .. } => read_operand(cond, reads),
        Terminator::Return(value) => {
            if let Some(value) = value {
                read_operand(value, reads);
            }
        }
        Terminator::Raise { value, cause } => {
            for value in value.iter().chain(cause.iter()) {
                read_operand(value, reads);
            }
        }
        Terminator::Suspend { value, .. } => read_operand(value, reads),
        Terminator::Goto(_) | Terminator::Unreachable => {}
    }
}
