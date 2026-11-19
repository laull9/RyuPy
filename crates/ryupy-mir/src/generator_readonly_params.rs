//! 生成器只读参数分析：识别 factory 已入帧且整个函数生命周期内引用绑定不变的 Dynamic 形参。
//!
//! 在普通生成器降级中，形参在 factory 阶段已被放入初始帧槽位（frame[slot + 4]）。
//! 若该形参在函数体内从未被重新赋值（无 Statement::Assign 目标，亦无 Terminator::Suspend.input 恢复定义），
//! 则其帧槽位引用的对象始终不变。挂起（Suspend）时的 ArraySet 重复写回完全多余，可安全跳过；
//! 每次从挂起恢复时的 ArrayGet 依然照常执行，非参数局部变量与控制/状态槽保存逻辑亦不受任何影响。
//!
//! 安全准入规则：
//! 1. 仅在 !product 时启用；
//! 2. 函数体无异常保护区（body.regions.is_empty()，EH 场景必须全量保存）；
//! 3. 必须为函数显式形参，严格排除 env、origin、module_state 等编译器隐藏参数；
//! 4. 类型必须为 MirType::Dynamic 且非 cell 参数；
//! 5. 穷举所有 BasicBlock 的 Statement 与 Terminator，确认其从未作为定义目标。

use crate::mir::{Body, Function, LocalId, MirType, Statement, Terminator};
use rustc_hash::FxHashSet;

/// 穷尽枚举所有基本块中的语句与终结符，收集所有被写入/重新定义的 LocalId 集合。
///
/// 任何赋值目标（Statement::Assign.dest）或恢复输入接收目标（Terminator::Suspend.input）
/// 均构成对局部的重新定义。必须完整匹配所有变体，不得遗漏。
fn collect_written_locals(body: &Body) -> FxHashSet<LocalId> {
    let mut written = FxHashSet::default();
    for (_, block) in body.blocks.iter() {
        for stmt in &block.stmts {
            match stmt {
                Statement::Assign { dest, .. } => {
                    written.insert(*dest);
                }
                Statement::Eval(_)
                | Statement::SetField { .. }
                | Statement::SetGlobal { .. }
                | Statement::Line(_) => {}
            }
        }
        match &block.terminator {
            Terminator::Suspend { input, .. } => {
                if let Some(dest) = input {
                    written.insert(*dest);
                }
            }
            Terminator::Goto(_)
            | Terminator::Branch { .. }
            | Terminator::Return(_)
            | Terminator::Raise { .. }
            | Terminator::Unreachable => {}
        }
    }
    written
}

/// 判断给定的局部槽位是否为编译器注入的隐藏参数（如闭包环境、函数来源或模块状态）。
fn is_hidden_parameter(function: &Function, body: &Body, slot: LocalId) -> bool {
    if function.env == Some(slot) {
        return true;
    }
    if function
        .dynamic_origin
        .as_ref()
        .is_some_and(|origin| origin.owner == slot)
    {
        return true;
    }
    if function.module_state == Some(slot) {
        return true;
    }
    if let Some(decl) = crate::validate::local_decl(&body.locals, slot) {
        if let Some(name) = &decl.name {
            if name.starts_with("__") {
                return true;
            }
        }
    }
    false
}

/// 在 Plan::take 之前计算生成器中可跳过 Suspend 写回的只读 Dynamic 形参集合。
///
/// 若不满足安全门禁（如处于 product 追踪降低模式、存在异常区域等），
/// 立即返回空集合，保持原有全量写回行为。
pub(crate) fn find_readonly_params(
    function: &Function,
    body: &Body,
    product: bool,
) -> FxHashSet<LocalId> {
    if product || !body.regions.is_empty() {
        return FxHashSet::default();
    }

    let written = collect_written_locals(body);
    let mut readonly = FxHashSet::default();

    for &param in &function.params {
        if is_hidden_parameter(function, body, param) {
            continue;
        }
        let Some(decl) = crate::validate::local_decl(&body.locals, param) else {
            continue;
        };
        if decl.ty != MirType::Dynamic || decl.is_cell {
            continue;
        }
        if written.contains(&param) {
            continue;
        }
        readonly.insert(param);
    }

    readonly
}
