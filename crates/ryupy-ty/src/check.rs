//! 模块级类型检查入口。
//!
//! 流程：
//! 1. 由每个函数的 `TypeRef` 标注计算 [`FnSig`]（缺标注 → 报错 `E0201`，Phase 2 起改为 `Dynamic`）；
//! 2. 逐函数体：为局部变量建 `TyVar` → 遍历语句收集约束并 unify → 最后 resolve 成具体 `Ty`；
//! 3. 仍未确定的变量（例如只声明从未赋值）报错 `E0202`。
//!
//! 运算规则（`int + float → float`、`int / int → float`、`bool` 参与算术视作 `int` 等）
//! 统一写在一个独立函数里，并与 docs/semantics.md 的表格保持一致。

use la_arena::ArenaMap;
use ryupy_base::{Diagnostic, FileId, Span};
use ryupy_hir::{
    AssignTarget, BinOp, Builtin, ExprId, ExprKind, Function, FunctionKind, Literal, LocalId,
    Module, StmtKind, TypeRef,
};

use crate::infer::{InferCtx, InferTy};
use crate::ty::{FnSig, ModuleTypes, Ty, TypeckResults};

pub fn check_module(module: &Module) -> (ModuleTypes, Vec<Diagnostic>) {
    let mut sigs = ArenaMap::default();
    let mut bodies = ArenaMap::default();
    let mut diagnostics = Vec::new();

    // 1. 计算所有函数签名
    for (func_id, func) in module.functions.iter() {
        let sig = compute_fn_sig(module.file, func, &mut diagnostics);
        sigs.insert(func_id, sig);
    }

    // 2. 逐个函数体做类型检查与局部推导
    for (func_id, func) in module.functions.iter() {
        let sig = sigs[func_id].clone();
        let mut results = TypeckResults::default();

        let mut checker = FunctionChecker {
            file: module.file,
            func,
            sig: &sig,
            all_sigs: &sigs,
            infer: InferCtx::default(),
            local_vars: ArenaMap::default(),
            results: &mut results,
            diagnostics: &mut diagnostics,
        };

        checker.check_function();
        bodies.insert(func_id, results);
    }

    (ModuleTypes { sigs, bodies }, diagnostics)
}

fn compute_fn_sig(file: FileId, func: &Function, diags: &mut Vec<Diagnostic>) -> FnSig {
    if func.kind == FunctionKind::ModuleMain {
        return FnSig {
            params: Vec::new(),
            ret: Ty::None,
        };
    }

    let mut params = Vec::new();
    for p in &func.params {
        match &p.annotation {
            Some(type_ref) => params.push(type_ref_to_ty(type_ref)),
            None => {
                diags.push(
                    Diagnostic::error(
                        Span::new(file, p.range),
                        format!("函数 `{}` 的参数缺少类型标注", func.name),
                    )
                    .with_code("E0201")
                    .with_primary_label("Phase 1 要求函数参数必须有类型标注"),
                );
                params.push(Ty::Error);
            }
        }
    }

    let ret = match &func.ret {
        Some(type_ref) => type_ref_to_ty(type_ref),
        None => {
            diags.push(
                Diagnostic::error(
                    Span::new(file, func.range),
                    format!("函数 `{}` 缺少返回值类型标注", func.name),
                )
                .with_code("E0201")
                .with_primary_label("Phase 1 要求函数必须标注返回值类型"),
            );
            Ty::Error
        }
    };

    FnSig { params, ret }
}

fn type_ref_to_ty(type_ref: &TypeRef) -> Ty {
    match type_ref {
        TypeRef::Int => Ty::Int,
        TypeRef::Float => Ty::Float,
        TypeRef::Bool => Ty::Bool,
        TypeRef::Str => Ty::Str,
        TypeRef::None => Ty::None,
        TypeRef::Class(id) => Ty::Class(*id),
        TypeRef::Error => Ty::Error,
    }
}

struct FunctionChecker<'a> {
    file: FileId,
    func: &'a Function,
    sig: &'a FnSig,
    all_sigs: &'a ArenaMap<ryupy_hir::FuncId, FnSig>,
    infer: InferCtx,
    local_vars: ArenaMap<LocalId, InferTy>,
    results: &'a mut TypeckResults,
    diagnostics: &'a mut Vec<Diagnostic>,
}

