//! 产品准入与真实link之间的不可伪造绑定。FnId/GlobalId按link返回的实际双射逐操作核对；
//! 最终证书保完整typed快照，任何后续改写都会在发射前拒绝，不把标签或哈希当语义证明。
use crate::generator_product_identity::FunctionImage;
use crate::trace_lowering::LinkRemap;
use crate::*;
use rustc_hash::{FxHashMap, FxHashSet};

#[derive(Debug)]
pub struct Proof {
    functions: Vec<FunctionImage>,
    classes: Vec<ClassDef>,
    globals: Vec<GlobalDecl>,
    exports: Vec<ModuleExport>,
    entry: FnId,
    source_name: String,
    initialization: crate::generator_product_initialization::Initialization,
}
impl Proof {
    pub(super) fn capture(
        program: &Program,
        exports: &[ModuleExport],
        initialization: crate::generator_product_initialization::Initialization,
    ) -> Self {
        Self {
            functions: program
                .functions
                .iter()
                .map(|(_, f)| FunctionImage::capture(f))
                .collect(),
            classes: program.classes.iter().map(|(_, c)| c.clone()).collect(),
            globals: program.globals.iter().map(|(_, g)| g.clone()).collect(),
            exports: exports.to_vec(),
            entry: program.entry,
            source_name: program.source_name.clone(),
            initialization,
        }
    }
    fn verify(&self, program: &Program, exports: &[ModuleExport]) -> Result<(), String> {
        if self.functions
            != program
                .functions
                .iter()
                .map(|(_, f)| FunctionImage::capture(f))
                .collect::<Vec<_>>()
            || self.classes
                != program
                    .classes
                    .iter()
                    .map(|(_, c)| c.clone())
                    .collect::<Vec<_>>()
            || self.globals
                != program
                    .globals
                    .iter()
                    .map(|(_, g)| g.clone())
                    .collect::<Vec<_>>()
            || self.exports != exports
            || self.entry != program.entry
            || self.source_name != program.source_name
        {
            return Err("已核低层程序在link前被改写".into());
        }
        Ok(())
    }
}

#[derive(Debug)]
pub struct Certificate {
    functions: Vec<FunctionImage>,
    classes: Vec<ClassDef>,
    globals: Vec<GlobalDecl>,
    modules: Vec<ModuleDef>,
    entry: ModuleId,
}
impl Certificate {
    fn capture(program: &LinkedProgram) -> Self {
        Self {
            functions: program
                .functions
                .iter()
                .map(|(_, f)| FunctionImage::capture(f))
                .collect(),
            classes: program.classes.iter().map(|(_, c)| c.clone()).collect(),
            globals: program.globals.iter().map(|(_, g)| g.clone()).collect(),
            modules: program.modules.iter().map(|(_, m)| m.clone()).collect(),
            entry: program.entry,
        }
    }
    pub(super) fn verify(&self, program: &LinkedProgram) -> Result<(), String> {
        if self.functions
            != program
                .functions
                .iter()
                .map(|(_, f)| FunctionImage::capture(f))
                .collect::<Vec<_>>()
            || self.classes
                != program
                    .classes
                    .iter()
                    .map(|(_, c)| c.clone())
                    .collect::<Vec<_>>()
            || self.globals
                != program
                    .globals
                    .iter()
                    .map(|(_, g)| g.clone())
                    .collect::<Vec<_>>()
            || self.modules
                != program
                    .modules
                    .iter()
                    .map(|(_, m)| m.clone())
                    .collect::<Vec<_>>()
            || self.entry != program.entry
        {
            return Err("产品link证书与实际待发射产物不一致".into());
        }
        Ok(())
    }
}

