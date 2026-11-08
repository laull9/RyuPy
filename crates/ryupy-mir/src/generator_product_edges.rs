//! 高层边的原图合同：固定源锚，分别在原图和最终图收拢纯空Goto链。
//! 锚只固定规范落点，不终止坏ID/空环检查；Suspend始终保留全部原始字段。
use crate::*;
use rustc_hash::FxHashSet;

pub(super) struct Edges {
    anchors: FxHashSet<BlockId>,
    canonical: Vec<Terminator>,
}

fn ordinal(id: BlockId) -> usize {
    id.into_raw().into_u32() as usize
}

fn check_id(body: &Body, id: BlockId) -> Result<(), String> {
    if ordinal(id) >= body.blocks.len() {
        return Err("高层控制边目标不在实际blocks arena中".into());
    }
    Ok(())
}

fn targets(terminator: &Terminator) -> [Option<BlockId>; 2] {
    match terminator {
        Terminator::Goto(to) | Terminator::Suspend { resume: to, .. } => [Some(*to), None],
        Terminator::Branch { then_, else_, .. } => [Some(*then_), Some(*else_)],
        _ => [None, None],
    }
}

fn same_regions(body: &Body, from: BlockId, to: BlockId) -> bool {
    body.regions.iter().all(|region| {
        let start = ordinal(region.try_start);
        let end = start + region.try_len;
        let contains = |id| (start..end).contains(&ordinal(id));
        contains(from) == contains(to)
    })
}

// 每次最多读取实际块数；即使规范落点已固定，仍继续检查后续实际纯空链。
fn target(
    body: &Body,
    mut id: BlockId,
    anchors: Option<&FxHashSet<BlockId>>,
) -> Result<BlockId, String> {
    let mut seen = FxHashSet::default();
    let mut canonical = None;
    loop {
        check_id(body, id)?;
        if !seen.insert(id) {
            return Err("高层空跳转形成环".into());
        }
        let block = &body.blocks[id];
        if block.stmts.is_empty()
            && let Terminator::Goto(next) = block.terminator
        {
            check_id(body, next)?;
            if canonical.is_none()
                && anchors
                    .is_some_and(|anchors| anchors.contains(&id) || !same_regions(body, id, next))
            {
                canonical = Some(id);
            }
            id = next;
        } else {
            return Ok(canonical.unwrap_or(id));
        }
    }
}

/// 必须先于check_stack/reachable的直接索引；两图分别检查，包括未选中的常量分支。
pub(super) fn check(body: &Body) -> Result<(), String> {
    check_id(body, body.entry)?;
    for region in &body.regions {
        check_id(body, region.try_start)?;
        check_id(body, region.handler)?;
        let end = ordinal(region.try_start).checked_add(region.try_len);
        if region.try_len == 0 || !end.is_some_and(|end| end <= body.blocks.len()) {
            return Err("高层Region实际区间不合法".into());
        }
    }
    for (_, block) in body.blocks.iter() {
        for id in targets(&block.terminator).into_iter().flatten() {
            target(body, id, None)?;
        }
    }
    Ok(())
}

impl Edges {
    pub fn capture(body: &Body) -> Result<Self, String> {
        check(body)?;
        let mut anchors = FxHashSet::default();
        anchors.insert(body.entry);
        for (id, block) in body.blocks.iter() {
            if !block.stmts.is_empty() {
                anchors.insert(id);
            }
            if let Terminator::Suspend { resume, .. } = block.terminator {
                anchors.insert(resume);
            }
        }
        for region in &body.regions {
            anchors.insert(region.try_start);
            anchors.insert(region.handler);
            let end = ordinal(region.try_start) + region.try_len;
            // exclusive-end等于arena长度时只是区间哨兵，不能捏造一个真实块。
            if end < body.blocks.len() {
                anchors.insert(BlockId::from_raw((end as u32).into()));
            }
        }
        let mut result = Self {
            anchors,
            canonical: vec![],
        };
        result.canonical = body
            .blocks
            .iter()
            .map(|(_, block)| result.normalize(body, &block.terminator))
            .collect::<Result<_, _>>()?;
        Ok(result)
    }

    pub fn matches(
        &self,
        body: &Body,
        block: BlockId,
        terminator: &Terminator,
    ) -> Result<bool, String> {
        Ok(self.canonical.get(ordinal(block)) == Some(&self.normalize(body, terminator)?))
    }

    fn normalize(&self, body: &Body, terminator: &Terminator) -> Result<Terminator, String> {
        let mut result = terminator.clone();
        if let Terminator::Branch {
            cond: Operand::Const(Constant::Bool(value)),
            then_,
            else_,
        } = result
        {
            result = Terminator::Goto(if value { then_ } else { else_ });
        }
        match &mut result {
            Terminator::Goto(to) => *to = target(body, *to, Some(&self.anchors))?,
            Terminator::Branch { then_, else_, .. } => {
                *then_ = target(body, *then_, Some(&self.anchors))?;
                *else_ = target(body, *else_, Some(&self.anchors))?;
            }
            // SimplifyCfg不改Suspend；不能借U→旧resume把新恢复入口洗成同一落点。
            _ => {}
        }
        Ok(result)
    }
}

#[cfg(test)]
#[path = "generator_product_edges_tests.rs"]
mod tests;
