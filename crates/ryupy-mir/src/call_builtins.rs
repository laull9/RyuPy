//! 内建调用的 MIR 降级：数值转换保留静态快路径，其余走运行时统一绑定。
//!
//! 从通用调用构建器拆出，不改变实参求值顺序、错误路径或 RuntimeFn ABI。

use crate::build::ty_to_mir;
use crate::expr_builder::ExprBuilder;
use crate::mir::{Callee, Constant, MirType, Operand, RuntimeFn, Rvalue, Statement};
use ryupy_hir::{Builtin, CallArg};
use ryupy_ty::Ty;

impl ExprBuilder<'_> {
    // ───────────────────────── 内建函数 ─────────────────────────

    pub(crate) fn build_builtin_call(
        &mut self,
        builtin: Builtin,
        args: &[CallArg],
        call_ty: &Ty,
    ) -> Operand {
        match builtin {
            Builtin::Print => self.call_builtin_by_name("print", args),
            Builtin::Dynamic("locals") if self.supports_function_locals() => {
                self.build_function_locals(args)
            }
            Builtin::Dynamic(name) => self.call_builtin_by_name(name, args),
            // 只有固定单位置数值转换可用快路；其它形态必须求完所有实参再绑定。
            Builtin::Int if matches!(args, [CallArg::Positional(_)]) => {
                self.build_numeric_conversion(args, call_ty, true)
            }
            Builtin::Float if matches!(args, [CallArg::Positional(_)]) => {
                self.build_numeric_conversion(args, call_ty, false)
            }
            Builtin::Abs if matches!(args, [CallArg::Positional(_)]) => self.build_abs(args),
            Builtin::Str
                if matches!(args, [CallArg::Positional(expr)] if matches!(
                    self.ty_of(*expr), Ty::Int | Ty::Float | Ty::Bool | Ty::Str | Ty::None
                ) || matches!(
                    self.hir.functions[self.current_hir_fn].body.exprs[*expr].kind,
                    ryupy_hir::ExprKind::Literal(_)
                )) =>
            {
                // 精确旁表或不可覆写的字面量才用string返回ABI；不扩变量/Unary推断。
                let expr = args[0].expr();
                let arg_op = self.build_expr(expr);
                let arg_ty = self.ty_of(expr);
                let op = self.coerce(arg_op, &arg_ty, &MirType::Dynamic);
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
            Builtin::Str => self.call_builtin_by_name("str", args),
            Builtin::Type => {
                let Some(arg) = args.first() else {
                    return self.none_value();
                };
                let expr = arg.expr();
                let arg_op = self.build_expr(expr);
                let arg_ty = self.ty_of(expr);
                let op = self.coerce(arg_op, &arg_ty, &MirType::Dynamic);
                self.runtime_call(RuntimeFn::DynType, vec![op], MirType::Dynamic)
            }
            Builtin::Exception(name) => {
                let mut args_ops = vec![Operand::Const(Constant::Str(name.to_owned()))];
                if let Some(arg) = args.first() {
                    let expr = arg.expr();
                    let arg_op = self.build_expr(expr);
                    let arg_ty = self.ty_of(expr);
                    args_ops.push(self.coerce(arg_op, &arg_ty, &MirType::Dynamic));
                } else {
                    args_ops.push(self.runtime_call(
                        RuntimeFn::DynNone,
                        Vec::new(),
                        MirType::Dynamic,
                    ));
                }
                self.runtime_call(RuntimeFn::MakeException, args_ops, MirType::Dynamic)
            }
            // 非快路的int形态及其它构造器的关键字调用，完整求值后交运行时绑定。
            Builtin::Int | Builtin::Float | Builtin::Abs => {
                let name = match builtin {
                    Builtin::Int => "int",
                    Builtin::Float => "float",
                    _ => "abs",
                };
                self.call_builtin_by_name(name, args)
            }
        }
    }

    pub(crate) fn call_builtin_by_name(&mut self, name: &str, args: &[CallArg]) -> Operand {
        let (positional, kw_names, kw_values) = self.build_call_arrays(args);
        self.runtime_call(
            RuntimeFn::DynBuiltin,
            vec![
                Operand::Const(Constant::Str(name.to_owned())),
                Operand::Array(positional),
                Operand::Array(kw_names),
                Operand::Array(kw_values),
            ],
            MirType::Dynamic,
        )
    }

    fn build_numeric_conversion(
        &mut self,
        args: &[CallArg],
        call_ty: &Ty,
        to_int: bool,
    ) -> Operand {
        let Some(arg) = args.first() else {
            return if to_int {
                Operand::Const(Constant::I64(0))
            } else {
                Operand::Const(Constant::F64(0.0))
            };
        };
        let expr = arg.expr();
        let arg_op = self.build_expr(expr);
        let arg_ty = self.ty_of(expr);
        if arg_ty.is_dynamic() {
            let op = self.coerce(arg_op, &arg_ty, &MirType::Dynamic);
            let rt_fn = if to_int {
                RuntimeFn::DynToInt
            } else {
                RuntimeFn::DynToFloat
            };
            return self.runtime_call(rt_fn, vec![op], MirType::Dynamic);
        }
        let result_ty = ty_to_mir(call_ty);
        match (to_int, arg_ty.clone()) {
            (true, Ty::Int) => arg_op,
            (true, Ty::Bool) => {
                let dest = self.alloc_temp(MirType::I64);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Cast {
                        kind: crate::mir::CastKind::BoolToI64,
                        operand: arg_op,
                    },
                });
                Operand::Local(dest)
            }
            (true, Ty::Float) => self.runtime_call(RuntimeFn::F64ToI64, vec![arg_op], MirType::I64),
            (false, Ty::Float) => arg_op,
            (false, Ty::Int | Ty::Bool) => {
                let op = if arg_ty == Ty::Bool {
                    let dest = self.alloc_temp(MirType::I64);
                    self.push_stmt(Statement::Assign {
                        dest,
                        value: Rvalue::Cast {
                            kind: crate::mir::CastKind::BoolToI64,
                            operand: arg_op,
                        },
                    });
                    Operand::Local(dest)
                } else {
                    arg_op
                };
                let dest = self.alloc_temp(MirType::F64);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Cast {
                        kind: crate::mir::CastKind::I64ToF64,
                        operand: op,
                    },
                });
                Operand::Local(dest)
            }
            _ => {
                let _ = result_ty;
                arg_op
            }
        }
    }

    fn build_abs(&mut self, args: &[CallArg]) -> Operand {
        let Some(arg) = args.first() else {
            return Operand::Const(Constant::I64(0));
        };
        let expr = arg.expr();
        let arg_op = self.build_expr(expr);
        let arg_ty = self.ty_of(expr);
        if arg_ty.is_dynamic() {
            let op = self.coerce(arg_op, &arg_ty, &MirType::Dynamic);
            return self.runtime_call(RuntimeFn::DynAbs, vec![op], MirType::Dynamic);
        }
        match arg_ty {
            Ty::Float => self.runtime_call(RuntimeFn::AbsF64, vec![arg_op], MirType::F64),
            _ => self.runtime_call(RuntimeFn::AbsI64, vec![arg_op], MirType::I64),
        }
    }
}
