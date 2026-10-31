//! 函数调用与构造器调用的 MIR 降级构建器。
//!
//! 职责：
//! 1. **用户函数调用**：按被调函数当前的 `FuncMode` 决定装箱策略——
//!    静态签名 + 动态实参先拆箱（`PyCast`），动态签名 + 静态实参先装箱；
//! 2. **dataclass 构造调用**：按字段声明顺序转换实参（仅静态路径）；
//! 3. **内建函数调用**：
//!    - `int(x)` / `float(x)` / `abs(x)`：静态实参走原生转换；
//!      动态实参走 `PyOps.ToIntValue` / `ToFloatValue` / `AbsValue`。
//!
//! 结果值的装箱由调用方上下文决定：动态函数里的调用结果要装箱后才能进局部变量。

use ryupy_hir::{Builtin, ExprId, ExprKind};
use ryupy_ty::{Ty, type_ref_to_ty};

use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, CastKind, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};

impl ExprBuilder<'_> {
    pub(crate) fn build_call(
        &mut self,
        callee: ExprId,
        args: &[ExprId],
        call_expr: ExprId,
    ) -> Operand {
        let callee_expr = &self.hir.functions[self.current_hir_fn].body.exprs[callee];
        let call_ty = self.ty_of(call_expr);
        match callee_expr.kind {
            ExprKind::Func(fn_id) => {
                let target_sig = self.types.sigs[fn_id].clone();
                let mir_fn_id = self.fn_map[&fn_id];
                let mut args_ops = Vec::new();
                for (i, &arg) in args.iter().enumerate() {
                    let arg_op = self.build_expr(arg);
                    let arg_ty = self.ty_of(arg);
                    let param_ty = target_sig.params.get(i).cloned().unwrap_or(Ty::Dynamic);
                    let param_mir = ty_to_mir(&param_ty);
                    args_ops.push(self.coerce(arg_op, &arg_ty, &param_mir));
                }
                let ret_mir = ty_to_mir(&target_sig.ret);
                let call = Rvalue::Call {
                    callee: Callee::Fn(mir_fn_id),
                    args: args_ops,
                };
                if matches!(ret_mir, MirType::Unit) {
                    // 返回 None 的函数编译成 void 方法：没有返回值可存。
                    // 调用表达式本身仍要占一个位置（例如 `x = f()`），用静态路径的
                    // None 占位值/动态 None 单例顶替，避免给 void 调用分配槽位。
                    self.push_stmt(Statement::Eval(call));
                    return self.none_value();
                }
                let dest = self.alloc_temp(ret_mir);
                self.push_stmt(Statement::Assign { dest, value: call });
                // 静态返回类型进入动态上下文时要装箱
                let result = Operand::Local(dest);
                self.coerce(result, &target_sig.ret, &ty_to_mir(&call_ty))
            }
            ExprKind::Class(cls_id) => {
                let mir_cls_id = la_arena::Idx::from_raw(cls_id.into_raw());
                let cls = &self.hir.classes[cls_id];
                let mut args_ops = Vec::new();
                for (i, &arg) in args.iter().enumerate() {
                    let arg_op = self.build_expr(arg);
                    let arg_ty = self.ty_of(arg);
                    if let Some(field) = cls.fields.get(i) {
                        let field_ty = type_ref_to_ty(&field.annotation);
                        let field_mir = ty_to_mir(&field_ty);
                        args_ops.push(self.coerce(arg_op, &arg_ty, &field_mir));
                    } else {
                        args_ops.push(arg_op);
                    }
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
                let arg_ty = self.ty_of(args[0]);
                if arg_ty.is_dynamic() {
                    let op = self.coerce(arg_op, &arg_ty, &MirType::Dynamic);
                    let dest = self.alloc_temp(MirType::Dynamic);
                    self.push_stmt(Statement::Assign {
                        dest,
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::DynToInt),
                            args: vec![op],
                        },
                    });
                    return Operand::Local(dest);
                }
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
                let arg_ty = self.ty_of(args[0]);
                if arg_ty.is_dynamic() {
                    let op = self.coerce(arg_op, &arg_ty, &MirType::Dynamic);
                    let dest = self.alloc_temp(MirType::Dynamic);
                    self.push_stmt(Statement::Assign {
                        dest,
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::DynToFloat),
                            args: vec![op],
                        },
                    });
                    return Operand::Local(dest);
                }
                match arg_ty {
                    Ty::Float => arg_op,
                    Ty::Int | Ty::Bool => {
                        let op = if arg_ty == Ty::Bool {
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
                let arg_ty = self.ty_of(args[0]);
                if arg_ty.is_dynamic() {
                    let op = self.coerce(arg_op, &arg_ty, &MirType::Dynamic);
                    let dest = self.alloc_temp(MirType::Dynamic);
                    self.push_stmt(Statement::Assign {
                        dest,
                        value: Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::DynAbs),
                            args: vec![op],
                        },
                    });
                    return Operand::Local(dest);
                }
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
            // `obj.method(args)`：方法调用统一走运行时容器协议
            ExprKind::Attribute {
                base,
                name: ref attr_name,
            } => {
                let (base, name) = (base, attr_name.as_str().to_string());
                let obj_op = self.build_expr(base);
                let obj_ty = self.ty_of(base);
                let obj = self.coerce(obj_op, &obj_ty, &MirType::Dynamic);
                let mut args_ops = Vec::new();
                for &arg in args {
                    let arg_op = self.build_expr(arg);
                    let arg_ty = self.ty_of(arg);
                    args_ops.push(self.coerce(arg_op, &arg_ty, &MirType::Dynamic));
                }
                self.call_with_args(
                    RuntimeFn::CallMethod,
                    vec![obj, Operand::Const(Constant::Str(name))],
                    args_ops,
                )
            }
            ExprKind::Builtin(Builtin::Dynamic(name)) => {
                let mut args_ops = Vec::new();
                for &arg in args {
                    let arg_op = self.build_expr(arg);
                    let arg_ty = self.ty_of(arg);
                    args_ops.push(self.coerce(arg_op, &arg_ty, &MirType::Dynamic));
                }
                self.call_with_args(
                    RuntimeFn::DynBuiltin,
                    vec![Operand::Const(Constant::Str(name.to_string()))],
                    args_ops,
                )
            }
            ExprKind::Builtin(Builtin::Str) => {
                // `str()` 无实参是空串；有实参时把值转成字符串（动态路径统一走运行时）
                let Some(&arg) = args.first() else {
                    return Operand::Const(Constant::Str(String::new()));
                };
                let arg_op = self.build_expr(arg);
                let arg_ty = self.ty_of(arg);
                let op = self.coerce(arg_op, &arg_ty, &MirType::Dynamic);
                // 动态函数里 `str(x)` 的结果要能直接参与动态运算（拼接、比较、装箱），
                // 因此槽位必须是 `object`；声明成 `Str` 会让 `str(1) + "x"` 这类表达式
                // 的动态运算拿到一个 str 类型的操作数，被 MIR 校验拒绝。
                let ret_ty = if self.is_dynamic_context() {
                    MirType::Dynamic
                } else {
                    MirType::Str
                };
                let dest = self.alloc_temp(ret_ty);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::DynStr),
                        args: vec![op],
                    },
                });
                Operand::Local(dest)
            }
            ExprKind::Builtin(Builtin::Type) => {
                let Some(&arg) = args.first() else {
                    return Operand::Const(Constant::I64(0));
                };
                let arg_op = self.build_expr(arg);
                let arg_ty = self.ty_of(arg);
                let op = self.coerce(arg_op, &arg_ty, &MirType::Dynamic);
                let dest = self.alloc_temp(MirType::Dynamic);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::DynType),
                        args: vec![op],
                    },
                });
                Operand::Local(dest)
            }
            ExprKind::Builtin(Builtin::Exception(name)) => {
                // `ValueError("msg")` → PyOps.MakeException("ValueError", msg)
                let mut args_ops = vec![Operand::Const(Constant::Str(name.to_owned()))];
                if let Some(&arg) = args.first() {
                    let arg_op = self.build_expr(arg);
                    let arg_ty = self.ty_of(arg);
                    args_ops.push(self.coerce(arg_op, &arg_ty, &MirType::Dynamic));
                } else {
                    args_ops.push(self.runtime_call(
                        RuntimeFn::DynNone,
                        Vec::new(),
                        MirType::Dynamic,
                    ));
                }
                let dest = self.alloc_temp(MirType::Dynamic);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::MakeException),
                        args: args_ops,
                    },
                });
                Operand::Local(dest)
            }
            _ => Operand::Const(Constant::I64(0)),
        }
    }
}
