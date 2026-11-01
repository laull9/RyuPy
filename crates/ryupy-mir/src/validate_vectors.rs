//! 私有向量的类型、无逃逸与硬件支配验证；不可达状态和已达空事实严格分离。
use crate::mir::*;
use crate::passes::dead_values::{read_operand, read_value};
use crate::validate::operand_type;
use rustc_hash::{FxHashMap, FxHashSet};
use std::collections::VecDeque;

#[derive(Clone, Default, PartialEq, Eq)]
struct Facts {
    hardware: bool,
    predicates: FxHashSet<LocalId>,
}
impl Facts {
    fn transfer(&mut self, statement: &Statement) {
        if let Statement::Assign { dest, value } = statement {
            self.predicates.remove(dest);
            if matches!(value, Rvalue::IsVector128Supported) {
                self.predicates.insert(*dest);
            }
        }
    }
    fn meet(&mut self, other: &Self) {
        self.hardware &= other.hardware;
        self.predicates.retain(|id| other.predicates.contains(id));
    }
}

fn inputs(function: &Function) -> FxHashMap<BlockId, Facts> {
    let mut inputs = FxHashMap::default();
    inputs.insert(function.body.entry, Facts::default());
    let mut queue = VecDeque::from([function.body.entry]);
    while let Some(id) = queue.pop_front() {
        let Some((_, block)) = function.body.blocks.iter().find(|(other, _)| *other == id) else {
            continue;
        };
        let mut state = inputs[&id].clone();
        for statement in &block.stmts {
            state.transfer(statement);
        }
        let edges = match &block.terminator {
            Terminator::Goto(next) => vec![(*next, state)],
            Terminator::Branch { cond, then_, else_ } => {
                let mut yes = state.clone();
                if let Operand::Local(condition) = cond
                    && state.predicates.contains(condition)
                {
                    yes.hardware = true;
                }
                vec![(*then_, yes), (*else_, state)]
            }
            _ => vec![],
        };
        for (target, incoming) in edges {
            if let Some(previous) = inputs.get_mut(&target) {
                let old = previous.clone();
                previous.meet(&incoming);
                if *previous != old {
                    queue.push_back(target);
                }
            } else {
                inputs.insert(target, incoming);
                queue.push_back(target);
            }
        }
    }
    inputs
}

pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    for (_, global) in program.globals.iter() {
        if global.ty == MirType::V128F64 {
            errors.push("向量不得存入模块全局".into());
        }
    }
    for (_, class) in program.classes.iter() {
        if class.fields.iter().any(|(_, ty)| *ty == MirType::V128F64) {
            errors.push("向量不得存入类字段".into());
        }
    }
    for (_, function) in program.functions.iter() {
        validate_function(program, function, errors);
    }
}

