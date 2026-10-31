//! 函数调用与构造器调用的类型检查。
//!
//! 职责：
//! 1. **内建函数调用**：检查 `print(...)`、`int(...)`、`float(...)`、`abs(...)` 的参数数量与类型转换约束；
//! 2. **用户函数调用**：比对函数签名 [`FnSig`]，允许 `int → float` 隐式数值提升；
//! 3. **dataclass 构造调用**：按声明字段顺序校验实参类型，返回 [`Ty::Class`]；
//! 4. **非法调用拦截**：对非函数/非类对象的调用报错 [`E0200`]。

use ryupy_base::{Diagnostic, Span, TextRange};
use ryupy_hir::{Builtin, ExprId, ExprKind};

use crate::check::{FunctionChecker, type_ref_to_ty};
use crate::ty::Ty;

impl FunctionChecker<'_> {
    pub(crate) fn check_call(&mut self, callee: ExprId, args: &[ExprId], range: TextRange) -> Ty {
        let callee_expr = &self.func.body.exprs[callee];
        match callee_expr.kind {
            ExprKind::Builtin(Builtin::Print) => {
                self.results.exprs.insert(callee, Ty::None);
                for &arg in args {
                    self.check_expr(arg);
                }
                Ty::None
            }
            ExprKind::Builtin(Builtin::Int) => {
                self.results.exprs.insert(callee, Ty::None);
                if args.len() != 1 {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!("int() 期望 1 个参数，实际传入 {} 个", args.len()),
                        )
                        .with_code("E0204")
                        .with_primary_label("参数个数不匹配"),
                    );
                    for &arg in args {
                        self.check_expr(arg);
                    }
                    return Ty::Int;
                }
                let arg_ty = self.check_expr(args[0]);
                if arg_ty.is_dynamic() {
                    return Ty::Dynamic;
                }
                match arg_ty {
                    Ty::Int | Ty::Float | Ty::Bool => Ty::Int,
                    Ty::Error => Ty::Error,
                    _ => {
                        // Python 不因静态类型拒绝：字符串等类型可能在运行时成功
                        // （`int("12")`），也可能抛 ValueError。降级走动态路径。
                        self.soft_demote = true;
                        Ty::Dynamic
                    }
                }
            }
            ExprKind::Builtin(Builtin::Float) => {
                self.results.exprs.insert(callee, Ty::None);
                if args.len() != 1 {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!("float() 期望 1 个参数，实际传入 {} 个", args.len()),
                        )
                        .with_code("E0204")
                        .with_primary_label("参数个数不匹配"),
                    );
                    for &arg in args {
                        self.check_expr(arg);
                    }
                    return Ty::Float;
                }
                let arg_ty = self.check_expr(args[0]);
                if arg_ty.is_dynamic() {
                    return Ty::Dynamic;
                }
                match arg_ty {
                    Ty::Int | Ty::Float | Ty::Bool => Ty::Float,
                    Ty::Error => Ty::Error,
                    _ => {
                        // Python 不因静态类型拒绝：字符串等类型可能在运行时成功
                        // （`int("12")`），也可能抛 ValueError。降级走动态路径。
                        self.soft_demote = true;
                        Ty::Dynamic
                    }
                }
            }
            ExprKind::Builtin(Builtin::Abs) => {
                self.results.exprs.insert(callee, Ty::None);
                if args.len() != 1 {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!("abs() 期望 1 个参数，实际传入 {} 个", args.len()),
                        )
                        .with_code("E0204")
                        .with_primary_label("参数个数不匹配"),
                    );
                    for &arg in args {
                        self.check_expr(arg);
                    }
                    return Ty::Error;
                }
                let arg_ty = self.check_expr(args[0]);
                if arg_ty.is_dynamic() {
                    return Ty::Dynamic;
                }
                match arg_ty {
                    Ty::Int => Ty::Int,
                    Ty::Float => Ty::Float,
                    Ty::Error => Ty::Error,
                    _ => {
                        // Python 不因静态类型拒绝：字符串等类型可能在运行时成功
                        // （`int("12")`），也可能抛 ValueError。降级走动态路径。
                        self.soft_demote = true;
                        Ty::Dynamic
                    }
                }
            }
            ExprKind::Builtin(Builtin::Exception(_)) => {
                // 异常类构造：接受 0~1 个参数，结果是动态值（异常对象）
                self.results.exprs.insert(callee, Ty::None);
                for &arg in args {
                    self.check_expr(arg);
                }
                Ty::Dynamic
            }
            ExprKind::Func(target_fn_id) => {
                let target_sig = self.all_sigs[target_fn_id].clone();
                let callee_dynamic = self
                    .all_modes
                    .get(target_fn_id)
                    .copied()
                    .unwrap_or(crate::ty::FuncMode::Static)
                    .is_dynamic();
                if args.len() != target_sig.params.len() {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!(
                                "函数调用参数数量不匹配：期望 {} 个，实际传入 {} 个",
                                target_sig.params.len(),
                                args.len()
                            ),
                        )
                        .with_code("E0204")
                        .with_primary_label("参数个数不匹配"),
                    );
                }

                for (i, &arg) in args.iter().enumerate() {
                    let arg_ty = self.check_expr(arg);
                    if callee_dynamic {
                        // 动态函数接受任何实参，参数个数已在上面检查
                        continue;
                    }
                    if let Some(expected_ty) = target_sig.params.get(i) {
                        if *expected_ty != Ty::Error
                            && arg_ty != Ty::Error
                            && *expected_ty != arg_ty
                            && !(*expected_ty == Ty::Float && arg_ty == Ty::Int)
                        {
                            // 标注不是运行时约束：实参不符时把被调函数降级为动态，
                            // 它会在动态路径上按 Python 语义执行（M2.1）。
                            self.demote_callees.push(target_fn_id);
                        }
                    }
                }
                if callee_dynamic {
                    Ty::Dynamic
                } else {
                    target_sig.ret
                }
            }
            ExprKind::Class(target_cls_id) => {
                let target_cls = &self.module.classes[target_cls_id];
                if args.len() != target_cls.fields.len() {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!(
                                "类 `{}` 构造参数数量不匹配：期望 {} 个，实际传入 {} 个",
                                target_cls.name,
                                target_cls.fields.len(),
                                args.len()
                            ),
                        )
                        .with_code("E0204")
                        .with_primary_label("构造参数个数不匹配"),
                    );
                }

                for (i, &arg) in args.iter().enumerate() {
                    let arg_ty = self.check_expr(arg);
                    if let Some(field) = target_cls.fields.get(i) {
                        let expected_ty = type_ref_to_ty(&field.annotation);
                        if expected_ty != Ty::Error
                            && arg_ty != Ty::Error
                            && expected_ty != arg_ty
                            && !(expected_ty == Ty::Float && arg_ty == Ty::Int)
                        {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, self.func.body.exprs[arg].range),
                                    format!(
                                        "字段 `{}` 的参数类型不匹配：期望 `{expected_ty:?}`，得到 `{arg_ty:?}`",
                                        field.name
                                    ),
                                )
                                .with_code("E0203")
                                .with_primary_label("构造实参类型不匹配"),
                            );
                        }
                    }
                }
                Ty::Class(target_cls_id)
            }
            _ => {
                self.check_expr(callee);
                for &arg in args {
                    self.check_expr(arg);
                }
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, range), "暂不支持调用非函数/类对象")
                        .with_code("E0200")
                        .with_primary_label("不是可调用的函数或类"),
                );
                Ty::Error
            }
        }
    }
}
