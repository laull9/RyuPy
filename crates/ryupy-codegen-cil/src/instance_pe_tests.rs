//! 独立解析真实 PE 的 owner 根门；普通方法 slot 定位允许同名同签名，绝不猜 FnId 编号。
use dotnetdll::prelude::*;
use std::collections::HashSet;

#[derive(Clone, Debug)]
pub(super) struct Locator {
    pub owner: String,
    pub name: String,
    pub signature: String,
    pub slot: u32,
}
#[derive(Clone, Debug)]
pub(super) struct Root {
    pub source: Locator,
    pub target: Locator,
    pub instruction: u32,
    pub role: String,
}
#[derive(Clone, Debug)]
pub(super) struct Owner {
    pub id: u32,
    pub core: Locator,
    pub root: Root,
    pub calls: u32,
}
#[derive(Clone, Debug)]
pub(super) struct Expected {
    pub entry: Locator,
    pub owners: Vec<Owner>,
    pub modules: Vec<Root>,
}

pub(super) fn locate(res: &Resolution<'_>, expected: &Locator) -> Result<MethodIndex, String> {
    if expected.owner != "RyuPy.Generated.Module" {
        return Err("超出普通方法保序边界".into());
    }
    let owners: Vec<_> = res
        .enumerate_type_definitions()
        .filter(|(_, ty)| ty.namespace.as_deref() == Some("RyuPy.Generated") && ty.name == "Module")
        .collect();
    if owners.len() != 1 || !owners[0].1.properties.is_empty() || !owners[0].1.events.is_empty() {
        return Err("生成类型不唯一或含 property/event".into());
    }
    let (index, method) = res
        .enumerate_methods(owners[0].0)
        .nth(expected.slot as usize)
        .ok_or("方法 slot 越界")?;
    if method.name != expected.name || method.signature.show(res) != expected.signature {
        return Err("方法 descriptor 不符".into());
    }
    if method.body.is_none() {
        return Err("方法没有 body".into());
    }
    Ok(index)
}
fn root(res: &Resolution<'_>, expected: &Root, role: &str) -> Result<MethodIndex, String> {
    if expected.role != role {
        return Err("根角色不符".into());
    }
    let source = locate(res, &expected.source)?;
    let target = locate(res, &expected.target)?;
    let instructions = &res[source].body.as_ref().ok_or("根没有 body")?.instructions;
    match instructions.get(expected.instruction as usize) {
        Some(Instruction::LoadMethodPointer(MethodSource::User(UserMethod::Definition(
            actual,
        )))) if *actual == target => Ok(source),
        _ => Err("指定指令不是准确目标的 ldftn Definition".into()),
    }
}

pub(super) fn verify(bytes: &[u8], expected: &Expected) -> Result<(), String> {
    let res = Resolution::parse(bytes, ReadOptions::default()).map_err(|e| e.to_string())?;
    let entry = locate(&res, &expected.entry)?;
    if !matches!(
        res.entry_point,
        Some(dotnetdll::resolution::EntryPoint::Method(actual)) if actual == entry
    ) {
        return Err("期望 Main 不是实际 PE 入口".into());
    }
    if expected.owners.is_empty() || expected.modules.is_empty() {
        return Err("空根集合".into());
    }
    let mut ids = HashSet::new();
    let mut thunks = HashSet::new();
    let mut cores = HashSet::new();
    let mut initializer = None;
    for owner in &expected.owners {
        let source = root(&res, &owner.root, "callable_thunk")?;
        let thunk = locate(&res, &owner.root.target)?;
        let core = locate(&res, &owner.core)?;
        if !ids.insert(owner.id) || !thunks.insert(thunk) || !cores.insert(core) {
            return Err("重复 FnId/thunk/core 映射".into());
        }
        if owner.root.source.name != "__init_functions" {
            return Err("owner 根不属于函数初始化器".into());
        }
        if initializer.replace(source).is_some_and(|old| old != source) {
            return Err("函数初始化器不唯一".into());
        }
        let mut calls = 0;
        for instruction in &res[thunk]
            .body
            .as_ref()
            .ok_or("thunk 没有 body")?
            .instructions
        {
            if let Instruction::Call {
                param0: MethodSource::User(UserMethod::Definition(actual)),
                ..
            } = instruction
                && actual.parent_type() == core.parent_type()
            {
                if *actual != core {
                    return Err("thunk 调用错误 core（包括同名同签名）".into());
                }
                calls += 1;
            }
        }
        if !matches!(owner.calls, 1 | 2) || calls != owner.calls {
            return Err("与 MIR binder 路径的 core 调用数量不符".into());
        }
    }
    let initializer = initializer.ok_or("缺少初始化器")?;
    let calls = res[entry]
        .body
        .as_ref()
        .ok_or("entry 没有 body")?
        .instructions
        .iter()
        .filter(|instruction| {
            matches!(instruction, Instruction::Call {
                param0: MethodSource::User(UserMethod::Definition(actual)), ..
            } if *actual == initializer)
        })
        .count();
    if calls != 1 {
        return Err("Main 没有准确且唯一的初始化器 Call Definition".into());
    }
    let mut module_targets = HashSet::new();
    let mut module_sites = HashSet::new();
    for module in &expected.modules {
        let source = root(&res, module, "module_init")?;
        let target = locate(&res, &module.target)?;
        if source != initializer
            || !module_targets.insert(target)
            || !module_sites.insert((source, module.instruction))
        {
            return Err("模块登记根不属于同一初始化器或重复".into());
        }
    }
    Ok(())
}

