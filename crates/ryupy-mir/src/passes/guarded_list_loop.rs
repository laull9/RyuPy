//! 将闭合的动态计数器布尔写入循环替换为受守卫Runtime调用。
//! 原CFG完整保留；精确类型/范围守卫失败无写入，null边返回原循环。
use super::MirPass;
use super::dead_values::{read_operand, read_value};
use super::numeric_accumulator_match::predecessors;
use crate::mir::*;
use rustc_hash::FxHashSet;

pub struct GuardedListLoop;

impl MirPass for GuardedListLoop {
    fn name(&self) -> &'static str {
        "guarded_list_loop"
    }
    fn run(&self, f: &mut Function) {
        if f.function_global.is_none() || f.env.is_some() || f.module_state.is_some()
            || !f.body.regions.is_empty() || !f.body.direct_globals_contracts.is_empty()
            || !f.body.source_handler_contracts.is_empty()
            || f.body.locals.iter().any(|(_, l)| l.is_cell)
            || f.body.blocks.iter().any(|(_, b)| {
                matches!(b.terminator, Terminator::Suspend { .. }) || b.stmts.iter().any(|s| {
                    matches!(s, Statement::Assign { value, .. } | Statement::Eval(value)
                        if matches!(value, Rvalue::MakeGenerator { .. } | Rvalue::MakeStateGenerator { .. }) || matches!(value,
                            Rvalue::Call { callee: Callee::Runtime(RuntimeFn::FunctionLocalsValidate
                                | RuntimeFn::FunctionLocalsEnsure | RuntimeFn::FunctionLocalsSync), .. }) || matches!(value,
                            Rvalue::Call { callee: Callee::Runtime(rt), .. } if crate::trace_product_abi::signature(*rt).is_some()))
                })
            }) { return; }
        let preds = predecessors(f);
        let plans: Vec<_> = f
            .body
            .blocks
            .iter()
            .filter_map(|(header, _)| candidate(f, header, &preds))
            .collect();
        for p in plans {
            expand(f, p);
        }
    }
}

struct Plan {
    pre: BlockId,
    header: BlockId,
    exit: BlockId,
    j: LocalId,
    args: Vec<Operand>,
}

fn candidate(
    f: &Function,
    header: BlockId,
    preds: &rustc_hash::FxHashMap<BlockId, Vec<BlockId>>,
) -> Option<Plan> {
    let Terminator::Branch {
        cond: Operand::Local(test),
        then_: body,
        else_: exit,
    } = f.body.blocks[header].terminator
    else {
        return None;
    };
    if !matches!(f.body.blocks[body].terminator, Terminator::Goto(back) if back == header)
        || preds.get(&body)?.as_slice() != [header]
    {
        return None;
    }
    let incoming = preds.get(&header)?;
    if incoming.len() != 2 || !incoming.contains(&body) {
        return None;
    }
    let pre = *incoming.iter().find(|&&b| b != body)?;
    if [pre, header, body, exit]
        .into_iter()
        .collect::<FxHashSet<_>>()
        .len()
        != 4
        || [header, body].contains(&f.body.entry)
        || !matches!(f.body.blocks[pre].terminator, Terminator::Goto(to) if to == header)
    {
        return None;
    }
    let hs: Vec<_> = f.body.blocks[header]
        .stmts
        .iter()
        .filter(|s| !matches!(s, Statement::Line(_)))
        .collect();
    let [
        Statement::Assign {
            dest: cmp,
            value:
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynRichCompareValue),
                    args,
                },
        },
        Statement::Assign {
            dest: truth,
            value:
                Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DynTruthy),
                    args: truth_args,
                },
        },
    ] = hs.as_slice()
    else {
        return None;
    };
    let [
        Operand::Const(Constant::I64(code)),
        Operand::Local(j),
        Operand::Local(bound),
    ] = args.as_slice()
    else {
        return None;
    };
    if *code != i64::from(DynOp::Le.code())
        || *truth != test
        || truth_args.as_slice() != [Operand::Local(*cmp)]
    {
        return None;
    }
    let bs: Vec<_> = f.body.blocks[body]
        .stmts
        .iter()
        .filter(|s| !matches!(s, Statement::Line(_)))
        .collect();
    let [
        Statement::Assign {
            dest: flag,
            value:
                Rvalue::Box {
                    value: Operand::Const(Constant::Bool(false)),
                    from: MirType::Bool,
                },
        },
        Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::SetItem),
            args: set,
        }),
        Statement::Assign {
            dest: sum,
            value:
                Rvalue::DynBinary {
                    op: DynOp::Add,
                    lhs: Operand::Local(left),
                    rhs: Operand::Local(step),
                    ..
                },
        },
        Statement::Assign {
            dest: target,
            value: Rvalue::Use(Operand::Local(result)),
        },
    ] = bs.as_slice()
    else {
        return None;
    };
    let [
        Operand::Local(list),
        Operand::Local(index),
        Operand::Local(value),
    ] = set.as_slice()
    else {
        return None;
    };
    if left != j || index != j || target != j || sum != result || flag != value {
        return None;
    }
    let inputs = [*list, *j, *bound, *step];
    let temps = [*cmp, *truth, *flag, *sum];
    // 独立临时与唯一计数器写入：禁止输入别名、重定义及跳过的临时槽向环外逃逸。
    if inputs.into_iter().collect::<FxHashSet<_>>().len() != 4
        || temps.into_iter().collect::<FxHashSet<_>>().len() != 4
        || temps
            .iter()
            .any(|t| inputs.contains(t) || f.params.contains(t))
        || inputs
            .iter()
            .any(|id| f.body.locals[*id].ty != MirType::Dynamic)
    {
        return None;
    }
    for (id, b) in f.body.blocks.iter() {
        if [header, body].contains(&id) {
            continue;
        }
        let mut reads = FxHashSet::default();
        for s in &b.stmts {
            if matches!(s, Statement::Assign { dest, .. } if temps.contains(dest)) {
                return None;
            }
            statement_reads(s, &mut reads);
        }
        terminator_reads(&b.terminator, &mut reads);
        if temps.iter().any(|t| reads.contains(t)) {
            return None;
        }
    }
    Some(Plan {
        pre,
        header,
        exit,
        j: *j,
        args: inputs.into_iter().map(Operand::Local).collect(),
    })
}