fn validate_function(program: &Program, function: &Function, errors: &mut Vec<String>) {
    let vectors: FxHashSet<_> = function
        .body
        .locals
        .iter()
        .filter(|(_, local)| local.ty == MirType::V128F64)
        .map(|(id, _)| id)
        .collect();
    if function.ret == MirType::V128F64
        || function.params.iter().any(|id| vectors.contains(id))
        || function
            .param_meta
            .iter()
            .any(|param| param.ty == MirType::V128F64)
        || function.env.is_some_and(|id| vectors.contains(&id))
    {
        errors.push(format!(
            "函数 {} 的向量不得进入参数或返回ABI",
            function.name
        ));
    }
    if vectors.iter().any(|id| function.body.locals[*id].is_cell) {
        errors.push("向量不得进入cell".into());
    }
    let has_vector_op = function.body.blocks.iter().any(|(_, block)| {
        block.stmts.iter().any(|stmt| {
            matches!(
                stmt,
                Statement::Assign {
                    value: Rvalue::Box {
                        from: MirType::V128F64,
                        ..
                    } | Rvalue::Unbox {
                        to: MirType::V128F64,
                        ..
                    } | Rvalue::IsVector128Supported
                        | Rvalue::VectorPackF64 { .. }
                        | Rvalue::VectorBinaryF64 { .. }
                        | Rvalue::VectorLaneF64 { .. },
                    ..
                } | Statement::Eval(
                    Rvalue::Box {
                        from: MirType::V128F64,
                        ..
                    } | Rvalue::Unbox {
                        to: MirType::V128F64,
                        ..
                    } | Rvalue::IsVector128Supported
                        | Rvalue::VectorPackF64 { .. }
                        | Rvalue::VectorBinaryF64 { .. }
                        | Rvalue::VectorLaneF64 { .. }
                )
            )
        })
    });
    if vectors.is_empty() && !has_vector_op {
        return;
    }
    if !function.body.regions.is_empty()
        || function.env.is_some()
        || function
            .body
            .blocks
            .iter()
            .any(|(_, b)| matches!(b.terminator, Terminator::Suspend { .. }))
    {
        errors.push("私有向量首版不支持EH、闭包或暂停函数".into());
    }
    let states = inputs(function);
    for (id, block) in function.body.blocks.iter() {
        let mut state = states.get(&id).cloned();
        for stmt in &block.stmts {
            match stmt {
                Statement::Assign { dest, value } => {
                    let result = function
                        .body
                        .locals
                        .iter()
                        .find(|(id, _)| id == dest)
                        .map(|(_, local)| &local.ty);
                    check_value(
                        program,
                        function,
                        value,
                        result,
                        state.as_ref(),
                        &vectors,
                        errors,
                    );
                }
                Statement::Eval(value) => check_value(
                    program,
                    function,
                    value,
                    None,
                    state.as_ref(),
                    &vectors,
                    errors,
                ),
                Statement::SetField { obj, value, .. } => {
                    reject_operand(obj, &vectors, errors);
                    reject_operand(value, &vectors, errors);
                }
                Statement::SetGlobal { value, .. } => reject_operand(value, &vectors, errors),
                Statement::Line(_) => {}
            }
            if let Some(state) = &mut state {
                state.transfer(stmt);
            }
        }
        match &block.terminator {
            Terminator::Branch { cond, .. } => reject_operand(cond, &vectors, errors),
            Terminator::Return(value) => {
                if let Some(value) = value {
                    reject_operand(value, &vectors, errors);
                }
            }
            Terminator::Raise { value, cause } => {
                for value in value.iter().chain(cause) {
                    reject_operand(value, &vectors, errors);
                }
            }
            Terminator::Suspend { value, .. } => reject_operand(value, &vectors, errors),
            Terminator::Goto(_) | Terminator::Unreachable => {}
        }
    }
}

fn reject_operand(operand: &Operand, vectors: &FxHashSet<LocalId>, errors: &mut Vec<String>) {
    let mut reads = FxHashSet::default();
    read_operand(operand, &mut reads);
    if reads.iter().any(|id| vectors.contains(id)) {
        errors.push("私有向量不能逃逸到Python或通用ABI".into());
    }
}

fn check_value(
    program: &Program,
    function: &Function,
    value: &Rvalue,
    result: Option<&MirType>,
    facts: Option<&Facts>,
    vectors: &FxHashSet<LocalId>,
    errors: &mut Vec<String>,
) {
    let is_type = |operand, ty| operand_type(program, function, operand) == Some(ty);
    let (expected, valid, needs_hardware) = match value {
        Rvalue::IsVector128Supported => (MirType::Bool, true, false),
        Rvalue::VectorPackF64 { low, high } => (
            MirType::V128F64,
            is_type(low, MirType::F64) && is_type(high, MirType::F64),
            true,
        ),
        Rvalue::VectorBinaryF64 { op, lhs, rhs } => (
            MirType::V128F64,
            matches!(op, BinOp::Add | BinOp::Sub | BinOp::Mul)
                && is_type(lhs, MirType::V128F64)
                && is_type(rhs, MirType::V128F64),
            true,
        ),
        Rvalue::VectorLaneF64 { value, lane } => (
            MirType::F64,
            *lane < 2 && is_type(value, MirType::V128F64),
            true,
        ),
        Rvalue::Use(Operand::Local(local)) if vectors.contains(local) => {
            (MirType::V128F64, true, false)
        }
        _ => {
            let mut reads = FxHashSet::default();
            read_value(value, &mut reads);
            if result == Some(&MirType::V128F64)
                || reads.iter().any(|id| vectors.contains(id))
                || matches!(
                    value,
                    Rvalue::Box {
                        from: MirType::V128F64,
                        ..
                    } | Rvalue::Unbox {
                        to: MirType::V128F64,
                        ..
                    }
                )
            {
                errors.push("私有向量只能通过向量节点或同型局部Use读写".into());
            }
            return;
        }
    };
    if !valid || result != Some(&expected) {
        errors.push("向量节点的操作、通道、参数或返回类型错误".into());
    }
    // 尚不可达的块不构成硬件证明；即使不会执行，也拒绝未受证明的intrinsic。
    if needs_hardware && !facts.is_some_and(|state| state.hardware) {
        errors.push("向量操作缺少硬件支持真边支配".into());
    }
}
