//! 字符串累加严格候选识别：
//! 仅匹配顶层模块全局（`GlobalId`）且 LHS 在 preheader 有严格确凿字符串字面量初值的循环。
//! 对整个 header / body / latch 实施严格指令白名单，拒绝任何动态调用、用户协议或中间观察。

use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(super) struct Plan {
    pub(super) preheader: BlockId,
    pub(super) header: BlockId,
    pub(super) body: BlockId,
    pub(super) latch: BlockId,
    pub(super) exit: BlockId,
    pub(super) update_start: usize,
    pub(super) update_len: usize,
    pub(super) target_id: GlobalId,
    pub(super) target_name: String,
    pub(super) rhs_literal: String,
    pub(super) init_operand: Operand,
}

pub(super) fn candidate(
    function: &Function,
    header: BlockId,
    predecessors: &FxHashMap<BlockId, Vec<BlockId>>,
) -> Option<Plan> {
    // 1. 严格 5 块自然循环控制流拓扑
    let Terminator::Branch {
        then_: body,
        else_: exit,
        ..
    } = &function.body.blocks[header].terminator
    else {
        return None;
    };
    let (body, exit) = (*body, *exit);

    let Terminator::Goto(latch) = function.body.blocks[body].terminator else {
        return None;
    };
    if !matches!(
        function.body.blocks[latch].terminator,
        Terminator::Goto(target) if target == header
    ) {
        return None;
    }

    let incoming = predecessors.get(&header)?;
    if incoming.len() != 2 || !incoming.contains(&latch) {
        return None;
    }
    let preheader = *incoming.iter().find(|&&block| block != latch)?;

    if !matches!(
        function.body.blocks[preheader].terminator,
        Terminator::Goto(target) if target == header
    ) {
        return None;
    }

    let block_set: FxHashSet<_> = [preheader, header, body, latch, exit].into_iter().collect();
    if block_set.len() != 5
        || function.body.entry == header
        || function.body.entry == body
        || function.body.entry == latch
        || function.body.entry == exit
        || predecessors.get(&body)?.as_slice() != [header]
        || predecessors.get(&latch)?.as_slice() != [body]
        || predecessors.get(&exit)?.as_slice() != [header]
    {
        return None;
    }

    // 2. 审查 header：仅允许纯原生 i64 / bool 循环条件计算
    if !is_pure_header_block(&function.body.blocks[header]) {
        return None;
    }

    // 3. 审查 latch：仅允许纯原生计数器步进
    if !is_pure_latch_block(&function.body.blocks[latch]) {
        return None;
    }

    let loop_blocks = [header, body, latch];

    // 4. 匹配模块全局字符串累加
    match_global_accumulator(function, preheader, header, body, latch, exit, &loop_blocks)
}

/// header 纯原生白名单：无函数调用、无动态协议、无全局读写
fn is_pure_header_block(block: &BasicBlock) -> bool {
    for stmt in &block.stmts {
        match stmt {
            Statement::Line(_) => {}
            Statement::Assign { value, .. } => match value {
                Rvalue::Binary { .. } | Rvalue::Unary { .. } | Rvalue::Use(_) => {}
                _ => return false,
            },
            _ => return false,
        }
    }
    matches!(block.terminator, Terminator::Branch { .. })
}

/// latch 纯原生白名单：仅允许原生算术步进与跳转
fn is_pure_latch_block(block: &BasicBlock) -> bool {
    for stmt in &block.stmts {
        match stmt {
            Statement::Line(_) => {}
            Statement::Assign { value, .. } => match value {
                Rvalue::Binary { op: BinOp::Add, .. } | Rvalue::Use(_) => {}
                _ => return false,
            },
            _ => return false,
        }
    }
    matches!(block.terminator, Terminator::Goto(_))
}