fn statement_reads(s: &Statement, reads: &mut FxHashSet<LocalId>) {
    match s {
        Statement::Assign { value, .. } | Statement::Eval(value) => read_value(value, reads),
        Statement::SetGlobal { value, .. } => read_operand(value, reads),
        Statement::SetField { obj, value, .. } => {
            read_operand(obj, reads);
            read_operand(value, reads);
        }
        Statement::Line(_) => {}
    }
}
fn terminator_reads(t: &Terminator, reads: &mut FxHashSet<LocalId>) {
    match t {
        Terminator::Branch { cond, .. } | Terminator::Suspend { value: cond, .. } => {
            read_operand(cond, reads)
        }
        Terminator::Return(value) => {
            if let Some(v) = value {
                read_operand(v, reads);
            }
        }
        Terminator::Raise { value, cause } => {
            for v in value.iter().chain(cause.iter()) {
                read_operand(v, reads);
            }
        }
        Terminator::Goto(_) | Terminator::Unreachable => {}
    }
}

fn expand(f: &mut Function, p: Plan) {
    let result = f.body.locals.alloc(LocalDecl {
        ty: MirType::Dynamic,
        name: None,
        is_cell: false,
    });
    let failed = f.body.locals.alloc(LocalDecl {
        ty: MirType::Bool,
        name: None,
        is_cell: false,
    });
    let success = f.body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: p.j,
            value: Rvalue::Use(Operand::Local(result)),
        }],
        terminator: Terminator::Goto(p.exit),
    });
    // 预头末尾已处在原while行号；快路最终资源分配失败的行号边界在ADR登记。
    let pre = &mut f.body.blocks[p.pre];
    pre.stmts.push(Statement::Assign {
        dest: result,
        value: Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::GuardedListSetFalseLoop),
            args: p.args,
        },
    });
    pre.stmts.push(Statement::Assign {
        dest: failed,
        value: Rvalue::Binary {
            op: BinOp::Eq,
            lhs: Operand::Local(result),
            rhs: Operand::Const(Constant::Null),
        },
    });
    pre.terminator = Terminator::Branch {
        cond: Operand::Local(failed),
        then_: p.header,
        else_: success,
    };
}
