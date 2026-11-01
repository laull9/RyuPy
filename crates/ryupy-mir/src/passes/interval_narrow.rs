//! 将有限值域证明安全的原动态Add自动拆箱；不折叠原Box/Use，不改变已有整数盒身份。
//! 分析完整收敛后才一次性改写；预算失败、未知值域和可能溢出时原调用逐字保留。
use super::{
    MirPass,
    interval_facts::{self, Facts},
};
use crate::mir::*;
pub struct IntervalNarrow;
impl MirPass for IntervalNarrow {
    fn name(&self) -> &'static str {
        "interval_narrow"
    }
    fn run(&self, f: &mut Function) {
        if f.signature_global.is_none()
            || f.env.is_some()
            || !f.body.regions.is_empty()
            || f.body.locals.iter().any(|(_, l)| l.is_cell)
            || f.body.blocks.iter().any(|(_, b)| {
                matches!(b.terminator, Terminator::Suspend { .. })
                    || b.stmts.iter().any(|s| {
                        matches!(
                            s,
                            Statement::Assign {
                                value: Rvalue::MakeGenerator { .. },
                                ..
                            }
                        )
                    })
            })
        {
            return;
        }
        let Some(states) = interval_facts::inputs(f) else {
            return;
        };
        let mut plans = Vec::new();
        for (bb, b) in f.body.blocks.iter() {
            let Some(input) = states.get(&bb) else {
                continue;
            };
            let mut facts = input.clone();
            for (index, stmt) in b.stmts.iter().enumerate() {
                if let Statement::Assign {
                    dest,
                    value:
                        Rvalue::DynBinary {
                            op: DynOp::Add,
                            lhs: Operand::Local(lhs),
                            rhs: Operand::Local(rhs),
                            inplace: false,
                        },
                } = stmt
                    && f.body.locals[*dest].ty == MirType::Dynamic
                    && f.body.locals[*lhs].ty == MirType::Dynamic
                    && f.body.locals[*rhs].ty == MirType::Dynamic
                    && safe(&facts, *lhs, *rhs)
                {
                    plans.push((bb, index, *dest, *lhs, *rhs));
                }
                facts.transfer(stmt);
            }
        }
        // 源位置倒序改写，插入局部不会使尚待处理的statement下标失效；最多32点控制代码体积。
        plans.truncate(32);
        for (bb, index, dest, lhs, rhs) in plans.into_iter().rev() {
            let left = local(f);
            let right = local(f);
            let sum = local(f);
            let stmts = vec![
                Statement::Assign {
                    dest: left,
                    value: Rvalue::UnboxExactI64 {
                        value: Operand::Local(lhs),
                    },
                },
                Statement::Assign {
                    dest: right,
                    value: Rvalue::UnboxExactI64 {
                        value: Operand::Local(rhs),
                    },
                },
                // 用checked Add保最后防线；区间证明保证不会触发CLR溢出，不能仅凭注解选择。
                Statement::Assign {
                    dest: sum,
                    value: Rvalue::Binary {
                        op: BinOp::Add,
                        lhs: Operand::Local(left),
                        rhs: Operand::Local(right),
                    },
                },
                Statement::Assign {
                    dest,
                    value: Rvalue::Box {
                        value: Operand::Local(sum),
                        from: MirType::I64,
                    },
                },
            ];
            f.body.blocks[bb].stmts.splice(index..=index, stmts);
        }
    }
}
fn safe(facts: &Facts, lhs: LocalId, rhs: LocalId) -> bool {
    facts
        .ranges
        .get(&lhs)
        .and_then(|left| left.add(*facts.ranges.get(&rhs)?))
        .is_some()
}
fn local(f: &mut Function) -> LocalId {
    f.body.locals.alloc(LocalDecl {
        ty: MirType::I64,
        name: None,
        is_cell: false,
    })
}
