//! 最后高层实际定义/调用闭包。HIR事实只给参数上界；所有真实调用、返回、helper与模块getter根重新核对。
//! 只允许无字段普通类的固定property、真实生成器next与固定stdio，不建立通用heap模型。
use crate::generator_product_failure::{Failure, Limitations};
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
) -> Result<(), Failure> {
    if !facts.converged || !facts.refusals.is_empty() {
        return Err(Failure::unsupported(format!(
            "HIR能力未闭合：{}",
            facts
                .refusals
                .iter()
                .cloned()
                .collect::<Vec<_>>()
                .join("；")
        )));
    }
    let mut limitations = Limitations::default();
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
            limitations.note("实际class带未迁入的constructor/field能力");
        }
    }
    for (_, global) in program.globals.iter() {
        if !matches!(
            global.storage,
            GlobalStorage::LegacyStatic
                | GlobalStorage::CodeSource {
                    legacy_entry: Some(_),
                    ..
                }
        ) {
            limitations.note("产品域不接受Instance全局");
        }
    }
    // 已要求的初始化/私有槽先独立核验，Bottom或未知值不能遮掉这些合同。
    limitations.check(initialization.inspect(program, exports))?;
    let defaults = crate::generator_product_defaults::Defaults::inspect_partial(
        program,
        exports,
        &mut limitations,
    )?;
    for (id, function) in program.functions.iter() {
        signature(function, generators.contains(&id), &mut limitations)?;
    }
    let bounds = Bounds::inspect(facts, origins, &mut limitations)?;
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
                limitations.check((|| -> Result<(), Failure> {
                    let [receiver, Operand::Const(Constant::Str(name)), value] = args.as_slice()
                    else {
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
                        return Err(Failure::unsupported("property重绑或特殊方法不在有限域"));
                    }
                    Ok(())
                })())?;
            }
        }
    }
    for (id, function) in program.functions.iter() {
        let live = super::generator_product_trace::reachable(&function.body);
        for (block_id, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                limitations.check((|| -> Result<(), Failure> {
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
                        _ => return Err(Failure::unsupported("最终高层含字段写入/非模块全局写入")),
                    }
                    Ok(())
                })())?;
            }
            if !live.contains(&block_id) {
                continue;
            }
            limitations.check((|| -> Result<(), Failure> {
                let mut operand_limits = Limitations::default();
                match &block.terminator {
                    Terminator::Branch { cond, .. } => {
                        if !fits(
                            &resolver.operand(id, cond)?,
                            &vec![Value::Primitive(trace_domain::Primitive::Bool)],
                        ) {
                            return Err(Failure::unsupported("真实控制条件不是封闭布尔值"));
                        }
                    }
                    Terminator::Suspend {
                        value,
                        delegated: false,
                        input: None,
                        ..
                    } if generators.contains(&id) => {
                        let actual = resolver.operand(id, value)?;
                        if let Some(expected) = operand_limits.check(
                            bounds
                                .yields
                                .get(&id)
                                .ok_or("generator缺实际yield摘要")?
                                .value(),
                        )? && !fits(&actual, expected)
                        {
                            return Err("yield值偏离实际函数摘要".into());
                        }
                        if !primitive(&actual) {
                            return Err(Failure::unsupported("yield值含未迁入的外逃能力"));
                        }
                    }
                    Terminator::Return(value) if id != program.entry => {
                        let actual = value
                            .as_ref()
                            .map(|value| resolver.operand(id, value))
                            .transpose()?
                            .unwrap_or_else(|| vec![Value::None]);
                        if let Some(expected) = operand_limits.check(
                            bounds
                                .returns
                                .get(&id)
                                .ok_or("函数缺实际return摘要")?
                                .value(),
                        )? && !fits(&actual, expected)
                        {
                            return Err("真实return超出参数上界推导的摘要".into());
                        }
                        if generators.contains(&id) && !primitive(&actual) {
                            return Err(Failure::unsupported("generator终值可能执行用户转换"));
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
                            return Err(Failure::unsupported(
                                "真实raise含未建源/未迁入异常（含SystemExit/PEP479）",
                            ));
                        }
                    }
                    Terminator::Goto(_) | Terminator::Unreachable => {}
                    _ => {
                        return Err(Failure::unsupported(
                            "高层含bare/cause/send/委托或未闭合退出能力",
                        ));
                    }
                }
                operand_limits.finish()
            })())?;
        }
    }
    limitations.finish()
}

// 普通签名能力与hidden owner硬义务分开；遍历所有函数后才允许能力拒绝。
fn signature(
    function: &Function,
    generator: bool,
    limitations: &mut Limitations,
) -> Result<(), Failure> {
    if function.env.is_some()
        || function.module_state.is_some()
        || function.module_state_source.is_some()
        || function.specialization_identity_global.is_some()
        || !function.body.source_handler_contracts.is_empty()
        || function.body.locals.iter().any(|(_, local)| {
            local.is_cell
                || !matches!(
                    local.ty,
                    MirType::Dynamic | MirType::I64 | MirType::F64 | MirType::Bool | MirType::Str
                )
        })
    {
        limitations.note("实际函数包含env/state/cell/专用heap能力");
    }
    if function.body.regions.iter().any(|region| matches!(&region.kind, RegionKind::Catch { type_names }
            if type_names.is_empty() || type_names.iter().any(|name| !matches!(name.as_str(), "ValueError" | "TypeError" | "RuntimeError")))) {
            limitations.note("最终高层typed EH含未迁入异常能力");
        }
    if !generator && !function.body.regions.is_empty() {
        limitations.note("首产品域的非生成器不得有额外handler");
    }
    let hidden = usize::from(function.dynamic_origin.is_some())
        + usize::from(function.env.is_some())
        + usize::from(function.module_state.is_some());
    if function.params.len() != function.param_meta.len() + hidden {
        return Err("实际Python签名或hidden owner数量不符".into());
    }
    if function.param_meta.iter().any(|meta| {
        meta.has_default
            || !matches!(
                meta.kind,
                ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
            )
    }) {
        limitations.note("实际Python default/keyword/variadic能力未迁入");
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
    Ok(())
}
