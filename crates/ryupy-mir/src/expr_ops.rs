//! 运算与比较的 MIR 降级（[`ExprBuilder`] 的运算侧方法）。
//!
//! 静态路径：混合算术插入 `Cast`、`//` `%` `**` 改写为运行时调用；
//! 动态路径：全部交给 `PyOps` / PIC。`and` / `or` 与条件表达式
//! 用基本块短路，返回操作数本身（Python 语义）。

use ryupy_hir::{BinOp as HirBinOp, CmpOp, ExprId, UnaryOp as HirUnaryOp};
use ryupy_ty::Ty;

use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{
    BasicBlock, BinOp, Callee, CastKind, Constant, DynOp, LocalId as MirLocalId, MirType, Operand,
    RuntimeFn, Rvalue, Statement, UnaryOp,
};

impl<'a> ExprBuilder<'a> {
    /// 动态二元运算。
    pub(crate) fn dyn_binary(
        &mut self,
        op: DynOp,
        lhs: Operand,
        rhs: Operand,
        inplace: bool,
    ) -> Operand {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::DynBinary {
                op,
                lhs,
                rhs,
                inplace,
            },
        });
        Operand::Local(dest)
    }

    // ───────────────────────── 运算 ─────────────────────────

    pub(crate) fn build_binary(
        &mut self,
        op: HirBinOp,
        lhs: ExprId,
        rhs: ExprId,
        inplace: bool,
        expr_id: ExprId,
    ) -> Operand {
        let mut lhs_op = self.build_expr(lhs);
        let mut rhs_op = self.build_expr(rhs);

        let lhs_ty = self.ty_of(lhs);
        let rhs_ty = self.ty_of(rhs);
        let expr_ty = self.ty_of(expr_id);

        if expr_ty.is_dynamic() {
            // 动态路径：操作数此时都是装箱值，直接交给运行时
            lhs_op = self.coerce(lhs_op, &lhs_ty, &MirType::Dynamic);
            rhs_op = self.coerce(rhs_op, &rhs_ty, &MirType::Dynamic);
            return self.dyn_binary(dyn_bin_op(op), lhs_op, rhs_op, inplace);
        }

        // 隐式提升：若两端类型分别为 int 和 float，将 int 一端提升为 float
        if lhs_ty == Ty::Int && rhs_ty == Ty::Float {
            lhs_op = self.cast_i64_to_f64(lhs_op);
        } else if lhs_ty == Ty::Float && rhs_ty == Ty::Int {
            rhs_op = self.cast_i64_to_f64(rhs_op);
        }

        // 字符串拼接
        if op == HirBinOp::Add && lhs_ty == Ty::Str && rhs_ty == Ty::Str {
            return self.call_runtime_binary(RuntimeFn::StrConcat, lhs_op, rhs_op, MirType::Str);
        }

        // Python 语义改写：向负无穷取整除法、取模、乘方等落入 RuntimeFn
        match op {
            HirBinOp::FloorDiv => {
                let rt_fn = if expr_ty == Ty::Float {
                    RuntimeFn::FloorDivF64
                } else {
                    RuntimeFn::FloorDivI64
                };
                self.call_runtime_binary(rt_fn, lhs_op, rhs_op, ty_to_mir(&expr_ty))
            }
            HirBinOp::Mod => {
                let rt_fn = if expr_ty == Ty::Float {
                    RuntimeFn::ModF64
                } else {
                    RuntimeFn::ModI64
                };
                self.call_runtime_binary(rt_fn, lhs_op, rhs_op, ty_to_mir(&expr_ty))
            }
            HirBinOp::Pow => {
                let rt_fn = if expr_ty == Ty::Float {
                    RuntimeFn::PowF64
                } else {
                    RuntimeFn::PowI64
                };
                self.call_runtime_binary(rt_fn, lhs_op, rhs_op, ty_to_mir(&expr_ty))
            }
            HirBinOp::TrueDiv => {
                if lhs_ty == Ty::Int {
                    lhs_op = self.cast_i64_to_f64(lhs_op);
                }
                if rhs_ty == Ty::Int {
                    rhs_op = self.cast_i64_to_f64(rhs_op);
                }
                // 除数为 0 时 Python 抛 ZeroDivisionError，CIL 的 `div` 只会得到 inf/NaN，
                // 因此真除法统一走运行时检查（语义陷阱 #12）。
                self.call_runtime_binary(RuntimeFn::TrueDivF64, lhs_op, rhs_op, MirType::F64)
            }
            HirBinOp::LShift => {
                // CIL 的 shl 把移位量按 63 取模（1 << 100 会算成 1 << 36）；
                // 运行时入口负责「溢出报 OverflowError」与正确的大移位语义
                self.call_runtime_binary(RuntimeFn::ShlI64, lhs_op, rhs_op, MirType::I64)
            }
            HirBinOp::RShift => {
                // 移位量 ≥ 64 时 Python 的结果是 0 / -1，CIL 的 shr 同样会取模
                self.call_runtime_binary(RuntimeFn::ShrI64, lhs_op, rhs_op, MirType::I64)
            }
            _ => {
                let mir_op = match op {
                    HirBinOp::Add => BinOp::Add,
                    HirBinOp::Sub => BinOp::Sub,
                    HirBinOp::Mul => BinOp::Mul,
                    HirBinOp::BitAnd => BinOp::BitAnd,
                    HirBinOp::BitOr => BinOp::BitOr,
                    HirBinOp::BitXor => BinOp::BitXor,
                    // 移位已在上面改写成运行时调用：CIL 指令的移位量会取模
                    _ => BinOp::Add,
                };
                let dest = self.alloc_temp(ty_to_mir(&expr_ty));
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Binary {
                        op: mir_op,
                        lhs: lhs_op,
                        rhs: rhs_op,
                    },
                });
                Operand::Local(dest)
            }
        }
    }

    /// 身份与成员比较固定返回 bool；调用方明确选择是否装箱为动态表达式结果。
    fn membership_values(
        &mut self,
        op: CmpOp,
        lhs: Operand,
        rhs: Operand,
        left: &Ty,
        right: &Ty,
        dynamic: bool,
    ) -> Operand {
        let lhs = self.coerce(lhs, left, &MirType::Dynamic);
        let rhs = self.coerce(rhs, right, &MirType::Dynamic);
        let runtime = if matches!(op, CmpOp::Is | CmpOp::IsNot) {
            RuntimeFn::IsSame
        } else {
            RuntimeFn::Contains
        };
        let args = if matches!(op, CmpOp::Is | CmpOp::IsNot) {
            vec![lhs, rhs]
        } else {
            vec![rhs, lhs]
        };
        let raw = self.runtime_call(runtime, args, MirType::Bool);
        let value = if matches!(op, CmpOp::NotIn | CmpOp::IsNot) {
            let dest = self.alloc_temp(MirType::Bool);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Unary {
                    op: UnaryOp::Not,
                    operand: raw,
                },
            });
            Operand::Local(dest)
        } else {
            raw
        };
        self.coerce(
            value,
            &Ty::Bool,
            &if dynamic {
                MirType::Dynamic
            } else {
                MirType::Bool
            },
        )
    }

    /// 把 CIL `bool` 装成动态值；静态上下文原样返回。
    pub(crate) fn bool_to_value(&mut self, op: Operand) -> Operand {
        if !self.is_dynamic_context() {
            return op;
        }
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Box {
                value: op,
                from: MirType::Bool,
            },
        });
        Operand::Local(dest)
    }

    pub(crate) fn cast_i64_to_f64(&mut self, op: Operand) -> Operand {
        let dest = self.alloc_temp(MirType::F64);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Cast {
                kind: CastKind::I64ToF64,
                operand: op,
            },
        });
        Operand::Local(dest)
    }

    pub(crate) fn build_unary(
        &mut self,
        op: HirUnaryOp,
        operand: ExprId,
        expr_id: ExprId,
    ) -> Operand {
        let op_val = self.build_expr(operand);
        let operand_ty = self.ty_of(operand);
        let expr_ty = self.ty_of(expr_id);

        if expr_ty.is_dynamic() || operand_ty.is_dynamic() {
            let value = self.coerce(op_val, &operand_ty, &MirType::Dynamic);
            if op == HirUnaryOp::Not {
                // `not x` 的结果是 CIL bool，需要时再按上下文装箱
                let cond = self.runtime_call(RuntimeFn::DynTruthy, vec![value], MirType::Bool);
                let dest = self.alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Unary {
                        op: UnaryOp::Not,
                        operand: cond,
                    },
                });
                return self.bool_to_value(Operand::Local(dest));
            }
            let dest = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::DynUnary {
                    op: dyn_unary_op(op),
                    operand: value,
                },
            });
            return Operand::Local(dest);
        }

        // 简单常量折叠：负整数与负浮点
        if op == HirUnaryOp::Neg {
            if let Operand::Const(Constant::I64(v)) = op_val {
                return Operand::Const(Constant::I64(-v));
            }
            if let Operand::Const(Constant::F64(v)) = op_val {
                return Operand::Const(Constant::F64(-v));
            }
        }
        let mir_op = match op {
            HirUnaryOp::Neg => UnaryOp::Neg,
            HirUnaryOp::Not => UnaryOp::Not,
            HirUnaryOp::Invert => UnaryOp::BitNot,
            HirUnaryOp::Pos => return op_val,
        };
        let dest = self.alloc_temp(ty_to_mir(&expr_ty));
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Unary {
                op: mir_op,
                operand: op_val,
            },
        });
        Operand::Local(dest)
    }

    pub(crate) fn build_compare(
        &mut self,
        op: CmpOp,
        lhs: ExprId,
        rhs: ExprId,
        expr_id: ExprId,
    ) -> Operand {
        let lhs_op = self.build_expr(lhs);
        let lhs_ty = self.ty_of(lhs);
        let rhs_op = self.build_expr(rhs);
        let rhs_ty = self.ty_of(rhs);
        self.compare_values(
            op,
            lhs_op,
            rhs_op,
            &lhs_ty,
            &rhs_ty,
            self.ty_of(expr_id).is_dynamic(),
        )
    }

    /// 二元比较与链式比较共享值级语义；操作数已经在各自求值点冻结。
    pub(crate) fn compare_values(
        &mut self,
        op: CmpOp,
        mut lhs_op: Operand,
        mut rhs_op: Operand,
        lhs_ty: &Ty,
        rhs_ty: &Ty,
        dynamic: bool,
    ) -> Operand {
        if matches!(op, CmpOp::In | CmpOp::NotIn | CmpOp::Is | CmpOp::IsNot) {
            return self.membership_values(op, lhs_op, rhs_op, lhs_ty, rhs_ty, dynamic);
        }
        if dynamic {
            lhs_op = self.coerce(lhs_op, lhs_ty, &MirType::Dynamic);
            rhs_op = self.coerce(rhs_op, rhs_ty, &MirType::Dynamic);
            // Python 富比较可以返回 ndarray 或用户对象；只在条件位置求真。
            return self.runtime_call(
                RuntimeFn::DynRichCompareValue,
                vec![
                    Operand::Const(Constant::I64(i64::from(dyn_cmp_op(op).code()))),
                    lhs_op,
                    rhs_op,
                ],
                MirType::Dynamic,
            );
        }

        if *lhs_ty == Ty::Int && *rhs_ty == Ty::Float {
            lhs_op = self.cast_i64_to_f64(lhs_op);
        } else if *lhs_ty == Ty::Float && *rhs_ty == Ty::Int {
            rhs_op = self.cast_i64_to_f64(rhs_op);
        }

        let mir_op = match op {
            CmpOp::Eq => BinOp::Eq,
            CmpOp::NotEq => BinOp::Ne,
            CmpOp::Lt => BinOp::Lt,
            CmpOp::LtE => BinOp::Le,
            CmpOp::Gt => BinOp::Gt,
            CmpOp::GtE => BinOp::Ge,
            // 上面已提前返回，这里只是让 match 完整
            CmpOp::In | CmpOp::NotIn | CmpOp::Is | CmpOp::IsNot => BinOp::Eq,
        };

        let dest = self.alloc_temp(MirType::Bool);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Binary {
                op: mir_op,
                lhs: lhs_op,
                rhs: rhs_op,
            },
        });
        Operand::Local(dest)
    }

    pub(crate) fn call_runtime_binary(
        &mut self,
        rt_fn: RuntimeFn,
        lhs: Operand,
        rhs: Operand,
        ret_ty: MirType,
    ) -> Operand {
        self.runtime_call(rt_fn, vec![lhs, rhs], ret_ty)
    }

    pub fn new_block(&mut self) -> crate::mir::BlockId {
        self.blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: crate::mir::Terminator::Unreachable,
        })
    }

    /// 把任意类型的值转成 CIL `bool`（分支条件用）。
    pub fn to_bool_operand(&mut self, op: Operand, ty: &Ty) -> Operand {
        match ty {
            Ty::Bool => op,
            Ty::Int => {
                let dest = self.alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Binary {
                        op: BinOp::Ne,
                        lhs: op,
                        rhs: Operand::Const(Constant::I64(0)),
                    },
                });
                Operand::Local(dest)
            }
            Ty::Float => {
                let dest = self.alloc_temp(MirType::Bool);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Binary {
                        op: BinOp::Ne,
                        lhs: op,
                        rhs: Operand::Const(Constant::F64(0.0)),
                    },
                });
                Operand::Local(dest)
            }
            Ty::Dynamic => self.runtime_call(RuntimeFn::DynTruthy, vec![op], MirType::Bool),
            // 其它静态类型在静态函数里已经降级，这里兜底按真值处理
            _ => {
                let boxed = self.coerce(op, ty, &MirType::Dynamic);
                self.runtime_call(RuntimeFn::DynTruthy, vec![boxed], MirType::Bool)
            }
        }
    }

    pub(crate) fn build_bool_op(
        &mut self,
        op: ryupy_hir::BoolOp,
        lhs: ExprId,
        rhs: ExprId,
        expr_id: ExprId,
    ) -> Operand {
        let expr_ty = self.ty_of(expr_id);
        let dest = self.alloc_temp(ty_to_mir(&expr_ty));

        let lhs_op = self.build_expr(lhs);
        let lhs_ty = self.ty_of(lhs);
        self.assign_value(dest, lhs_op, &lhs_ty);

        let cond_bool = self.to_bool_operand(Operand::Local(dest), &lhs_ty);

        let rhs_bb = self.new_block();
        let join_bb = self.new_block();

        match op {
            ryupy_hir::BoolOp::And => {
                self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Branch {
                    cond: cond_bool,
                    then_: rhs_bb,
                    else_: join_bb,
                };
            }
            ryupy_hir::BoolOp::Or => {
                self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Branch {
                    cond: cond_bool,
                    then_: join_bb,
                    else_: rhs_bb,
                };
            }
        }

        *self.current_bb = rhs_bb;
        let rhs_op = self.build_expr(rhs);
        let rhs_ty = self.ty_of(rhs);
        self.assign_value(dest, rhs_op, &rhs_ty);
        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Goto(join_bb);

        *self.current_bb = join_bb;
        Operand::Local(dest)
    }

    pub(crate) fn build_if_exp(
        &mut self,
        cond: ExprId,
        then_: ExprId,
        else_: ExprId,
        expr_id: ExprId,
    ) -> Operand {
        let expr_ty = self.ty_of(expr_id);
        let dest = self.alloc_temp(ty_to_mir(&expr_ty));

        let cond_raw = self.build_expr(cond);
        let cond_ty = self.ty_of(cond);
        let cond_bool = self.to_bool_operand(cond_raw, &cond_ty);

        let then_bb = self.new_block();
        let else_bb = self.new_block();
        let join_bb = self.new_block();

        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Branch {
            cond: cond_bool,
            then_: then_bb,
            else_: else_bb,
        };

        *self.current_bb = then_bb;
        let then_op = self.build_expr(then_);
        let then_ty = self.ty_of(then_);
        self.assign_value(dest, then_op, &then_ty);
        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Goto(join_bb);

        *self.current_bb = else_bb;
        let else_op = self.build_expr(else_);
        let else_ty = self.ty_of(else_);
        self.assign_value(dest, else_op, &else_ty);
        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Goto(join_bb);

        *self.current_bb = join_bb;
        Operand::Local(dest)
    }

    /// 动态属性读取：`PyObject.GetAttr(obj, "name")`。
    pub(crate) fn call_runtime_attr(&mut self, obj: Operand, name: &str) -> Operand {
        let dest = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::DynGetAttr),
                args: vec![obj, Operand::Const(Constant::Str(name.to_string()))],
            },
        });
        Operand::Local(dest)
    }

    /// 把值写进目标局部变量，必要时插入装箱 / 拆箱。
    pub fn assign_value(&mut self, dest: MirLocalId, op: Operand, from: &Ty) {
        let target_ty = self.locals[dest].ty.clone();
        let coerced = self.coerce(op, from, &target_ty);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Use(coerced),
        });
    }
}

