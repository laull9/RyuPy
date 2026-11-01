//! 受控数值列表读取的must证明：冻结局部pair、真边支配、写入及潜在回调使证明失效。
//! 不依赖Python标注或列表名字，也不将未到达的回边视为已知成功。
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};
use std::collections::VecDeque;

type Pair = (LocalId, LocalId);
#[derive(Clone, Default, PartialEq, Eq)]
struct Facts {
    readable: FxHashSet<Pair>,
    predicates: FxHashMap<LocalId, Pair>,
}
impl Facts {
    fn meet(&mut self, other: &Self) {
        self.readable.retain(|pair| other.readable.contains(pair));
        self.predicates
            .retain(|id, pair| other.predicates.get(id) == Some(pair));
    }
    fn kill(&mut self, local: LocalId) {
        // 保守地也移除该条件建立的事实；不借旧bool在写入后重建证明。
        if let Some(pair) = self.predicates.remove(&local) {
            self.readable.remove(&pair);
        }
        self.readable
            .retain(|(list, index)| *list != local && *index != local);
        self.predicates
            .retain(|_, (list, index)| *list != local && *index != local);
    }
    fn transfer(&mut self, statement: &Statement) {
        match statement {
            Statement::Assign { dest, value } => {
                if !pure(value) {
                    *self = Self::default();
                }
                self.kill(*dest);
                if let Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::NumericListCanReadF64),
                    args,
                } = value
                    && let Some(pair) = pair(args)
                    && pair.0 != *dest
                    && pair.1 != *dest
                {
                    self.predicates.insert(*dest, pair);
                }
            }
            Statement::Eval(value) => {
                if !pure(value) {
                    *self = Self::default();
                }
            }
            Statement::SetField { .. } | Statement::SetGlobal { .. } => *self = Self::default(),
            Statement::Line(_) => {}
        }
    }
}
fn pair(args: &[Operand]) -> Option<Pair> {
    match args {
        [Operand::Local(list), Operand::Local(index)] => Some((*list, *index)),
        _ => None,
    }
}
/// 只有无Python回调/写入的节点保留事实；新增操作默认不纯，避免漏审后产生不安全读取。
fn pure(value: &Rvalue) -> bool {
    match value {
        Rvalue::Use(_)
        | Rvalue::Box { .. }
        | Rvalue::Binary { .. }
        | Rvalue::Unary { .. }
        | Rvalue::Cast { .. }
        | Rvalue::IsBoxedI64 { .. }
        | Rvalue::UnboxExactI64 { .. }
        | Rvalue::IsBoxedF64 { .. }
        | Rvalue::UnboxExactF64 { .. }
        | Rvalue::IsVector128Supported
        | Rvalue::VectorPackF64 { .. }
        | Rvalue::VectorBinaryF64 { .. }
        | Rvalue::VectorLaneF64 { .. } => true,
        Rvalue::Call {
            callee: Callee::Runtime(runtime),
            ..
        } => matches!(
            runtime,
            RuntimeFn::NumericListPeekList
                | RuntimeFn::NumericListCanReadF64
                | RuntimeFn::NumericListReadF64
                | RuntimeFn::NumericListReadCheckedF64
        ),
        _ => false,
    }
}
fn inputs(function: &Function) -> FxHashMap<BlockId, Facts> {
    let mut states = FxHashMap::default();
    states.insert(function.body.entry, Facts::default());
    let mut queue = VecDeque::from([function.body.entry]);
    while let Some(id) = queue.pop_front() {
        let Some((_, block)) = function.body.blocks.iter().find(|(other, _)| *other == id) else {
            continue;
        };
        let mut out = states[&id].clone();
        for statement in &block.stmts {
            out.transfer(statement);
        }
        let edges = match &block.terminator {
            Terminator::Goto(target) => vec![(*target, out)],
            Terminator::Branch { cond, then_, else_ } => {
                let mut yes = out.clone();
                if let Operand::Local(condition) = cond
                    && let Some(pair) = out.predicates.get(condition).copied()
                {
                    yes.readable.insert(pair);
                    out.readable.remove(&pair);
                }
                vec![(*then_, yes), (*else_, out)]
            }
            _ => vec![],
        };
        for (target, incoming) in edges {
            if let Some(previous) = states.get_mut(&target) {
                let old = previous.clone();
                previous.meet(&incoming);
                if old != *previous {
                    queue.push_back(target);
                }
            } else {
                states.insert(target, incoming);
                queue.push_back(target);
            }
        }
    }
    states
}
fn checked_args(value: &Rvalue) -> Option<&[Operand]> {
    if let Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::NumericListReadCheckedF64),
        args,
    } = value
    {
        Some(args)
    } else {
        None
    }
}
pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    for (_, function) in program.functions.iter() {
        // 受控ABI不是打包参数调用；禁止另一调用表示绕过固定实参和支配证明。
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                if matches!(
                    statement,
                    Statement::Assign {
                        value: Rvalue::CallWithArgs {
                            callee: RuntimeFn::NumericListReadCheckedF64,
                            ..
                        },
                        ..
                    } | Statement::Eval(Rvalue::CallWithArgs {
                        callee: RuntimeFn::NumericListReadCheckedF64,
                        ..
                    })
                ) {
                    errors.push(format!(
                        "函数 {} 的受控列表读取禁止CallWithArgs",
                        function.name
                    ));
                }
            }
        }
        let uses = function.body.blocks.iter().any(|(_, block)| {
            block.stmts.iter().any(|statement| match statement {
                Statement::Assign { value, .. } | Statement::Eval(value) => {
                    checked_args(value).is_some()
                }
                _ => false,
            })
        });
        if !uses {
            continue;
        }
        if !function.body.regions.is_empty()
            || function
                .body
                .blocks
                .iter()
                .any(|(_, block)| matches!(block.terminator, Terminator::Suspend { .. }))
        {
            errors.push(format!(
                "函数 {} 的受控列表读取不支持EH或暂停",
                function.name
            ));
            continue;
        }
        let states = inputs(function);
        for (id, block) in function.body.blocks.iter() {
            let mut state = states.get(&id).cloned();
            for statement in &block.stmts {
                if let Statement::Assign { value, .. } | Statement::Eval(value) = statement
                    && let Some(args) = checked_args(value)
                    && !pair(args).is_some_and(|pair| {
                        state
                            .as_ref()
                            .is_some_and(|facts| facts.readable.contains(&pair))
                    })
                {
                    errors.push(format!(
                        "函数 {} 的受控列表读取缺少同一局部pair的有效CanRead真边证明",
                        function.name
                    ));
                }
                if let Some(state) = &mut state {
                    state.transfer(statement);
                }
            }
        }
    }
}
