//! 类定义 cell 的来源校验：区分真实 cell、原类、模块环境与函数 owner。
//! 空 cell 在类体里被调用是合法 NameError；这里验证来源与填充相位，不要求捕获时已有值。
use crate::validate::local_decl;
use crate::validate_class_cell_uses::{
    validate_cell_access, validate_forwarded_exits, validate_forwarded_uses, validate_owner_uses,
};
use crate::validate_instance_module::context_dominates;
use crate::*;
use rustc_hash::{FxHashMap, FxHashSet};
pub(crate) type Position = (BlockId, usize);

pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    let mut captures = FxHashMap::default();
    let mut locals = FxHashSet::default();
    let mut originals = FxHashSet::default();
    for (class_id, class) in program.classes.iter() {
        let Some(cell) = &class.class_cell else {
            continue;
        };
        let Some(owner) = function(program, cell.owner) else {
            errors.push("类 cell 的定义 owner 不存在".into());
            continue;
        };
        if cell.owner != program.entry
            || owner.module_state_source != Some(InstanceStateSource::ModuleEntryParameter)
            || class.is_exception
            || class.is_dataclass
            || !locals.insert(cell.cell)
            || !originals.insert(cell.original_class)
            || cell.captures.is_empty()
            || cell.cell == cell.original_class
            || !local_decl(&owner.body.locals, cell.cell)
                .is_some_and(|local| local.ty == MirType::Dynamic && local.is_cell)
            || !local_decl(&owner.body.locals, cell.original_class)
                .is_some_and(|local| local.ty == MirType::Dynamic && !local.is_cell)
        {
            errors.push(format!(
                "类 `{}` 的定义 cell/原类/实例入口来源非法",
                class.name
            ));
        }
        validate_definition(owner, cell, &class.name, errors);
        validate_definition_flow(owner, cell, &class.name, errors);
        for capture in &cell.captures {
            if captures
                .insert(capture.function, (class_id, cell, capture))
                .is_some()
            {
                errors.push("同一函数不能捕获两个词法类 cell".into());
            }
            validate_environment(program, cell, capture, errors);
            if let Some(target) = function(program, capture.function) {
                validate_forwarded_exits(target, capture, errors);
            }
        }
    }
    // 模块入口不会为普通模块名字创建局部 cell；新隐式 cell 必须有反向来源计划。
    if let Some(entry) = function(program, program.entry)
        && entry.module_state_source == Some(InstanceStateSource::ModuleEntryParameter)
        && entry
            .body
            .locals
            .iter()
            .any(|(id, local)| local.is_cell && !locals.contains(&id))
    {
        errors.push("实例入口存在未登记来源的类定义 cell".into());
    }
    for (caller_id, caller) in program.functions.iter() {
        for (block, body) in caller.body.blocks.iter() {
            for (index, statement) in body.stmts.iter().enumerate() {
                let (Statement::Assign { value, .. } | Statement::Eval(value)) = statement else {
                    continue;
                };
                let position = (block, index);
                if let Rvalue::MakeFunction {
                    func: target, env, ..
                } = value
                    && let Some((class, cell, capture)) = captures.get(target)
                {
                    let valid = matches!(capture.source, ClassCellEnvSource::Parameter)
                        && env.get(capture.index).is_some_and(|operand| {
                            if caller_id == cell.owner {
                                matches!(operand, Operand::Local(local) if *local == cell.cell)
                                    && context_dominates(caller, cell.cell, block, index)
                            } else {
                                captures
                                    .get(&caller_id)
                                    .is_some_and(|(parent_class, _, parent)| {
                                        parent_class == class
                                            && extracted_cell(caller, parent, operand, position)
                                    })
                            }
                        });
                    if !valid {
                        errors.push(format!(
                            "函数 `{}` 捕获的类 cell 不是当前定义/同源 Env 的真实 cell",
                            caller.name
                        ));
                    }
                }
                if let Rvalue::MakeGenerator {
                    resume,
                    frame: values,
                    ..
                } = value
                    && let Some((_, _, resumed)) = captures.get(resume)
                {
                    let valid = matches!(resumed.source, ClassCellEnvSource::GeneratorFrame { factory, environment, slot, .. }
                        if caller_id == factory && caller.env == Some(environment)
                            && matches!(values.get(slot), Some(Operand::Local(env)) if *env == environment));
                    if !valid {
                        errors.push("生成器类 cell 帧必须由登记的 factory 保存实际 Env".into());
                    }
                }
                if let Some((class, _, capture)) = captures.get(&caller_id) {
                    validate_cell_access(caller, capture, value, position, errors);
                    validate_forwarded_uses(caller, capture, value, &captures, *class, errors);
                    if let Rvalue::MakeGenerator { resume, .. } = value
                        && !captures.get(resume).is_some_and(|(resume_class, _, resumed)| {
                            resume_class == class && resumed.index == capture.index
                                && matches!(resumed.source, ClassCellEnvSource::GeneratorFrame { factory, .. } if factory == caller_id)
                        })
                    {
                        errors.push("捕获类 cell 的生成器必须登记同源恢复环境".into());
                    }
                }
            }
        }
    }
}

