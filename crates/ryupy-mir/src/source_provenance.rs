//! 代码来源的单一所属表与窄生产者校验；不按名称/文件名配对，不新增CFG来源固定点。
//! 私有角色的ID随真实link映射搬运；完整分区仍须独立link/PE测试保护整组互换。
use crate::*;
use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};

#[path = "source_provenance_entry.rs"]
mod entry;
use entry::{entry_name, name_export};

pub(crate) fn allocate(
    hir: &ryupy_hir::Module,
    functions: &FxHashMap<ryupy_hir::FuncId, FnId>,
    variables: &FxHashMap<ryupy_hir::ModuleVarId, GlobalId>,
    definitions: &Arena<Function>,
    globals: &mut Arena<GlobalDecl>,
    errors: &mut Vec<ryupy_base::Diagnostic>,
) -> GlobalId {
    let body = &hir.functions[hir.main].body;
    let legacy_entry = if hir.info.instance_globals {
        None
    } else {
        // 精确取HIR首条合成name赋值与真实ModuleVar映射，不能只按名字找到一个槽就认可。
        let value = body
            .root
            .first()
            .and_then(|id| match &body.stmts[*id].kind {
                ryupy_hir::StmtKind::Assign {
                    target: ryupy_hir::AssignTarget::ModuleVar(var),
                    value,
                } if hir.vars[*var].name.as_str() == "__name__" => match &body.exprs[*value].kind {
                    ryupy_hir::ExprKind::Literal(ryupy_hir::Literal::Str(name))
                        if name.as_ref() == hir.info.name.as_str() =>
                    {
                        Some(LegacySourceEntry {
                            initial_name: name.to_string(),
                            name_global: variables[var],
                        })
                    }
                    _ => None,
                },
                _ => None,
            });
        if value.is_none() {
            errors.push(
                ryupy_base::Diagnostic::error(
                    ryupy_base::Span::new(hir.file, hir.functions[hir.main].range),
                    "代码来源缺少真实Legacy入口name前言",
                )
                .with_code("E0305"),
            );
        }
        value
    };
    globals.alloc(GlobalDecl {
        name: "__code_source".into(),
        ty: MirType::Dynamic,
        storage: GlobalStorage::CodeSource {
            entry: functions[&hir.main],
            functions: definitions
                .iter()
                .filter_map(|(id, f)| f.signature_global.map(|_| id))
                .collect(),
            legacy_entry,
        },
    })
}

/// 已验证所属表的机械索引；不是按函数名字/诊断文件查找。
pub fn for_function(globals: &Arena<GlobalDecl>, function: FnId) -> Option<GlobalId> {
    globals
        .iter()
        .find_map(|(id, global)| match &global.storage {
            GlobalStorage::CodeSource {
                entry, functions, ..
            } if *entry == function || functions.contains(&function) => Some(id),
            _ => None,
        })
}

/// 真实源码driver的外部必需合同；整表/新压帧一起被擦掉也不能降为旧手工MIR。
pub fn require_program(program: &Program) -> Result<(), Vec<String>> {
    let mut errors = crate::validate::validate(program).err().unwrap_or_default();
    if !has_entry(&program.globals, program.entry) {
        errors.push("源码unit缺少实际ModuleMain的必需来源".into());
    }
    if errors.is_empty() {
        Ok(())
    } else {
        Err(errors)
    }
}

/// 普通源码link/发射边界无例外：每个真实注册模块都必须拥有完整来源。
pub fn require_linked(program: &LinkedProgram) -> Result<(), Vec<String>> {
    require_modules(
        program,
        &program.modules.iter().map(|(id, _)| id).collect::<Vec<_>>(),
    )
}

/// 仅供已验证的固定绑定出口指定原源码集合；调用方须另证其余模块恰为合法新增绑定。
pub fn require_modules(program: &LinkedProgram, modules: &[ModuleId]) -> Result<(), Vec<String>> {
    let mut errors = crate::validate::validate_linked_exports(program)
        .err()
        .unwrap_or_default();
    for id in modules {
        if !program
            .modules
            .iter()
            .any(|(actual, module)| actual == *id && has_entry(&program.globals, module.init))
        {
            errors.push("源码模块缺少实际入口的必需来源".into());
        }
    }
    if errors.is_empty() {
        Ok(())
    } else {
        Err(errors)
    }
}

