//! 控制流简化 Pass（Simplify CFG）。
//!
//! 优化项：
//! 1. **折叠连续跳转链（Goto Chain Collapse）**：
//!    若一个基本块没有语句且终结符为直接跳转 `Goto(target)`，将所有前驱块的跳转目标直接重定向到 `target`；
//! 2. **消除无效分支（Dead Branch Elimination）**：
//!    若分支条件已知为布尔常量（`const true` / `const false`），将其退化为直接的 `Goto`；
//! 3. **不可达基本块清理（Unreachable Block Cleanup）**：
//!    从函数入口块进行可达性遍历，不可达块清空语句并设置为安全的 `Return(None)`。

use rustc_hash::FxHashSet;

use crate::mir::{BlockId, Constant, Function, Operand, Terminator};
use crate::passes::MirPass;

pub struct SimplifyCfg;

impl MirPass for SimplifyCfg {
    fn name(&self) -> &'static str {
        "simplify_cfg"
    }

    fn run(&self, func: &mut Function) {
        // 1. 折叠常量分支：branch const true/false -> goto
        for (_bb_id, bb) in func.body.blocks.iter_mut() {
            if let Terminator::Branch {
                cond: Operand::Const(Constant::Bool(b)),
                then_,
                else_,
            } = &bb.terminator
            {
                bb.terminator = Terminator::Goto(if *b { *then_ } else { *else_ });
            }
        }

        // 2. 收集空基本块的直接跳转目标（A -> B (empty goto C) => A -> C）
        let mut forward_map: rustc_hash::FxHashMap<BlockId, BlockId> =
            rustc_hash::FxHashMap::default();
        for (bb_id, bb) in func.body.blocks.iter() {
            if bb.stmts.is_empty() {
                if let Terminator::Goto(target) = bb.terminator {
                    if target != bb_id {
                        forward_map.insert(bb_id, target);
                    }
                }
            }
        }

        // 链式收拢（A -> B -> C => A -> C，带跳数限制防止环）
        for (_, bb) in func.body.blocks.iter_mut() {
            match &mut bb.terminator {
                Terminator::Goto(target) => {
                    let mut cur = *target;
                    let mut steps = 0;
                    while let Some(&next) = forward_map.get(&cur) {
                        cur = next;
                        steps += 1;
                        if steps > 16 {
                            break;
                        }
                    }
                    *target = cur;
                }
                Terminator::Branch { then_, else_, .. } => {
                    let mut cur_then = *then_;
                    let mut steps = 0;
                    while let Some(&next) = forward_map.get(&cur_then) {
                        cur_then = next;
                        steps += 1;
                        if steps > 16 {
                            break;
                        }
                    }
                    *then_ = cur_then;

                    let mut cur_else = *else_;
                    let mut steps = 0;
                    while let Some(&next) = forward_map.get(&cur_else) {
                        cur_else = next;
                        steps += 1;
                        if steps > 16 {
                            break;
                        }
                    }
                    *else_ = cur_else;
                }
                _ => {}
            }
        }

        // 3. 可达性分析
        let mut reachable = FxHashSet::default();
        let mut worklist = vec![func.body.entry];
        reachable.insert(func.body.entry);

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
                Terminator::Return(_) | Terminator::Unreachable => {}
            }
        }

        // 清空不可达块中的语句，避免残存无效引用
        for (bb_id, bb) in func.body.blocks.iter_mut() {
            if !reachable.contains(&bb_id) {
                bb.stmts.clear();
                bb.terminator = Terminator::Unreachable;
            }
        }
    }
}