fn function(program: &Program, id: FnId) -> Option<&Function> {
    program
        .functions
        .iter()
        .find_map(|(found, function)| (found == id).then_some(function))
}
pub(crate) fn assignments(function: &Function, local: LocalId) -> Vec<(Position, &Rvalue)> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(block, body)| {
            body.stmts
                .iter()
                .enumerate()
                .filter_map(move |(index, statement)| {
                    if let Statement::Assign { dest, value } = statement
                        && *dest == local
                    {
                        Some(((block, index), value))
                    } else {
                        None
                    }
                })
        })
        .collect()
}
pub(crate) fn environment(function: &Function, capture: &ClassCellCapture) -> Option<LocalId> {
    match capture.source {
        ClassCellEnvSource::Parameter => function.env,
        ClassCellEnvSource::GeneratorFrame { environment, .. } => Some(environment),
    }
}

fn validate_definition(
    function: &Function,
    cell: &ClassCellDefinition,
    name: &str,
    errors: &mut Vec<String>,
) {
    let created = assignments(function, cell.cell);
    let original = assignments(function, cell.original_class);
    // SimplifyCfg 可以整体清空不可达的 class 定义，不能因此要求死定义仍有运行操作。
    let removed = created.is_empty()
        && original.is_empty()
        && function.body.blocks.iter().any(|(id, block)| {
            id == cell.block
                && block.stmts.is_empty()
                && matches!(block.terminator, Terminator::Unreachable)
        });
    if removed {
        return;
    }
    let valid_new = matches!(created.as_slice(), [((block, _), Rvalue::Call { callee: Callee::Runtime(RuntimeFn::CellNew), args })] if *block == cell.block && args.is_empty());
    let valid_class = matches!((original.as_slice(), created.as_slice()),
        ([((class_block, class_index), Rvalue::Call { callee: Callee::Runtime(RuntimeFn::InstanceMakeClass), .. })], [((cell_block, cell_index), _)])
        if class_block == cell_block && class_index + 1 == *cell_index);
    validate_owner_uses(function, cell, errors);
    let mut fills = Vec::new();
    let mut publications = Vec::new();
    for (block, body) in function.body.blocks.iter() {
        for (index, statement) in body.stmts.iter().enumerate() {
            if is_publication(statement, cell, name) {
                publications.push(
                    block == cell.completion
                        && context_dominates(function, cell.published_class, block, index),
                );
            }
            if let Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CellSet),
                args,
            }) = statement
                && matches!(args.first(), Some(Operand::Local(local)) if *local == cell.cell)
            {
                let previous = index.checked_sub(1).and_then(|i| body.stmts.get(i));
                let finalized = index.checked_sub(2).and_then(|i| body.stmts.get(i));
                let valid = block == cell.completion
                    && matches!(args.as_slice(), [_, Operand::Local(value)] if *value == cell.original_class)
                    && matches!(previous, Some(Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::InstanceClassCellGuard), args })) if matches!(args.as_slice(), [Operand::Local(value)] if *value == cell.original_class))
                    && matches!(finalized, Some(Statement::Eval(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::FinalizeOrdinaryClass), args })) if matches!(args.as_slice(), [Operand::Local(value)] if *value == cell.original_class))
                    && context_dominates(function, cell.cell, block, index)
                    && context_dominates(function, cell.original_class, block, index);
                fills.push(valid);
            }
        }
    }
    if publications.as_slice() != [true] {
        errors.push("类 cell 缺少唯一实际类绑定发布接点".into());
    }
    if !valid_new || !valid_class || fills.as_slice() != [true] {
        errors.push(format!(
            "类 `{name}` 必须由唯一 CellNew 捕获，并在原类成功终结后唯一填充"
        ));
    }
}