fn has_entry(globals: &Arena<GlobalDecl>, init: FnId) -> bool {
    globals.iter().filter(|(_, g)| matches!(&g.storage, GlobalStorage::CodeSource { entry, .. } if *entry == init)).count() == 1
}

pub(crate) fn validate_program(program: &Program, errors: &mut Vec<String>) {
    validate(&program.functions, &program.globals, errors);
    let sources: Vec<_> = program
        .globals
        .iter()
        .filter_map(|(_, g)| match &g.storage {
            GlobalStorage::CodeSource { entry, .. } => Some(*entry),
            _ => None,
        })
        .collect();
    if !sources.is_empty() && sources != [program.entry] {
        errors.push("单unit必须仅有一个属于实际ModuleMain的代码来源".into());
    }
}

pub(crate) fn validate_linked(program: &LinkedProgram, errors: &mut Vec<String>) {
    validate(&program.functions, &program.globals, errors);
    for (_, global) in program.globals.iter() {
        let GlobalStorage::CodeSource {
            entry,
            legacy_entry,
            ..
        } = &global.storage
        else {
            continue;
        };
        let modules: Vec<_> = program
            .modules
            .iter()
            .filter(|(_, m)| m.init == *entry)
            .collect();
        let [(_, module)] = modules.as_slice() else {
            errors.push("代码来源没有唯一真实模块入口".into());
            continue;
        };
        if (module.execution == ModuleExecutionKind::LegacySingleton) != legacy_entry.is_some() {
            errors.push("代码来源的环境种类与模块注册不一致".into());
        }
        if let Some(initial) = legacy_entry
            && (initial.initial_name != module.name
                || !module.file_backed_by_global
                || !name_export(program, module, initial))
        {
            errors.push("Legacy入口初值/真实name导出与来源不一致".into());
        }
    }
}

fn validate(functions: &Arena<Function>, globals: &Arena<GlobalDecl>, errors: &mut Vec<String>) {
    let mut owners = FxHashMap::default();
    let mut private = FxHashSet::default();
    for (global_id, global) in globals.iter() {
        let GlobalStorage::CodeSource {
            entry,
            functions: members,
            legacy_entry,
        } = &global.storage
        else {
            continue;
        };
        private.insert(global_id);
        if global.ty != MirType::Dynamic {
            errors.push("代码来源槽必须Dynamic".into());
        }
        for id in std::iter::once(entry).chain(members) {
            let Some((_, function)) = functions.iter().find(|(actual, _)| actual == id) else {
                errors.push("代码来源引用了不存在的函数".into());
                continue;
            };
            if owners.insert(*id, global_id).is_some()
                || (*id == *entry) != function.signature_global.is_none()
            {
                errors.push("代码来源的入口/signature分区不完整或重复".into());
            }
        }
        if let Some((_, module)) = functions.iter().find(|(id, _)| id == entry) {
            if module.module_state.is_none() != legacy_entry.is_some() {
                errors.push("代码来源环境种类与真实入口参数不一致".into());
            }
            if let Some(initial) = legacy_entry {
                if !globals.iter().any(|(id, g)| {
                    id == initial.name_global && g.storage == GlobalStorage::LegacyStatic
                }) || entry_name(module, initial).is_none()
                {
                    errors.push("代码来源缺少真实Legacy入口初值发布".into());
                }
            }
        }
    }
    let has_sources = !owners.is_empty();
    for (id, function) in functions.iter() {
        if has_sources && function.signature_global.is_some() && !owners.contains_key(&id) {
            errors.push(format!("函数 `{}` 缺少代码来源所属关系", function.name));
        }
        for global in [
            function.signature_global,
            function.function_global,
            function.defaults_global,
        ]
        .into_iter()
        .flatten()
        {
            if private.contains(&global) {
                errors.push("代码来源槽与函数元数据别名".into());
            }
        }
        check_private_reads(function, globals, &private, &owners, id, errors);
        if let Some(source) = owners.get(&id) {
            check_frame(functions, globals, id, *source, errors);
        }
        for (_, block) in function.body.blocks.iter() {
            for statement in &block.stmts {
                if let Statement::SetGlobal { dest, .. } = statement
                    && (private.contains(dest)
                        || functions
                            .iter()
                            .any(|(_, f)| f.signature_global == Some(*dest)))
                {
                    errors.push("普通MIR不得覆盖代码来源/signature冷槽".into());
                }
                if let Statement::Assign {
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode),
                            ..
                        },
                    ..
                } = statement
                    && !owners.contains_key(&id)
                {
                    errors.push("新版模块来源helper缺少不可变所属表".into());
                }
            }
        }
    }
}

