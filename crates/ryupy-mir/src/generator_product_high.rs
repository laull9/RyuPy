//! 最后高层实际定义/调用闭包。HIR事实只给参数上界；所有真实调用、返回、helper与模块getter根重新核对。
//! 只允许无字段普通类的固定property、真实生成器next与固定stdio，不建立通用heap模型。
use crate::generator_product_values::{Bounds, Resolver, Value, fits, primitive};
use crate::trace_lowering::LoweringOrigins;
use crate::*;
use rustc_hash::FxHashMap;

pub(super) fn inspect(
    program: &Program,
    facts: &trace_domain::Facts,
    origins: &LoweringOrigins,
    generators: &[FnId],
    exports: &[ModuleExport],
    initialization: &crate::generator_product_initialization::Initialization,
) -> Result<(), String> {
    if !facts.converged || !facts.refusals.is_empty() {
        return Err(format!(
            "HIR能力未闭合：{}",
            facts
                .refusals
                .iter()
                .cloned()
                .collect::<Vec<_>>()
                .join("；")
        ));
    }
    let bounds = Bounds::from_facts(facts, origins)?;
    if program.functions.len() != origins.functions.len() || generators.is_empty() {
        return Err("实际函数身份集合改变或没有HIR生成器身份".into());
    }
    for (_, class) in program.classes.iter() {
        if class.class_cell.is_some()
            || class.base.is_some()
            || class.builtin_base.is_some()
            || class.is_dataclass
            || class.is_exception
            || class.is_value_type
            || !class.fields.is_empty()
            || class.factory_global.is_some()
        {
            return Err("实际class带未迁入的constructor/field能力".into());
        }
    }
    for (_, global) in program.globals.iter() {
        if global.storage != GlobalStorage::LegacyStatic {
            return Err("产品域不接受Instance全局".into());
        }
    }
    initialization.inspect(program, exports)?;
    let defaults = crate::generator_product_defaults::Defaults::inspect(program, exports)?;
    let mut resolver = Resolver {
        program,
        bounds: &bounds,
        generators,
        defaults: &defaults,
        properties: FxHashMap::default(),
    };
    // 先从真实SetAttr闭合property表。只允许模块一次写入，不能以HIR名称代替实际receiver/getter。
    let module = &program.functions[program.entry];
    for (_, block) in module.body.blocks.iter() {
        for statement in &block.stmts {
            if let Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::SetAttr),
                args,
            }) = statement
            {
                let [receiver, Operand::Const(Constant::Str(name)), value] = args.as_slice() else {
                    return Err("property安装不具有固定实际名称".into());
                };
                let receivers = resolver.operand(program.entry, receiver)?;
                let values = resolver.operand(program.entry, value)?;
                let ([Value::Class(class)], [Value::Property(getter)]) =
                    (receivers.as_slice(), values.as_slice())
                else {
                    return Err("property安装receiver/getter不是唯一实际身份".into());
                };
                if name.starts_with("__")
                    || resolver
                        .properties
                        .insert((*class, name.clone()), *getter)
                        .is_some()
                {
                    return Err("property重绑或特殊方法不在有限域".into());
                }
            }
        }
    }
    for (id, function) in program.functions.iter() {
        if function.env.is_some()
            || function.module_state.is_some()
            || function.module_state_source.is_some()
            || function.specialization_identity_global.is_some()
            || !function.body.source_handler_contracts.is_empty()
            || function.body.locals.iter().any(|(_, local)| {
                local.is_cell
                    || !matches!(
                        local.ty,
                        MirType::Dynamic
                            | MirType::I64
                            | MirType::F64
                            | MirType::Bool
                            | MirType::Str
                    )
            })
        {
            return Err("实际函数包含env/state/cell/专用heap能力".into());
        }
        if function.body.regions.iter().any(|region| matches!(&region.kind, RegionKind::Catch { type_names }
            if type_names.is_empty() || type_names.iter().any(|name| !matches!(name.as_str(), "ValueError" | "TypeError" | "RuntimeError")))) {
            return Err("最终高层typed EH含未迁入异常能力".into());
        }
        let hidden = usize::from(function.dynamic_origin.is_some());
        if function.params.len() != function.param_meta.len() + hidden
            || function.param_meta.iter().any(|meta| {
                meta.has_default
                    || !matches!(
                        meta.kind,
                        ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
                    )
            })
        {
            return Err("实际Python签名或hidden owner数量不符".into());
        }
        for (local, metadata) in function.params.iter().zip(&function.param_meta) {
            if function.body.locals[*local].ty != metadata.ty {
                return Err("真实参数局部类型与thunk签名不一致".into());
            }
        }
        if let Some(origin) = function.dynamic_origin {
            if function.params.last() != Some(&origin.owner)
                || function.body.locals[origin.owner].ty != MirType::Dynamic
            {
                return Err("hidden owner不是最后一个真实Dynamic参数".into());
            }
        }
        let live = super::generator_product_trace::reachable(&function.body);
        for (block_id, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                match statement {
                    Statement::Assign { dest, value } => {
                        resolver.inspect_value(id, Some(*dest), value)?;
                    }
                    Statement::Eval(value) => {
                        resolver.inspect_value(id, None, value)?;
                    }
                    Statement::SetGlobal { value, .. } if id == program.entry => {
                        resolver.operand(id, value)?;
                    }
                    Statement::Line(_) => {}
                    _ => return Err("最终高层含字段写入/非模块全局写入".into()),
                }
            }
            if !live.contains(&block_id) {
                continue;
            }
            match &block.terminator {
                Terminator::Branch { cond, .. } => {
                    if !fits(
                        &resolver.operand(id, cond)?,
                        &vec![Value::Primitive(trace_domain::Primitive::Bool)],
                    ) {
                        return Err("真实控制条件不是封闭布尔值".into());
                    }
                }
                Terminator::Suspend {
                    value,
                    delegated: false,
                    input: None,
                    ..
                } if generators.contains(&id) => {
                    let actual = resolver.operand(id, value)?;
                    if !primitive(&actual) || !fits(&actual, &bounds.yields[&id]) {
                        return Err("yield值外逃或偏离实际函数摘要".into());
                    }
                }
                Terminator::Return(value) if id != program.entry => {
                    let actual = value
                        .as_ref()
                        .map(|value| resolver.operand(id, value))
                        .transpose()?
                        .unwrap_or_else(|| vec![Value::None]);
                    if !fits(&actual, &bounds.returns[&id]) {
                        return Err("真实return超出参数上界推导的摘要".into());
                    }
                    if generators.contains(&id) && !primitive(&actual) {
                        return Err("generator终值可能执行用户转换".into());
                    }
                }
                Terminator::Return(None) if id == program.entry => {}
                Terminator::Raise {
                    value: Some(value),
                    cause: None,
                } => {
                    if resolver
                        .operand(id, value)?
                        .iter()
                        .any(|value| *value != Value::Error)
                    {
                        return Err("真实raise含未建源/未迁入异常（含SystemExit/PEP479）".into());
                    }
                }
                Terminator::Goto(_) | Terminator::Unreachable => {}
                _ => return Err("高层含bare/cause/send/委托或未闭合退出能力".into()),
            }
        }
    }
    Ok(())
}
