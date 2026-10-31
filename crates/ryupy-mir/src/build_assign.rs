//! 赋值目标与删除语句的 MIR 构建（M2.3）。
//!
//! 覆盖三类目标：
//! - `obj[key] = value`：运行时 `PyOps.SetItem`；
//! - `a, b = ...` / `first, *middle, last = ...`：先取长度校验，再逐位置取下标，
//!   星号位置用切片一次性取中间部分；
//! - `del obj[key]`：运行时 `PyOps.DelItem`。
//!
//! 序列解包的长度校验放在**取元素之前**：CPython 对 `a, b = [1]` 抛
//! `ValueError: not enough values to unpack`，而不是先做一半再报错。

use ryupy_hir::{Binding, ExprId, SequenceItem};

use crate::build::FunctionBuilder;
use crate::mir::{
    Callee, Constant, LocalId as MirLocalId, MirType, Operand, RuntimeFn, Rvalue, Statement,
};

impl FunctionBuilder<'_> {
    /// `obj[key] = value`。
    pub(crate) fn build_subscript_assign(&mut self, base: ExprId, index: ExprId, value: ExprId) {
        let val_op = self.expr_builder().build_expr(value);
        let val_ty = self.expr_builder().ty_of(value);
        let val = self
            .expr_builder()
            .coerce(val_op, &val_ty, &MirType::Dynamic);
        let base_op = self.expr_builder().build_expr(base);
        let base_ty = self.expr_builder().ty_of(base);
        let target = self
            .expr_builder()
            .coerce(base_op, &base_ty, &MirType::Dynamic);
        let key_op = self.expr_builder().build_expr(index);
        let key_ty = self.expr_builder().ty_of(index);
        let key = self
            .expr_builder()
            .coerce(key_op, &key_ty, &MirType::Dynamic);
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::SetItem),
            args: vec![target, key, val],
        }));
    }

    /// `a, b = value` / `first, *middle, last = value`。
    pub(crate) fn build_sequence_assign(&mut self, items: &[SequenceItem], value: ExprId) {
        let seq_op = self.expr_builder().build_expr(value);
        let seq_ty = self.expr_builder().ty_of(value);
        let seq = self
            .expr_builder()
            .coerce(seq_op, &seq_ty, &MirType::Dynamic);

        let star_index = items
            .iter()
            .position(|item| matches!(item, SequenceItem::Rest(_)));
        let before = star_index.unwrap_or(items.len());
        let after = items.len() - before - if star_index.is_some() { 1 } else { 0 };

        // 长度：`len(seq)`，再校验数量。星号形态只要求下限，非星号形态必须精确匹配
        // （多出来的值也要报错，否则 `a, b = [1, 2, 3]` 会静默丢掉第三个元素）。
        let length = self.alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: length,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::Len),
                args: vec![seq.clone()],
            },
        });
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::UnpackCheck),
            args: vec![
                Operand::Local(length),
                Operand::Const(Constant::I64((before + after) as i64)),
                Operand::Const(Constant::Bool(star_index.is_some())),
            ],
        }));

        // 前半部分：正向下标
        for (i, item) in items.iter().take(before).enumerate() {
            let binding = sequence_binding(item);
            let index = self.box_index(Constant::I64(i as i64));
            self.emit_get_item(seq.clone(), index, binding);
        }

        // 星号：中间段用切片，负数终点表示「从末尾往前数」
        if let Some(star) = star_index {
            let binding = sequence_binding(&items[star]);
            let start = self.box_index(Constant::I64(before as i64));
            let stop = if after == 0 {
                self.expr_builder()
                    .runtime_call(RuntimeFn::DynNone, Vec::new(), MirType::Dynamic)
            } else {
                self.box_index(Constant::I64(-(after as i64)))
            };
            let none =
                self.expr_builder()
                    .runtime_call(RuntimeFn::DynNone, Vec::new(), MirType::Dynamic);
            let slice_dest = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: slice_dest,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::MakeSlice),
                    args: vec![start, stop, none],
                },
            });
            let slice = Operand::Local(slice_dest);
            self.emit_get_item(seq.clone(), slice, binding);
        }

        // 后半部分：从长度倒推下标，避免再分配一个临时列表
        for (offset, item) in items
            .iter()
            .skip(before + usize::from(star_index.is_some()))
            .enumerate()
        {
            let binding = sequence_binding(item);
            let dest_local = self.alloc_temp(MirType::I64);
            self.push_stmt(Statement::Assign {
                dest: dest_local,
                value: Rvalue::Binary {
                    op: crate::mir::BinOp::Sub,
                    lhs: Operand::Local(length),
                    rhs: Operand::Const(Constant::I64((after - offset) as i64)),
                },
            });
            let index = self.box_i64_local(dest_local);
            self.emit_get_item(seq.clone(), index, binding);
        }
    }

    /// `del obj[key]` / `del obj.attr`。
    pub(crate) fn build_delete(&mut self, target: ExprId) {
        let kind = {
            let hir_body = &self.hir.functions[self.current_hir_fn].body;
            hir_body.exprs[target].kind.clone()
        };
        match kind {
            ryupy_hir::ExprKind::Subscript { base, index } => {
                let base_op = self.expr_builder().build_expr(base);
                let base_ty = self.expr_builder().ty_of(base);
                let obj = self
                    .expr_builder()
                    .coerce(base_op, &base_ty, &MirType::Dynamic);
                let key_op = self.expr_builder().build_expr(index);
                let key_ty = self.expr_builder().ty_of(index);
                let key = self
                    .expr_builder()
                    .coerce(key_op, &key_ty, &MirType::Dynamic);
                self.push_stmt(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DelItem),
                    args: vec![obj, key],
                }));
            }
            ryupy_hir::ExprKind::Attribute { base, name } => {
                let base_op = self.expr_builder().build_expr(base);
                let base_ty = self.expr_builder().ty_of(base);
                let obj = self
                    .expr_builder()
                    .coerce(base_op, &base_ty, &MirType::Dynamic);
                self.push_stmt(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::DelAttr),
                    args: vec![obj, Operand::Const(Constant::Str(name.as_str().to_owned()))],
                }));
            }
            _ => {}
        }
    }

    /// 把整数常量装箱成动态下标。
    fn box_index(&mut self, constant: Constant) -> Operand {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Box {
                value: Operand::Const(constant),
                from: MirType::I64,
            },
        });
        Operand::Local(dest)
    }

    /// 把 i64 局部变量装箱成动态下标。
    fn box_i64_local(&mut self, local: MirLocalId) -> Operand {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Box {
                value: Operand::Local(local),
                from: MirType::I64,
            },
        });
        Operand::Local(dest)
    }

    /// 取下标并写进目标（局部变量或模块槽位）。解包出来的元素都是动态值。
    fn emit_get_item(&mut self, seq: Operand, index: Operand, binding: Binding) {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::GetItem),
                args: vec![seq, index],
            },
        });
        self.store_binding(binding, Operand::Local(dest), &MirType::Dynamic);
    }

    /// 记录解包目标已赋值（供确定赋值分析使用）。
    pub(crate) fn mark_sequence_targets(&mut self, items: &[SequenceItem]) {
        for item in items {
            let binding = match item {
                SequenceItem::One(binding) | SequenceItem::Rest(binding) => *binding,
            };
            if let Binding::Local(local) = binding {
                self.assigned_locals.insert(local);
            }
        }
    }
}

/// 序列解包目标的绑定。
fn sequence_binding(item: &SequenceItem) -> Binding {
    match item {
        SequenceItem::One(binding) | SequenceItem::Rest(binding) => *binding,
    }
}
