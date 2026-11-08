//! 每次分析独立构建不可变三表；保留原线性查找的首匹配与None语义。
use super::*;

pub(super) struct LayoutIndex {
    dispatch: Vec<Option<i64>>,
    exit: Vec<Option<i64>>,
    retry: Vec<Option<i64>>,
}

impl LayoutIndex {
    pub(super) fn build(
        body: &Body,
        layout: &GeneratorLayout,
        budget: &mut Budget,
    ) -> Result<Self, String> {
        if body.blocks.len() > 2048 {
            return Err("frame 块数量超限".into());
        }
        let mut result = Self {
            dispatch: Vec::new(),
            exit: Vec::new(),
            retry: Vec::new(),
        };
        for (block, _) in body.blocks.iter() {
            budget.work(3)?;
            let mut dispatch = None;
            for (pc, target) in &layout.pc_targets {
                budget.work(1)?;
                if *target == block {
                    dispatch = Some(*pc);
                    break;
                }
            }
            let mut exit = None;
            for (target, pc) in &layout.exit_pcs {
                budget.work(1)?;
                if *target == block {
                    exit = Some(*pc);
                    break;
                }
            }
            let mut original = None;
            for (position, target) in layout.original_blocks.iter().enumerate() {
                budget.work(1)?;
                if *target == block {
                    original = Some(position);
                    break;
                }
            }
            let mut retry = None;
            if let Some(old) = original {
                for route in &layout.exception_routes {
                    budget.work(1)?;
                    if old as i64 >= route.start
                        && (old as i64) < route.start + route.length as i64
                    {
                        retry = Some(route.handler_pc);
                        break;
                    }
                }
            }
            // 三个真实槽写也收费；预算失败不向Engine发布未完成表。
            budget.work(3)?;
            result.dispatch.push(dispatch);
            result.exit.push(exit);
            result.retry.push(retry);
        }
        Ok(result)
    }

    fn lookup(
        table: &[Option<i64>],
        block: BlockId,
        budget: &mut Budget,
    ) -> Result<Option<i64>, String> {
        budget.work(1)?;
        table
            .get(block.into_raw().into_u32() as usize)
            .copied()
            .ok_or_else(|| "CFG 目标缺失".into())
    }

    pub(super) fn dispatch(
        &self,
        block: BlockId,
        budget: &mut Budget,
    ) -> Result<Option<i64>, String> {
        Self::lookup(&self.dispatch, block, budget)
    }

    pub(super) fn exit(&self, block: BlockId, budget: &mut Budget) -> Result<Option<i64>, String> {
        Self::lookup(&self.exit, block, budget)
    }

    pub(super) fn retry(&self, block: BlockId, budget: &mut Budget) -> Result<Option<i64>, String> {
        Self::lookup(&self.retry, block, budget)
    }
}
