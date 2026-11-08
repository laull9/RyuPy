//! 高层 trace 操作守恒合同。先核真实 Push/Pop/owner，再固定源码构建的 effect 序列及 EH；
//! pass 只能删除纯死值与收拢空跳转，不能凭 RuntimeFn 名字擦掉额外源操作后宣称等价。
use crate::generator_product_edge_witness::EdgeWitness;
use crate::generator_product_edges::Edges;
use crate::generator_product_identity::FunctionIdentity;
use crate::*;
use rustc_hash::FxHashSet;

pub(super) struct Contract {
    identities: Vec<FunctionIdentity>,
    statements: Vec<Vec<Vec<Statement>>>,
    locals: Vec<Vec<LocalDecl>>,
    edges: Vec<Vec<Option<Terminator>>>,
    // 原图typed规范边与固定源锚；raw边只保留用于可达性登记和失败说明。
    canonical_edges: Vec<Edges>,
    regions: Vec<Vec<Region>>,
    // 只用于首个边拒绝的说明；capture时保存原图，不影响任何比较或准入条件。
    edge_witnesses: Vec<Vec<EdgeWitness>>,
}
fn effect(statement: &Statement) -> bool {
    !matches!(
        statement,
        Statement::Assign {
            value: Rvalue::Use(_) | Rvalue::Box { .. },
            ..
        }
    )
}
pub(super) fn reachable(body: &Body) -> FxHashSet<BlockId> {
    let mut seen = FxHashSet::default();
    let mut todo = vec![body.entry];
    todo.extend(body.regions.iter().map(|r| r.handler));
    while let Some(id) = todo.pop() {
        if !seen.insert(id) {
            continue;
        }
        match &body.blocks[id].terminator {
            Terminator::Goto(target) | Terminator::Suspend { resume: target, .. } => {
                todo.push(*target)
            }
            Terminator::Branch { then_, else_, .. } => {
                todo.push(*then_);
                todo.push(*else_);
            }
            _ => {}
        }
    }
    seen
}
impl Contract {
    pub fn capture(program: &Program, generators: &[FnId]) -> Result<Self, String> {
        let mut result = Self {
            identities: vec![],
            statements: vec![],
            locals: vec![],
            edges: vec![],
            canonical_edges: vec![],
            regions: vec![],
            edge_witnesses: vec![],
        };
        for (id, function) in program.functions.iter() {
            result.canonical_edges.push(Edges::capture(&function.body)?);
            check_stack(program, id, generators.contains(&id))?;
            let live = reachable(&function.body);
            result.identities.push(FunctionIdentity::capture(function));
            result.locals.push(
                function
                    .body
                    .locals
                    .iter()
                    .map(|(_, local)| local.clone())
                    .collect(),
            );
            result.statements.push(
                function
                    .body
                    .blocks
                    .iter()
                    .map(|(id, block)| {
                        if live.contains(&id) {
                            block.stmts.clone()
                        } else {
                            vec![]
                        }
                    })
                    .collect(),
            );
            result.edges.push(
                function
                    .body
                    .blocks
                    .iter()
                    .map(|(id, block)| {
                        if live.contains(&id) {
                            Some(block.terminator.clone())
                        } else {
                            None
                        }
                    })
                    .collect(),
            );
            result.regions.push(function.body.regions.clone());
            result.edge_witnesses.push(
                function
                    .body
                    .blocks
                    .iter()
                    .map(|(_, block)| EdgeWitness::capture(&function.body, &block.terminator))
                    .collect(),
            );
        }
        Ok(result)
    }
    pub fn verify(&self, program: &Program, generators: &[FnId]) -> Result<(), String> {
        if self.identities.len() != program.functions.len() {
            return Err("pass改写了真实函数身份集合".into());
        }
        for (index, (function_id, function)) in program.functions.iter().enumerate() {
            crate::generator_product_edges::check(&function.body)?;
            check_stack(program, function_id, generators.contains(&function_id))?;
            if self.identities[index] != FunctionIdentity::capture(function)
                || self.regions[index] != function.body.regions
                || self.edges[index].len() != function.body.blocks.len()
            {
                return Err("pass改变了源函数签名、owner、EH或控制块集合".into());
            }
            if self.locals[index]
                != function
                    .body
                    .locals
                    .iter()
                    .map(|(_, local)| local.clone())
                    .collect::<Vec<_>>()
            {
                return Err("pass改变了实际局部类型/身份/cell声明".into());
            }
            let reads = reads(&function.body);
            let live = reachable(&function.body);
            for (ordinal, (id, block)) in function.body.blocks.iter().enumerate() {
                let actual = &block.stmts;
                if let Some(expected) = &self.edges[index][ordinal] {
                    // 合并空跳转后原空块可以不可达；有源操作的块不能消失。
                    if !live.contains(&id)
                        && self.statements[index][ordinal].iter().all(|s| !effect(s))
                    {
                        continue;
                    }
                    preserve(&self.statements[index][ordinal], actual, &reads)?;
                    if !self.canonical_edges[index].matches(
                        &function.body,
                        id,
                        &block.terminator,
                    )? {
                        return Err(crate::generator_product_edge_witness::failure(
                            function_id,
                            id,
                            &self.edge_witnesses[index][ordinal],
                            &function.body,
                            expected,
                            &block.terminator,
                        ));
                    }
                } else if live.contains(&id) || !actual.is_empty() {
                    return Err("pass激活了原本不可达的源码操作".into());
                }
            }
        }
        Ok(())
    }
}

