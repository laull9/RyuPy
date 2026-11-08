//! 检查最终私有帧的写入与恢复模板；来源旁证只提供身份，不豁免任何真实操作。
use crate::trace_frame_values::{index, inputs};
use crate::trace_lowering::{GeneratorLayout, GeneratorOrigin};
use crate::*;

fn contains(value: &Operand, frame: LocalId) -> bool {
    match value {
        Operand::Local(id) => *id == frame,
        Operand::Array(items) => items.iter().any(|item| contains(item, frame)),
        _ => false,
    }
}

/// 沿当前基本块中先前的装箱/复制逐项回溯，不能引用未来写或同名临时。
fn saved_operand(value: &Operand, stmts: &[Statement], expected: LocalId, budget: usize) -> bool {
    if budget == 0 { return false; }
    let Operand::Local(id) = value else {
        return false
    };
    if *id == expected {
        return true;
    }
    for (position, stmt) in stmts.iter().enumerate().rev() {
        if let Statement::Assign { dest, value } = stmt
            && dest == id
        {
            return match value {
                Rvalue::Box { value, .. } | Rvalue::Use(value) => {
                    saved_operand(value, &stmts[..position], expected, budget - 1)
                }
                _ => false,
            };
        }
    }
    false
}

pub(super) fn check(
    body: &Body,
    origin: &GeneratorOrigin,
    layout: &GeneratorLayout,
) -> Result<usize, String> {
    let frame = origin.frame_parameter;
    let mut handlers = Vec::new();
    for route in &layout.exception_routes {
        let target = layout.pc_targets.iter().find(|(pc, _)| *pc == route.handler_pc)
            .map(|(_, block)| *block).ok_or("caught handler 缺失最终 PC 映射")?;
        if !handlers.contains(&target) { handlers.push(target); }
    }
    if handlers.len() != layout.catch_reads.len()
        || handlers.iter().any(|handler|
            layout.catch_reads.iter().filter(|(block, _, _)| block == handler).count() != 1)
    {
        return Err("caught 读取旁证没有逐一绑定逻辑 handler".into());
    }
    let mut obligations = 0;
    let mut catch_reads = Vec::new();
    let mut restored = vec![false; layout.saved_locals.len()];
    for (block_id, block) in body.blocks.iter() {
        for (position, stmt) in block.stmts.iter().enumerate() {
            match stmt {
                Statement::Assign { dest, .. } if *dest == frame => {
                    return Err("私有 frame 参数被覆盖".into());
                }
                Statement::SetField { .. } | Statement::SetGlobal { .. } => {
                    return Err("生成器 frame 范围不接纳字段或全局写".into());
                }
                _ => {}
            }
            let value = match stmt {
                Statement::Assign { value, .. } | Statement::Eval(value) => value,
                Statement::Line(_) => continue,
                _ => return Err("未登记语句".into()),
            };
            if matches!(value, Rvalue::CatchException)
                && (position != 0 || !body.regions.iter().any(|region| region.handler == block_id))
            {
                return Err("CatchException 不在真实物理 handler 首句".into());
            }
            let args = inputs(value)?;
            if let Rvalue::Call { callee: Callee::Runtime(runtime), args } = value
                && matches!(runtime, RuntimeFn::ArrayGet | RuntimeFn::ArraySet)
            {
                let expected_len = if *runtime == RuntimeFn::ArrayGet { 2 } else { 3 };
                if args.len() != expected_len
                    || !matches!(args[0], Operand::Local(id) if id == frame)
                {
                    return Err("数组操作不是直接私有 frame 访问".into());
                }
                let Operand::Const(Constant::I64(slot)) = args[1] else {
                    return Err("未知或别名 frame 槽".into());
                };
                if slot < 0 || slot as usize >= origin.initial_slots.len() {
                    return Err("frame 槽越界".into());
                }
                if slot == 1 || slot == 2 {
                    return Err("正常 next 的控制槽不允许 MIR 直接读写".into());
                }
                if slot == 3 && *runtime == RuntimeFn::ArrayGet {
                    let Statement::Assign { dest, .. } = stmt else {
                        return Err("caught 读取没有目标局部".into());
                    };
                    let site = (block_id, position, *dest);
                    if !layout.catch_reads.contains(&site) {
                        return Err("caught 读取偏离真实 handler 替换点或目标局部".into());
                    }
                    catch_reads.push(site);
                    if !matches!(block.stmts.get(position + 1),
                        Some(Statement::Eval(Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ArraySet), args
                        })) if args.len() == 3
                            && matches!(args[0], Operand::Local(id) if id == frame)
                            && matches!(args[1], Operand::Const(Constant::I64(3)))
                            && matches!(args[2], Operand::Const(Constant::None)))
                    {
                        return Err("handler 读取 caught 后没有立即清 None".into());
                    }
                }
                if slot >= 4 {
                    let local = *layout.saved_locals.get(slot as usize - 4)
                        .ok_or("缺失保存局部旁证")?;
                    if index(local) + 4 != slot as usize {
                        return Err("保存局部槽旁证不连续".into());
                    }
                    if *runtime == RuntimeFn::ArraySet {
                        if !saved_operand(&args[2], &block.stmts[..position], local, 32) {
                            return Err("保存槽写入了其他局部或未知值".into());
                        }
                    } else {
                        if block_id != body.entry {
                            return Err("普通局部槽在恢复入口以外读取".into());
                        }
                        let Statement::Assign { dest, .. } = stmt else {
                            return Err("恢复槽读取没有目标".into());
                        };
                        if *dest != local {
                            let Some(Statement::Assign { dest: output,
                                value: Rvalue::Unbox { value: Operand::Local(input), .. } })
                                = block.stmts.get(position + 1) else {
                                    return Err("恢复槽缺少紧邻拆箱".into());
                                };
                            if *output != local || input != dest {
                                return Err("恢复槽拆箱目标错误".into());
                            }
                        }
                        if restored[slot as usize - 4] {
                            return Err("恢复槽重复读取".into());
                        }
                        restored[slot as usize - 4] = true;
                    }
                }
                if args.iter().skip(1).any(|arg| contains(arg, frame)) {
                    return Err("frame 自身写入槽或索引".into());
                }
                obligations += 1;
            } else if args.iter().any(|arg| contains(arg, frame)) {
                if !matches!(value, Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::GeneratorInput), args
                } if args.len() == 1 && matches!(args[0], Operand::Local(id) if id == frame)) {
                    return Err("frame 逃逸、别名或未登记 helper".into());
                }
                obligations += 1;
            }
        }
        let values: Vec<&Operand> = match &block.terminator {
            Terminator::Goto(_) | Terminator::Unreachable => vec![],
            Terminator::Branch { cond, .. } => vec![cond],
            Terminator::Return(value) => value.iter().collect(),
            Terminator::Raise { value, cause } => value.iter().chain(cause).collect(),
            Terminator::Suspend { .. } => return Err("仍有未降级暂停边".into()),
        };
        if values.iter().any(|value| contains(value, frame)) {
            return Err("frame 从终结符逃逸".into());
        }
    }
    if catch_reads.len() != layout.catch_reads.len()
        || layout.catch_reads.iter().any(|site| !catch_reads.contains(site))
    {
        return Err("登记的 caught 读取缺失或被替换".into());
    }
    if restored.iter().any(|done| !done) {
        return Err("局部恢复槽缺失".into());
    }
    Ok(obligations)
}
