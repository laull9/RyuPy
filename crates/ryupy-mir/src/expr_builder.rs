//! 表达式 HIR → MIR 降级构建器。
//!
//! 职责：
//! 1. **字面量与操作数**：转换常量字面量为 [`Operand::Const`]，读取局部变量为 [`Operand::Local`]；
//! 2. **Python 语义改写**：
//!    - 向下取整除法 `//`、取模 `%`、乘方 `**` 改写为运行时辅助调用 [`RuntimeFn`]；
//!    - 真除法 `/` 将整数操作数显式提升为浮点数后发射 [`BinOp::Div`]；
//!    - 混合类型的二元运算自动插入 [`CastKind::I64ToF64`] 提升操作；
//! 3. **一元与比较**：
//!    - 一元运算映射为 [`UnaryOp`]；比较运算映射为 [`BinOp`] 比较指令。

use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Diagnostic, Span};
use ryupy_hir::{
    BinOp as HirBinOp, Builtin, CmpOp, ExprId, ExprKind, Literal, LocalId as HirLocalId, Module,
    UnaryOp as HirUnaryOp,
};
use ryupy_ty::{ModuleTypes, Ty};

use crate::build::ty_to_mir;
use crate::mir::{
    BasicBlock, BinOp, Callee, CastKind, Constant, FnId, LocalDecl, LocalId as MirLocalId, MirType,
    Operand, RuntimeFn, Rvalue, Statement, UnaryOp,
};

pub struct ExprBuilder<'a> {
    pub hir: &'a Module,
    pub types: &'a ModuleTypes,
    pub fn_map: &'a FxHashMap<ryupy_hir::FuncId, FnId>,
    pub current_hir_fn: ryupy_hir::FuncId,
    pub locals: &'a mut Arena<LocalDecl>,
    pub blocks: &'a mut Arena<BasicBlock>,
    pub current_bb: &'a mut crate::mir::BlockId,
    pub local_map: &'a FxHashMap<HirLocalId, MirLocalId>,
    pub assigned_locals: &'a mut FxHashSet<HirLocalId>,
    pub reported_unbound: &'a mut FxHashSet<HirLocalId>,
    pub diagnostics: &'a mut Vec<Diagnostic>,
}

impl<'a> ExprBuilder<'a> {
    pub fn push_stmt(&mut self, stmt: Statement) {
        self.blocks[*self.current_bb].stmts.push(stmt);
    }

    pub fn alloc_temp(&mut self, ty: MirType) -> MirLocalId {
        self.locals.alloc(LocalDecl { ty, name: None })
    }

