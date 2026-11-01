//! MIR 函数体 → CIL 指令序列。
//!
//! Phase 1 采用最直接的翻译，正确性优先：
//! - 每个 MIR 局部变量 = 一个 CIL local（参数用 `ldarg`/`starg`）；
//! - `dest = lhs op rhs` → `ld lhs; ld rhs; op; stloc dest`；
//! - 基本块按 arena 顺序线性排列，先记下每块起始指令下标，最后回填分支目标
//!   （dotnetdll 的分支指令目标是**指令下标**，不是字节偏移）；
//! - 比较运算：`Lt` → `clt`，`Le` → `cgt` + `ldc.i4.0` + `ceq`（CIL 没有 `cle`）；
//!   浮点比较遇到 NaN 时要用 `cgt.un` / `clt.un` 变体，务必写测试覆盖；
//! - 整数 `+ - *` 使用 `add.ovf` / `sub.ovf` / `mul.ovf`，溢出抛异常而不是静默回绕（ADR-0004）。
//!
//! 进阶（Phase 3+）：把「定义后立即且仅使用一次」的临时变量留在求值栈上，省掉 stloc/ldloc。
//! RyuJIT 本身会做这类优化，所以只有在 IL 体积成为问题时才值得做。

use std::cell::Cell;

use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{BlockId, ClassDefId, FnId, LocalId, Operand, Statement, Terminator};

use crate::stackify_operands::lower_operand;
use crate::stackify_rvalue::{lower_rvalue, rvalue_arg_count};
use crate::types::ClassTypeMap;

pub use crate::stackify_refs::{RuntimeRefs, emit_box};

#[derive(Clone, Debug)]
pub struct ClassCodegenInfo {
    pub ctor: MethodIndex,
    pub fields: Vec<FieldIndex>,
}

pub type ClassInfoMap = FxHashMap<ClassDefId, ClassCodegenInfo>;

pub(crate) struct LowerCtx<'a> {
    pub(crate) runtime_refs: &'a RuntimeRefs,
    pub(crate) fn_map: &'a FxHashMap<FnId, MethodIndex>,
    pub(crate) class_info: &'a ClassInfoMap,
    pub(crate) class_types: &'a ClassTypeMap,
    pub(crate) param_slots: &'a FxHashMap<LocalId, u16>,
    /// PIC 调用点编号由整程序共享的计数器分配。
    pub(crate) site_counter: &'a Cell<u32>,
    pub(crate) local_slots: &'a FxHashMap<LocalId, u16>,
    pub(crate) locals: &'a la_arena::Arena<ryupy_mir::LocalDecl>,
    /// 全部 MIR 函数（MakeFunction 需要被创建函数的名字 / 签名 / usesSuper）。
    pub(crate) functions: &'a la_arena::Arena<ryupy_mir::Function>,
    /// FnId → thunk 方法（MakeFunction 用 `ldftn` 取委托）。
    pub(crate) thunks: &'a FxHashMap<FnId, MethodIndex>,
}

impl LowerCtx<'_> {
    /// 分配一个新的 PIC 调用点编号。
    pub(crate) fn next_site(&self) -> i32 {
        let site = self.site_counter.get();
        self.site_counter.set(site + 1);
        site as i32
    }
}

pub struct CompiledBody {
    pub instructions: Vec<Instruction>,
    pub locals: Vec<LocalVariable>,
    /// CIL 异常表（M2.2）。为空表示函数没有 `try`。
    pub exceptions: Vec<body::Exception>,
    /// 求值栈峰值上限（`MaxStack`）。
    pub max_stack_size: usize,
}

enum PendingTarget {
    Branch(usize, BlockId),
    BranchTruthy(usize, BlockId),
    /// `leave`：离开受保护区域时使用，CLR 会先执行被展开区域的处理器。
    Leave(usize, BlockId),
}

/// 受保护区域的块下标窗口。
struct RegionWindow {
    start: usize,
    end: usize,
}

impl RegionWindow {
    fn contains(&self, block: usize) -> bool {
        block >= self.start && block < self.end
    }
}

