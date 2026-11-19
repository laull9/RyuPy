//! 推导式中的有守卫整数执行区：纯操作组成DAG，边界统一物化；失败从原区入口继续。
//! 分析与改写分离；不跨迭代、用户协议、容器写入或暂停，且不从函数名猜测来源。
use super::MirPass;
use crate::local_access::{statement_reads, terminator_reads};
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(super) const LIMIT: i64 = (1 << 30) - 1;
pub(super) struct Plan {
    pub bounded: bool,
    pub block: BlockId,
    pub start: usize,
    pub end: usize,
    pub inputs: Vec<LocalId>,
    pub values: FxHashMap<LocalId, Value>,
    pub steps: Vec<(LocalId, Op)>,
    pub live: FxHashSet<LocalId>,
}
#[derive(Clone)]
pub(super) struct Value {
    pub lo: i64,
    pub hi: i64,
    pub boolean: bool,
    /// 已有盒及其别名必须保留；None表示本区新鲜算术结果。
    pub original: Option<Operand>,
}
#[derive(Clone)]
pub(super) enum Op {
    Copy(Operand),
    Binary(BinOp, Operand, Operand),
    Mod(Operand, Operand),
    Line(u32),
}
pub struct ScalarRegion;
impl MirPass for ScalarRegion {
    fn name(&self) -> &'static str {
        "scalar_region"
    }
    fn run(&self, f: &mut Function) {
        if !f.is_comprehension
            || f.env.is_some()
            || f.module_state.is_some()
            || !f.body.regions.is_empty()
            || f.body.locals.len() > 256
            || f.body.blocks.len() > 64
            || f.body.locals.iter().any(|(_, l)| l.is_cell)
            || !f.body.source_handler_contracts.is_empty()
            || !f.body.direct_globals_contracts.is_empty()
            || f.body.blocks.iter().any(|(_,b)| b.stmts.iter().any(|s| matches!(s,
                Statement::Assign {value:Rvalue::Call {callee:Callee::Runtime(rt),..},..}
                | Statement::Eval(Rvalue::Call {callee:Callee::Runtime(rt),..}) if crate::trace_product_abi::signature(*rt).is_some())))
        {
            return;
        }
        let mut plans = Vec::new();
        for (block, b) in f.body.blocks.iter() {
            let mut start = 0;
            while start < b.stmts.len() && plans.len() < 12 {
                if let Some(plan) = analyze(f, block, start) {
                    start = plan.end;
                    plans.push(plan);
                } else {
                    start += 1;
                }
            }
        }
        // 倒序切分，已有statement下标仍指向原块前缀；新增慢路不再次优化。
        for plan in plans.into_iter().rev() {
            super::scalar_region_expand::expand(f, plan);
        }
    }
}
fn analyze(f: &Function, block: BlockId, start: usize) -> Option<Plan> {
    analyze_mode(f, block, start, false).or_else(|| analyze_mode(f, block, start, true))
}
fn analyze_mode(f: &Function, block: BlockId, start: usize, bounded: bool) -> Option<Plan> {
    let stmts = &f.body.blocks[block].stmts;
    let mut p = Plan {
        bounded,
        block,
        start,
        end: start,
        inputs: vec![],
        values: FxHashMap::default(),
        steps: vec![],
        live: FxHashSet::default(),
    };
    let mut assigned = FxHashSet::default();
    let mut operations = 0;
    for (at, stmt) in stmts.iter().enumerate().skip(start).take(24) {
        if let Statement::Line(line) = stmt {
            // Line不定义值；复用任意LocalId仅作携带，展开时不访问此ID。
            let Some((id, _)) = f.body.locals.iter().next() else {
                break;
            };
            p.steps.push((id, Op::Line(*line)));
            p.end = at + 1;
            continue;
        }
        let Statement::Assign { dest, value } = stmt else {
            break;
        };
        if assigned.contains(dest) || p.inputs.contains(dest) {
            break;
        }
        let snapshot = (p.values.clone(), p.inputs.clone());
        let Some((step, mut fact, arithmetic)) = step(f, &mut p, value) else {
            p.values = snapshot.0;
            p.inputs = snapshot.1;
            break;
        };
        if p.inputs.len() > 4 {
            p.values = snapshot.0;
            p.inputs = snapshot.1;
            break;
        }
        if fact.original.is_none() && f.body.locals[*dest].ty == MirType::Dynamic {
            if let Op::Copy(Operand::Local(source)) = &step {
                if f.body.locals[*source].ty == MirType::Dynamic {
                    fact.original = Some(Operand::Local(*source));
                }
            }
        }
        assigned.insert(*dest);
        p.values.insert(*dest, fact);
        p.steps.push((*dest, step));
        operations += usize::from(arithmetic);
        p.end = at + 1;
    }
    if operations == 0 || p.inputs.is_empty() || assigned.is_empty() {
        return None;
    }
    // 全函数读集合比边界活跃性更保守；保留命名绑定、跨块/跨迭代的值和当前终结符输入。
    for (id, b) in f.body.blocks.iter() {
        for (at, s) in b.stmts.iter().enumerate() {
            if id != block || at < start || at >= p.end {
                p.live.extend(statement_reads(s));
            }
        }
        p.live.extend(terminator_reads(&b.terminator));
    }
    p.live.extend(
        f.body
            .locals
            .iter()
            .filter_map(|(id, l)| l.name.is_some().then_some(id)),
    );
    p.live.retain(|id| assigned.contains(id));
    // 需要发布的别名反向要求原定义也物化；DAG定义顺序保证随后只装箱一次。
    for _ in 0..assigned.len() {
        let more: Vec<_> = p
            .live
            .iter()
            .filter_map(|id| match &p.values[id].original {
                Some(Operand::Local(source)) if assigned.contains(source) => Some(*source),
                _ => None,
            })
            .collect();
        let previous = p.live.len();
        p.live.extend(more);
        if previous == p.live.len() {
            break;
        }
    }
    Some(p)
}
fn value(f: &Function, p: &mut Plan, op: &Operand) -> Option<Value> {
    match op {
        Operand::Const(Constant::I64(v)) => Some(Value {
            lo: *v,
            hi: *v,
            boolean: false,
            original: None,
        }),
        Operand::Local(id) => {
            if let Some(v) = p.values.get(id) {
                return Some(v.clone());
            }
            if f.body.locals[*id].ty != MirType::Dynamic {
                return None;
            }
            let v = Value {
                lo: if p.bounded { -LIMIT } else { i64::MIN },
                hi: if p.bounded { LIMIT } else { i64::MAX },
                boolean: false,
                original: Some(op.clone()),
            };
            p.inputs.push(*id);
            p.values.insert(*id, v.clone());
            Some(v)
        }
        _ => None,
    }
}
fn step(f: &Function, p: &mut Plan, v: &Rvalue) -> Option<(Op, Value, bool)> {
    match v {
        Rvalue::Use(op)
        | Rvalue::Box {
            value: op,
            from: MirType::I64,
        } => Some((Op::Copy(op.clone()), value(f, p, op)?, false)),
        Rvalue::DynBinary {
            op,
            lhs,
            rhs,
            inplace: false,
        } => {
            let a = value(f, p, lhs)?;
            let b = value(f, p, rhs)?;
            if a.boolean || b.boolean {
                return None;
            }
            let (operation, lo, hi) = match op {
                DynOp::Add => (
                    Op::Binary(BinOp::Add, lhs.clone(), rhs.clone()),
                    i128::from(a.lo) + i128::from(b.lo),
                    i128::from(a.hi) + i128::from(b.hi),
                ),
                DynOp::Sub => (
                    Op::Binary(BinOp::Sub, lhs.clone(), rhs.clone()),
                    i128::from(a.lo) - i128::from(b.hi),
                    i128::from(a.hi) - i128::from(b.lo),
                ),
                DynOp::Mul => {
                    let ends = [
                        i128::from(a.lo) * i128::from(b.lo),
                        i128::from(a.lo) * i128::from(b.hi),
                        i128::from(a.hi) * i128::from(b.lo),
                        i128::from(a.hi) * i128::from(b.hi),
                    ];
                    (
                        Op::Binary(BinOp::Mul, lhs.clone(), rhs.clone()),
                        *ends.iter().min()?,
                        *ends.iter().max()?,
                    )
                }
                // 非零常量除数排除ZeroDivision及Min/-1；符号语义交既有ModI64。
                DynOp::Mod if b.lo == b.hi && b.lo != 0 && !(a.lo == i64::MIN && b.lo == -1) => (
                    Op::Mod(lhs.clone(), rhs.clone()),
                    if b.lo < 0 { i128::from(b.lo) + 1 } else { 0 },
                    if b.lo > 0 { i128::from(b.lo) - 1 } else { 0 },
                ),
                _ => return None,
            };
            Some((
                operation,
                Value {
                    lo: lo.try_into().ok()?,
                    hi: hi.try_into().ok()?,
                    boolean: false,
                    original: None,
                },
                true,
            ))
        }
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::DynRichCompareValue),
            args,
        } if args.len() == 3 => {
            let Operand::Const(Constant::I64(code)) = args[0] else {
                return None;
            };
            let op = match code {
                1 => BinOp::Eq,
                2 => BinOp::Ne,
                3 => BinOp::Lt,
                4 => BinOp::Le,
                5 => BinOp::Gt,
                6 => BinOp::Ge,
                _ => return None,
            };
            let a = value(f, p, &args[1])?;
            let b = value(f, p, &args[2])?;
            if a.boolean || b.boolean {
                return None;
            }
            Some((
                Op::Binary(op, args[1].clone(), args[2].clone()),
                Value {
                    lo: 0,
                    hi: 1,
                    boolean: true,
                    original: None,
                },
                true,
            ))
        }
        Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::DynTruthy),
            args,
        } if args.len() == 1 => truth(f, p, &args[0]),
        Rvalue::DynTruthy { value: op } => truth(f, p, op),
        _ => None,
    }
}
fn truth(f: &Function, p: &mut Plan, op: &Operand) -> Option<(Op, Value, bool)> {
    let v = value(f, p, op)?;
    let step = if v.boolean {
        Op::Copy(op.clone())
    } else {
        Op::Binary(BinOp::Ne, op.clone(), Operand::Const(Constant::I64(0)))
    };
    Some((
        step,
        Value {
            lo: 0,
            hi: 1,
            boolean: true,
            original: None,
        },
        true,
    ))
}