/// 追踪本次定义的空/已填状态；循环重新执行 CellNew 会开始新一代，不能沿回边混用。
/// 异常边保留每个可能的旧状态，因此早填也不能借普通 CFG 分块躲过相位验证。
fn validate_definition_flow(
    function: &Function,
    cell: &ClassCellDefinition,
    name: &str,
    errors: &mut Vec<String>,
) {
    let mut visited = FxHashSet::default();
    let mut pending = vec![(function.body.entry, 0_u8)];
    while let Some((block_id, mut state)) = pending.pop() {
        if !visited.insert((block_id, state)) {
            continue;
        }
        let Some(block) = function
            .body
            .blocks
            .iter()
            .find_map(|(id, block)| (id == block_id).then_some(block))
        else {
            continue;
        };
        let handlers: Vec<_> = function
            .body
            .regions
            .iter()
            .filter(|region| {
                let index = block_id.into_raw().into_u32() as usize;
                let start = region.try_start.into_raw().into_u32() as usize;
                index >= start && index < start + region.try_len
            })
            .map(|region| region.handler)
            .collect();
        for statement in &block.stmts {
            if (is_publication(statement, cell, name) || is_class_decorator(statement, cell))
                && state != 2
            {
                errors.push("类 cell 在class decorator或正常发布之前必须完成填充".into());
            }
            pending.extend(handlers.iter().map(|handler| (*handler, state)));
            match statement {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::CellNew),
                            ..
                        },
                } if *dest == cell.cell => state = 1,
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::CellSet),
                    args,
                }) if matches!(args.first(), Some(Operand::Local(local)) if *local == cell.cell) => {
                    if state != 1 {
                        errors.push("类 cell 只能在本次空 cell 上填充一次".into());
                    }
                    state = 2;
                }
                Statement::Assign {
                    value: Rvalue::MakeFunction { env, .. },
                    ..
                } if env
                    .iter()
                    .any(|value| matches!(value, Operand::Local(local) if *local == cell.cell))
                    && state != 1 =>
                {
                    errors.push("类体函数必须在本次 CellNew 之后、原类填充之前捕获 cell".into());
                }
                _ => {}
            }
        }
        pending.extend(handlers.iter().map(|handler| (*handler, state)));
        match block.terminator {
            Terminator::Goto(next) => pending.push((next, state)),
            Terminator::Branch { then_, else_, .. } => {
                pending.extend([(then_, state), (else_, state)])
            }
            _ => {}
        }
    }
}

pub(crate) fn extracted_cell(
    function: &Function,
    capture: &ClassCellCapture,
    operand: &Operand,
    usage: Position,
) -> bool {
    let (Some(env), Operand::Local(local)) = (environment(function, capture), operand) else {
        return false;
    };
    // 实际环境抽取和 MakeFunction 位于同一块；生成器入口还会恢复全部旧临时，
    // 不能把那个无关恢复当成唯一赋值。只消费本调用点之前最后一次真实 Env 抽取。
    let definition = function
        .body
        .blocks
        .iter()
        .find_map(|(id, block)| (id == usage.0).then_some(block))
        .and_then(|block| {
            block.stmts[..usage.1].iter().rev().find_map(|statement| {
                if let Statement::Assign { dest, value } = statement
                    && *dest == *local
                {
                    Some(value)
                } else {
                    None
                }
            })
        });
    matches!(definition, Some(Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArrayGet), args })
        if matches!(args.as_slice(), [Operand::Local(source), Operand::Const(Constant::I64(index))] if *source == env && *index == capture.index as i64))
        && environment_available(function, capture, usage)
}
pub(crate) fn environment_available(
    function: &Function,
    capture: &ClassCellCapture,
    usage: Position,
) -> bool {
    match capture.source {
        ClassCellEnvSource::Parameter => true,
        ClassCellEnvSource::GeneratorFrame { environment, .. } => {
            context_dominates(function, environment, usage.0, usage.1)
        }
    }
}