impl FunctionChecker<'_> {
    fn check_function(&mut self) {
        // 绑定参数类型
        for (i, p) in self.func.params.iter().enumerate() {
            let param_ty = self.sig.params[i].clone();
            self.local_vars.insert(p.local, InferTy::Known(param_ty));
        }

        // 为非参数局部变量分配推导变量
        for (loc_id, loc) in self.func.body.locals.iter() {
            if !loc.is_param {
                let v = self.infer.new_var();
                if let Some(ann) = &loc.annotation {
                    let ann_ty = type_ref_to_ty(ann);
                    let _ = self.infer.unify(&InferTy::Var(v), &InferTy::Known(ann_ty));
                }
                self.local_vars.insert(loc_id, InferTy::Var(v));
            }
        }

        for &stmt_id in &self.func.body.root {
            self.check_stmt(stmt_id);
        }

        // 解析并决议所有局部变量类型
        for (loc_id, loc) in self.func.body.locals.iter() {
            let infer_ty = &self.local_vars[loc_id];
            let resolved = match self.infer.resolve(infer_ty) {
                Some(t) => t,
                None => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, loc.range),
                            format!("无法推导局部变量 `{}` 的类型", loc.name),
                        )
                        .with_code("E0202")
                        .with_primary_label("该变量从未被赋值，类型未定"),
                    );
                    Ty::Error
                }
            };
            self.results.locals.insert(loc_id, resolved);
        }
    }

    fn check_stmt(&mut self, stmt_id: ryupy_hir::StmtId) {
        let stmt = &self.func.body.stmts[stmt_id];
        match &stmt.kind {
            StmtKind::Expr(e) => {
                self.check_expr(*e);
            }
            StmtKind::Assign { target, value } => {
                let val_ty = self.check_expr(*value);
                if let AssignTarget::Local(loc_id) = target {
                    let loc_infer = self.local_vars[*loc_id].clone();
                    if let Err((expected, actual)) =
                        self.infer.unify(&loc_infer, &InferTy::Known(val_ty))
                    {
                        let loc = &self.func.body.locals[*loc_id];
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, self.func.body.exprs[*value].range),
                                format!("类型不匹配：期望 `{expected:?}`，得到 `{actual:?}`"),
                            )
                            .with_code("E0203")
                            .with_primary_label("类型不相容")
                            .with_label(
                                Span::new(self.file, loc.range),
                                "期望类型来自该变量的定义",
                            ),
                        );
                    }
                }
            }
            StmtKind::Return(opt_val) => {
                let ret_ty = match opt_val {
                    Some(e) => self.check_expr(*e),
                    None => Ty::None,
                };
                if self.sig.ret != Ty::Error && ret_ty != Ty::Error && self.sig.ret != ret_ty {
                    let span = match opt_val {
                        Some(e) => Span::new(self.file, self.func.body.exprs[*e].range),
                        None => Span::new(self.file, stmt.range),
                    };
                    self.diagnostics.push(
                        Diagnostic::error(
                            span,
                            format!(
                                "返回值类型不匹配：期望 `{:?}`，得到 `{:?}`",
                                self.sig.ret, ret_ty
                            ),
                        )
                        .with_code("E0203")
                        .with_primary_label("与函数返回类型标注不一致"),
                    );
                }
            }
            StmtKind::Pass => {}
            _ => {}
        }
    }

    fn check_expr(&mut self, expr_id: ExprId) -> Ty {
        let expr = &self.func.body.exprs[expr_id];
        let ty = match &expr.kind {
            ExprKind::Literal(lit) => match lit {
                Literal::Int(_) => Ty::Int,
                Literal::Float(_) => Ty::Float,
                Literal::Bool(_) => Ty::Bool,
                Literal::Str(_) => Ty::Str,
                Literal::None => Ty::None,
            },
            ExprKind::Local(loc_id) => {
                let infer_ty = &self.local_vars[*loc_id];
                self.infer.resolve(infer_ty).unwrap_or(Ty::Error)
            }
            ExprKind::Func(_) => Ty::None,
            ExprKind::Builtin(Builtin::Print) => Ty::None,
            ExprKind::Binary { op, lhs, rhs } => {
                let lhs_ty = self.check_expr(*lhs);
                let rhs_ty = self.check_expr(*rhs);
                self.check_binary_op(*op, lhs_ty, rhs_ty, expr.range)
            }
            ExprKind::Call { callee, args } => self.check_call(*callee, args, expr.range),
            ExprKind::Missing => Ty::Error,
            _ => Ty::Error,
        };

        self.results.exprs.insert(expr_id, ty.clone());
        ty
    }

    fn check_binary_op(&mut self, op: BinOp, lhs: Ty, rhs: Ty, range: ryupy_base::TextRange) -> Ty {
        if lhs == Ty::Error || rhs == Ty::Error {
            return Ty::Error;
        }

        match (lhs, rhs) {
            (Ty::Int, Ty::Int) => match op {
                BinOp::Add | BinOp::Sub | BinOp::Mul => Ty::Int,
                _ => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!("暂不支持运算符 `{op:?}`"),
                        )
                        .with_code("E0200")
                        .with_primary_label("不支持的运算符"),
                    );
                    Ty::Error
                }
            },
            (l, r) => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, range),
                        format!("不支持类型 `{l:?}` 和 `{r:?}` 的二元运算"),
                    )
                    .with_code("E0203")
                    .with_primary_label("运算数类型不匹配"),
                );
                Ty::Error
            }
        }
    }

    fn check_call(&mut self, callee: ExprId, args: &[ExprId], range: ryupy_base::TextRange) -> Ty {
        let callee_expr = &self.func.body.exprs[callee];
        match callee_expr.kind {
            ExprKind::Builtin(Builtin::Print) => {
                self.results.exprs.insert(callee, Ty::None);
                for &arg in args {
                    self.check_expr(arg);
                }
                Ty::None
            }
            ExprKind::Func(target_fn_id) => {
                let target_sig = self.all_sigs[target_fn_id].clone();
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
                    if let Some(expected_ty) = target_sig.params.get(i) {
                        if *expected_ty != Ty::Error
                            && arg_ty != Ty::Error
                            && *expected_ty != arg_ty
                        {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, self.func.body.exprs[arg].range),
                                    format!(
                                        "实参类型不匹配：期望 `{expected_ty:?}`，得到 `{arg_ty:?}`"
                                    ),
                                )
                                .with_code("E0203")
                                .with_primary_label("类型不匹配"),
                            );
                        }
                    }
                }
                target_sig.ret
            }
            _ => {
                self.check_expr(callee);
                for &arg in args {
                    self.check_expr(arg);
                }
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, range), "暂不支持调用非函数对象")
                        .with_code("E0200")
                        .with_primary_label("不是可调用的函数"),
                );
                Ty::Error
            }
        }
    }
}