fn dyn_bin_op(op: HirBinOp) -> DynOp {
    match op {
        HirBinOp::Add => DynOp::Add,
        HirBinOp::Sub => DynOp::Sub,
        HirBinOp::Mul => DynOp::Mul,
        HirBinOp::TrueDiv => DynOp::TrueDiv,
        HirBinOp::FloorDiv => DynOp::FloorDiv,
        HirBinOp::Mod => DynOp::Mod,
        HirBinOp::Pow => DynOp::Pow,
        HirBinOp::BitAnd => DynOp::BitAnd,
        HirBinOp::BitOr => DynOp::BitOr,
        HirBinOp::BitXor => DynOp::BitXor,
        HirBinOp::LShift => DynOp::LShift,
        HirBinOp::RShift => DynOp::RShift,
    }
}

fn dyn_cmp_op(op: CmpOp) -> DynOp {
    match op {
        CmpOp::Eq => DynOp::Eq,
        CmpOp::NotEq => DynOp::Ne,
        CmpOp::Lt => DynOp::Lt,
        CmpOp::LtE => DynOp::Le,
        CmpOp::Gt => DynOp::Gt,
        CmpOp::GtE => DynOp::Ge,
        // 这三个走专用运行时调用，不会进入这里
        CmpOp::In | CmpOp::NotIn | CmpOp::Is | CmpOp::IsNot => DynOp::Eq,
    }
}

fn dyn_unary_op(op: HirUnaryOp) -> DynOp {
    match op {
        HirUnaryOp::Neg => DynOp::Neg,
        HirUnaryOp::Pos => DynOp::Pos,
        HirUnaryOp::Not => DynOp::Not,
        HirUnaryOp::Invert => DynOp::Invert,
    }
}
