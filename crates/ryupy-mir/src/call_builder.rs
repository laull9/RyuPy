//! 函数调用与构造器调用的 MIR 降级构建器。
//!
//! 职责：
//! 1. **用户函数调用**：比对函数签名，若实参为 `int` 且形参为 `float` 则自动插入浮点提升，发射 [`Rvalue::Call`]；
//! 2. **dataclass 构造调用**：按字段声明顺序转换实参（同样支持数值提升），发射 [`Rvalue::NewObject`]；
//! 3. **内建函数调用**：
//!    - `int(x)`：按参数类型执行直接使用、布尔转整型或调用运行时辅助 [`RuntimeFn::F64ToI64`]；
//!    - `float(x)`：整型/布尔提升为浮点数；
//!    - `abs(x)`：发射绝对值辅助调用 [`RuntimeFn::AbsI64`] 或 [`RuntimeFn::AbsF64`]。

use ryupy_hir::{Builtin, ExprId, ExprKind};
use ryupy_ty::{Ty, type_ref_to_ty};

use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, CastKind, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

impl ExprBuilder<'_> {
    pub(crate) fn build_call(&mut self, callee: ExprId, args: &[ExprId]) -> Operand {
        let callee_expr = &self.hir.functions[self.current_hir_fn].body.exprs[callee];
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
            ExprKind::Class(cls_id) => {
                let mir_cls_id = la_arena::Idx::from_raw(cls_id.into_raw());
                let cls = &self.hir.classes[cls_id];
                let mut args_ops = Vec::new();
                for (i, &arg) in args.iter().enumerate() {
                    let mut arg_op = self.build_expr(arg);
                    let arg_ty = &self.types.bodies[self.current_hir_fn].exprs[arg];
                    if let Some(field) = cls.fields.get(i) {
                        let expected_ty = type_ref_to_ty(&field.annotation);
                        if expected_ty == Ty::Float && arg_ty == &Ty::Int {
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
                let dest = self.alloc_temp(MirType::Class(mir_cls_id));
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::NewObject {
                        class: mir_cls_id,
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
}