fn validate_environment(
    program: &Program,
    cell: &ClassCellDefinition,
    capture: &ClassCellCapture,
    errors: &mut Vec<String>,
) {
    let Some(target) = function(program, capture.function) else {
        errors.push("类 cell 捕获函数不存在".into());
        return;
    };
    let valid = match capture.source {
        ClassCellEnvSource::Parameter => target.env.is_some_and(|env| {
            target.module_state_source == Some(InstanceStateSource::FunctionParameter)
                && Some(env) != target.module_state
                && target.params.iter().filter(|param| **param == env).count() == 1
                && assignments(target, env).is_empty()
        }),
        ClassCellEnvSource::GeneratorFrame {
            factory,
            environment: env,
            frame,
            slot,
        } => {
            let restored = assignments(target, env);
            let restore_valid = matches!(restored.as_slice(), [((block, _), Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ArrayGet), args })]
                if *block == target.body.entry && matches!(args.as_slice(), [Operand::Local(source), Operand::Const(Constant::I64(index))] if *source == frame && *index == slot as i64));
            let factory_valid = function(program, factory).is_some_and(|factory_function| {
                factory_function.env == Some(env)
                    && cell.captures.iter().any(|parent| {
                        parent.function == factory
                            && parent.index == capture.index
                            && matches!(parent.source, ClassCellEnvSource::Parameter)
                    })
                    && factory_function
                        .body
                        .blocks
                        .iter()
                        .flat_map(|(_, block)| &block.stmts)
                        .filter(|statement| {
                            matches!(statement,
                        Statement::Assign { value: Rvalue::MakeGenerator { resume, .. }, .. }
                        if *resume == capture.function)
                        })
                        .count()
                        == 1
            });
            target.params == [frame]
                && assignments(target, frame).is_empty()
                && target.env.is_none()
                && Some(env) != target.module_state
                && env != frame
                && slot == env.into_raw().into_u32() as usize + 4
                && restore_valid
                && factory_valid
        }
    };
    if !valid {
        errors.push(format!(
            "函数 `{}` 的类 cell Env 参数/生成器帧来源非法",
            target.name
        ));
    }
}

/// 发布必须是实际class绑定，不以返回或普通属性写入代替成功创建接点。
fn is_publication(statement: &Statement, cell: &ClassCellDefinition, name: &str) -> bool {
    matches!(statement, Statement::Eval(Rvalue::Call {
        callee: Callee::Runtime(RuntimeFn::InstanceStoreName), args
    }) if matches!(args.as_slice(), [_, Operand::Const(Constant::Str(binding)), Operand::Local(value)]
        if binding == name && *value == cell.published_class))
}

/// 首个class decorator直接接收原类；类体namespace操作不会合法地把原类传给用户调用。
fn is_class_decorator(statement: &Statement, cell: &ClassCellDefinition) -> bool {
    let (Statement::Assign { value, .. } | Statement::Eval(value)) = statement else {
        return false;
    };
    if !matches!(
        value,
        Rvalue::Call {
            callee: Callee::Runtime(
                RuntimeFn::CallValue | RuntimeFn::ApplyDataclass | RuntimeFn::Decorate
            ),
            ..
        }
    ) {
        return false;
    }
    let mut reads = FxHashSet::default();
    crate::passes::dead_values::read_value(value, &mut reads);
    reads.contains(&cell.original_class)
}