// 私有来源值只进入指定helper；不允许返回、存入普通槽或透过数组外逃。
fn check_private_reads(
    function: &Function,
    globals: &Arena<GlobalDecl>,
    private: &FxHashSet<GlobalId>,
    owners: &FxHashMap<FnId, GlobalId>,
    id: FnId,
    errors: &mut Vec<String>,
) {
    for (_, block) in function.body.blocks.iter() {
        for statement in &block.stmts {
            let (values, allowed) = match statement {
                Statement::Assign { value, .. } | Statement::Eval(value) => {
                    let allowed = match value {
                        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode), args }
                            if args.len() == 2 => owners.get(&id).copied().filter(|source|
                                matches!(&globals[*source].storage, GlobalStorage::CodeSource { entry, .. } if *entry == id)
                                    && args[0] == Operand::Global(*source)
                                    && args[1] == function.module_state.map(Operand::Local).unwrap_or(Operand::Const(Constant::Null))),
                        _ => None,
                    };
                    (operands(value), allowed)
                }
                Statement::SetGlobal { value, .. } => (vec![value], None),
                Statement::SetField { obj, value, .. } => (vec![obj, value], None),
                Statement::Line(_) => continue,
            };
            private_values(values, private, allowed, errors);
        }
        let values = match &block.terminator {
            Terminator::Return(value) => value.iter().collect(),
            Terminator::Raise { value, cause } => value.iter().chain(cause).collect(),
            Terminator::Branch { cond, .. } => vec![cond],
            Terminator::Suspend { value, .. } => vec![value],
            _ => vec![],
        };
        private_values(values, private, None, errors);
    }
}
fn private_values(
    mut values: Vec<&Operand>,
    private: &FxHashSet<GlobalId>,
    allowed: Option<GlobalId>,
    errors: &mut Vec<String>,
) {
    while let Some(value) = values.pop() {
        match value {
            Operand::Array(items) => values.extend(items),
            Operand::Global(global) if private.contains(global) && Some(*global) != allowed => {
                errors.push("私有代码来源只能用于所属模块的执行marker".into())
            }
            _ => {}
        }
    }
}

