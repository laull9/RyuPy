//! 极窄死值消除：只删除从未读取的局部赋值中的Use/Box，保留全部可能抛错或回调操作。
//!
//! 不按类型标注猜测数值范围，不做跨块重排，不删除局部声明。每轮重新收集所有块、
//! 异常入口及递归数组中的读取，直到无删除；因此可依次消去unused=tmp与tmp=box(counter)。
use crate::mir::{Function, Rvalue, Statement, Terminator};
use crate::passes::MirPass;
use rustc_hash::FxHashSet;

pub struct DeadValues;

impl MirPass for DeadValues {
    fn name(&self) -> &'static str {
        "dead_values"
    }

    fn run(&self, func: &mut Function) {
        loop {
            let mut reads = FxHashSet::default();
            for region in &func.body.regions {
                // 异常表携带的局部也保守视为使用，不破坏catch入口的特殊栈契约。
                if let Some(local) = region.exc_local {
                    reads.insert(local);
                }
            }
            for (_, block) in func.body.blocks.iter() {
                for statement in &block.stmts {
                    match statement {
                        Statement::Assign { value, .. } | Statement::Eval(value) => {
                            read_value(value, &mut reads)
                        }
                        Statement::SetField { obj, value, .. } => {
                            read_operand(obj, &mut reads);
                            read_operand(value, &mut reads);
                        }
                        Statement::SetGlobal { value, .. } => read_operand(value, &mut reads),
                        Statement::Line(_) => {}
                    }
                }
                match &block.terminator {
                    Terminator::Branch { cond, .. } => read_operand(cond, &mut reads),
                    Terminator::Return(value) => {
                        if let Some(value) = value {
                            read_operand(value, &mut reads);
                        }
                    }
                    Terminator::Raise { value, cause } => {
                        for operand in value.iter().chain(cause.iter()) {
                            read_operand(operand, &mut reads);
                        }
                    }
                    Terminator::Suspend { value, input, .. } => {
                        read_operand(value, &mut reads);
                        if let Some(input) = input {
                            reads.insert(*input);
                        }
                    }
                    Terminator::Goto(_) | Terminator::Unreachable => {}
                }
            }
            let mut removed = false;
            for (_, block) in func.body.blocks.iter_mut() {
                block.stmts.retain(|statement| {
                    let discard = matches!(statement,
                        Statement::Assign { dest, value: Rvalue::Use(_) | Rvalue::Box { .. } }
                            if !reads.contains(dest));
                    removed |= discard;
                    !discard
                });
            }
            if !removed {
                break;
            }
        }
    }
}

pub(crate) use crate::local_access::{read_operand, read_value};