fn match_global_accumulator(
    function: &Function,
    preheader: BlockId,
    header: BlockId,
    body: BlockId,
    latch: BlockId,
    exit: BlockId,
    loop_blocks: &[BlockId; 3],
) -> Option<Plan> {
    let statements = &function.body.blocks[body].stmts;

    // 扫描 body 寻找 LoadName(target_id) -> DynBinary(Add) -> SetGlobal(target_id)
    for (start_idx, stmt) in statements.iter().enumerate() {
        let Statement::Assign {
            dest: temp_load,
            value:
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::LoadName),
                    args,
                },
        } = stmt
        else {
            continue;
        };
        let [Operand::Global(target_id), Operand::Const(Constant::Str(var_name))] = args.as_slice()
        else {
            continue;
        };
        let target_id = *target_id;
        let var_name = var_name.clone();

        // 证明 LHS 在 preheader 中具有确凿字符串字面量直接赋值且中途无改变
        let Some((init_op, _init_str)) = prove_preheader_string_literal(function, preheader, target_id)
        else {
            continue;
        };

        // 在 body 紧随其后查找加法与写回
        let mut add_info = None;
        let mut store_idx = None;
        let mut rhs_literal = None;
        let mut rhs_local = None;

        for (offset, follow) in statements[start_idx + 1..].iter().enumerate() {
            let cur_idx = start_idx + 1 + offset;
            match follow {
                Statement::Line(_) => continue,
                // 支持 RHS 在 DynBinary 前单步装箱或直接传递
                Statement::Assign {
                    dest: rhs_loc,
                    value:
                        Rvalue::Box {
                            value: Operand::Const(Constant::Str(s)),
                            from: MirType::Str,
                        }
                        | Rvalue::Use(Operand::Const(Constant::Str(s))),
                } if add_info.is_none() => {
                    rhs_local = Some(*rhs_loc);
                    rhs_literal = Some(s.clone());
                }
                Statement::Assign {
                    dest: temp_res,
                    value:
                        Rvalue::DynBinary {
                            op: DynOp::Add,
                            lhs: Operand::Local(lhs_loc),
                            rhs,
                            ..
                        },
                } if add_info.is_none() && lhs_loc == temp_load => {
                    add_info = Some((cur_idx, *temp_res));
                    if let Some(lit) = &rhs_literal {
                        if rhs != &Operand::Local(rhs_local.unwrap()) {
                            break;
                        }
                    } else if let Operand::Const(Constant::Str(s)) = rhs {
                        rhs_literal = Some(s.clone());
                    } else {
                        break;
                    }
                }
                Statement::SetGlobal {
                    dest: store_target,
                    value: Operand::Local(val_loc),
                } if add_info.is_some() => {
                    let (_, expected_res) = add_info.unwrap();
                    if store_target == &target_id && val_loc == &expected_res {
                        store_idx = Some(cur_idx);
                        break;
                    } else {
                        break;
                    }
                }
                _ => break,
            }
        }

        let (Some((_, temp_res)), Some(end_idx), Some(literal)) =
            (add_info, store_idx, rhs_literal)
        else {
            continue;
        };

        // 审查 body 的所有其余语句：必须在白名单内（无未知调用、无动态协议）
        if !is_pure_body_block(&function.body.blocks[body], start_idx, end_idx, target_id) {
            continue;
        }

        // 验证中间临时量与目标全局槽的严格唯一读写
        let mut target_reads = 0;
        let mut target_writes = 0;
        let mut temp_load_reads = 0;
        let mut temp_load_writes = 0;
        let mut temp_res_reads = 0;
        let mut temp_res_writes = 0;
        let mut rhs_local_reads = 0;
        let mut rhs_local_writes = 0;

        for (id, block) in function.body.blocks.iter() {
            let in_loop = loop_blocks.contains(&id);
            for s in &block.stmts {
                visit_operands_in_stmt(s, &mut |op| {
                    if let Operand::Local(loc) = op {
                        if *loc == temp_load {
                            temp_load_reads += 1;
                        }
                        if *loc == temp_res {
                            temp_res_reads += 1;
                        }
                        if let Some(rl) = rhs_local {
                            if *loc == rl {
                                rhs_local_reads += 1;
                            }
                        }
                    }
                    if in_loop {
                        if let Operand::Global(gid) = op {
                            if *gid == target_id {
                                target_reads += 1;
                            }
                        }
                    }
                });

                if let Statement::Assign { dest, .. } = s {
                    if *dest == temp_load {
                        temp_load_writes += 1;
                    }
                    if *dest == temp_res {
                        temp_res_writes += 1;
                    }
                    if let Some(rl) = rhs_local {
                        if *dest == rl {
                            rhs_local_writes += 1;
                        }
                    }
                }
                if in_loop {
                    if let Statement::SetGlobal { dest, .. } = s {
                        if *dest == target_id {
                            target_writes += 1;
                        }
                    }
                }
            }

            visit_operands_in_terminator(&block.terminator, &mut |op| {
                if let Operand::Local(loc) = op {
                    if *loc == temp_load {
                        temp_load_reads += 1;
                    }
                    if *loc == temp_res {
                        temp_res_reads += 1;
                    }
                }
                if in_loop {
                    if let Operand::Global(gid) = op {
                        if *gid == target_id {
                            target_reads += 1;
                        }
                    }
                }
            });
        }

        // target_id 在循环内有且仅有单次 LoadName 读取和单次 SetGlobal 写入
        // 中间临时量有且仅有单次定义与单次使用
        let rhs_ok = if rhs_local.is_some() {
            rhs_local_reads == 1 && rhs_local_writes == 1
        } else {
            true
        };

        if target_reads == 1
            && target_writes == 1
            && temp_load_reads == 1
            && temp_load_writes == 1
            && temp_res_reads == 1
            && temp_res_writes == 1
            && rhs_ok
        {
            return Some(Plan {
                preheader,
                header,
                body,
                latch,
                exit,
                update_start: start_idx,
                update_len: end_idx + 1 - start_idx,
                target_id,
                target_name: var_name,
                rhs_literal: literal,
                init_operand: init_op,
            });
        }
    }
    None
}

