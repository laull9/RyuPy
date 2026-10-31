//! 函数调用与构造器调用的类型检查。
//!
//! 职责：
//! 1. **内建函数调用**：`print` / `int` / `float` / `abs` / `str` / `type` 等；
//! 2. **用户函数调用**：按参数种类（位置 / 关键字 / `*args` / `**kwargs`）推导实参类型，
//!    允许 `int → float` 隐式提升；实参类型不符时把被调函数降级为动态；
//! 3. **dataclass 构造调用**：位置或关键字实参按字段名匹配；
//! 4. **可调用值**：函数名作为一等值使用时类型是 [`Ty::Func`]，经变量调用也能解析到目标函数。
//!
//! 参数绑定错误（数量不符、重复绑定、未知关键字）**不是**编译错误：Python 在调用时抛
//! `TypeError`，因此这里只做类型推导，绑定与报错交给 MIR / 运行时。

use ryupy_base::{Diagnostic, Span, TextRange};
use ryupy_hir::{Builtin, CallArg, ExprId, ExprKind, FuncId, Name, ParamKind};

use crate::check::{FunctionChecker, type_ref_to_ty};
use crate::ty::{FuncMode, Ty};

impl FunctionChecker<'_> {
    pub(crate) fn check_call(&mut self, callee: ExprId, args: &[CallArg], range: TextRange) -> Ty {
        let callee_kind = self.func.body.exprs[callee].kind.clone();

        // 实参先全部检查：求值顺序与调用形态无关，也保证类型信息对后续绑定可用。
        for arg in args {
            self.check_expr(arg.expr());
        }

        match callee_kind {
            ExprKind::Builtin(Builtin::Print) => {
                self.results.exprs.insert(callee, Ty::None);
                Ty::None
            }
            ExprKind::Builtin(Builtin::Int) => {
                self.results.exprs.insert(callee, Ty::None);
                self.check_unary_builtin(args, range, "int", Ty::Int)
            }
            ExprKind::Builtin(Builtin::Float) => {
                self.results.exprs.insert(callee, Ty::None);
                self.check_unary_builtin(args, range, "float", Ty::Float)
            }
            ExprKind::Builtin(Builtin::Abs) => {
                self.results.exprs.insert(callee, Ty::None);
                self.check_abs_builtin(args, range)
            }
            ExprKind::Builtin(Builtin::Str) => {
                self.results.exprs.insert(callee, Ty::None);
                Ty::Str
            }
            ExprKind::Builtin(Builtin::Type) => {
                self.results.exprs.insert(callee, Ty::None);
                Ty::Dynamic
            }
            ExprKind::Builtin(Builtin::Exception(_)) => {
                self.results.exprs.insert(callee, Ty::None);
                Ty::Dynamic
            }
            // `len` / `sorted` / `isinstance` 等：语义完全由运行时决定
            ExprKind::Builtin(Builtin::Dynamic(_)) => {
                self.results.exprs.insert(callee, Ty::None);
                self.soft_demote = true;
                Ty::Dynamic
            }
            ExprKind::Func(fn_id) => self.check_user_call(fn_id, args),
            ExprKind::Class(cls_id) => self.check_class_call(cls_id, args),
            _ => {
                let callee_ty = self.check_expr(callee);
                match callee_ty {
                    Ty::Func(fn_id) => self.check_user_call(fn_id, args),
                    Ty::Dynamic => Ty::Dynamic,
                    Ty::Error => Ty::Error,
                    _ => {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, range),
                                "暂不支持调用非函数/类对象",
                            )
                            .with_code("E0200")
                            .with_primary_label("不是可调用的函数或类"),
                        );
                        Ty::Error
                    }
                }
            }
        }
    }

    /// `int(x)` / `float(x)`：只接受一个位置实参。
    fn check_unary_builtin(
        &mut self,
        args: &[CallArg],
        range: TextRange,
        name: &str,
        ok_ty: Ty,
    ) -> Ty {
        let Some(arg) = args.first() else {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, range),
                    format!("{name}() 期望 1 个参数，实际传入 0 个"),
                )
                .with_code("E0204")
                .with_primary_label("参数个数不匹配"),
            );
            return ok_ty;
        };
        let arg_ty = self.expr_ty_of(arg.expr());
        if arg_ty.is_dynamic() {
            return Ty::Dynamic;
        }
        match arg_ty {
            Ty::Int | Ty::Float | Ty::Bool => ok_ty,
            Ty::Error => Ty::Error,
            _ => {
                // 字符串等类型可能在运行时成功（`int("12")`），也可能抛 ValueError
                self.soft_demote = true;
                Ty::Dynamic
            }
        }
    }

    fn check_abs_builtin(&mut self, args: &[CallArg], range: TextRange) -> Ty {
        let Some(arg) = args.first() else {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, range),
                    "abs() 期望 1 个参数，实际传入 0 个",
                )
                .with_code("E0204")
                .with_primary_label("参数个数不匹配"),
            );
            return Ty::Error;
        };
        let arg_ty = self.expr_ty_of(arg.expr());
        if arg_ty.is_dynamic() {
            return Ty::Dynamic;
        }
        match arg_ty {
            Ty::Int => Ty::Int,
            Ty::Float => Ty::Float,
            Ty::Error => Ty::Error,
            _ => {
                self.soft_demote = true;
                Ty::Dynamic
            }
        }
    }

    /// 用户函数调用：推导返回类型，并在实参类型不符时降级被调函数。
    fn check_user_call(&mut self, fn_id: FuncId, args: &[CallArg]) -> Ty {
        let sig = self.all_sigs[fn_id].clone();
        let callee_dynamic = self
            .all_modes
            .get(fn_id)
            .copied()
            .unwrap_or(FuncMode::Static)
            .is_dynamic();
        if !callee_dynamic {
            let infos = self.param_infos(fn_id);
            let positional: Vec<&(Name, ParamKind, Ty)> = infos
                .iter()
                .filter(|(_, kind, _)| {
                    matches!(
                        kind,
                        ParamKind::PositionalOnly | ParamKind::PositionalOrKeyword
                    )
                })
                .collect();
            let has_var_positional = infos
                .iter()
                .any(|(_, kind, _)| *kind == ParamKind::VarPositional);
            let has_var_keyword = infos
                .iter()
                .any(|(_, kind, _)| *kind == ParamKind::VarKeyword);

            let mut position = 0usize;
            let mut demote = false;
            for arg in args {
                let arg_ty = self.expr_ty_of(arg.expr());
                let expected = match arg {
                    CallArg::Positional(_) => {
                        let expected = if position < positional.len() {
                            Some(positional[position].2.clone())
                        } else if has_var_positional {
                            Some(Ty::Dynamic)
                        } else {
                            None
                        };
                        if position < positional.len() {
                            position += 1;
                        }
                        expected
                    }
                    CallArg::Keyword(name, _) => infos
                        .iter()
                        .find(|(param, kind, _)| {
                            matches!(
                                kind,
                                ParamKind::PositionalOrKeyword | ParamKind::KeywordOnly
                            ) && param == name
                        })
                        .map(|(_, _, ty)| ty.clone())
                        .or(if has_var_keyword {
                            Some(Ty::Dynamic)
                        } else {
                            None
                        }),
                    CallArg::Starred(_) | CallArg::DoubleStarred(_) => None,
                };
                if let Some(expected) = expected
                    && expected != Ty::Error
                    && arg_ty != Ty::Error
                    && expected != arg_ty
                    && !(expected == Ty::Float && arg_ty == Ty::Int)
                {
                    // 标注不是运行时约束：实参不符时把被调函数降级为动态（M2.1）
                    demote = true;
                }
            }
            if demote {
                self.demote_callees.push(fn_id);
            }
        }
        if callee_dynamic { Ty::Dynamic } else { sig.ret }
    }

    /// dataclass 构造：位置实参按声明顺序、关键字实参按字段名。
    ///
    /// 普通 class（M2.5）的构造是运行时 `PyClass.Call`：`__init__` 绑定、参数错误与
    /// 属性副作用都只有运行时才知道，因此这里直接返回 `Dynamic` 并让函数降级。
    fn check_class_call(&mut self, cls_id: ryupy_hir::ClassId, args: &[CallArg]) -> Ty {
        let cls = &self.module.classes[cls_id];
        if !cls.is_dataclass {
            self.soft_demote = true;
            return Ty::Dynamic;
        }
        let mut position = 0usize;
        for arg in args {
            let arg_ty = self.expr_ty_of(arg.expr());
            let field = match arg {
                CallArg::Positional(_) => {
                    let field = cls.fields.get(position);
                    position += 1;
                    field
                }
                CallArg::Keyword(name, _) => cls.fields.iter().find(|field| &field.name == name),
                CallArg::Starred(_) | CallArg::DoubleStarred(_) => None,
            };
            let Some(field) = field else { continue };
            let expected_ty = type_ref_to_ty(&field.annotation);
            if expected_ty != Ty::Error
                && arg_ty != Ty::Error
                && expected_ty != arg_ty
                && !(expected_ty == Ty::Float && arg_ty == Ty::Int)
            {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, self.func.body.exprs[arg.expr()].range),
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
        Ty::Class(cls_id)
    }

    /// 函数参数的名字、种类与静态类型（顺序与函数签名一致）。
    fn param_infos(&self, fn_id: FuncId) -> Vec<(Name, ParamKind, Ty)> {
        let func = &self.module.functions[fn_id];
        let sig = &self.all_sigs[fn_id];
        func.params
            .iter()
            .enumerate()
            .map(|(index, param)| {
                (
                    func.body.locals[param.local].name.clone(),
                    param.kind,
                    sig.params.get(index).cloned().unwrap_or(Ty::Dynamic),
                )
            })
            .collect()
    }

    /// 读取实参表达式的类型（此前已通过 `check_expr` 写入旁表）。
    fn expr_ty_of(&self, expr: ExprId) -> Ty {
        self.results.exprs.get(expr).cloned().unwrap_or(Ty::Dynamic)
    }
}
