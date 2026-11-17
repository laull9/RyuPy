//! 生成器源码handler的typed选择与帧核验；顺序游标只消费真实邻接操作，不猜测旧值来源。
use crate::generator_product_cursor::{Cursor, block};
use crate::generator_source_handlers::Check;
use crate::*;
use rustc_hash::FxHashSet;

impl Check<'_> {
    pub(super) fn typed(
        &mut self,
        mut matched: BlockId,
        next: BlockId,
        caught: LocalId,
        targets: &[Operand],
        unconditional: bool,
    ) -> Result<BlockId, String> {
        if unconditional {
            return if targets.is_empty() {
                Ok(matched)
            } else {
                Err("无条件route混入类型分派".into())
            };
        }
        if targets.is_empty() {
            return Err("typed route没有真实类型目标".into());
        }
        let mut chosen = None;
        for target in targets {
            if !self.seen.insert(matched) {
                return Err("typed路由重复或形成环".into());
            }
            let value = self.body(matched)?;
            let mut cursor = Cursor::new(&value.stmts, self.original + 1);
            let ty = match target {
                Operand::Global(_) => cursor.use_value(target)?,
                Operand::Const(Constant::Str(_)) => {
                    cursor.assign_call(RuntimeFn::TypeObject, std::slice::from_ref(target))?
                }
                _ => return Err("typed路由类型来源非法".into()),
            };
            self.private(ty, MirType::Dynamic)?;
            let answer = cursor.assign_call(
                RuntimeFn::DynBuiltin,
                &[
                    Operand::Const(Constant::Str("isinstance".into())),
                    Operand::Array(vec![Operand::Local(caught), Operand::Local(ty)]),
                    Operand::Array(vec![]),
                    Operand::Array(vec![]),
                ],
            )?;
            self.private(answer, MirType::Dynamic)?;
            let condition = cursor.assign_call(RuntimeFn::DynTruthy, &[Operand::Local(answer)])?;
            self.private(condition, MirType::Bool)?;
            cursor.end()?;
            let Terminator::Branch {
                cond: Operand::Local(actual),
                then_,
                else_,
            } = value.terminator
            else {
                return Err("typed匹配结果没有实际分支".into());
            };
            if condition != actual || chosen.is_some_and(|id| id != then_) {
                return Err("typed分派选择目标不一致".into());
            }
            chosen = Some(then_);
            matched = else_;
        }
        if matched != next {
            return Err("typed all-miss绕过下一个窗口".into());
        }
        chosen.ok_or_else(|| "typed分派没有目标".into())
    }
    pub(super) fn exits(
        &mut self,
        layout: &crate::trace_lowering::GeneratorLayout,
        types: &[MirType],
        protected: usize,
    ) -> Result<(), String> {
        let mut result = None;
        let spill = types
            .iter()
            .map(|ty| if *ty == MirType::Dynamic { 1 } else { 2 })
            .sum::<usize>();
        for (id, pc) in &layout.exit_pcs {
            let value = self.body(*id)?;
            if value.stmts.is_empty()
                && value.terminator == Terminator::Unreachable
                && !self.reachable.contains(id)
            {
                continue;
            }
            let suffix = 3 + if *pc >= 0 { spill } else { 0 };
            let start = value
                .stmts
                .len()
                .checked_sub(suffix)
                .ok_or("挂起出口缺少保存前言")?;
            let mut cursor = Cursor::new(&value.stmts[start..], self.original + 1);
            if *pc >= 0 {
                cursor.spill(self.frame, types)?;
                if !layout.pc_targets.iter().any(|(label, _)| label == pc) {
                    return Err("挂起PC没有实际恢复入口".into());
                }
            } else if *pc != -1 {
                return Err("完成PC不是终态".into());
            }
            cursor.pc(self.frame, Operand::Const(Constant::I64(*pc)))?;
            let (dest, operation) = cursor.assignment()?;
            self.private(dest, MirType::Dynamic)?;
            if !matches!(operation, Rvalue::Use(_))
                || result.is_some_and(|old| old != dest)
                || value.terminator != Terminator::Goto(block(protected))
            {
                return Err("挂起/完成没有共同保护区外出口".into());
            }
            result = Some(dest);
            self.role_writes.insert((*id, value.stmts.len() - 1));
            cursor.end()?;
            self.mark_frame_ops(*id, start, value.stmts.len());
        }
        let result = result.ok_or("生成器没有完成或挂起出口")?;
        let value = self.body(block(protected))?;
        if !value.stmts.is_empty()
            || value.terminator != Terminator::Return(Some(Operand::Local(result)))
        {
            return Err("保护区外未返回实际保存值".into());
        }
        self.result = Some(result);
        if [self.frame, self.active, self.caught, self.pc, self.state].contains(&result) {
            return Err("出口值与路由私有角色别名".into());
        }
        Ok(())
    }
    pub(super) fn catch_reads(
        &mut self,
        layout: &crate::trace_lowering::GeneratorLayout,
    ) -> Result<(), String> {
        let mut actual = FxHashSet::default();
        for id in &layout.original_blocks {
            for (index, stmt) in self.body(*id)?.stmts.iter().enumerate() {
                if let Statement::Assign {
                    dest,
                    value:
                        Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::ArrayGet),
                            args,
                        },
                } = stmt
                    && matches!(args.as_slice(),[Operand::Local(frame),Operand::Const(Constant::I64(3))]if *frame==self.frame)
                {
                    if !actual.insert((*id, index, *dest)) {
                        return Err("异常槽重复读取".into());
                    }
                    let mut cursor =
                        Cursor::new(&self.body(*id)?.stmts[index + 1..], self.original + 1);
                    cursor.slot(self.frame, 3, Operand::Const(Constant::None))?;
                    self.mark_frame_ops(*id, index, index + 2);
                }
            }
        }
        if actual.len() != layout.catch_reads.len()
            || layout
                .catch_reads
                .iter()
                .any(|entry| !actual.contains(entry))
        {
            return Err("逻辑Catch读取未完整对应并清待接槽".into());
        }
        Ok(())
    }
    pub(super) fn private_roles(&self) -> Result<(), String> {
        let roles = [
            self.frame,
            self.active,
            self.caught,
            self.pc,
            self.state,
            self.result.ok_or("缺少出口值角色")?,
        ];
        if roles.into_iter().collect::<FxHashSet<_>>().len() != roles.len() {
            return Err("frame/active/caught/handlerPC角色别名".into());
        }
        for (id, body) in self.function.body.blocks.iter() {
            for (index, stmt) in body.stmts.iter().enumerate() {
                let mut reads = FxHashSet::default();
                match stmt {
                    Statement::Assign { value, .. } | Statement::Eval(value) => {
                        crate::local_access::read_value(value, &mut reads)
                    }
                    Statement::SetField { obj, value, .. } => {
                        crate::local_access::read_operand(obj, &mut reads);
                        crate::local_access::read_operand(value, &mut reads);
                    }
                    Statement::SetGlobal { value, .. } => {
                        crate::local_access::read_operand(value, &mut reads)
                    }
                    Statement::Line(_) => {}
                }
                if reads.contains(&self.frame) && !self.frame_ops.contains(&(id, index)) {
                    let allowed = matches!(stmt,Statement::Assign{value:Rvalue::Call{callee:Callee::Runtime(RuntimeFn::GeneratorInput|RuntimeFn::GeneratorDelegateStep|RuntimeFn::GeneratorTracebackReceive),args},..}|Statement::Eval(Rvalue::Call{callee:Callee::Runtime(RuntimeFn::GeneratorInput|RuntimeFn::GeneratorDelegateStep|RuntimeFn::GeneratorTracebackReceive),args})
                        if matches!(args.first(),Some(Operand::Local(frame))if *frame==self.frame) && !args.iter().skip(1).any(|value|{let mut reads=FxHashSet::default();crate::local_access::read_operand(value,&mut reads);reads.contains(&self.frame)}));
                    if !allowed {
                        return Err("私有frame逃出已核保存/恢复及控制协议".into());
                    }
                }
                if let Statement::Assign { dest, .. } = stmt
                    && roles.contains(dest)
                    && !self.role_writes.contains(&(id, index))
                {
                    return Err("私有路由角色被额外覆盖".into());
                }
            }
            let operands: Vec<&Operand> = match &body.terminator {
                Terminator::Branch { cond, .. } => vec![cond],
                Terminator::Return(value) => value.iter().collect(),
                Terminator::Raise { value, cause } => value.iter().chain(cause).collect(),
                Terminator::Suspend { value, .. } => vec![value],
                _ => vec![],
            };
            for value in operands {
                let mut reads = FxHashSet::default();
                crate::local_access::read_operand(value, &mut reads);
                if reads.contains(&self.frame) {
                    return Err("私有frame泄漏到控制返回值".into());
                }
            }
        }
        Ok(())
    }
}