/// 证明 target_id 在 preheader 中具有确凿字符串字面量初值，且其后至 Goto(header) 无副作用/写覆盖
fn prove_preheader_string_literal(
    function: &Function,
    preheader: BlockId,
    target_id: GlobalId,
) -> Option<(Operand, String)> {
    let stmts = &function.body.blocks[preheader].stmts;
    let mut set_pos = None;
    let mut string_literal = None;
    let mut init_operand = None;

    for (idx, stmt) in stmts.iter().enumerate() {
        if let Statement::SetGlobal { dest, value } = stmt {
            if *dest == target_id {
                set_pos = Some(idx);
                match value {
                    Operand::Const(Constant::Str(s)) => {
                        string_literal = Some(s.clone());
                        init_operand = Some(value.clone());
                    }
                    Operand::Local(loc) => {
                        // 在 preheader 之前语句中查找 loc 的唯一赋值
                        for prev in &stmts[..idx] {
                            if let Statement::Assign {
                                dest: prev_dest,
                                value:
                                    Rvalue::Box {
                                        value: Operand::Const(Constant::Str(s)),
                                        from: MirType::Str,
                                    }
                                    | Rvalue::Use(Operand::Const(Constant::Str(s))),
                            } = prev
                            {
                                if prev_dest == loc {
                                    string_literal = Some(s.clone());
                                    init_operand = Some(Operand::Local(*loc));
                                    break;
                                }
                            }
                        }
                    }
                    _ => {
                        string_literal = None;
                        init_operand = None;
                    }
                }
            }
        }
    }

    let set_idx = set_pos?;
    let literal = string_literal?;
    let op = init_operand?;

    // 检查 SetGlobal 之后的语句：禁止再次写 target_id，禁止包含任意函数调用或动态协议
    for stmt in &stmts[set_idx + 1..] {
        match stmt {
            Statement::Line(_) => {}
            Statement::Assign { value, .. } => match value {
                Rvalue::Use(_) | Rvalue::Binary { .. } | Rvalue::Unary { .. } => {}
                _ => return None,
            },
            Statement::SetGlobal { dest, .. } if *dest == target_id => return None,
            Statement::SetGlobal { .. } => {}
            _ => return None,
        }
    }

    Some((op, literal))
}

/// 审查 body 中除了累加切片之外的其它语句：仅允许原生循环计数变量发布与行号
fn is_pure_body_block(
    block: &BasicBlock,
    slice_start: usize,
    slice_end: usize,
    target_id: GlobalId,
) -> bool {
    for (idx, stmt) in block.stmts.iter().enumerate() {
        if idx >= slice_start && idx <= slice_end {
            continue;
        }
        match stmt {
            Statement::Line(_) => {}
            Statement::Assign { value, .. } => match value {
                // 仅允许循环计数器装箱与基本赋值
                Rvalue::Box {
                    from: MirType::I64, ..
                }
                | Rvalue::Use(_) => {}
                _ => return false,
            },
            Statement::SetGlobal { dest, .. } => {
                if *dest == target_id {
                    return false;
                }
            }
            _ => return false,
        }
    }
    true
}

pub(super) fn predecessors(function: &Function) -> FxHashMap<BlockId, Vec<BlockId>> {
    let mut result: FxHashMap<_, Vec<_>> = function
        .body
        .blocks
        .iter()
        .map(|(id, _)| (id, vec![]))
        .collect();
    for (id, block) in function.body.blocks.iter() {
        let destinations = match &block.terminator {
            Terminator::Goto(target) | Terminator::Suspend { resume: target, .. } => vec![*target],
            Terminator::Branch { then_, else_, .. } => vec![*then_, *else_],
            Terminator::Return(_) | Terminator::Raise { .. } | Terminator::Unreachable => vec![],
        };
        for target in destinations {
            if let Some(incoming) = result.get_mut(&target) {
                incoming.push(id);
            }
        }
    }
    result
}

