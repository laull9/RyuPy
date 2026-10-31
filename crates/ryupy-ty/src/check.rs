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
    AssignTarget, BinOp, ExprId, ExprKind, Function, FunctionKind, Literal, LocalId, Module,
    StmtKind, TypeRef,
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
            module,
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

pub fn type_ref_to_ty(type_ref: &TypeRef) -> Ty {
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

pub(crate) struct FunctionChecker<'a> {
    pub(crate) file: FileId,
    pub(crate) module: &'a Module,
    pub(crate) func: &'a Function,
    pub(crate) sig: &'a FnSig,
    pub(crate) all_sigs: &'a ArenaMap<ryupy_hir::FuncId, FnSig>,
    pub(crate) infer: InferCtx,
    pub(crate) local_vars: ArenaMap<LocalId, InferTy>,
    pub(crate) results: &'a mut TypeckResults,
    pub(crate) diagnostics: &'a mut Vec<Diagnostic>,
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
                match target {
                    AssignTarget::Local(loc_id) => {
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
                    AssignTarget::Attribute { base, name } => {
                        let base_ty = self.check_expr(*base);
                        match base_ty {
                            Ty::Class(cls_id) => {
                                let cls = &self.module.classes[cls_id];
                                if cls.frozen {
                                    self.diagnostics.push(
                                        Diagnostic::error(
                                            Span::new(self.file, self.func.body.exprs[*base].range),
                                            format!(
                                                "不能修改被 @dataclass(frozen=True) 修饰的 `{}` 实例属性",
                                                cls.name
                                            ),
                                        )
                                        .with_code("E0203")
                                        .with_primary_label("frozen dataclass 实例不可变"),
                                    );
                                } else if let Some(field) =
                                    cls.fields.iter().find(|f| f.name == *name)
                                {
                                    let expected_ty = type_ref_to_ty(&field.annotation);
                                    if expected_ty != Ty::Error
                                        && val_ty != Ty::Error
                                        && expected_ty != val_ty
                                        && !(expected_ty == Ty::Float && val_ty == Ty::Int)
                                    {
                                        self.diagnostics.push(
                                            Diagnostic::error(
                                                Span::new(
                                                    self.file,
                                                    self.func.body.exprs[*value].range,
                                                ),
                                                format!(
                                                    "字段 `{}` 赋值类型不匹配：期望 `{expected_ty:?}`，得到 `{val_ty:?}`",
                                                    field.name
                                                ),
                                            )
                                            .with_code("E0203")
                                            .with_primary_label("类型不匹配"),
                                        );
                                    }
                                } else {
                                    self.diagnostics.push(
                                        Diagnostic::error(
                                            Span::new(self.file, stmt.range),
                                            format!("类 `{}` 没有名为 `{}` 的属性", cls.name, name),
                                        )
                                        .with_code("E0203")
                                        .with_primary_label("未定义的属性"),
                                    );
                                }
                            }
                            Ty::Error => {}
                            _ => {
                                self.diagnostics.push(
                                    Diagnostic::error(
                                        Span::new(self.file, stmt.range),
                                        format!("类型 `{base_ty:?}` 不支持属性赋值"),
                                    )
                                    .with_code("E0203")
                                    .with_primary_label("不支持属性赋值"),
                                );
                            }
                        }
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
            StmtKind::If { cond, then_, else_ } => {
                self.check_cond(*cond);
                for &s in then_ {
                    self.check_stmt(s);
                }
                for &s in else_ {
                    self.check_stmt(s);
                }
            }
            StmtKind::While { cond, body } => {
                self.check_cond(*cond);
                for &s in body {
                    self.check_stmt(s);
                }
            }
            StmtKind::ForRange {
                var,
                start,
                stop,
                step,
                body,
            } => {
                let start_ty = self.check_expr(*start);
                let stop_ty = self.check_expr(*stop);
                let step_ty = step.map(|s| self.check_expr(s)).unwrap_or(Ty::Int);

                if start_ty != Ty::Error && start_ty != Ty::Int {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, self.func.body.exprs[*start].range),
                            format!("range() 的 start 必须为 int，得到 `{start_ty:?}`"),
                        )
                        .with_code("E0203")
                        .with_primary_label("期望 int 类型"),
                    );
                }
                if stop_ty != Ty::Error && stop_ty != Ty::Int {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, self.func.body.exprs[*stop].range),
                            format!("range() 的 stop 必须为 int，得到 `{stop_ty:?}`"),
                        )
                        .with_code("E0203")
                        .with_primary_label("期望 int 类型"),
                    );
                }
                if step_ty != Ty::Error && step_ty != Ty::Int {
                    if let Some(s) = step {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, self.func.body.exprs[*s].range),
                                format!("range() 的 step 必须为 int，得到 `{step_ty:?}`"),
                            )
                            .with_code("E0203")
                            .with_primary_label("期望 int 类型"),
                        );
                    }
                }

                // 循环变量 var 的类型必须是 int
                let var_ty = self.local_vars[*var].clone();
                let _ = self
                    .infer
                    .unify(&var_ty, &crate::infer::InferTy::Known(Ty::Int));

                for &s in body {
                    self.check_stmt(s);
                }
            }
            StmtKind::Break | StmtKind::Continue => {}
        }
    }

    fn check_cond(&mut self, cond: ExprId) {
        let cond_ty = self.check_expr(cond);
        if cond_ty != Ty::Error && cond_ty != Ty::Bool && cond_ty != Ty::Int && cond_ty != Ty::Float
        {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, self.func.body.exprs[cond].range),
                    format!("条件必须为布尔值或数值，得到 `{cond_ty:?}`"),
                )
                .with_code("E0203")
                .with_primary_label("条件表达式类型无效"),
            );
        }
    }

    pub(crate) fn check_expr(&mut self, expr_id: ExprId) -> Ty {
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
            ExprKind::Builtin(_) => Ty::None,
            ExprKind::Binary { op, lhs, rhs } => {
                let lhs_ty = self.check_expr(*lhs);
                let rhs_ty = self.check_expr(*rhs);
                self.check_binary_op(*op, lhs_ty, rhs_ty, expr.range)
            }
            ExprKind::Unary { op, operand } => {
                let operand_ty = self.check_expr(*operand);
                match crate::ops::check_unary_op(*op, &operand_ty) {
                    Some(t) => t,
                    None => {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, expr.range),
                                format!("类型 `{operand_ty:?}` 不支持一元运算符 `{op:?}`"),
                            )
                            .with_code("E0203")
                            .with_primary_label("类型不支持此一元操作"),
                        );
                        Ty::Error
                    }
                }
            }
            ExprKind::Compare { op, lhs, rhs } => {
                let lhs_ty = self.check_expr(*lhs);
                let rhs_ty = self.check_expr(*rhs);
                match crate::ops::check_cmp_op(*op, &lhs_ty, &rhs_ty) {
                    Some(t) => t,
                    None => {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, expr.range),
                                format!("类型 `{lhs_ty:?}` 和 `{rhs_ty:?}` 之间不支持比较运算"),
                            )
                            .with_code("E0203")
                            .with_primary_label("比较运算类型不匹配"),
                        );
                        Ty::Error
                    }
                }
            }
            ExprKind::BoolOp { op: _, lhs, rhs } => {
                let lhs_ty = self.check_expr(*lhs);
                let rhs_ty = self.check_expr(*rhs);
                if lhs_ty == Ty::Error || rhs_ty == Ty::Error {
                    Ty::Error
                } else if lhs_ty == rhs_ty {
                    lhs_ty
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, expr.range),
                            format!("短路逻辑两端类型不一致：`{lhs_ty:?}` vs `{rhs_ty:?}`"),
                        )
                        .with_code("E0203")
                        .with_primary_label("Phase 1 要求短路逻辑操作数类型一致"),
                    );
                    Ty::Error
                }
            }
            ExprKind::IfExp { cond, then_, else_ } => {
                self.check_cond(*cond);
                let then_ty = self.check_expr(*then_);
                let else_ty = self.check_expr(*else_);
                if then_ty == Ty::Error || else_ty == Ty::Error {
                    Ty::Error
                } else if then_ty == else_ty {
                    then_ty
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, expr.range),
                            format!("条件表达式两分支类型不一致：`{then_ty:?}` vs `{else_ty:?}`"),
                        )
                        .with_code("E0203")
                        .with_primary_label("条件表达式两分支类型必须相同"),
                    );
                    Ty::Error
                }
            }
            ExprKind::Class(cls_id) => Ty::Class(*cls_id),
            ExprKind::Attribute { base, name } => {
                let base_ty = self.check_expr(*base);
                match base_ty {
                    Ty::Class(cls_id) => {
                        let cls = &self.module.classes[cls_id];
                        if let Some(field) = cls.fields.iter().find(|f| f.name == *name) {
                            type_ref_to_ty(&field.annotation)
                        } else {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, expr.range),
                                    format!("类 `{}` 没有名为 `{}` 的属性", cls.name, name),
                                )
                                .with_code("E0203")
                                .with_primary_label("未定义的属性"),
                            );
                            Ty::Error
                        }
                    }
                    Ty::Error => Ty::Error,
                    _ => {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, expr.range),
                                format!("类型 `{base_ty:?}` 不支持属性访问 `{name}`"),
                            )
                            .with_code("E0203")
                            .with_primary_label("类型不支持属性访问"),
                        );
                        Ty::Error
                    }
                }
            }
            ExprKind::Call { callee, args } => self.check_call(*callee, args, expr.range),
            ExprKind::Missing => Ty::Error,
        };

        self.results.exprs.insert(expr_id, ty.clone());
        ty
    }

    fn check_binary_op(&mut self, op: BinOp, lhs: Ty, rhs: Ty, range: ryupy_base::TextRange) -> Ty {
        match crate::ops::check_binary_op(op, &lhs, &rhs) {
            Some(t) => t,
            None => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, range),
                        format!("不支持类型 `{lhs:?}` 和 `{rhs:?}` 的二元运算符 `{op:?}`"),
                    )
                    .with_code("E0203")
                    .with_primary_label("运算数类型不匹配"),
                );
                Ty::Error
            }
        }
    }
}
