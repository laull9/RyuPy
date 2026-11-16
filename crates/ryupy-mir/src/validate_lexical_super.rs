//! 零参super只消费当前真实Env/首位置形参；禁止伪造cell来源、隐藏参数或入口快照。
//! 元数据不能独立授权：隐式类cell必须对上S0捕获计划，own-cell必须实际由原形参初始化。
use crate::validate::{local_decl, operand_type};
use crate::validate_class_cells::assignments;
use crate::*;

pub(crate) fn validate(program: &Program, errors: &mut Vec<String>) {
    for (id, function) in program.functions.iter() {
        let capture = program
            .classes
            .iter()
            .filter_map(|(_, class)| class.class_cell.as_ref())
            .flat_map(|cell| &cell.captures)
            .find(|capture| capture.function == id);
        for (block_id, block) in function.body.blocks.iter() {
            for (statement_index, statement) in block.stmts.iter().enumerate() {
                let (Statement::Assign {
                    value: Rvalue::InstanceZeroCall { callee, frame },
                    ..
                }
                | Statement::Eval(Rvalue::InstanceZeroCall { callee, frame })) = statement
                else {
                    continue;
                };
                if let Statement::Assign { dest, .. } = statement
                    && !local_decl(&function.body.locals, *dest)
                        .is_some_and(|decl| decl.ty == MirType::Dynamic && !decl.is_cell)
                {
                    errors.push("零参frame调用结果必须写入普通dyn槽".into());
                }
                if function.module_state_source != Some(InstanceStateSource::FunctionParameter)
                    || function.definition.is_none()
                    || operand_type(program, function, callee) != Some(MirType::Dynamic)
                    || matches!(callee, Operand::Local(local) if local_decl(&function.body.locals, *local).is_some_and(|decl| decl.is_cell))
                {
                    errors.push("零参frame调用只能属于真实实例普通函数并接收dyn目标".into());
                }
                let expected = function
                    .param_meta
                    .iter()
                    .position(|parameter| {
                        matches!(
                            parameter.kind,
                            ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
                        )
                    })
                    .and_then(|index| function.params.get(index))
                    .copied();
                if frame.first_parameter != expected {
                    errors.push(
                        "零参frame首参必须是当前函数真实首位置形参，不能是快照或隐藏参数".into(),
                    );
                }
                if let Some(parameter) = frame.first_parameter {
                    match local_decl(&function.body.locals, parameter) {
                        Some(local) if local.ty == MirType::Dynamic => {
                            if local.is_cell
                                && !valid_parameter_cell(
                                    function,
                                    parameter,
                                    block_id,
                                    statement_index,
                                )
                            {
                                errors.push(
                                    "零参frame首参cell必须由本次原形参唯一初始化并保留当前绑定"
                                        .into(),
                                );
                            }
                        }
                        _ => errors.push("零参frame首参必须是有效dyn绑定槽".into()),
                    }
                }
                let valid = match (frame.class, capture) {
                    (SuperClassSource::Lexical { environment, index }, Some(capture)) => {
                        matches!(capture.source, ClassCellEnvSource::Parameter)
                            && function.env == Some(environment)
                            && capture.index == index
                            && assignments(function, environment).is_empty()
                    }
                    (SuperClassSource::Missing, None) => true,
                    (SuperClassSource::UnsupportedFree, None) => function.env.is_some(),
                    _ => false,
                };
                if !valid {
                    errors.push(
                        "零参frame类cell必须是当前函数S0登记的真实Env槽，不能补造或换源".into(),
                    );
                }
            }
        }
    }
}

fn valid_parameter_cell(
    function: &Function,
    parameter: LocalId,
    usage_block: BlockId,
    usage_index: usize,
) -> bool {
    let writes = assignments(function, parameter);
    let [
        (
            (block, index),
            Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::CellNew),
                args,
            },
        ),
    ] = writes.as_slice()
    else {
        return false;
    };
    if *block != function.body.entry
        || !args.is_empty()
        || (usage_block == *block && usage_index <= index + 1)
    {
        return false;
    }
    let statements = &function.body.blocks[*block].stmts;
    let Some(Statement::Assign {
        dest: saved,
        value: Rvalue::Use(Operand::Local(source)),
    }) = index.checked_sub(1).and_then(|index| statements.get(index))
    else {
        return false;
    };
    *source == parameter
        && *saved != parameter
        && matches!(statements.get(index + 1), Some(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::CellSet), args
        })) if matches!(args.as_slice(), [Operand::Local(target), Operand::Local(value)] if *target == parameter && *value == *saved))
}