pub(super) fn link(
    mut units: Vec<(Program, UnitMeta)>,
    entry: usize,
    mut proofs: Vec<Proof>,
) -> Result<LinkedProgram, String> {
    if units.len() != 1 || proofs.len() != 1 || entry != 0 {
        return Err("产品V1只接受单源码、无外宿主的完整可执行产物".into());
    }
    let (program, meta) = units.pop().ok_or("缺少实际模块")?;
    let mut proof = proofs.pop().ok_or("缺少实际模块证明")?;
    proof.verify(&program, &meta.exports)?;
    proof.initialization.meta(&meta)?;
    if meta.execution != ModuleExecutionKind::LegacySingleton
        || meta.instance_capabilities != Default::default()
        || meta.name != "__main__"
        || !meta.package.is_empty()
        || meta.is_package
        || !meta.protected_exports.is_empty()
    {
        return Err("实际注册根不是封闭Legacy __main__".into());
    }
    let expected_meta = meta.clone();
    let (mut linked, maps) = crate::trace_lowering::link_for_audit(vec![(program, meta)], 0);
    let [map] = maps.as_slice() else {
        return Err("实际link映射数量错误".into());
    };
    proof.initialization.linked(&linked, map)?;
    if map.functions.len() != proof.functions.len()
        || map.globals.len() != proof.globals.len()
        || map
            .functions
            .values()
            .copied()
            .collect::<FxHashSet<_>>()
            .len()
            != proof.functions.len()
        || map
            .globals
            .values()
            .copied()
            .collect::<FxHashSet<_>>()
            .len()
            != proof.globals.len()
    {
        return Err("实际FnId/GlobalId映射不是完整双射".into());
    }
    for (old, actual) in &map.functions {
        let mut expected = proof.functions[old.into_raw().into_u32() as usize].clone();
        remap_function(&mut expected, map)?;
        if FunctionImage::capture(&linked.functions[*actual]) != expected {
            return Err("link改写了实际函数ABI/源操作/恢复委托".into());
        }
    }
    for (old, actual) in &map.globals {
        let mut expected = proof.globals[old.into_raw().into_u32() as usize].clone();
        expected.name = format!("m0_{}", expected.name);
        if linked.globals[*actual] != expected {
            return Err("link全局槽/类型/存储身份不符".into());
        }
    }
    let classes: Vec<_> = linked.classes.iter().map(|(_, c)| c.clone()).collect();
    for class in &mut proof.classes {
        class.codegen_name = format!("m0_{}", class.codegen_name);
    }
    if classes != proof.classes
        || linked.functions.len() != proof.functions.len()
        || linked.globals.len() != proof.globals.len()
        || linked.modules.len() != 1
    {
        return Err("link增加了未核函数/class/global/module".into());
    }
    let mut exports = expected_meta.exports;
    for export in &mut exports {
        export.value = mapped(&map.globals, export.value)?;
        if let ExportGuard::BoundFlag(flag) = &mut export.guard {
            *flag = mapped(&map.globals, *flag)?;
        }
    }
    let expected = ModuleDef {
        instance_capabilities: expected_meta.instance_capabilities,
        execution: expected_meta.execution,
        environment_reason: expected_meta.environment_reason,
        location: expected_meta.location,
        file_backed_by_global: expected_meta.file_backed_by_global,
        is_package: expected_meta.is_package,
        protected_exports: expected_meta.protected_exports,
        name: expected_meta.name,
        package: expected_meta.package,
        file: expected_meta.file,
        init: mapped(&map.functions, proof.entry)?,
        exports,
    };
    if linked.modules[linked.entry] != expected {
        return Err("实际注册init/getter/export映射改变".into());
    }
    crate::validate::validate_linked_exports(&linked).map_err(|e| e.join("\n"))?;
    linked.trace_product = Some(Certificate::capture(&linked));
    Ok(linked)
}

fn mapped<K: Eq + std::hash::Hash + Copy>(map: &FxHashMap<K, K>, id: K) -> Result<K, String> {
    map.get(&id)
        .copied()
        .ok_or_else(|| "实际link引用未纳入映射".into())
}
fn operand(value: &mut Operand, map: &LinkRemap) -> Result<(), String> {
    match value {
        Operand::Global(id) => *id = mapped(&map.globals, *id)?,
        Operand::Array(values) => {
            for value in values {
                operand(value, map)?;
            }
        }
        Operand::Const(_) | Operand::Local(_) => {}
    }
    Ok(())
}
fn rvalue(value: &mut Rvalue, map: &LinkRemap) -> Result<(), String> {
    match value {
        Rvalue::Use(value) | Rvalue::Box { value, .. } | Rvalue::Unbox { value, .. } => {
            operand(value, map)?
        }
        Rvalue::Binary { lhs, rhs, .. } => {
            operand(lhs, map)?;
            operand(rhs, map)?;
        }
        Rvalue::Call { callee, args } => {
            if let Callee::Fn(id) = callee {
                *id = mapped(&map.functions, *id)?;
            }
            for value in args {
                operand(value, map)?;
            }
        }
        Rvalue::DynCallValue { callee, args } => {
            operand(callee, map)?;
            for value in args {
                operand(value, map)?;
            }
        }
        Rvalue::MakeFunction {
            func,
            instance_context: None,
            env,
            defaults,
        } if env.is_empty() => {
            *func = mapped(&map.functions, *func)?;
            for value in defaults {
                operand(value, map)?;
            }
        }
        Rvalue::MakeGenerator {
            resume,
            frame,
            origin,
        } => {
            *resume = mapped(&map.functions, *resume)?;
            for value in frame {
                operand(value, map)?;
            }
            if let Some(value) = origin {
                operand(value, map)?;
            }
        }
        Rvalue::CatchException => {}
        _ => return Err("link出现未核高层/低层操作".into()),
    }
    Ok(())
}
fn remap_function(function: &mut FunctionImage, map: &LinkRemap) -> Result<(), String> {
    let identity = &mut function.identity;
    identity.codegen_name = format!("m0_{}", identity.codegen_name);
    for id in [
        &mut identity.function_global,
        &mut identity.specialization_global,
        &mut identity.defaults_global,
        &mut identity.signature_global,
    ]
    .into_iter()
    .flatten()
    {
        *id = mapped(&map.globals, *id)?;
    }
    if let Some(origin) = &mut identity.origin
        && let Some(id) = &mut origin.source_global
    {
        *id = mapped(&map.globals, *id)?;
    }
    for block in &mut function.blocks {
        for statement in &mut block.stmts {
            match statement {
                Statement::Assign { value, .. } | Statement::Eval(value) => rvalue(value, map)?,
                Statement::SetGlobal { dest, value } => {
                    *dest = mapped(&map.globals, *dest)?;
                    operand(value, map)?;
                }
                Statement::Line(_) => {}
                _ => return Err("link含未认证字段写入".into()),
            }
        }
        match &mut block.terminator {
            Terminator::Branch { cond, .. } => operand(cond, map)?,
            Terminator::Return(Some(value)) => operand(value, map)?,
            Terminator::Raise { value, cause } => {
                for value in value.iter_mut().chain(cause) {
                    operand(value, map)?;
                }
            }
            Terminator::Goto(_) | Terminator::Return(None) | Terminator::Unreachable => {}
            _ => return Err("link还含高层Suspend".into()),
        }
    }
    Ok(())
}
