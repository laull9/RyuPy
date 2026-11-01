//! 数值列表岛的机械MIR展开：无副作用预检先行，失败回到原GetItem序列且仅执行一次。
//! typed读取与旧boxed读取汇入同一原生算术块；出口仅dirty值绕过原append物化。
use super::numeric_list::Plan;
use crate::mir::*;
use rustc_hash::FxHashMap;

pub(super) struct ReadTargets<'a> {
    pub(super) mode: LocalId,
    pub(super) ordinary_guards: BlockId,
    pub(super) dynamic: BlockId,
    pub(super) arithmetic: BlockId,
    pub(super) raw: &'a FxHashMap<LocalId, LocalId>,
}
pub(super) fn rewrite_factory(f: &mut Function, plan: &Plan) {
    if let Statement::Assign { value, .. } =
        &mut f.body.blocks[plan.factory.0].stmts[plan.factory.1]
    {
        *value = Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::NumericListCreate),
            args: vec![],
        };
    }
}
pub(super) fn read_path(
    f: &mut Function,
    plan: &Plan,
    mut original: Vec<Statement>,
    boxed_inputs: Vec<Statement>,
    targets: ReadTargets<'_>,
) -> Terminator {
    original.extend(boxed_inputs);
    let original_path = block(
        f,
        original,
        branch(targets.mode, targets.ordinary_guards, targets.dynamic),
    );
    let mut children = FxHashMap::default();
    let mut guards = Vec::new();
    let mut typed = Vec::new();
    for load in &plan.loads {
        let root = children.get(&load.root).copied().unwrap_or(load.root);
        let condition = local(f, MirType::Bool);
        let statements = if load.child {
            let child = local(f, MirType::Dynamic);
            children.insert(load.dest, child);
            vec![
                call(
                    child,
                    RuntimeFn::NumericListPeekList,
                    vec![get(root), get(load.key)],
                ),
                assign(
                    condition,
                    Rvalue::Binary {
                        op: BinOp::Ne,
                        lhs: get(child),
                        rhs: Operand::Const(Constant::Null),
                    },
                ),
            ]
        } else {
            // 编译器专用读取：同一root/key的CanReadF64真边支配此调用；期间仅纯预检。
            // 独立validator保证该事实未被调用/写失效；仅省重复显式范围判断，保CLR界限。
            typed.push(call(
                targets.raw[&load.dest],
                RuntimeFn::NumericListReadCheckedF64,
                vec![get(root), get(load.key)],
            ));
            vec![call(
                condition,
                RuntimeFn::NumericListCanReadF64,
                vec![get(root), get(load.key)],
            )]
        };
        guards.push((condition, statements));
    }
    let mut success = block(f, typed, Terminator::Goto(targets.arithmetic));
    for (condition, statements) in guards.into_iter().rev() {
        success = block(f, statements, branch(condition, success, original_path));
    }
    branch(targets.mode, success, original_path)
}

pub(super) fn exit_paths(
    f: &mut Function,
    plan: &Plan,
    raw_total: LocalId,
    mut statements: Vec<Statement>,
    terminator: Terminator,
) -> (BlockId, BlockId) {
    let tail = statements.split_off(plan.sink_position + 1);
    let after = block(f, tail, terminator);
    // clean路径完整保留原Line/append（共享盒和零轮初值）；dirty路径仅替换这一条调用。
    let mut typed: Vec<_> = statements[..plan.sink_position]
        .iter()
        .filter_map(|stmt| match stmt {
            Statement::Line(line) => Some(Statement::Line(*line)),
            _ => None,
        })
        .collect();
    typed.push(call(
        plan.sink_dest,
        RuntimeFn::NumericListAppendF64,
        vec![get(plan.receiver), get(raw_total)],
    ));
    let ordinary = block(f, statements, Terminator::Goto(after));
    let numeric = block(f, typed, Terminator::Goto(after));
    (ordinary, numeric)
}
fn local(f: &mut Function, ty: MirType) -> LocalId {
    f.body.locals.alloc(LocalDecl {
        ty,
        name: None,
        is_cell: false,
    })
}
fn block(f: &mut Function, stmts: Vec<Statement>, terminator: Terminator) -> BlockId {
    f.body.blocks.alloc(BasicBlock { stmts, terminator })
}
fn assign(dest: LocalId, value: Rvalue) -> Statement {
    Statement::Assign { dest, value }
}
fn call(dest: LocalId, callee: RuntimeFn, args: Vec<Operand>) -> Statement {
    assign(
        dest,
        Rvalue::Call {
            callee: Callee::Runtime(callee),
            args,
        },
    )
}
fn get(local: LocalId) -> Operand {
    Operand::Local(local)
}
fn branch(cond: LocalId, then_: BlockId, else_: BlockId) -> Terminator {
    Terminator::Branch {
        cond: get(cond),
        then_,
        else_,
    }
}
