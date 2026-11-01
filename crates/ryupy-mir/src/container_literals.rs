//! 容器字面量的 MIR 构建，保留元素求值与展开顺序。

use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_hir::{ExprId, ExprKind};

impl ExprBuilder<'_> {
    /// 容器字面量：没有星号时一次构造；有星号时先建空容器再逐个追加。
    pub(crate) fn build_sequence_literal(
        &mut self,
        items: &[ExprId],
        callee: RuntimeFn,
    ) -> Operand {
        let has_star = items.iter().any(|id| {
            matches!(
                self.hir.functions[self.current_hir_fn].body.exprs[*id].kind,
                ExprKind::Starred(_)
            )
        });
        if !has_star {
            let mut args = Vec::new();
            for &item in items {
                let op = self.build_expr(item);
                let ty = self.ty_of(item);
                args.push(self.coerce(op, &ty, &MirType::Dynamic));
            }
            return self.call_with_args(callee, vec![], args);
        }

        if callee == RuntimeFn::MakeSet {
            return self.build_starred_set(items);
        }
        // 含 `*x`：先建空列表，再 append / extend
        let container = self.call_with_args(RuntimeFn::MakeList, vec![], vec![]);
        for &item in items {
            let starred = match &self.hir.functions[self.current_hir_fn].body.exprs[item].kind {
                ExprKind::Starred(inner) => Some(*inner),
                _ => None,
            };
            match starred {
                Some(inner) => {
                    let value = self.build_expr(inner);
                    let ty = self.ty_of(inner);
                    let boxed = self.coerce(value, &ty, &MirType::Dynamic);
                    self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ListExtend),
                        args: vec![container.clone(), boxed],
                    }));
                }
                None => {
                    let value = self.build_expr(item);
                    let ty = self.ty_of(item);
                    let boxed = self.coerce(value, &ty, &MirType::Dynamic);
                    self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::ListAppend),
                        args: vec![container.clone(), boxed],
                    }));
                }
            }
        }
        if callee == RuntimeFn::MakeTuple {
            // tuple 只在展开结束后冻结；绝不能把可变构建列表作为结果返回。
            self.runtime_call(
                RuntimeFn::DynBuiltin,
                vec![
                    Operand::Const(Constant::Str("tuple".into())),
                    Operand::Array(vec![container]),
                    Operand::Array(Vec::new()),
                    Operand::Array(Vec::new()),
                ],
                MirType::Dynamic,
            )
        } else {
            container
        }
    }

    /// set 首星号前先求全部前缀元素再建集合，后续插入/展开按源码顺序立即发生。
    fn build_starred_set(&mut self, items: &[ExprId]) -> Operand {
        let first = items
            .iter()
            .position(|item| {
                matches!(
                    self.hir.functions[self.current_hir_fn].body.exprs[*item].kind,
                    ExprKind::Starred(_)
                )
            })
            .unwrap_or(items.len());
        let mut prefix = Vec::new();
        for &item in &items[..first] {
            let value = self.build_expr(item);
            let ty = self.ty_of(item);
            prefix.push(self.coerce(value, &ty, &MirType::Dynamic));
        }
        let container = self.call_with_args(RuntimeFn::MakeSet, Vec::new(), prefix);
        for &item in &items[first..] {
            let (expression, operation) =
                match self.hir.functions[self.current_hir_fn].body.exprs[item].kind {
                    ExprKind::Starred(inner) => (inner, RuntimeFn::SetExtend),
                    _ => (item, RuntimeFn::SetAdd),
                };
            let value = self.build_expr(expression);
            let ty = self.ty_of(expression);
            let value = self.coerce(value, &ty, &MirType::Dynamic);
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(operation),
                args: vec![container.clone(), value],
            }));
        }
        container
    }

    /// 字典字面量：空字典 + 逐项写入。
    pub(crate) fn build_dict_literal(
        &mut self,
        pairs: &[(ExprId, ExprId)],
        _expr_id: ExprId,
    ) -> Operand {
        // MakeDict 没有数组形参，走普通调用
        let dict_dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: dict_dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::MakeDict),
                args: Vec::new(),
            },
        });
        let dict = Operand::Local(dict_dest);
        for &(key, value) in pairs {
            let key_op = self.build_expr(key);
            let key_ty = self.ty_of(key);
            let key_boxed = self.coerce(key_op, &key_ty, &MirType::Dynamic);
            let val_op = self.build_expr(value);
            let val_ty = self.ty_of(value);
            let val_boxed = self.coerce(val_op, &val_ty, &MirType::Dynamic);
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::SetItem),
                args: vec![dict.clone(), key_boxed, val_boxed],
            }));
        }
        dict
    }
}
