//! 单Legacy模块前言的有限事务见证：实际reset与发布分开，所有读取点须在发布之后。
//! 只认无回入直链和最多7个身份/初值角色；不改变普通Global union或metadata未绑定语义。
use crate::generator_product_failure::{Failure, Limitations};
use crate::trace_lowering::{LinkRemap, LoweringOrigins};
use crate::*;
use rustc_hash::{FxHashMap, FxHashSet};

#[path = "generator_product_initialization_values.rs"]
mod values;
use values::Context;

type Point = (BlockId, usize);
fn error(reason: &str) -> String {
    format!("模块初始化合同：{reason}")
}
#[derive(Clone, Debug, PartialEq)]
enum Initial {
    Text(String),
    File,
    Metadata(String),
}
#[derive(Clone, Debug)]
struct Role {
    export: ModuleExport,
    declaration: GlobalDecl,
    initial: Initial,
}
#[derive(Clone, Debug)]
pub(super) struct Initialization {
    entry: FnId,
    file: String,
    roles: Vec<Role>,
    resets: FxHashMap<GlobalId, GlobalDecl>,
}

fn function(program: &Program, id: FnId) -> Result<&Function, String> {
    if (id.into_raw().into_u32() as usize) < program.functions.len() {
        Ok(&program.functions[id])
    } else {
        Err(error("实际FnId不存在"))
    }
}
fn global(program: &Program, id: GlobalId) -> Result<&GlobalDecl, String> {
    if (id.into_raw().into_u32() as usize) < program.globals.len() {
        Ok(&program.globals[id])
    } else {
        Err(error("实际GlobalId不存在"))
    }
}

impl Initialization {
    /// 输入旁证只记录必须重核的角色；不把HIR名字、export标签或槽前缀直接当证明。
    pub fn expected(
        hir: &ryupy_hir::Module,
        origins: &LoweringOrigins,
        program: &Program,
        exports: &[ModuleExport],
    ) -> Result<Self, Failure> {
        if hir.info.name != "__main__" || !hir.info.package.is_empty() || hir.info.instance_globals
        {
            return Err(Failure::unsupported(error("输入不是单Legacy主模块")));
        }
        let mut result = Self {
            entry: program.entry,
            file: hir.info.file.clone(),
            roles: vec![],
            resets: FxHashMap::default(),
        };
        // 普通前言reset的资格也来自实际ModuleVar映射/导出flag，不凭任意槽类型放行。
        let mut module_slots = FxHashSet::default();
        for (variable, _) in hir.vars.iter() {
            let slot = *origins
                .globals
                .get(&variable)
                .ok_or_else(|| error("ModuleVar缺实际槽"))?;
            module_slots.insert(slot);
            let declaration = global(program, slot)?;
            if declaration.ty == MirType::Dynamic {
                result.resets.insert(slot, declaration.clone());
            }
        }
        for export in exports.iter().filter(|e| module_slots.contains(&e.value)) {
            if let ExportGuard::BoundFlag(flag) = export.guard {
                let declaration = global(program, flag)?;
                if declaration.ty != MirType::Bool {
                    return Err(error("普通变量reset flag类型错误").into());
                }
                result.resets.insert(flag, declaration.clone());
            }
        }
        for key in [
            "__name__",
            "__package__",
            "__file__",
            "__spec__",
            "__loader__",
            "__cached__",
            "__path__",
        ] {
            let metadata = matches!(key, "__spec__" | "__loader__" | "__cached__" | "__path__");
            if metadata && !hir.info.resolve_file_at_runtime {
                if exports.iter().any(|e| e.name == key) {
                    return Err(error("输入未要求的metadata导出").into());
                }
                continue;
            }
            let variable = hir.vars.iter().find(|(_, v)| v.name.as_str() == key);
            let Some((id, _)) = variable else {
                if key == "__file__" && !exports.iter().any(|e| e.name == key) {
                    continue;
                }
                return Err(error("输入要求的前言角色缺失").into());
            };
            let slot = *origins
                .globals
                .get(&id)
                .ok_or_else(|| error("角色没有真实GlobalId来源"))?;
            let matches: Vec<_> = exports.iter().filter(|e| e.name == key).collect();
            let [export] = matches.as_slice() else {
                return Err(error("角色没有唯一实际导出").into());
            };
            if export.value != slot || export.guard != ExportGuard::ValueSentinel {
                return Err(error("角色导出槽或ValueSentinel协议错误").into());
            }
            let declaration = global(program, slot)?.clone();
            if declaration.ty != MirType::Dynamic
                || declaration.storage != GlobalStorage::LegacyStatic
                || result.roles.iter().any(|r| r.export.value == slot)
            {
                return Err(error("角色不是互异Dynamic Legacy槽").into());
            }
            let initial = match key {
                "__name__" => Initial::Text("__main__".into()),
                "__package__" => Initial::Text(String::new()),
                "__file__" if hir.info.resolve_file_at_runtime => Initial::File,
                "__file__" => Initial::Text(hir.info.file.clone()),
                _ => Initial::Metadata(key.into()),
            };
            result.roles.push(Role {
                export: (*export).clone(),
                declaration,
                initial,
            });
        }
        Ok(result)
    }