#[allow(clippy::too_many_arguments)]
pub fn lower_function(
    func: &ryupy_mir::Function,
    all_functions: &la_arena::Arena<ryupy_mir::Function>,
    thunks: &FxHashMap<FnId, MethodIndex>,
    runtime_refs: &RuntimeRefs,
    fn_map: &FxHashMap<FnId, MethodIndex>,
    class_types: &ClassTypeMap,
    class_info: &ClassInfoMap,
    site_counter: &Cell<u32>,
) -> CompiledBody {
    // 区分形参（ldarg）与局部变量（ldloc）
    let mut param_slots: FxHashMap<LocalId, u16> = FxHashMap::default();
    for (idx, &p) in func.params.iter().enumerate() {
        param_slots.insert(p, idx as u16);
    }

    let mut local_slots: FxHashMap<LocalId, u16> = FxHashMap::default();
    let mut locals = Vec::new();
    for (loc_id, local_decl) in func.body.locals.iter() {
        if param_slots.contains_key(&loc_id) {
            continue;
        }
        if let Some(var) = crate::types::mir_type_to_local_var(&local_decl.ty, class_types) {
            let idx = locals.len() as u16;
            locals.push(var);
            local_slots.insert(loc_id, idx);
        }
    }

    // 求值栈上限：语句之间所有值都存回局部变量，峰值只会出现在
    // 「一次调用 / 容器构造把全部实参同时压栈」时。按最大实参个数加余量估算，
    // 而不是写死 64——宽签名的函数会超过固定值，普通函数也不至于预留过多栈。
    let max_args = func
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| block.stmts.iter())
        .map(|stmt| match stmt {
            Statement::Assign { value, .. } | Statement::Eval(value) => rvalue_arg_count(value),
            Statement::SetField { .. } | Statement::SetGlobal { .. } | Statement::Line(_) => 0,
        })
        .max()
        .unwrap_or(0);
    // 16 个槽位的余量覆盖「实参本身又是一次嵌套调用 / 动态数组构造」的叠加
    let max_stack_size = (max_args + 16).max(256);

    let mut instructions = Vec::new();
    let mut block_starts: FxHashMap<BlockId, usize> = FxHashMap::default();
    let mut block_ends: FxHashMap<BlockId, usize> = FxHashMap::default();
    let mut pending_targets: Vec<PendingTarget> = Vec::new();

    // 异常区域：块窗口（判断跳转是否离开受保护区）+ 处理入口块集合
    let windows: Vec<RegionWindow> = func
        .body
        .regions
        .iter()
        .map(|r| RegionWindow {
            start: r.try_start.into_raw().into_u32() as usize,
            end: r.try_start.into_raw().into_u32() as usize + r.try_len,
        })
        .collect();
    let mut handler_blocks: rustc_hash::FxHashSet<BlockId> = rustc_hash::FxHashSet::default();
    for region in &func.body.regions {
        handler_blocks.insert(region.handler);
    }

    // 计算可达基本块集（避免发射死代码导致 JIT 校验失败）
    let mut reachable = rustc_hash::FxHashSet::default();
    let mut worklist = vec![func.body.entry];
    reachable.insert(func.body.entry);
    // handler 只能经由异常边到达，普通前驱遍历看不到，必须单独作为根
    for region in &func.body.regions {
        if reachable.insert(region.handler) {
            worklist.push(region.handler);
        }
    }
    while let Some(bb_id) = worklist.pop() {
        let bb = &func.body.blocks[bb_id];
        match &bb.terminator {
            Terminator::Goto(target) => {
                if reachable.insert(*target) {
                    worklist.push(*target);
                }
            }
            Terminator::Branch { then_, else_, .. } => {
                if reachable.insert(*then_) {
                    worklist.push(*then_);
                }
                if reachable.insert(*else_) {
                    worklist.push(*else_);
                }
            }
            Terminator::Return(_) | Terminator::Raise { .. } | Terminator::Unreachable => {}
        }
    }

    let ctx = LowerCtx {
        runtime_refs,
        fn_map,
        class_info,
        class_types,
        param_slots: &param_slots,
        site_counter,
        local_slots: &local_slots,
        locals: &func.body.locals,
        functions: all_functions,
        thunks,
    };

    for (block_id, block) in func.body.blocks.iter() {
        if !reachable.contains(&block_id) {
            continue;
        }
        block_starts.insert(block_id, instructions.len());

        for stmt in &block.stmts {
            match stmt {
                Statement::Assign { dest, value } => {
                    lower_rvalue(value, &mut instructions, &ctx);
                    if let Some(&arg_idx) = param_slots.get(dest) {
                        instructions.push(Instruction::StoreArgument(arg_idx));
                    } else if let Some(&local_idx) = local_slots.get(dest) {
                        instructions.push(Instruction::StoreLocal(local_idx));
                    }
                }
                Statement::Eval(rvalue) => {
                    lower_rvalue(rvalue, &mut instructions, &ctx);
                }
                Statement::Line(line) => {
                    // 只写一个静态字段，用于 traceback 定位
                    instructions.push(Instruction::LoadConstantInt32(*line as i32));
                    instructions.push(Instruction::call(runtime_refs.trace_line));
                }
                Statement::SetGlobal { dest, value } => {
                    lower_operand(
                        value,
                        &mut instructions,
                        &param_slots,
                        &local_slots,
                        runtime_refs,
                    );
                    if let Some(&field) = runtime_refs.global_fields.get(dest) {
                        instructions.push(Instruction::StoreStaticField {
                            volatile: false,
                            param0: FieldSource::Definition(field),
                        });
                    }
                }
                Statement::SetField { obj, field, value } => {
                    lower_operand(
                        obj,
                        &mut instructions,
                        &param_slots,
                        &local_slots,
                        runtime_refs,
                    );
                    lower_operand(
                        value,
                        &mut instructions,
                        &param_slots,
                        &local_slots,
                        runtime_refs,
                    );
                    let class_id = match obj {
                        Operand::Local(loc) => match func.body.locals[*loc].ty {
                            ryupy_mir::MirType::Class(id) => id,
                            _ => panic!("SetField obj 不是 Class 类型"),
                        },
                        _ => panic!("SetField obj 必须是局部变量操作数"),
                    };
                    let field_idx = class_info[&class_id].fields[*field as usize];
                    instructions.push(Instruction::StoreField {
                        unaligned: None,
                        volatile: false,
                        param0: field_idx.into(),
                    });
                }
            }
        }

        let block_end_marker = block_id;
        match &block.terminator {
            Terminator::Return(None) => {
                instructions.push(Instruction::Return);
            }
            Terminator::Return(Some(op)) => {
                lower_operand(
                    op,
                    &mut instructions,
                    &param_slots,
                    &local_slots,
                    runtime_refs,
                );
                instructions.push(Instruction::Return);
            }
            Terminator::Goto(target) => {
                let pos = instructions.len();
                // 处理入口必须用 leave 离开；普通跳转只要跨出任一受保护区也用 leave
                let needs_leave = handler_blocks.contains(&block_id)
                    || leaves_region(&windows, block_id, *target);
                if needs_leave {
                    instructions.push(Instruction::Leave(0)); // 占位，稍后回填
                    pending_targets.push(PendingTarget::Leave(pos, *target));
                } else {
                    instructions.push(Instruction::Branch(0)); // 占位，稍后回填
                    pending_targets.push(PendingTarget::Branch(pos, *target));
                }
            }
            Terminator::Branch { cond, then_, else_ } => {
                lower_operand(
                    cond,
                    &mut instructions,
                    &param_slots,
                    &local_slots,
                    runtime_refs,
                );
                let pos_then = instructions.len();
                instructions.push(Instruction::BranchTruthy(0)); // 占位
                pending_targets.push(PendingTarget::BranchTruthy(pos_then, *then_));

                let pos_else = instructions.len();
                instructions.push(Instruction::Branch(0)); // 占位
                pending_targets.push(PendingTarget::Branch(pos_else, *else_));
            }
            Terminator::Raise { value, cause } => {
                match value {
                    Some(v) => lower_operand(
                        v,
                        &mut instructions,
                        &param_slots,
                        &local_slots,
                        runtime_refs,
                    ),
                    None => {
                        instructions.push(Instruction::call(runtime_refs.dyn_none));
                    }
                }
                if let Some(c) = cause {
                    lower_operand(
                        c,
                        &mut instructions,
                        &param_slots,
                        &local_slots,
                        runtime_refs,
                    );
                    instructions.push(Instruction::call(runtime_refs.with_cause));
                }
                instructions.push(Instruction::Throw);
            }
            Terminator::Unreachable => {}
        }
        block_ends.insert(block_end_marker, instructions.len());
    }

    // 回填所有分支指令的跳转目标下标
    for pending in pending_targets {
        match pending {
            PendingTarget::Branch(idx, target_block) => {
                let target_ins = block_starts.get(&target_block).copied().unwrap_or(0);
                instructions[idx] = Instruction::Branch(target_ins);
            }
            PendingTarget::BranchTruthy(idx, target_block) => {
                let target_ins = block_starts.get(&target_block).copied().unwrap_or(0);
                instructions[idx] = Instruction::BranchTruthy(target_ins);
            }
            PendingTarget::Leave(idx, target_block) => {
                let target_ins = block_starts.get(&target_block).copied().unwrap_or(0);
                instructions[idx] = Instruction::Leave(target_ins);
            }
        }
    }

    let exceptions =
        build_exception_table(func, &windows, &block_starts, &block_ends, runtime_refs);

    CompiledBody {
        instructions,
        locals,
        exceptions,
        max_stack_size,
    }
}

