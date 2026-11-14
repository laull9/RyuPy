//! 末端结构检查的顺序游标：只认可实际相邻定义，不能越过后续写回溯旧 Box/Use。
use crate::*;

pub(super) fn same<T: PartialEq + ?Sized>(left: &T, right: &T) -> bool {
    left == right
}
pub(super) fn number(id: BlockId) -> usize {
    id.into_raw().into_u32() as usize
}
pub(super) fn local(number: usize) -> LocalId {
    la_arena::Idx::from_raw(la_arena::RawIdx::from_u32(number as u32))
}
pub(super) fn block(number: usize) -> BlockId {
    la_arena::Idx::from_raw(la_arena::RawIdx::from_u32(number as u32))
}

pub(super) struct Cursor<'a> {
    pub statements: &'a [Statement],
    pub position: usize,
    // 工厂从原locals末尾开始；恢复体还必须越过独立frame参数。
    minimum_box_local: usize,
}
impl<'a> Cursor<'a> {
    pub fn new(statements: &'a [Statement], minimum_box_local: usize) -> Self {
        Self {
            statements,
            position: 0,
            minimum_box_local,
        }
    }
    pub fn next(&mut self) -> Result<&'a Statement, String> {
        let value = self
            .statements
            .get(self.position)
            .ok_or("末端模板缺少实际操作")?;
        self.position += 1;
        Ok(value)
    }
    pub fn end(&self) -> Result<(), String> {
        if self.position == self.statements.len() {
            Ok(())
        } else {
            Err("末端模板含额外操作/隐藏回调".into())
        }
    }
    pub fn assignment(&mut self) -> Result<(LocalId, &'a Rvalue), String> {
        match self.next()? {
            Statement::Assign { dest, value } => Ok((*dest, value)),
            _ => Err("末端模板缺少赋值".into()),
        }
    }
    pub fn use_value(&mut self, expected: &Operand) -> Result<LocalId, String> {
        let (dest, value) = self.assignment()?;
        if matches!(value, Rvalue::Use(value) if same(value, expected)) {
            Ok(dest)
        } else {
            Err("末端模板使用了错误的当前值".into())
        }
    }
    pub fn assign_call(&mut self, runtime: RuntimeFn, args: &[Operand]) -> Result<LocalId, String> {
        let (dest, value) = self.assignment()?;
        if matches!(value, Rvalue::Call { callee: Callee::Runtime(kind), args: actual }
            if *kind == runtime && actual.as_slice() == args)
        {
            Ok(dest)
        } else {
            Err(format!("末端模板的 {runtime:?} 实参或helper不符"))
        }
    }
    pub fn eval_call(&mut self, runtime: RuntimeFn, args: &[Operand]) -> Result<(), String> {
        if matches!(self.next()?, Statement::Eval(Rvalue::Call { callee: Callee::Runtime(kind), args: actual })
            if *kind == runtime && actual.as_slice() == args)
        {
            Ok(())
        } else {
            Err(format!("末端模板的 {runtime:?} 副作用不符"))
        }
    }
    pub fn boxed(&mut self, expected: Operand, ty: &MirType) -> Result<Operand, String> {
        if *ty == MirType::Dynamic {
            return Ok(expected);
        }
        let (dest, value) = self.assignment()?;
        if (dest.into_raw().into_u32() as usize) < self.minimum_box_local {
            return Err("模板Box覆盖了原局部/参数或私有frame".into());
        }
        if matches!(value, Rvalue::Box { value, from } if same(value, &expected) && from == ty) {
            Ok(Operand::Local(dest))
        } else {
            Err("保存槽缺少紧邻当前局部的类型相容 Box".into())
        }
    }
    pub fn slot(&mut self, frame: LocalId, index: usize, value: Operand) -> Result<(), String> {
        self.eval_call(
            RuntimeFn::ArraySet,
            &[
                Operand::Local(frame),
                Operand::Const(Constant::I64(index as i64)),
                value,
            ],
        )
    }
    pub fn spill(&mut self, frame: LocalId, types: &[MirType]) -> Result<(), String> {
        for (index, ty) in types.iter().enumerate() {
            let value = self.boxed(Operand::Local(local(index)), ty)?;
            self.slot(frame, index + 4, value)?;
        }
        Ok(())
    }
    pub fn pc(&mut self, frame: LocalId, value: Operand) -> Result<(), String> {
        let value = self.boxed(value, &MirType::I64)?;
        self.slot(frame, 0, value)
    }
}