    pub fn inspect(&self, program: &Program, exports: &[ModuleExport]) -> Result<(), Failure> {
        if program.entry != self.entry || function(program, self.entry)?.source_file != self.file {
            return Err(error("实际初始化入口或来源文件改变").into());
        }
        let module = function(program, self.entry)?;
        let body = &module.body;
        crate::generator_product_edges::check(body)?;
        let mut region_errors = Vec::new();
        crate::validate_regions::validate_regions(module, &mut region_errors);
        if !region_errors.is_empty() {
            return Err(error(&format!(
                "初始化入口含异常绕入：{}",
                region_errors.join("；")
            ))
            .into());
        }
        let mut limitations = Limitations::default();
        if !body.regions.is_empty() || !body.source_handler_contracts.is_empty() {
            limitations.note(error("初始化入口含异常绕入"));
        }
        let mut order = FxHashMap::default();
        let mut at = body.entry;
        loop {
            let rank = order.len();
            if order.insert(at, rank).is_some() {
                limitations.note(error("模块控制链回入"));
                break;
            }
            match body.blocks[at].terminator {
                Terminator::Goto(next) => at = next,
                Terminator::Return(None) => break,
                _ => {
                    limitations.note(error("模块初始化不是确定直链出口"));
                    break;
                }
            }
        }
        for (_, block) in body.blocks.iter() {
            if matches!(block.terminator, Terminator::Goto(to) | Terminator::Suspend { resume: to, .. } if to == body.entry)
                || matches!(block.terminator, Terminator::Branch { then_, else_, .. } if then_ == body.entry || else_ == body.entry)
            {
                limitations.note(error("模块基本块回入entry"));
            }
        }
        let mut definitions: FxHashMap<LocalId, Vec<Point>> = FxHashMap::default();
        let mut writes: FxHashMap<GlobalId, Vec<(FnId, Point, &Operand)>> = FxHashMap::default();
        for (id, definition) in program.functions.iter() {
            for (block_id, block) in definition.body.blocks.iter() {
                for (position, statement) in block.stmts.iter().enumerate() {
                    if id == self.entry
                        && let Statement::Assign { dest, .. } = statement
                    {
                        definitions
                            .entry(*dest)
                            .or_default()
                            .push((block_id, position));
                    }
                    if let Statement::SetGlobal { dest, value } = statement {
                        writes
                            .entry(*dest)
                            .or_default()
                            .push((id, (block_id, position), value));
                    }
                }
            }
        }
        let mut context = Context {
            program,
            body,
            order: &order,
            definitions: &definitions,
            private: FxHashSet::default(),
            transfers: FxHashSet::default(),
            pending_chains: vec![],
        };
        let mut published = FxHashMap::default();
        let mut producer_sites = FxHashSet::default();
        let mut resets = vec![];
        let mut chains_complete = true;
        for role in &self.roles {
            if global(program, role.export.value)? != &role.declaration
                || exports
                    .iter()
                    .filter(|e| e.name == role.export.name)
                    .collect::<Vec<_>>()
                    != [&role.export]
            {
                return Err(error("角色声明或导出协议改变").into());
            }
            let slot = role.export.value;
            if exports.iter().any(|e| {
                e.name != role.export.name
                    && (e.value == slot
                        || matches!(e.guard, ExportGuard::BoundFlag(flag) if flag == slot))
            }) || program.functions.iter().any(|(_, f)| {
                [
                    f.function_global,
                    f.defaults_global,
                    f.signature_global,
                    f.specialization_identity_global,
                ]
                .contains(&Some(slot))
            }) || program
                .classes
                .iter()
                .any(|(_, c)| c.factory_global == Some(slot))
            {
                return Err(error("角色槽与其它导出/私有角色冲突").into());
            }
            let Some(writes) = writes.get(&role.export.value) else {
                return Err(error("角色缺少reset与发布").into());
            };
            let [
                (reset_fn, reset, Operand::Const(Constant::Null)),
                (publish_fn, publish, value),
            ] = writes.as_slice()
            else {
                return Err(error("角色不是唯一Null reset随后唯一发布").into());
            };
            if *reset_fn != self.entry
                || *publish_fn != self.entry
                || reset.0 != body.entry
                || publish.0 != body.entry
                || reset.1 >= publish.1
            {
                return Err(error("角色发布不在entry同块有序前言").into());
            }
            if crate::validate::operand_type(program, module, value) != Some(MirType::Dynamic) {
                return Err(error("角色发布producer/helper/key/常量不符").into());
            }
            match limitations.check(context.initial(value, *publish, &mut producer_sites))? {
                Some(actual) if actual != role.initial => {
                    return Err(error("角色发布producer/helper/key/常量不符").into());
                }
                Some(_) => {}
                None => chains_complete = false,
            }
            resets.push(reset.1);
            published.insert(role.export.value, publish.1);
        }
        let first = *published
            .values()
            .min()
            .ok_or_else(|| error("没有真实身份发布"))?;
        let last = *published
            .values()
            .max()
            .ok_or_else(|| error("没有真实身份发布"))?;
        if resets.iter().any(|reset| *reset >= first) {
            return Err(error("reset区晚于真实发布").into());
        }
        // Source的name实参由builder先Box；同样追到真实字面量，而非假定实参是常量。
        let sources: Vec<_> = body.blocks[body.entry]
            .stmts
            .iter()
            .enumerate()
            .filter_map(|(at, s)| {
                if let Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ModuleExecutionSource),
                            args,
                        },
                } = s
                {
                    Some((at, *dest, args))
                } else {
                    None
                }
            })
            .collect();
        let [(source_at, source, args)] = sources.as_slice() else {
            return Err(error("前言缺少唯一Source注册来源").into());
        };
        let [argument] = args.as_slice() else {
            return Err(error("Source真实参数不符").into());
        };
        if *source_at >= first
            || crate::validate::operand_type(program, module, argument) != Some(MirType::Dynamic)
            || crate::validate::local_decl(&body.locals, *source)
                .is_none_or(|d| d.ty != MirType::Dynamic || d.is_cell)
            || definitions.get(source).map(Vec::as_slice) != Some(&[(body.entry, *source_at)][..])
        {
            return Err(error("Source真实参数不符").into());
        }
        match limitations.check(context.initial(
            argument,
            (body.entry, *source_at),
            &mut producer_sites,
        ))? {
            Some(actual) if actual != Initial::Text("__main__".into()) => {
                return Err(error("Source真实参数不符").into());
            }
            Some(_) => {}
            None => chains_complete = false,
        }
        // producer临时值只能沿已核纯链进入对应store；metadata不因store完成而获得用户能力。
        context.transfers = producer_sites.clone();
        context.transfers.insert((body.entry, *source_at));
        for site in &producer_sites {
            if let Statement::Assign { dest, .. } = body.blocks[site.0].stmts[site.1] {
                context.private.insert(dest);
            }
        }
        context
            .transfers
            .extend(published.values().map(|at| (body.entry, *at)));
        limitations.check(self.consumers(&context, &published, &producer_sites))?;
        // 能力未完成也执行同一个前言检查：reset/Push/effect不能被长纯链遮蔽。
        // 仅实际截断链上尚未证明的producer保持NotProven，不认证初始值。
        let mut push = None;
        let mut ordinary_resets = FxHashSet::default();
        for (position, statement) in body.blocks[body.entry].stmts[..=last].iter().enumerate() {
            let site = (body.entry, position);
            let allowed = match statement {
                Statement::Line(_) => true,
                Statement::SetGlobal { dest, .. } if published.contains_key(dest) => true,
                Statement::SetGlobal { dest, value } if position < first => {
                    let slot = global(program, *dest)?;
                    ordinary_resets.insert(*dest)
                        && self.resets.get(dest) == Some(slot)
                        && slot.storage == GlobalStorage::LegacyStatic
                        && matches!(
                            (&slot.ty, value),
                            (MirType::Dynamic, Operand::Const(Constant::Null))
                                | (MirType::Bool, Operand::Const(Constant::Bool(false)))
                        )
                }
                Statement::Assign { .. } if producer_sites.contains(&site) => true,
                Statement::Assign { .. } if position == *source_at => true,
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::TracePushWithOrigin),
                            args,
                        },
                } if position > *source_at
                    && position < first
                    && push.is_none()
                    && args
                        == &[
                            Operand::Const(Constant::Str(self.file.clone())),
                            Operand::Const(Constant::Str("<module>".into())),
                            Operand::Local(*source),
                        ] =>
                {
                    push = Some(*dest);
                    true
                }
                Statement::Assign {
                    value:
                        Rvalue::Use(_)
                        | Rvalue::Box { .. }
                        | Rvalue::Call {
                            callee:
                                Callee::Runtime(RuntimeFn::ModuleFile | RuntimeFn::ModuleMetadata),
                            ..
                        },
                    ..
                } if !chains_complete => limitations
                    .check(context.pending_site(site))?
                    .unwrap_or(true),
                _ => false,
            };
            if !allowed {
                return Err(error("前言全部发布前存在未知effect或伪reset").into());
            }
        }
        if push.is_none() {
            return Err(error("前言缺少真实Source注册来源/Push").into());
        }
        limitations.finish()
    }

    pub fn meta(&self, meta: &UnitMeta) -> Result<(), String> {
        let file_backed = self.roles.iter().any(|role| role.export.name == "__file__");
        if meta.name != "__main__"
            || !meta.package.is_empty()
            || meta.file != self.file
            || meta.execution != ModuleExecutionKind::LegacySingleton
            || meta.is_package
            || meta.instance_capabilities != Default::default()
            || !meta.protected_exports.is_empty()
            || meta.file_backed_by_global != file_backed
            || (!meta.file_backed_by_global && meta.location == ModuleLocation::LegacyDiagnostic)
        {
            return Err(error("UnitMeta注册身份/公开file槽能力降级"));
        }
        let valid = match &meta.location {
            ModuleLocation::LegacyDiagnostic => true, // file-backed使实际codegen仍选择Source注册。
            ModuleLocation::SourceAbsolute(path) => std::path::Path::new(path).is_absolute(),
            ModuleLocation::ExecutableRelative(path) => {
                !path.is_empty()
                    && !std::path::Path::new(path).is_absolute()
                    && !std::path::Path::new(path)
                        .components()
                        .any(|c| matches!(c, std::path::Component::ParentDir))
            }
        };
        if !valid {
            return Err(error("UnitMeta定位不在已核JIT/AOT族"));
        }
        Ok(())
    }
    pub fn linked(&self, program: &LinkedProgram, map: &LinkRemap) -> Result<(), String> {
        let module = program
            .modules
            .iter()
            .find(|(id, _)| *id == program.entry)
            .map(|(_, m)| m)
            .ok_or_else(|| error("实际link入口不存在"))?;
        if map.functions.get(&self.entry) != Some(&module.init) {
            return Err(error("实际link init映射错误"));
        }
        for role in &self.roles {
            let slot = map
                .globals
                .get(&role.export.value)
                .ok_or_else(|| error("实际link缺角色映射"))?;
            let matches: Vec<_> = module
                .exports
                .iter()
                .filter(|e| e.name == role.export.name)
                .collect();
            if matches.len() != 1
                || matches[0].value != *slot
                || matches[0].guard != ExportGuard::ValueSentinel
            {
                return Err(error("实际link角色/导出/guard映射错误"));
            }
        }
        Ok(())
    }
}