/// `block` 是否离开了某个包含它的受保护区域。
fn leaves_region(windows: &[RegionWindow], block: BlockId, target: BlockId) -> bool {
    let from = block.into_raw().into_u32() as usize;
    let to = target.into_raw().into_u32() as usize;
    windows.iter().any(|w| w.contains(from) && !w.contains(to))
}

/// 由 MIR 区域生成 CIL 异常表。
///
/// 每个区域的处理入口都被构造成**两条指令**（`stloc` 取下异常对象 + `leave` 跳出），
/// 所以 handler 区间可以静态给出，不需要额外的块尾追踪。
fn build_exception_table(
    func: &ryupy_mir::Function,
    windows: &[RegionWindow],
    block_starts: &FxHashMap<BlockId, usize>,
    block_ends: &FxHashMap<BlockId, usize>,
    refs: &RuntimeRefs,
) -> Vec<body::Exception> {
    let mut clauses = Vec::new();
    let catch_all = refs.exception_types.get("BaseException").cloned();

    for (region, window) in func.body.regions.iter().zip(windows) {
        // 受保护区间的指令范围：只统计真正发射过的块（死代码会被跳过）
        let mut try_start = usize::MAX;
        let mut try_end = 0usize;
        for index in window.start..window.end {
            let bb = BlockId::from_raw(la_arena::RawIdx::from(index as u32));
            if let Some(&start) = block_starts.get(&bb) {
                try_start = try_start.min(start);
                try_end = try_end.max(block_ends.get(&bb).copied().unwrap_or(start));
            }
        }
        if try_start == usize::MAX || try_end <= try_start {
            continue;
        }
        let handler_start = match block_starts.get(&region.handler) {
            Some(&offset) => offset,
            None => continue,
        };
        let handler_end = block_ends
            .get(&region.handler)
            .copied()
            .unwrap_or(handler_start);
        let try_length = try_end - try_start;
        let handler_length = handler_end - handler_start;

        let mut push_clause = |kind: body::ExceptionKind| {
            clauses.push(body::Exception {
                kind,
                try_offset: try_start,
                try_length,
                handler_offset: handler_start,
                handler_length,
            });
        };

        match &region.kind {
            ryupy_mir::RegionKind::Catch { type_names } => {
                let mut emitted = false;
                for name in type_names {
                    if let Some(ty) = refs.exception_types.get(name) {
                        push_clause(body::ExceptionKind::TypedException(ty.clone()));
                        emitted = true;
                    }
                }
                // 名字没登记（理论上不会发生）：退化成 catch-all，至少保证语义正确
                if !emitted {
                    if let Some(ty) = &catch_all {
                        push_clause(body::ExceptionKind::TypedException(ty.clone()));
                    }
                }
            }
            ryupy_mir::RegionKind::CatchAll | ryupy_mir::RegionKind::FinallyGuard => {
                if let Some(ty) = &catch_all {
                    push_clause(body::ExceptionKind::TypedException(ty.clone()));
                }
            }
        }
    }
    // 异常表顺序不是随意的：CLR 按顺序匹配，且要求**内层（更早结束）的子句排在
    // 外层之前**，否则 JIT 直接判 InvalidProgramException——而且不会告诉你是哪一条。
    // 排序键取「try 结束位置升序」= 内层优先；结束位置相同（同一个 try 的多个 catch）
    // 时按起点降序排，保持源码书写顺序（Python 按书写顺序匹配类型）。
    // 早期实现按起点升序排，嵌套 try 一律失败，就是踩了这个坑。
    clauses.sort_by_key(|c| (c.try_offset + c.try_length, std::cmp::Reverse(c.try_offset)));
    clauses
}