fn check_frame(
    functions: &Arena<Function>,
    globals: &Arena<GlobalDecl>,
    id: FnId,
    source: GlobalId,
    errors: &mut Vec<String>,
) {
    let function = &functions[id];
    let GlobalStorage::CodeSource { entry, .. } = &globals[source].storage else {
        return;
    };
    let mut pushes = 0;
    let mut factories = 0;
    let mut markers = 0;
    for (block_id, block) in function.body.blocks.iter() {
        for (at, statement) in block.stmts.iter().enumerate() {
            match statement {
                Statement::Assign {
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode),
                            ..
                        },
                    ..
                } => {
                    markers += 1;
                }
                Statement::Assign {
                    value: Rvalue::MakeGenerator { origin, .. },
                    ..
                } => {
                    if function
                        .dynamic_origin
                        .is_some_and(|o| origin == &Some(Operand::Local(o.owner)))
                    {
                        factories += 1;
                    } else {
                        errors.push("生成器来源必须是实际Dynamic owner".into());
                    }
                }
                Statement::Assign {
                    value:
                        Rvalue::Call {
                            callee:
                                Callee::Runtime(
                                    runtime @ (RuntimeFn::TracePush
                                    | RuntimeFn::TracePushWithOrigin),
                                ),
                            args,
                        },
                    ..
                } => {
                    pushes += 1;
                    let carrier = if id == *entry {
                        matches!(args.get(2), Some(Operand::Local(local)) if block.stmts[..at].iter().rev().find(|s| matches!(s, Statement::Assign { dest, .. } if dest == local)).is_some_and(|s|
                            matches!(s, Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ModuleExecutionSourceWithCode), args: actual } }
                                if dest == local && actual == &vec![Operand::Global(source), function.module_state.map(Operand::Local).unwrap_or(Operand::Const(Constant::Null))])))
                    } else if let Some(origin) = function.dynamic_origin {
                        args.get(2) == Some(&Operand::Local(origin.owner))
                    } else {
                        function.signature_global.is_some_and(|signature| {
                            args.get(2) == Some(&Operand::Global(signature))
                        })
                    };
                    if *runtime != RuntimeFn::TracePushWithOrigin
                        || block_id != function.body.entry
                        || !carrier
                        || args.len() != 3
                        || args.first()
                            != Some(&Operand::Const(Constant::Str(function.source_file.clone())))
                        || args.get(1)
                            != Some(&Operand::Const(Constant::Str(if id == *entry {
                                "<module>".into()
                            } else {
                                function.name.clone()
                            })))
                    {
                        errors.push(format!("函数 `{}` 的新版压帧载体错误", function.name));
                    }
                }
                _ => {}
            }
        }
    }
    // 生成器factory由既有降级移除压帧，运行时恢复仍消费同一actual function；其它入口必须正向存在。
    if markers != usize::from(id == *entry)
        || !((pushes == 1 && factories == 0) || (pushes == 0 && factories == 1))
    {
        errors.push(format!(
            "函数 `{}` 缺少唯一新版帧/生成器来源",
            function.name
        ));
    }
}

// 穷尽读取实际操作数；合法但未迁入的Rvalue由high能力检查拒绝，不能遮后续发布前读取。
pub(crate) fn operands(value: &Rvalue) -> Vec<&Operand> {
    match value {
        Rvalue::Use(value)
        | Rvalue::Box { value, .. }
        | Rvalue::Unbox { value, .. }
        | Rvalue::GeneratorDelegateStep { delegate: value }
        | Rvalue::GetField { obj: value, .. }
        | Rvalue::VectorLaneF64 { value, .. }
        | Rvalue::IsBoxedF64 { value }
        | Rvalue::UnboxExactF64 { value }
        | Rvalue::IsBoxedI64 { value }
        | Rvalue::UnboxExactI64 { value }
        | Rvalue::DynTruthy { value }
        | Rvalue::Unary { operand: value, .. }
        | Rvalue::Cast { operand: value, .. }
        | Rvalue::DynUnary { operand: value, .. } => vec![value],
        Rvalue::Binary { lhs, rhs, .. }
        | Rvalue::VectorBinaryF64 { lhs, rhs, .. }
        | Rvalue::DynBinary { lhs, rhs, .. }
        | Rvalue::DynCompare { lhs, rhs, .. } => vec![lhs, rhs],
        Rvalue::VectorPackF64 { low, high } => vec![low, high],
        Rvalue::Call { args, .. } | Rvalue::NewObject { args, .. } => args.iter().collect(),
        Rvalue::CallWithArgs { prefix, args, .. } => prefix.iter().chain(args).collect(),
        Rvalue::DynCallValue { callee, args } => std::iter::once(callee).chain(args).collect(),
        Rvalue::DynCallMethod {
            captured,
            obj,
            args,
            ..
        } => captured
            .iter()
            .chain(std::iter::once(obj))
            .chain(args)
            .collect(),
        Rvalue::MakeFunction {
            env,
            defaults,
            instance_context,
            ..
        } => env.iter().chain(defaults).chain(instance_context).collect(),
        Rvalue::MakeGenerator { frame, origin, .. } => frame.iter().chain(origin).collect(),
        Rvalue::InstanceZeroCall { callee, .. } => vec![callee],
        Rvalue::CatchException | Rvalue::IsVector128Supported => vec![],
    }
}