    pub fn build_expr(&mut self, expr_id: ExprId) -> Operand {
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let expr = &hir_body.exprs[expr_id];
        match &expr.kind {
            ExprKind::Literal(lit) => match lit {
                Literal::Int(v) => Operand::Const(Constant::I64(*v)),
                Literal::Float(v) => Operand::Const(Constant::F64(*v)),
                Literal::Bool(b) => Operand::Const(Constant::Bool(*b)),
                Literal::Str(s) => Operand::Const(Constant::Str(s.to_string())),
                Literal::None => Operand::Const(Constant::I64(0)),
            },
            ExprKind::Local(loc) => {
                // 确定赋值分析：若局部变量在赋初值前被读取，报错 E0301（语义陷阱 #10）
                if !self.assigned_locals.contains(loc) && !self.reported_unbound.contains(loc) {
                    let var_name = &hir_body.locals[*loc].name;
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.hir.file, expr.range),
                            format!("局部变量 `{var_name}` 在赋值前被读取"),
                        )
                        .with_code("E0301")
                        .with_primary_label("在赋值前被使用（运行时将引发 UnboundLocalError）"),
                    );
                    self.reported_unbound.insert(*loc);
                }
                Operand::Local(self.local_map[loc])
            }
            ExprKind::Binary { op, lhs, rhs } => self.build_binary(*op, *lhs, *rhs, expr_id),
            ExprKind::Unary { op, operand } => self.build_unary(*op, *operand, expr_id),
            ExprKind::Compare { op, lhs, rhs } => self.build_compare(*op, *lhs, *rhs, expr_id),
            ExprKind::BoolOp { op, lhs, rhs } => self.build_bool_op(*op, *lhs, *rhs, expr_id),
            ExprKind::IfExp { cond, then_, else_ } => {
                self.build_if_exp(*cond, *then_, *else_, expr_id)
            }
            ExprKind::Call { callee, args } => self.build_call(*callee, args),
            _ => Operand::Const(Constant::I64(0)),
        }
    }

    fn build_binary(&mut self, op: HirBinOp, lhs: ExprId, rhs: ExprId, expr_id: ExprId) -> Operand {
        let mut lhs_op = self.build_expr(lhs);
        let mut rhs_op = self.build_expr(rhs);

        let lhs_ty = &self.types.bodies[self.current_hir_fn].exprs[lhs];
        let rhs_ty = &self.types.bodies[self.current_hir_fn].exprs[rhs];

        // 隐式提升：若两端类型分别为 int 和 float，将 int 一端提升为 float
        if lhs_ty == &Ty::Int && rhs_ty == &Ty::Float {
            let cast_temp = self.alloc_temp(MirType::F64);
            self.push_stmt(Statement::Assign {
                dest: cast_temp,
                value: Rvalue::Cast {
                    kind: CastKind::I64ToF64,
                    operand: lhs_op,
                },
            });
            lhs_op = Operand::Local(cast_temp);
        } else if lhs_ty == &Ty::Float && rhs_ty == &Ty::Int {
            let cast_temp = self.alloc_temp(MirType::F64);
            self.push_stmt(Statement::Assign {
                dest: cast_temp,
                value: Rvalue::Cast {
                    kind: CastKind::I64ToF64,
                    operand: rhs_op,
                },
            });
            rhs_op = Operand::Local(cast_temp);
        }

        // 字符串拼接
        if op == HirBinOp::Add && lhs_ty == &Ty::Str && rhs_ty == &Ty::Str {
            return self.call_runtime_binary(RuntimeFn::StrConcat, lhs_op, rhs_op, MirType::Str);
        }

        // Python 语义改写：向负无穷取整除法、取模、乘方等落入 RuntimeFn
        let expr_ty = &self.types.bodies[self.current_hir_fn].exprs[expr_id];
        match op {
            HirBinOp::FloorDiv => {
                let rt_fn = if expr_ty == &Ty::Float {
                    RuntimeFn::FloorDivF64
                } else {
                    RuntimeFn::FloorDivI64
                };
                self.call_runtime_binary(rt_fn, lhs_op, rhs_op, ty_to_mir(expr_ty))
            }
            HirBinOp::Mod => {
                let rt_fn = if expr_ty == &Ty::Float {
                    RuntimeFn::ModF64
                } else {
                    RuntimeFn::ModI64
                };
                self.call_runtime_binary(rt_fn, lhs_op, rhs_op, ty_to_mir(expr_ty))
            }
            HirBinOp::Pow => {
                let rt_fn = if expr_ty == &Ty::Float {
                    RuntimeFn::PowF64
                } else {
                    RuntimeFn::PowI64
                };
                self.call_runtime_binary(rt_fn, lhs_op, rhs_op, ty_to_mir(expr_ty))
            }
            HirBinOp::TrueDiv => {
                // 真除法：若操作数原本是整数，先提升到 F64
                if lhs_ty == &Ty::Int {
                    let cast_temp = self.alloc_temp(MirType::F64);
                    self.push_stmt(Statement::Assign {
                        dest: cast_temp,
                        value: Rvalue::Cast {
                            kind: CastKind::I64ToF64,
                            operand: lhs_op,
                        },
                    });
                    lhs_op = Operand::Local(cast_temp);
                }
                if rhs_ty == &Ty::Int {
                    let cast_temp = self.alloc_temp(MirType::F64);
                    self.push_stmt(Statement::Assign {
                        dest: cast_temp,
                        value: Rvalue::Cast {
                            kind: CastKind::I64ToF64,
                            operand: rhs_op,
                        },
                    });
                    rhs_op = Operand::Local(cast_temp);
                }
                let dest = self.alloc_temp(MirType::F64);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Binary {
                        op: BinOp::Div,
                        lhs: lhs_op,
                        rhs: rhs_op,
                    },
                });
                Operand::Local(dest)
            }
            _ => {
                let mir_op = match op {
                    HirBinOp::Add => BinOp::Add,
                    HirBinOp::Sub => BinOp::Sub,
                    HirBinOp::Mul => BinOp::Mul,
                    HirBinOp::BitAnd => BinOp::BitAnd,
                    HirBinOp::BitOr => BinOp::BitOr,
                    HirBinOp::BitXor => BinOp::BitXor,
                    HirBinOp::LShift => BinOp::Shl,
                    HirBinOp::RShift => BinOp::Shr,
                    _ => BinOp::Add,
                };
                let dest = self.alloc_temp(ty_to_mir(expr_ty));
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

    fn build_unary(&mut self, op: HirUnaryOp, operand: ExprId, expr_id: ExprId) -> Operand {
        let op_val = self.build_expr(operand);
        // 简单常量折叠：负整数与负浮点
        if op == HirUnaryOp::Neg {
            if let Operand::Const(Constant::I64(v)) = op_val {
                return Operand::Const(Constant::I64(-v));
            }
            if let Operand::Const(Constant::F64(v)) = op_val {
                return Operand::Const(Constant::F64(-v));
            }
        }
        let expr_ty = &self.types.bodies[self.current_hir_fn].exprs[expr_id];
        let mir_op = match op {
            HirUnaryOp::Neg => UnaryOp::Neg,
            HirUnaryOp::Not => UnaryOp::Not,
            HirUnaryOp::Invert => UnaryOp::BitNot,
            HirUnaryOp::Pos => return op_val,
        };
        let dest = self.alloc_temp(ty_to_mir(expr_ty));
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Unary {
                op: mir_op,
                operand: op_val,
            },
        });
        Operand::Local(dest)
    }

    fn build_compare(&mut self, op: CmpOp, lhs: ExprId, rhs: ExprId, _expr_id: ExprId) -> Operand {
        let mut lhs_op = self.build_expr(lhs);
        let mut rhs_op = self.build_expr(rhs);

        let lhs_ty = &self.types.bodies[self.current_hir_fn].exprs[lhs];
        let rhs_ty = &self.types.bodies[self.current_hir_fn].exprs[rhs];

        if lhs_ty == &Ty::Int && rhs_ty == &Ty::Float {
            let cast_temp = self.alloc_temp(MirType::F64);
            self.push_stmt(Statement::Assign {
                dest: cast_temp,
                value: Rvalue::Cast {
                    kind: CastKind::I64ToF64,
                    operand: lhs_op,
                },
            });
            lhs_op = Operand::Local(cast_temp);
        } else if lhs_ty == &Ty::Float && rhs_ty == &Ty::Int {
            let cast_temp = self.alloc_temp(MirType::F64);
            self.push_stmt(Statement::Assign {
                dest: cast_temp,
                value: Rvalue::Cast {
                    kind: CastKind::I64ToF64,
                    operand: rhs_op,
                },
            });
            rhs_op = Operand::Local(cast_temp);
        }

        let mir_op = match op {
            CmpOp::Eq => BinOp::Eq,
            CmpOp::NotEq => BinOp::Ne,
            CmpOp::Lt => BinOp::Lt,
            CmpOp::LtE => BinOp::Le,
            CmpOp::Gt => BinOp::Gt,
            CmpOp::GtE => BinOp::Ge,
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

    fn build_call(&mut self, callee: ExprId, args: &[ExprId]) -> Operand {
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let callee_expr = &hir_body.exprs[callee];
        match callee_expr.kind {
            ExprKind::Func(fn_id) => {
                let mir_fn_id = self.fn_map[&fn_id];
                let mut args_ops = Vec::new();
                for (i, &arg) in args.iter().enumerate() {
                    let mut arg_op = self.build_expr(arg);
                    let arg_ty = &self.types.bodies[self.current_hir_fn].exprs[arg];
                    if let Some(expected_ty) = self.types.sigs[fn_id].params.get(i) {
                        if expected_ty == &Ty::Float && arg_ty == &Ty::Int {
                            let cast_temp = self.alloc_temp(MirType::F64);
                            self.push_stmt(Statement::Assign {
                                dest: cast_temp,
                                value: Rvalue::Cast {
                                    kind: CastKind::I64ToF64,
                                    operand: arg_op,
                                },
                            });
                            arg_op = Operand::Local(cast_temp);
                        }
                    }
                    args_ops.push(arg_op);
                }
                let ret_ty = ty_to_mir(&self.types.sigs[fn_id].ret);
                let dest = self.alloc_temp(ret_ty);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Call {
                        callee: Callee::Fn(mir_fn_id),
                        args: args_ops,
                    },
                });
                Operand::Local(dest)
            }
            ExprKind::Builtin(Builtin::Int) => {
                if args.is_empty() {
                    return Operand::Const(Constant::I64(0));
                }
                let arg_op = self.build_expr(args[0]);
                let arg_ty = &self.types.bodies[self.current_hir_fn].exprs[args[0]];
                match arg_ty {
                    Ty::Int => arg_op,
                    Ty::Bool => {
                        let dest = self.alloc_temp(MirType::I64);
                        self.push_stmt(Statement::Assign {
                            dest,
                            value: Rvalue::Cast {
                                kind: CastKind::BoolToI64,
                                operand: arg_op,
                            },
                        });
                        Operand::Local(dest)
                    }
                    Ty::Float => {
                        let dest = self.alloc_temp(MirType::I64);
                        self.push_stmt(Statement::Assign {
                            dest,
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::F64ToI64),
                                args: vec![arg_op],
                            },
                        });
                        Operand::Local(dest)
                    }
                    _ => arg_op,
                }
            }
            ExprKind::Builtin(Builtin::Float) => {
                if args.is_empty() {
                    return Operand::Const(Constant::F64(0.0));
                }
                let arg_op = self.build_expr(args[0]);
                let arg_ty = &self.types.bodies[self.current_hir_fn].exprs[args[0]];
                match arg_ty {
                    Ty::Float => arg_op,
                    Ty::Int | Ty::Bool => {
                        let op = if arg_ty == &Ty::Bool {
                            let temp_i64 = self.alloc_temp(MirType::I64);
                            self.push_stmt(Statement::Assign {
                                dest: temp_i64,
                                value: Rvalue::Cast {
                                    kind: CastKind::BoolToI64,
                                    operand: arg_op,
                                },
                            });
                            Operand::Local(temp_i64)
                        } else {
                            arg_op
                        };
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
                    _ => arg_op,
                }
            }
            ExprKind::Builtin(Builtin::Abs) => {
                if args.is_empty() {
                    return Operand::Const(Constant::I64(0));
                }
                let arg_op = self.build_expr(args[0]);
                let arg_ty = &self.types.bodies[self.current_hir_fn].exprs[args[0]];
                match arg_ty {
                    Ty::Float => {
                        let dest = self.alloc_temp(MirType::F64);
                        self.push_stmt(Statement::Assign {
                            dest,
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::AbsF64),
                                args: vec![arg_op],
                            },
                        });
                        Operand::Local(dest)
                    }
                    _ => {
                        let dest = self.alloc_temp(MirType::I64);
                        self.push_stmt(Statement::Assign {
                            dest,
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::AbsI64),
                                args: vec![arg_op],
                            },
                        });
                        Operand::Local(dest)
                    }
                }
            }
            _ => Operand::Const(Constant::I64(0)),
        }
    }

    fn call_runtime_binary(
        &mut self,
        rt_fn: RuntimeFn,
        lhs: Operand,
        rhs: Operand,
        ret_ty: MirType,
    ) -> Operand {
        let dest = self.alloc_temp(ret_ty);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Call {
                callee: Callee::Runtime(rt_fn),
                args: vec![lhs, rhs],
            },
        });
        Operand::Local(dest)
    }

    pub fn new_block(&mut self) -> crate::mir::BlockId {
        self.blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: crate::mir::Terminator::Unreachable,
        })
    }

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
            _ => op,
        }
    }

    fn build_bool_op(
        &mut self,
        op: ryupy_hir::BoolOp,
        lhs: ExprId,
        rhs: ExprId,
        expr_id: ExprId,
    ) -> Operand {
        let expr_ty = &self.types.bodies[self.current_hir_fn].exprs[expr_id];
        let dest = self.alloc_temp(ty_to_mir(expr_ty));

        let lhs_op = self.build_expr(lhs);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Use(lhs_op.clone()),
        });

        let lhs_ty = &self.types.bodies[self.current_hir_fn].exprs[lhs];
        let cond_bool = self.to_bool_operand(lhs_op, lhs_ty);

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
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Use(rhs_op),
        });
        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Goto(join_bb);

        *self.current_bb = join_bb;
        Operand::Local(dest)
    }

    fn build_if_exp(
        &mut self,
        cond: ExprId,
        then_: ExprId,
        else_: ExprId,
        expr_id: ExprId,
    ) -> Operand {
        let expr_ty = &self.types.bodies[self.current_hir_fn].exprs[expr_id];
        let dest = self.alloc_temp(ty_to_mir(expr_ty));

        let cond_raw = self.build_expr(cond);
        let cond_ty = &self.types.bodies[self.current_hir_fn].exprs[cond];
        let cond_bool = self.to_bool_operand(cond_raw, cond_ty);

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
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Use(then_op),
        });
        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Goto(join_bb);

        *self.current_bb = else_bb;
        let else_op = self.build_expr(else_);
        self.push_stmt(Statement::Assign {
            dest,
            value: Rvalue::Use(else_op),
        });
        self.blocks[*self.current_bb].terminator = crate::mir::Terminator::Goto(join_bb);

        *self.current_bb = join_bb;
        Operand::Local(dest)
    }
}