struct Decoder<'a>(&'a [u8]);
impl<'a> Decoder<'a> {
    fn take(&mut self, count: usize) -> Result<&'a [u8], String> {
        if count > self.0.len() {
            return Err("审计 envelope 截断".into());
        }
        let (value, rest) = self.0.split_at(count);
        self.0 = rest;
        Ok(value)
    }
    fn number(&mut self) -> Result<u32, String> {
        Ok(u32::from_le_bytes(
            self.take(4)?.try_into().map_err(|_| "整数截断")?,
        ))
    }
    fn count(&mut self) -> Result<usize, String> {
        let value = self.number()? as usize;
        if value > 100_000 {
            return Err("记录过多".into());
        }
        Ok(value)
    }
    fn string(&mut self) -> Result<String, String> {
        let size = self.count()?;
        String::from_utf8(self.take(size)?.to_vec()).map_err(|e| e.to_string())
    }
    fn locator(&mut self) -> Result<Locator, String> {
        Ok(Locator {
            owner: self.string()?,
            name: self.string()?,
            signature: self.string()?,
            slot: self.number()?,
        })
    }
    fn root(&mut self) -> Result<Root, String> {
        Ok(Root {
            source: self.locator()?,
            target: self.locator()?,
            instruction: self.number()?,
            role: self.string()?,
        })
    }
}
pub(super) fn decode<'a>(bytes: &'a [u8], commit: &str, case: &str) -> Result<(Expected, &'a [u8]), String> {
    if bytes.len() > 64 * 1024 * 1024 {
        return Err("envelope 过大".into());
    }
    let mut input = Decoder(bytes);
    if input.take(8)? != b"RYUPE002" || input.number()? != 2 {
        return Err("envelope/report 版本不符".into());
    }
    if input.string()? != commit || input.string()? != case {
        return Err("陈旧 commit/case".into());
    }
    let entry = input.locator()?;
    let mut owners = Vec::new();
    for _ in 0..input.count()? {
        owners.push(Owner {
            id: input.number()?,
            core: input.locator()?,
            root: input.root()?,
            calls: input.number()?,
        });
    }
    let mut modules = Vec::new();
    for _ in 0..input.count()? {
        modules.push(input.root()?);
    }
    let size = input.number()? as usize;
    let pe = input.take(size)?;
    if !input.0.is_empty() {
        return Err("envelope 有尾随数据".into());
    }
    Ok((
        Expected {
            entry,
            owners,
            modules,
        },
        pe,
    ))
}

#[test]
#[ignore = "需要 driver 的同次 PE/报告及 Python 哈希绑定 envelope"]
fn explicit_instance_actual_pe_gate() {
    let directory =
        std::path::PathBuf::from(std::env::var("RYUPY_INSTANCE_AOT_STRUCTURE_OUT").unwrap());
    let commit = std::env::var("GITHUB_SHA").unwrap();
    for case in ["original", "combined"] {
        let bytes = std::fs::read(directory.join(format!("{case}.pe-envelope"))).unwrap();
        let (expected, pe) = decode(&bytes, &commit, case).unwrap();
        verify(pe, &expected).unwrap();
    }
}

#[test]
fn instance_pe_envelope_rejects_stale_truncated_and_trailing_inputs() {
    fn number(bytes: &mut Vec<u8>, value: u32) {
        bytes.extend(value.to_le_bytes());
    }
    fn text(bytes: &mut Vec<u8>, value: &str) {
        number(bytes, value.len() as u32);
        bytes.extend(value.as_bytes());
    }
    let mut bytes = b"RYUPE002".to_vec();
    number(&mut bytes, 2);
    text(&mut bytes, "current");
    text(&mut bytes, "original");
    for value in ["RyuPy.Generated.Module", "Main", "static void ()"] {
        text(&mut bytes, value);
    }
    number(&mut bytes, 0);
    number(&mut bytes, 0);
    number(&mut bytes, 0);
    number(&mut bytes, 2);
    bytes.extend(b"PE");
    assert_eq!(decode(&bytes, "current", "original").unwrap().1, b"PE");
    assert!(decode(&bytes, "old", "original").is_err());
    assert!(decode(&bytes, "current", "combined").is_err());
    for length in 0..bytes.len() {
        assert!(decode(&bytes[..length], "current", "original").is_err());
    }
    let mut wrong = bytes.clone();
    wrong.push(0);
    assert!(decode(&wrong, "current", "original").is_err());
    let mut wrong = bytes.clone();
    wrong[8] = 1;
    assert!(decode(&wrong, "current", "original").is_err());
    let mut wrong = bytes;
    wrong[12..16].copy_from_slice(&u32::MAX.to_le_bytes());
    assert!(decode(&wrong, "current", "original").is_err());
}
