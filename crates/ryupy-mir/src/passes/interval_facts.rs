//! 有预算的i64闭区间数据流；未访问与Top区分，合流取凸包，重复扩张提升Top保证终止。
//! 仅局部精确I64构造/复制与可证明不溢出的加减传播；bool、全局读取与未知调用不产生范围。
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};
use std::collections::VecDeque;

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub(super) struct Range {
    pub lo: i64,
    pub hi: i64,
}
impl Range {
    fn point(value: i64) -> Self {
        Self {
            lo: value,
            hi: value,
        }
    }
    pub fn add(self, other: Self) -> Option<Self> {
        Self::checked(
            i128::from(self.lo) + i128::from(other.lo),
            i128::from(self.hi) + i128::from(other.hi),
        )
    }
    fn sub(self, other: Self) -> Option<Self> {
        Self::checked(
            i128::from(self.lo) - i128::from(other.hi),
            i128::from(self.hi) - i128::from(other.lo),
        )
    }
    fn checked(lo: i128, hi: i128) -> Option<Self> {
        Some(Self {
            lo: lo.try_into().ok()?,
            hi: hi.try_into().ok()?,
        })
    }
    fn hull(self, other: Self) -> Self {
        Self {
            lo: self.lo.min(other.lo),
            hi: self.hi.max(other.hi),
        }
    }
}
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub(super) struct Facts {
    pub ranges: FxHashMap<LocalId, Range>,
}
impl Facts {
    fn operand(&self, op: &Operand) -> Option<Range> {
        match op {
            Operand::Const(Constant::I64(value)) => Some(Range::point(*value)),
            Operand::Local(id) => self.ranges.get(id).copied(),
            _ => None,
        }
    }
    pub fn transfer(&mut self, stmt: &Statement) {
        let Statement::Assign { dest, value } = stmt else {
            return;
        };
        // 读后kill保持x=x及x=x+1的旧值语义；Map缺项代表已达但无法证明范围。
        let range = match value {
            Rvalue::Use(op)
            | Rvalue::Box {
                value: op,
                from: MirType::I64,
            }
            | Rvalue::UnboxExactI64 { value: op } => self.operand(op),
            Rvalue::Binary {
                op: BinOp::Add,
                lhs,
                rhs,
            } => self
                .operand(lhs)
                .and_then(|left| left.add(self.operand(rhs)?)),
            Rvalue::Binary {
                op: BinOp::Sub,
                lhs,
                rhs,
            } => self
                .operand(lhs)
                .and_then(|left| left.sub(self.operand(rhs)?)),
            // 未改写的DynBinary保持未知：不能让后继候选依赖预算外的自动改写。
            _ => None,
        };
        self.ranges.remove(dest);
        if let Some(range) = range {
            self.ranges.insert(*dest, range);
        }
    }
}

pub(super) fn inputs(f: &Function) -> Option<FxHashMap<BlockId, Facts>> {
    if f.body.blocks.len() > 128 || f.body.locals.len() > 512 {
        return None;
    }
    let mut states = FxHashMap::default();
    states.insert(f.body.entry, Facts::default());
    let mut queue = VecDeque::from([f.body.entry]);
    let mut queued = FxHashSet::default();
    queued.insert(f.body.entry);
    let mut expansions: FxHashMap<(BlockId, LocalId), usize> = FxHashMap::default();
    let mut visits = 0;
    let mut steps = 0;
    while let Some(id) = queue.pop_front() {
        queued.remove(&id);
        visits += 1;
        if visits > 4096 {
            return None;
        }
        let block = &f.body.blocks[id];
        let mut output = states[&id].clone();
        for stmt in &block.stmts {
            steps += 1;
            if steps > 100_000 {
                return None;
            }
            output.transfer(stmt);
        }
        let targets: Vec<_> = match block.terminator {
            Terminator::Goto(to) => vec![to],
            Terminator::Branch { then_, else_, .. } => vec![then_, else_],
            _ => vec![],
        };
        for target in targets {
            let changed = if let Some(previous) = states.get_mut(&target) {
                let before = previous.clone();
                previous.ranges.retain(|local, range| {
                    let Some(incoming) = output.ranges.get(local) else {
                        return false;
                    };
                    let hull = range.hull(*incoming);
                    if hull != *range {
                        let count = expansions.entry((target, *local)).or_default();
                        *count += 1;
                        if *count > 2 {
                            return false;
                        }
                        *range = hull;
                    }
                    true
                });
                *previous != before
            } else {
                states.insert(target, output.clone());
                true
            };
            if changed && queued.insert(target) {
                queue.push_back(target);
            }
        }
    }
    Some(states)
}
