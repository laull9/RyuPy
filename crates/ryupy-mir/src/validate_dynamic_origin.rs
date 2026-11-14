//! Dynamic owner的窄ABI与私有发布门：不引入跨CFG来源推断或静态纯度证明。
use crate::mir::*;
use rustc_hash::{FxHashMap, FxHashSet};

pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    let metadata: FxHashSet<_> = program
        .functions
        .iter()
        .flat_map(|(_, f)| {
            [
                f.function_global,
                f.defaults_global,
                f.signature_global,
                f.specialization_identity_global,
            ]
        })
        .flatten()
        .collect();
    let mut sources = FxHashMap::default();
    for (id, function) in program.functions.iter() {
        let Some(origin) = function.dynamic_origin else {
            continue;
        };
        let local = function
            .body
            .locals
            .iter()
            .find(|(id, _)| *id == origin.owner);
        if function.params.last() != Some(&origin.owner)
            || function
                .params
                .iter()
                .filter(|p| **p == origin.owner)
                .count()
                != 1
            || !local.is_some_and(|(_, local)| local.ty == MirType::Dynamic && !local.is_cell)
            || function.env == Some(origin.owner)
            || function.params.len()
                != function.param_meta.len()
                    + usize::from(function.env.is_some())
                    + usize::from(function.module_state.is_some())
                    + 1
            || function.signature_global.is_none()
        {
            errors.push(format!(
                "函数 `{}` 的Dynamic owner必须是Python参数/env之后的独立Dynamic尾参数",
                function.name
            ));
        }
        if let Some(env) = function.env
            && function.params.get(function.param_meta.len()) != Some(&env)
        {
            errors.push(format!("函数 `{}` 的env必须在owner之前", function.name));
        }
        if let Some(source) = origin.source_global {
            if sources.insert(source, id).is_some()
                || metadata.contains(&source)
                || function.function_global.is_none()
                || function.env.is_some()
                || !program
                    .globals
                    .iter()
                    .any(|(id, g)| id == source && g.ty == MirType::Dynamic)
            {
                errors.push(format!(
                    "函数 `{}` 的私有来源槽必须唯一、独立且属于普通定义",
                    function.name
                ));
            }
        }
        for (_, block) in function.body.blocks.iter() {
            for stmt in &block.stmts {
                if matches!(stmt, Statement::Assign {dest, ..} if *dest == origin.owner) {
                    errors.push(format!("函数 `{}` 不得覆写实际owner参数", function.name));
                }
                if let Statement::Assign { value, .. } | Statement::Eval(value) = stmt {
                    match value {
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::TracePushWithOrigin),
                            args,
                        } if !matches!(args.as_slice(), [_, _, Operand::Local(owner)] if *owner == origin.owner) =>
                        {
                            errors
                                .push(format!("函数 `{}` 压帧必须传实际owner参数", function.name));
                        }
                        Rvalue::MakeGenerator { origin: value, .. } if !matches!(value, Some(Operand::Local(owner)) if *owner == origin.owner) =>
                        {
                            errors.push(format!(
                                "函数 `{}` 的生成器必须持有实际owner参数",
                                function.name
                            ));
                        }
                        _ => {}
                    }
                }
            }
        }
    }
    // 普通def构造/冷metadata安装与发布处于同一块，不允许任意SetGlobal伪造来源。
    // 精确要求对应MakeFunction的未覆写局部，且紧邻同引用的公开发布；不容许decorator替换。
    for (_, function) in program.functions.iter() {
        for (_, block) in function.body.blocks.iter() {
            let mut fresh = FxHashMap::default();
            for (position, stmt) in block.stmts.iter().enumerate() {
                match stmt {
                    Statement::Assign {
                        dest,
                        value: Rvalue::MakeFunction { func, .. },
                    } => {
                        fresh.insert(*dest, *func);
                    }
                    Statement::Assign { dest, .. } => {
                        fresh.remove(dest);
                    }
                    Statement::SetGlobal { dest, value } if sources.contains_key(dest) => {
                        let target = sources[dest];
                        let expected = program.functions[target].function_global;
                        let valid = matches!(value, Operand::Local(local) if fresh.get(local) == Some(&target))
                            && matches!(block.stmts.get(position + 1),
                                Some(Statement::SetGlobal {dest: public, value: published})
                                if Some(*public) == expected && same_local(value, published));
                        if !valid {
                            errors.push(format!(
                                "函数 `{}` 私有来源只能由对应新函数成功发布",
                                function.name
                            ));
                        }
                    }
                    _ => {}
                }
            }
        }
    }
}

fn same_local(left: &Operand, right: &Operand) -> bool {
    matches!((left, right), (Operand::Local(a), Operand::Local(b)) if a == b)
}