// 仅允许删除最终完全未读取的Use/Box。不能增加或改写纯赋值后借相同effect序列偷换控制条件。
fn preserve(
    before: &[Statement],
    after: &[Statement],
    reads: &FxHashSet<LocalId>,
) -> Result<(), String> {
    let mut position = 0;
    for expected in before {
        if after.get(position) == Some(expected) {
            position += 1;
            continue;
        }
        if matches!(expected, Statement::Assign { dest, value: Rvalue::Use(_) | Rvalue::Box { .. } } if !reads.contains(dest))
        {
            continue;
        }
        return Err("pass改变了仍可读的局部定义或实际effect顺序".into());
    }
    if position != after.len() {
        return Err("pass加入了未核的纯定义/操作".into());
    }
    Ok(())
}
fn reads(body: &Body) -> FxHashSet<LocalId> {
    use crate::passes::dead_values::{read_operand, read_value};
    let mut reads = FxHashSet::default();
    for (_, block) in body.blocks.iter() {
        for statement in &block.stmts {
            match statement {
                Statement::Assign { value, .. } | Statement::Eval(value) => {
                    read_value(value, &mut reads)
                }
                Statement::SetGlobal { value, .. } => read_operand(value, &mut reads),
                Statement::SetField { obj, value, .. } => {
                    read_operand(obj, &mut reads);
                    read_operand(value, &mut reads);
                }
                Statement::Line(_) => {}
            }
        }
        match &block.terminator {
            Terminator::Suspend { value, input, .. } => {
                read_operand(value, &mut reads);
                reads.extend(input);
            }
            Terminator::Branch { cond, .. } => read_operand(cond, &mut reads),
            Terminator::Return(Some(value)) => read_operand(value, &mut reads),
            Terminator::Raise { value, cause } => {
                for value in value.iter().chain(cause) {
                    read_operand(value, &mut reads);
                }
            }
            _ => {}
        }
    }
    for region in &body.regions {
        reads.extend(region.exc_local);
    }
    reads
}

fn check_stack(program: &Program, id: FnId, generator: bool) -> Result<(), String> {
    let function = &program.functions[id];
    let body = &function.body;
    let mut push = None;
    let mut pops = 0;
    let live = reachable(body);
    for (block_id, block) in body.blocks.iter() {
        for (position, statement) in block.stmts.iter().enumerate() {
            match statement {
                Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee:
                                Callee::Runtime(RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin),
                            args,
                        },
                } => {
                    let name = if id == program.entry {
                        "<module>"
                    } else {
                        &function.name
                    };
                    if push.is_some()
                        || block_id != body.entry
                        || body.locals[*dest].ty != MirType::I64
                        || args.first()
                            != Some(&Operand::Const(Constant::Str(function.source_file.clone())))
                        || args.get(1) != Some(&Operand::Const(Constant::Str(name.into())))
                    {
                        return Err("高层TracePush次数、位置、文件、函数或depth类型错误".into());
                    }
                    match function.dynamic_origin {
                        Some(origin)
                            if args.as_slice()
                                == [
                                    Operand::Const(Constant::Str(function.source_file.clone())),
                                    Operand::Const(Constant::Str(name.into())),
                                    Operand::Local(origin.owner),
                                ] => {}
                        None if id == program.entry && args.len() == 3 => {
                            let Operand::Local(origin) = args[2] else {
                                return Err("模块Push缺实际来源局部".into());
                            };
                            if !block.stmts[..position].iter().any(|stmt| matches!(stmt,
                                Statement::Assign { dest, value: Rvalue::Call { callee: Callee::Runtime(RuntimeFn::ModuleExecutionSource), .. } } if *dest == origin)) {
                                return Err("模块Push来源并非本次执行的实际定义".into());
                            }
                        }
                        None if id != program.entry && args.len() == 2 => {}
                        _ => return Err("高层Push的真实hidden owner错误".into()),
                    }
                    push = Some(*dest);
                }
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    args,
                }) => {
                    if args.as_slice() != [Operand::Local(push.ok_or("Pop先于真实Push定义")?)]
                        || position + 1 != block.stmts.len()
                        || !matches!(block.terminator, Terminator::Return(_))
                    {
                        return Err("高层Pop不是唯一正常返回末操作".into());
                    }
                    pops += 1;
                }
                _ => {}
            }
        }
        if live.contains(&block_id)
            && matches!(block.terminator, Terminator::Return(_))
            && !matches!(
                block.stmts.last(),
                Some(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    ..
                }))
            )
        {
            return Err("高层正常返回缺少实际Pop".into());
        }
    }
    let depth = push.ok_or("高层缺少真实Push")?;
    if pops > 1
        || body
            .blocks
            .iter()
            .flat_map(|(_, b)| &b.stmts)
            .filter(|s| matches!(s, Statement::Assign { dest, .. } if *dest == depth))
            .count()
            != 1
    {
        return Err("高层depth被覆盖或出现额外Pop".into());
    }
    if !generator && !body.regions.is_empty() {
        return Err("首产品域的非生成器不得有额外handler".into());
    }
    Ok(())
}
