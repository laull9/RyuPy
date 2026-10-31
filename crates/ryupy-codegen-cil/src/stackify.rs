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
use ryupy_mir::{
    BinOp, BlockId, Callee, CastKind, ClassDefId, Constant as MirConstant, FnId, GlobalId, LocalId,
    Operand, RuntimeFn, Rvalue, Statement, Terminator, UnaryOp,
};

use crate::types::ClassTypeMap;

#[derive(Clone, Debug)]
pub struct ClassCodegenInfo {
    pub ctor: MethodIndex,
    pub fields: Vec<FieldIndex>,
}

pub type ClassInfoMap = FxHashMap<ClassDefId, ClassCodegenInfo>;

struct LowerCtx<'a> {
    runtime_refs: &'a RuntimeRefs,
    fn_map: &'a FxHashMap<FnId, MethodIndex>,
    class_info: &'a ClassInfoMap,
    class_types: &'a ClassTypeMap,
    param_slots: &'a FxHashMap<LocalId, u16>,
    /// PIC 调用点编号由整程序共享的计数器分配。
    site_counter: &'a Cell<u32>,
    local_slots: &'a FxHashMap<LocalId, u16>,
    locals: &'a la_arena::Arena<ryupy_mir::LocalDecl>,
}

impl LowerCtx<'_> {
    /// 分配一个新的 PIC 调用点编号。
    fn next_site(&self) -> i32 {
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

pub struct RuntimeRefs {
    pub write_i64: MethodRefIndex,
    pub write_f64: MethodRefIndex,
    pub write_bool: MethodRefIndex,
    pub write_str: MethodRefIndex,
    pub write_newline: MethodRefIndex,
    pub write_sep: MethodRefIndex,
    pub str_concat: MethodRefIndex,
    pub floor_div_i64: MethodRefIndex,
    pub mod_i64: MethodRefIndex,
    pub pow_i64: MethodRefIndex,
    pub true_div_f64: MethodRefIndex,
    pub floor_div_f64: MethodRefIndex,
    pub mod_f64: MethodRefIndex,
    pub pow_f64: MethodRefIndex,
    pub f64_to_i64: MethodRefIndex,
    pub abs_i64: MethodRefIndex,
    pub abs_f64: MethodRefIndex,
    pub shl_i64: MethodRefIndex,
    pub shr_i64: MethodRefIndex,
    // ── 动态值 ABI（ADR-0010）──
    pub write_object: MethodRefIndex,
    pub dyn_binary: MethodRefIndex,
    pub dyn_compare: MethodRefIndex,
    pub dyn_unary: MethodRefIndex,
    pub pic_binary: MethodRefIndex,
    pub call_value: MethodRefIndex,
    pub pic_call_value: [MethodRefIndex; 4],
    pub pic_call_method: [MethodRefIndex; 4],
    pub pic_get_attr: MethodRefIndex,
    pub pic_set_attr: MethodRefIndex,
    pub dyn_truthy: MethodRefIndex,
    pub dyn_none: MethodRefIndex,
    /// `PyOps.EmptyArray()`：共享空 `object[]`，避免每次无关键字调用都 `newarr`。
    pub empty_array: MethodRefIndex,
    /// 动态数值字面量池：`Rvalue::Box` 直接复用静态字段里的装箱对象。
    pub literal_fields: FxHashMap<String, FieldIndex>,
    pub dyn_from_decimal: MethodRefIndex,
    pub dyn_to_int: MethodRefIndex,
    pub dyn_to_float: MethodRefIndex,
    pub dyn_abs: MethodRefIndex,
    pub cast_to_i64: MethodRefIndex,
    pub cast_to_f64: MethodRefIndex,
    pub cast_to_bool: MethodRefIndex,
    pub cast_to_str: MethodRefIndex,
    pub make_exception: MethodRefIndex,
    pub with_cause: MethodRefIndex,
    pub range_step: MethodRefIndex,
    // ── 异常区域与栈帧（M2.2）──
    pub handler_enter: MethodRefIndex,
    pub handler_leave: MethodRefIndex,
    pub reraise: MethodRefIndex,
    pub raise_prepare: MethodRefIndex,
    pub make_unbound: MethodRefIndex,
    pub load_checked: MethodRefIndex,
    /// `PyErrors.LoadName(object, string)`：模块级名字的未赋值检查。
    pub load_name: MethodRefIndex,
    pub dyn_str: MethodRefIndex,
    pub dyn_type: MethodRefIndex,
    pub trace_push: MethodRefIndex,
    pub trace_line: MethodRefIndex,
    pub trace_pop_to: MethodRefIndex,
    /// Python 异常类型名 → CLR 类型（CIL 类型化 catch 用）。
    pub exception_types: FxHashMap<String, MethodType>,
    // ── 容器协议（M2.3）──
    pub make_list: MethodRefIndex,
    pub make_tuple: MethodRefIndex,
    pub make_set: MethodRefIndex,
    pub make_dict: MethodRefIndex,
    pub make_slice: MethodRefIndex,
    pub make_bytes: MethodRefIndex,
    pub get_item: MethodRefIndex,
    pub set_item: MethodRefIndex,
    pub del_item: MethodRefIndex,
    pub contains: MethodRefIndex,
    pub len: MethodRefIndex,
    pub is_same: MethodRefIndex,
    pub repr_value: MethodRefIndex,
    pub call_builtin: MethodRefIndex,
    pub call_method: MethodRefIndex,
    pub array_get: MethodRefIndex,
    pub array_set: MethodRefIndex,
    pub type_object: MethodRefIndex,
    pub arg_error: MethodRefIndex,
    pub star_arg: MethodRefIndex,
    pub format_value: MethodRefIndex,
    pub list_append: MethodRefIndex,
    pub list_extend: MethodRefIndex,
    pub unpack_check: MethodRefIndex,
    pub iter_start: MethodRefIndex,
    pub iter_next_ok: MethodRefIndex,
    pub iter_value: MethodRefIndex,
    // ── 类与属性协议（M2.5 / M2.6）──
    pub make_class: MethodRefIndex,
    pub set_attr: MethodRefIndex,
    pub del_attr: MethodRefIndex,
    pub decorate: MethodRefIndex,
    /// `RyuPy.Runtime.PyClass` 的类型（异常类构造函数参数）。
    pub py_class_type: MethodType,
    /// `PyExceptionFactory` 委托的构造函数。
    pub exception_factory_ctor: MethodRefIndex,
    /// `PyBaseException.BindPythonClass(PyClass)`。
    pub bind_python_class: MethodRefIndex,
    /// `PyErrors.UserExceptionMessage(object[]) -> string`。
    pub user_exception_message: MethodRefIndex,
    /// `System.Object` 的 `MethodType`：`object[]` 的元素类型。
    pub object_type: MethodType,
    // ── 一等函数与参数绑定（M2.1）──
    /// 全局槽位 → 模块静态字段。
    pub global_fields: FxHashMap<GlobalId, FieldIndex>,
    pub signature_create: MethodRefIndex,
    pub bind_args: MethodRefIndex,
    pub py_function_ctor: MethodRefIndex,
    pub py_call_ctor: MethodRefIndex,
    // ── 模块系统（M2.7）──
    pub import_module: MethodRefIndex,
    pub import_attr: MethodRefIndex,
    pub module_register: MethodRefIndex,
    pub module_run_main: MethodRefIndex,
    pub module_same_name: MethodRefIndex,
    pub module_missing_attr: MethodRefIndex,
    pub module_set_dynamic: MethodRefIndex,
    pub module_init_ctor: MethodRefIndex,
    pub module_get_ctor: MethodRefIndex,
    pub module_set_ctor: MethodRefIndex,
    /// `RyuPy.Runtime.PyModule` 的类型（setter 的第一个参数）。
    pub py_module_type: MethodType,
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

pub fn lower_function(
    func: &ryupy_mir::Function,
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

fn lower_operand(
    op: &Operand,
    ins: &mut Vec<Instruction>,
    param_slots: &FxHashMap<LocalId, u16>,
    local_slots: &FxHashMap<LocalId, u16>,
    refs: &RuntimeRefs,
) {
    match op {
        Operand::Local(loc) => {
            if let Some(&arg_idx) = param_slots.get(loc) {
                ins.push(Instruction::LoadArgument(arg_idx));
            } else if let Some(&local_idx) = local_slots.get(loc) {
                ins.push(Instruction::LoadLocal(local_idx));
            }
        }
        Operand::Global(global) => {
            if let Some(&field) = refs.global_fields.get(global) {
                ins.push(Instruction::LoadStaticField {
                    volatile: false,
                    param0: FieldSource::Definition(field),
                });
            }
        }
        Operand::Array(items) => {
            if items.is_empty() {
                ins.push(Instruction::call(refs.empty_array));
                return;
            }
            let object_type = refs.object_type.clone();
            ins.push(Instruction::LoadConstantInt32(items.len() as i32));
            ins.push(Instruction::NewArray(object_type.clone()));
            for (index, item) in items.iter().enumerate() {
                ins.push(Instruction::Duplicate);
                ins.push(Instruction::LoadConstantInt32(index as i32));
                lower_operand(item, ins, param_slots, local_slots, refs);
                ins.push(Instruction::StoreElement {
                    skip_type_check: false,
                    skip_range_check: false,
                    skip_null_check: false,
                    param0: object_type.clone(),
                });
            }
        }
        Operand::Const(c) => match c {
            MirConstant::I64(v) => ins.push(Instruction::LoadConstantInt64(*v)),
            MirConstant::F64(v) => ins.push(Instruction::LoadConstantFloat64(*v)),
            MirConstant::Bool(b) => {
                ins.push(Instruction::LoadConstantInt32(if *b { 1 } else { 0 }))
            }
            MirConstant::Str(s) => ins.push(Instruction::load_string(s)),
            MirConstant::BigInt(s) => {
                ins.push(Instruction::load_string(s));
                ins.push(Instruction::call(refs.dyn_from_decimal));
            }
            MirConstant::None => ins.push(Instruction::call(refs.dyn_none)),
            MirConstant::Null => ins.push(Instruction::LoadNull),
        },
    }
}

/// 构造 `object[]`：`newarr` 后逐个 `stelem.ref`。
///
/// 容器构造、方法调用与内建调用都用它，因此只写一份。
fn lower_object_array(args: &[Operand], ins: &mut Vec<Instruction>, ctx: &LowerCtx<'_>) {
    if args.is_empty() {
        ins.push(Instruction::call(ctx.runtime_refs.empty_array));
        return;
    }
    let object_type = ctx.runtime_refs.object_type.clone();
    ins.push(Instruction::LoadConstantInt32(args.len() as i32));
    ins.push(Instruction::NewArray(object_type.clone()));
    for (index, op) in args.iter().enumerate() {
        ins.push(Instruction::Duplicate);
        ins.push(Instruction::LoadConstantInt32(index as i32));
        lower_operand(op, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
        ins.push(Instruction::StoreElement {
            skip_type_check: false,
            skip_range_check: false,
            skip_null_check: false,
            param0: object_type.clone(),
        });
    }
}

/// 一条 rvalue 最多会把多少个值同时留在求值栈上。
///
/// 只关心「同一层」的压栈数：调用/构造的实参全部先求值再压栈，因此取实参个数；
/// 嵌套调用的额外深度已经包含在各自的实参计数里，这里不重复累加。
fn rvalue_arg_count(value: &Rvalue) -> usize {
    match value {
        Rvalue::Call { args, .. } | Rvalue::NewObject { args, .. } => args.len(),
        _ => 0,
    }
}

/// `RuntimeFn` → 方法引用。容器相关的调用与普通调用共用这张表。
fn runtime_method(callee: RuntimeFn, refs: &RuntimeRefs) -> MethodRefIndex {
    match callee {
        RuntimeFn::MakeList => refs.make_list,
        RuntimeFn::MakeTuple => refs.make_tuple,
        RuntimeFn::MakeSet => refs.make_set,
        RuntimeFn::MakeDict => refs.make_dict,
        RuntimeFn::MakeSlice => refs.make_slice,
        RuntimeFn::MakeBytes => refs.make_bytes,
        RuntimeFn::GetItem => refs.get_item,
        RuntimeFn::SetItem => refs.set_item,
        RuntimeFn::DelItem => refs.del_item,
        RuntimeFn::Contains => refs.contains,
        RuntimeFn::Len => refs.len,
        RuntimeFn::IsSame => refs.is_same,
        RuntimeFn::ReprValue => refs.repr_value,
        RuntimeFn::DynBuiltin => refs.call_builtin,
        RuntimeFn::CallMethod => refs.call_method,
        RuntimeFn::ListAppend => refs.list_append,
        RuntimeFn::ListExtend => refs.list_extend,
        RuntimeFn::UnpackCheck => refs.unpack_check,
        RuntimeFn::IterStart => refs.iter_start,
        RuntimeFn::IterNextOk => refs.iter_next_ok,
        RuntimeFn::IterValue => refs.iter_value,
        RuntimeFn::MakeClass => refs.make_class,
        RuntimeFn::SetAttr => refs.set_attr,
        RuntimeFn::DelAttr => refs.del_attr,
        RuntimeFn::Decorate => refs.decorate,
        other => unreachable!("{other:?} 不是容器调用"),
    }
}

fn is_f64_operand(op: &Operand, locals: &la_arena::Arena<ryupy_mir::LocalDecl>) -> bool {
    match op {
        Operand::Const(MirConstant::F64(_)) => true,
        Operand::Local(loc) => locals[*loc].ty == ryupy_mir::MirType::F64,
        _ => false,
    }
}

fn lower_rvalue(rval: &Rvalue, ins: &mut Vec<Instruction>, ctx: &LowerCtx<'_>) {
    match rval {
        Rvalue::Use(op) => {
            lower_operand(op, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs)
        }
        Rvalue::Binary { op, lhs, rhs } => {
            lower_operand(lhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            lower_operand(rhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            let is_float = is_f64_operand(lhs, ctx.locals) || is_f64_operand(rhs, ctx.locals);
            match op {
                BinOp::Add => {
                    if is_float {
                        ins.push(Instruction::Add);
                    } else {
                        ins.push(Instruction::AddOverflow(NumberSign::Signed));
                    }
                }
                BinOp::Sub => {
                    if is_float {
                        ins.push(Instruction::Subtract);
                    } else {
                        ins.push(Instruction::SubtractOverflow(NumberSign::Signed));
                    }
                }
                BinOp::Mul => {
                    if is_float {
                        ins.push(Instruction::Multiply);
                    } else {
                        ins.push(Instruction::MultiplyOverflow(NumberSign::Signed));
                    }
                }
                BinOp::Div => ins.push(Instruction::Divide(NumberSign::Signed)),
                BinOp::BitAnd => ins.push(Instruction::And),
                BinOp::BitOr => ins.push(Instruction::Or),
                BinOp::BitXor => ins.push(Instruction::Xor),
                BinOp::Shl => ins.push(Instruction::ShiftLeft),
                BinOp::Shr => ins.push(Instruction::ShiftRight(NumberSign::Signed)),
                BinOp::Eq => ins.push(Instruction::CompareEqual),
                BinOp::Ne => {
                    ins.push(Instruction::CompareEqual);
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
                BinOp::Lt => ins.push(Instruction::CompareLess(NumberSign::Signed)),
                BinOp::Gt => ins.push(Instruction::CompareGreater(NumberSign::Signed)),
                BinOp::Le => {
                    // CIL 无 cle，以 !(a > b) 实现
                    ins.push(Instruction::CompareGreater(NumberSign::Signed));
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
                BinOp::Ge => {
                    // CIL 无 cge，以 !(a < b) 实现
                    ins.push(Instruction::CompareLess(NumberSign::Signed));
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
            }
        }
        Rvalue::Unary { op, operand } => {
            lower_operand(
                operand,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            match op {
                UnaryOp::Neg => ins.push(Instruction::Negate),
                UnaryOp::Not => {
                    ins.push(Instruction::LoadConstantInt32(0));
                    ins.push(Instruction::CompareEqual);
                }
                UnaryOp::BitNot => ins.push(Instruction::Not),
            }
        }
        Rvalue::Cast { kind, operand } => {
            lower_operand(
                operand,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            match kind {
                CastKind::I64ToF64 => ins.push(Instruction::ConvertFloat64),
                CastKind::F64ToI64 => ins.push(Instruction::call(ctx.runtime_refs.f64_to_i64)),
                CastKind::BoolToI64 => ins.push(Instruction::Convert(ConversionType::Int64)),
            }
        }
        Rvalue::Call { callee, args } => {
            // 动态调用把调用点编号作为第一个实参，运行时据此做有界内联缓存。
            if matches!(
                callee,
                Callee::Runtime(
                    RuntimeFn::CallValue
                        | RuntimeFn::CallMethod
                        | RuntimeFn::DynGetAttr
                        | RuntimeFn::SetAttr
                )
            ) {
                let site = ctx.next_site();
                ins.push(Instruction::LoadConstantInt32(site));
            }
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            match callee {
                Callee::Runtime(rt_fn) => match rt_fn {
                    RuntimeFn::WriteI64 => ins.push(Instruction::call(ctx.runtime_refs.write_i64)),
                    RuntimeFn::WriteF64 => ins.push(Instruction::call(ctx.runtime_refs.write_f64)),
                    RuntimeFn::WriteBool => {
                        ins.push(Instruction::call(ctx.runtime_refs.write_bool))
                    }
                    RuntimeFn::WriteStr => ins.push(Instruction::call(ctx.runtime_refs.write_str)),
                    RuntimeFn::WriteNewline => {
                        ins.push(Instruction::call(ctx.runtime_refs.write_newline))
                    }
                    RuntimeFn::WriteSep => ins.push(Instruction::call(ctx.runtime_refs.write_sep)),
                    RuntimeFn::StrConcat => {
                        ins.push(Instruction::call(ctx.runtime_refs.str_concat))
                    }
                    RuntimeFn::FloorDivI64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.floor_div_i64))
                    }
                    RuntimeFn::ModI64 => ins.push(Instruction::call(ctx.runtime_refs.mod_i64)),
                    RuntimeFn::PowI64 => ins.push(Instruction::call(ctx.runtime_refs.pow_i64)),
                    RuntimeFn::TrueDivF64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.true_div_f64))
                    }
                    RuntimeFn::FloorDivF64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.floor_div_f64))
                    }
                    RuntimeFn::ModF64 => ins.push(Instruction::call(ctx.runtime_refs.mod_f64)),
                    RuntimeFn::PowF64 => ins.push(Instruction::call(ctx.runtime_refs.pow_f64)),
                    RuntimeFn::F64ToI64 => ins.push(Instruction::call(ctx.runtime_refs.f64_to_i64)),
                    RuntimeFn::AbsI64 => ins.push(Instruction::call(ctx.runtime_refs.abs_i64)),
                    RuntimeFn::AbsF64 => ins.push(Instruction::call(ctx.runtime_refs.abs_f64)),
                    RuntimeFn::ShlI64 => ins.push(Instruction::call(ctx.runtime_refs.shl_i64)),
                    RuntimeFn::ShrI64 => ins.push(Instruction::call(ctx.runtime_refs.shr_i64)),
                    RuntimeFn::ArrayGet => ins.push(Instruction::call(ctx.runtime_refs.array_get)),
                    RuntimeFn::ArraySet => ins.push(Instruction::call(ctx.runtime_refs.array_set)),
                    RuntimeFn::CallValue => {
                        ins.push(Instruction::call(ctx.runtime_refs.call_value))
                    }
                    RuntimeFn::TypeObject => {
                        ins.push(Instruction::call(ctx.runtime_refs.type_object))
                    }
                    RuntimeFn::ArgError => ins.push(Instruction::call(ctx.runtime_refs.arg_error)),
                    RuntimeFn::StarArg => ins.push(Instruction::call(ctx.runtime_refs.star_arg)),
                    RuntimeFn::FormatValue => {
                        ins.push(Instruction::call(ctx.runtime_refs.format_value))
                    }
                    RuntimeFn::WriteObject => {
                        ins.push(Instruction::call(ctx.runtime_refs.write_object))
                    }
                    RuntimeFn::DynBinary => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_binary))
                    }
                    RuntimeFn::DynCompare => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_compare))
                    }
                    RuntimeFn::DynUnary => ins.push(Instruction::call(ctx.runtime_refs.dyn_unary)),
                    RuntimeFn::DynTruthy => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_truthy))
                    }
                    RuntimeFn::DynNone => ins.push(Instruction::call(ctx.runtime_refs.dyn_none)),
                    RuntimeFn::DynFromDecimal => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_from_decimal))
                    }
                    RuntimeFn::DynToInt => ins.push(Instruction::call(ctx.runtime_refs.dyn_to_int)),
                    RuntimeFn::DynToFloat => {
                        ins.push(Instruction::call(ctx.runtime_refs.dyn_to_float))
                    }
                    RuntimeFn::DynAbs => ins.push(Instruction::call(ctx.runtime_refs.dyn_abs)),
                    RuntimeFn::MakeException => {
                        ins.push(Instruction::call(ctx.runtime_refs.make_exception))
                    }
                    RuntimeFn::WithCause => {
                        ins.push(Instruction::call(ctx.runtime_refs.with_cause))
                    }
                    RuntimeFn::RangeStep => {
                        ins.push(Instruction::call(ctx.runtime_refs.range_step))
                    }
                    RuntimeFn::HandlerEnter => {
                        ins.push(Instruction::call(ctx.runtime_refs.handler_enter))
                    }
                    RuntimeFn::HandlerLeave => {
                        ins.push(Instruction::call(ctx.runtime_refs.handler_leave))
                    }
                    RuntimeFn::Reraise => ins.push(Instruction::call(ctx.runtime_refs.reraise)),
                    RuntimeFn::RaisePrepare => {
                        ins.push(Instruction::call(ctx.runtime_refs.raise_prepare))
                    }
                    RuntimeFn::MakeUnbound => {
                        ins.push(Instruction::call(ctx.runtime_refs.make_unbound))
                    }
                    RuntimeFn::TracePush => {
                        ins.push(Instruction::call(ctx.runtime_refs.trace_push))
                    }
                    RuntimeFn::TraceLine => {
                        ins.push(Instruction::call(ctx.runtime_refs.trace_line))
                    }
                    RuntimeFn::TracePopTo => {
                        ins.push(Instruction::call(ctx.runtime_refs.trace_pop_to))
                    }
                    RuntimeFn::DynStr => ins.push(Instruction::call(ctx.runtime_refs.dyn_str)),
                    RuntimeFn::DynType => ins.push(Instruction::call(ctx.runtime_refs.dyn_type)),
                    RuntimeFn::DynGetAttr => {
                        ins.push(Instruction::call(ctx.runtime_refs.pic_get_attr))
                    }
                    RuntimeFn::SetAttr => {
                        ins.push(Instruction::call(ctx.runtime_refs.pic_set_attr))
                    }
                    RuntimeFn::LoadChecked => {
                        ins.push(Instruction::call(ctx.runtime_refs.load_checked))
                    }
                    RuntimeFn::LoadName => ins.push(Instruction::call(ctx.runtime_refs.load_name)),
                    RuntimeFn::ImportModule => {
                        ins.push(Instruction::call(ctx.runtime_refs.import_module))
                    }
                    RuntimeFn::ImportAttr => {
                        ins.push(Instruction::call(ctx.runtime_refs.import_attr))
                    }
                    RuntimeFn::CallMethod => {
                        ins.push(Instruction::call(ctx.runtime_refs.call_method))
                    }
                    // 容器相关的调用与 CallWithArgs 走同一张表
                    RuntimeFn::MakeList
                    | RuntimeFn::MakeTuple
                    | RuntimeFn::MakeSet
                    | RuntimeFn::MakeDict
                    | RuntimeFn::MakeSlice
                    | RuntimeFn::MakeBytes
                    | RuntimeFn::GetItem
                    | RuntimeFn::SetItem
                    | RuntimeFn::DelItem
                    | RuntimeFn::Contains
                    | RuntimeFn::Len
                    | RuntimeFn::IsSame
                    | RuntimeFn::ReprValue
                    | RuntimeFn::DynBuiltin
                    | RuntimeFn::ListAppend
                    | RuntimeFn::ListExtend
                    | RuntimeFn::UnpackCheck
                    | RuntimeFn::IterStart
                    | RuntimeFn::IterNextOk
                    | RuntimeFn::IterValue
                    | RuntimeFn::MakeClass
                    | RuntimeFn::DelAttr
                    | RuntimeFn::Decorate => {
                        ins.push(Instruction::call(runtime_method(*rt_fn, ctx.runtime_refs)))
                    }
                    RuntimeFn::TraceAttach => {
                        // M2.2 用 PrepareRaise 合并了「挂栈帧 + 挂上下文」，这里不再单独发射
                        ins.push(Instruction::NoOperation);
                    }
                    RuntimeFn::CastToI64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.cast_to_i64))
                    }
                    RuntimeFn::CastToF64 => {
                        ins.push(Instruction::call(ctx.runtime_refs.cast_to_f64))
                    }
                    RuntimeFn::CastToBool => {
                        ins.push(Instruction::call(ctx.runtime_refs.cast_to_bool))
                    }
                    RuntimeFn::CastToStr => {
                        ins.push(Instruction::call(ctx.runtime_refs.cast_to_str))
                    }
                },
                Callee::Fn(fn_id) => {
                    let method_idx = ctx.fn_map[fn_id];
                    ins.push(Instruction::call(method_idx));
                }
            }
        }
        Rvalue::NewObject { class, args } => {
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            let ctor = ctx.class_info[class].ctor;
            ins.push(Instruction::NewObject(ctor.into()));
        }
        Rvalue::Box { value, from } => {
            // 数值字面量只装箱一次：模块初始化时存进静态字段，循环里直接读。
            if let Operand::Const(constant) = value
                && let Some(field) = boxed_literal_key(constant, from)
                    .and_then(|key| ctx.runtime_refs.literal_fields.get(&key).copied())
            {
                ins.push(Instruction::LoadStaticField {
                    volatile: false,
                    param0: FieldSource::Definition(field),
                });
                return;
            }
            lower_operand(
                value,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            // 引用类型（str / class / 已是 object）不需要 box
            let needs_box = !matches!(
                from,
                ryupy_mir::MirType::Str
                    | ryupy_mir::MirType::Dynamic
                    | ryupy_mir::MirType::Class(_)
            );
            if needs_box
                && let Some(mt) = crate::types::mir_type_to_method_type(from, ctx.class_types)
            {
                ins.push(Instruction::BoxValue(mt));
            }
        }
        Rvalue::Unbox { value, to } => {
            lower_operand(
                value,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            match to {
                ryupy_mir::MirType::Class(_) => {
                    // 引用类型直接 castclass（dataclass 实例）。
                    if let Some(mt) = crate::types::mir_type_to_method_type(to, ctx.class_types) {
                        ins.push(Instruction::CastClass {
                            skip_type_check: false,
                            param0: mt,
                        });
                    }
                }
                _ => {
                    let cast = match to {
                        ryupy_mir::MirType::I64 => ctx.runtime_refs.cast_to_i64,
                        ryupy_mir::MirType::F64 => ctx.runtime_refs.cast_to_f64,
                        ryupy_mir::MirType::Bool => ctx.runtime_refs.cast_to_bool,
                        ryupy_mir::MirType::Str => ctx.runtime_refs.cast_to_str,
                        _ => ctx.runtime_refs.cast_to_i64,
                    };
                    ins.push(Instruction::call(cast));
                }
            }
        }
        Rvalue::DynBinary { op, lhs, rhs } => {
            let site = ctx.next_site();
            ins.push(Instruction::LoadConstantInt32(site));
            ins.push(Instruction::LoadConstantInt32(op.code()));
            lower_operand(lhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            lower_operand(rhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            ins.push(Instruction::call(ctx.runtime_refs.pic_binary));
        }
        Rvalue::DynCompare { op, lhs, rhs } => {
            ins.push(Instruction::LoadConstantInt32(op.code()));
            lower_operand(lhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            lower_operand(rhs, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            ins.push(Instruction::call(ctx.runtime_refs.dyn_compare));
        }
        Rvalue::DynUnary { op, operand } => {
            ins.push(Instruction::LoadConstantInt32(op.code()));
            lower_operand(
                operand,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            ins.push(Instruction::call(ctx.runtime_refs.dyn_unary));
        }
        Rvalue::DynTruthy { value } => {
            lower_operand(
                value,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            ins.push(Instruction::call(ctx.runtime_refs.dyn_truthy));
        }
        Rvalue::DynCallValue { callee, args } => {
            let site = ctx.next_site();
            ins.push(Instruction::LoadConstantInt32(site));
            lower_operand(
                callee,
                ins,
                ctx.param_slots,
                ctx.local_slots,
                ctx.runtime_refs,
            );
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            ins.push(Instruction::call(
                ctx.runtime_refs.pic_call_value[args.len()],
            ));
        }
        Rvalue::DynCallMethod { obj, name, args } => {
            let site = ctx.next_site();
            ins.push(Instruction::LoadConstantInt32(site));
            lower_operand(obj, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            ins.push(Instruction::load_string(name.clone()));
            for arg in args {
                lower_operand(arg, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            ins.push(Instruction::call(
                ctx.runtime_refs.pic_call_method[args.len()],
            ));
        }
        Rvalue::CallWithArgs {
            callee,
            prefix,
            args,
        } => {
            let is_method_call = matches!(callee, RuntimeFn::CallMethod);
            if is_method_call {
                let site = ctx.next_site();
                ins.push(Instruction::LoadConstantInt32(site));
            }
            for op in prefix {
                lower_operand(op, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            }
            lower_object_array(args, ins, ctx);
            let method = runtime_method(*callee, ctx.runtime_refs);
            ins.push(Instruction::call(method));
        }
        Rvalue::CatchException => {
            // 异常对象由 CLR 在进入 handler 时压栈，这里不发射任何取值指令，
            // 随后的 `stloc` 直接把它存进局部变量。
        }
        Rvalue::GetField { obj, field } => {
            lower_operand(obj, ins, ctx.param_slots, ctx.local_slots, ctx.runtime_refs);
            let class_id = match obj {
                Operand::Local(loc) => match ctx.locals[*loc].ty {
                    ryupy_mir::MirType::Class(id) => id,
                    _ => panic!("GetField obj 不是 Class 类型"),
                },
                _ => panic!("GetField obj 必须是局部变量操作数"),
            };
            let field_idx = ctx.class_info[&class_id].fields[*field as usize];
            ins.push(Instruction::LoadField {
                unaligned: None,
                volatile: false,
                param0: field_idx.into(),
            });
        }
    }
}

/// 可池化的动态数值字面量。
#[derive(Clone, Debug)]
pub(crate) enum LiteralInit {
    I64(i64),
    F64(f64),
    Bool(bool),
    BigInt(String),
}

/// 为 `Box` 的常量生成稳定的池键；不是数值字面量时返回 `None`。
pub(crate) fn boxed_literal_key(
    constant: &MirConstant,
    from: &ryupy_mir::MirType,
) -> Option<String> {
    use ryupy_mir::MirType;
    match (from, constant) {
        (MirType::I64, MirConstant::I64(v)) => Some(format!("i:{v}")),
        (MirType::F64, MirConstant::F64(v)) => Some(format!("f:{:016x}", v.to_bits())),
        (MirType::Bool, MirConstant::Bool(v)) => Some(format!("b:{v}")),
        (MirType::Dynamic, MirConstant::BigInt(text)) => Some(format!("n:{text}")),
        _ => None,
    }
}

/// 生成池条目的初始化值；与 [`boxed_literal_key`] 一一对应。
pub(crate) fn boxed_literal_init(
    constant: &MirConstant,
    from: &ryupy_mir::MirType,
) -> Option<LiteralInit> {
    use ryupy_mir::MirType;
    match (from, constant) {
        (MirType::I64, MirConstant::I64(v)) => Some(LiteralInit::I64(*v)),
        (MirType::F64, MirConstant::F64(v)) => Some(LiteralInit::F64(*v)),
        (MirType::Bool, MirConstant::Bool(v)) => Some(LiteralInit::Bool(*v)),
        (MirType::Dynamic, MirConstant::BigInt(text)) => Some(LiteralInit::BigInt(text.clone())),
        _ => None,
    }
}