/// 完整递归遍历 Statement 中引用的所有操作数
pub(super) fn visit_operands_in_stmt<F: FnMut(&Operand)>(stmt: &Statement, visitor: &mut F) {
    match stmt {
        Statement::Assign { value, .. } | Statement::Eval(value) => {
            visit_operands_in_rvalue(value, visitor);
        }
        Statement::SetField { obj, value, .. } => {
            visit_operand(obj, visitor);
            visit_operand(value, visitor);
        }
        Statement::SetGlobal { value, .. } => {
            visit_operand(value, visitor);
        }
        Statement::Line(_) => {}
    }
}

/// 完整递归遍历 Terminator 中引用的所有操作数
pub(super) fn visit_operands_in_terminator<F: FnMut(&Operand)>(
    terminator: &Terminator,
    visitor: &mut F,
) {
    match terminator {
        Terminator::Branch { cond, .. } => visit_operand(cond, visitor),
        Terminator::Suspend { value, .. } => visit_operand(value, visitor),
        Terminator::Return(Some(val)) => visit_operand(val, visitor),
        Terminator::Raise { value, cause } => {
            for op in value.iter().chain(cause.iter()) {
                visit_operand(op, visitor);
            }
        }
        Terminator::Return(None) | Terminator::Goto(_) | Terminator::Unreachable => {}
    }
}

/// 完整递归遍历 Rvalue 中引用的所有操作数
pub(super) fn visit_operands_in_rvalue<F: FnMut(&Operand)>(value: &Rvalue, visitor: &mut F) {
    match value {
        Rvalue::IsVector128Supported => {}
        Rvalue::VectorPackF64 { low, high } => {
            visit_operand(low, visitor);
            visit_operand(high, visitor);
        }
        Rvalue::Use(v)
        | Rvalue::GeneratorDelegateStep { delegate: v }
        | Rvalue::GetField { obj: v, .. }
        | Rvalue::Box { value: v, .. }
        | Rvalue::Unbox { value: v, .. }
        | Rvalue::VectorLaneF64 { value: v, .. }
        | Rvalue::IsBoxedF64 { value: v }
        | Rvalue::UnboxExactF64 { value: v }
        | Rvalue::IsBoxedI64 { value: v }
        | Rvalue::UnboxExactI64 { value: v }
        | Rvalue::DynTruthy { value: v }
        | Rvalue::Unary { operand: v, .. }
        | Rvalue::Cast { operand: v, .. }
        | Rvalue::DynUnary { operand: v, .. } => visit_operand(v, visitor),
        Rvalue::Binary { lhs, rhs, .. }
        | Rvalue::VectorBinaryF64 { lhs, rhs, .. }
        | Rvalue::DynBinary { lhs, rhs, .. }
        | Rvalue::DynCompare { lhs, rhs, .. } => {
            visit_operand(lhs, visitor);
            visit_operand(rhs, visitor);
        }
        Rvalue::MakeGenerator { frame, origin, .. } => {
            for v in frame.iter().chain(origin.iter()) {
                visit_operand(v, visitor);
            }
        }
        Rvalue::Call { args, .. } | Rvalue::NewObject { args, .. } => {
            for arg in args {
                visit_operand(arg, visitor);
            }
        }
        Rvalue::CallWithArgs { prefix, args, .. } => {
            for arg in prefix.iter().chain(args) {
                visit_operand(arg, visitor);
            }
        }
        Rvalue::InstanceZeroCall { callee, .. } => {
            visit_operand(callee, visitor);
        }
        Rvalue::DynCallValue { callee, args } => {
            visit_operand(callee, visitor);
            for arg in args {
                visit_operand(arg, visitor);
            }
        }
        Rvalue::DynCallMethod { obj, args, .. } => {
            visit_operand(obj, visitor);
            for arg in args {
                visit_operand(arg, visitor);
            }
        }
        Rvalue::Array { elements, .. } => {
            for el in elements {
                visit_operand(el, visitor);
            }
        }
        Rvalue::ArrayView { source, .. } => {
            visit_operand(source, visitor);
        }
    }
}

/// 完整递归遍历 Operand（包含 Operand::Array）
fn visit_operand<F: FnMut(&Operand)>(op: &Operand, visitor: &mut F) {
    visitor(op);
    if let Operand::Array(elements) = op {
        for el in elements {
            visit_operand(el, visitor);
        }
    }
}
