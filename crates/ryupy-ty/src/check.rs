//! 模块级类型检查入口（M2.1：静态优先，无法证明则整函数降级为动态）。
//!
//! 流程：
//! 1. **初始模式**：标注齐全的 `def` 与模块入口先按 `Static` 处理；缺任何标注直接 `Dynamic`。
//!    Python 不因标注拒绝实参，所以「缺标注」不再是错误（`E0201` 已取消）。
//! 2. **不动点迭代**：按当前模式跑一遍静态检查。任何函数只要出现以下情况就降级为 `Dynamic`：
//!    - 函数体里有表达式 / 局部变量只能定成 `Dynamic`（调用了动态函数、异构赋值、多返回类型…）；
//!    - 出现超出 i64 的整数字面量（需要装箱 BigInteger）；
//!    - 某个调用点的实参类型与静态签名不符 → 把**被调函数**降级（它的标注只是优化线索）。
//!
//!    降级是单调的，因此迭代一定收敛；收敛后用最终模式重跑一遍产出类型旁表。
//! 3. 动态函数不产出旁表：下游把缺失的类型一律读成 `Dynamic`，也就自然走装箱路径。
//!
//! 运算规则仍集中在 [`crate::ops`]，与 docs/semantics.md 的表格保持一致。

use la_arena::ArenaMap;
use ryupy_base::{Diagnostic, FileId, Span, TextRange};
use ryupy_hir::{
    AssignTarget, BinOp, ExprId, ExprKind, Function, FunctionKind, Literal, LocalId, Module, Name,
    StmtKind, TypeRef,
};

use crate::infer::{InferCtx, InferTy};
use crate::ty::{FnSig, FuncMode, ModuleTypes, Ty, TypeckResults};

/// 不动点迭代上限。正常情况下 2~3 轮即收敛；上限只是防止将来引入新的降级来源时死循环。
const MAX_ROUNDS: usize = 16;

pub fn check_module(module: &Module) -> (ModuleTypes, Vec<Diagnostic>) {
    let mut modes: ArenaMap<ryupy_hir::FuncId, FuncMode> = ArenaMap::default();
    for (func_id, func) in module.functions.iter() {
        modes.insert(func_id, initial_mode(func));
    }

    // ── 不动点：只降不升 ──────────────────────────────────────────────
    for _ in 0..MAX_ROUNDS {
        let sigs = compute_sigs(module, &modes);
        let mut changed = false;
        for (func_id, _func) in module.functions.iter() {
            if modes[func_id].is_dynamic() {
                continue;
            }
            let outcome = try_static(module, &sigs, &modes, func_id);
            if outcome.needs_dynamic {
                modes.insert(func_id, FuncMode::Dynamic);
                changed = true;
            }
            for callee in outcome.demote_callees {
                if !modes[callee].is_dynamic() {
                    modes.insert(callee, FuncMode::Dynamic);
                    changed = true;
                }
            }
        }
        if !changed {
            break;
        }
    }

    // ── 最终一轮：产出签名与旁表 ─────────────────────────────────────
    let sigs = compute_sigs(module, &modes);
    let mut bodies = ArenaMap::default();
    let mut diagnostics = Vec::new();
    for (func_id, func) in module.functions.iter() {
        if modes[func_id].is_dynamic() {
            // 动态函数没有静态事实，保留空旁表即可
            bodies.insert(func_id, TypeckResults::default());
            continue;
        }
        let sig = sigs[func_id].clone();
        let mut results = TypeckResults::default();
        let mut checker = FunctionChecker {
            file: module.file,
            module,
            func,
            sig: &sig,
            all_sigs: &sigs,
            all_modes: &modes,
            infer: InferCtx::default(),
            local_vars: ArenaMap::default(),
            results: &mut results,
            diagnostics: &mut diagnostics,
            soft_demote: false,
            demote_callees: Vec::new(),
        };
        checker.check_function();
        bodies.insert(func_id, results);
    }

    let types = ModuleTypes {
        sigs,
        bodies,
        modes,
    };
    (types, diagnostics)
}

/// 标注齐全才可能走静态路径；模块入口没有标注，但从静态开始（Phase 1 行为）。
fn initial_mode(func: &Function) -> FuncMode {
    if func.kind == FunctionKind::ModuleMain {
        return FuncMode::Static;
    }
    let params_annotated = func.params.iter().all(|p| p.annotation.is_some());
    if params_annotated && func.ret.is_some() {
        FuncMode::Static
    } else {
        FuncMode::Dynamic
    }
}

fn compute_sigs(
    module: &Module,
    modes: &ArenaMap<ryupy_hir::FuncId, FuncMode>,
) -> ArenaMap<ryupy_hir::FuncId, FnSig> {
    let mut sigs = ArenaMap::default();
    for (func_id, func) in module.functions.iter() {
        let sig = if modes[func_id].is_dynamic() {
            // 动态函数：参数与返回值全部装箱；入口仍是 void
            FnSig {
                params: func.params.iter().map(|_| Ty::Dynamic).collect(),
                ret: if func.kind == FunctionKind::ModuleMain {
                    Ty::None
                } else {
                    Ty::Dynamic
                },
            }
        } else if func.kind == FunctionKind::ModuleMain {
            FnSig {
                params: Vec::new(),
                ret: Ty::None,
            }
        } else {
            FnSig {
                params: func
                    .params
                    .iter()
                    .map(|p| {
                        p.annotation
                            .as_ref()
                            .map(type_ref_to_ty)
                            .unwrap_or(Ty::Dynamic)
                    })
                    .collect(),
                ret: func.ret.as_ref().map(type_ref_to_ty).unwrap_or(Ty::Dynamic),
            }
        };
        sigs.insert(func_id, sig);
    }
    sigs
}

struct StaticOutcome {
    /// 本函数必须降级（体内出现 Dynamic 或超出 i64 的字面量）。
    needs_dynamic: bool,
    /// 需要降级的被调函数（调用点实参与静态签名不符）。
    demote_callees: Vec<ryupy_hir::FuncId>,
}

fn try_static(
    module: &Module,
    sigs: &ArenaMap<ryupy_hir::FuncId, FnSig>,
    modes: &ArenaMap<ryupy_hir::FuncId, FuncMode>,
    func_id: ryupy_hir::FuncId,
) -> StaticOutcome {
    let func = &module.functions[func_id];
    let sig = sigs[func_id].clone();
    let mut results = TypeckResults::default();
    let (soft_demote, demote_callees) = {
        let mut diagnostics = Vec::new();
        let mut checker = FunctionChecker {
            file: module.file,
            module,
            func,
            sig: &sig,
            all_sigs: sigs,
            all_modes: modes,
            infer: InferCtx::default(),
            local_vars: ArenaMap::default(),
            results: &mut results,
            diagnostics: &mut diagnostics,
            soft_demote: false,
            demote_callees: Vec::new(),
        };
        checker.check_function();
        (
            checker.soft_demote,
            std::mem::take(&mut checker.demote_callees),
        )
    };

    // 只要函数体里出现 Dynamic 事实，静态发射就无法完成，降级处理
    let has_dynamic = results.exprs.iter().any(|(_, ty)| ty.is_dynamic())
        || results.locals.iter().any(|(_, ty)| ty.is_dynamic());

    StaticOutcome {
        needs_dynamic: soft_demote || has_dynamic,
        demote_callees,
    }
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
    pub(crate) all_modes: &'a ArenaMap<ryupy_hir::FuncId, FuncMode>,
    pub(crate) infer: InferCtx,
    pub(crate) local_vars: ArenaMap<LocalId, InferTy>,
    pub(crate) results: &'a mut TypeckResults,
    pub(crate) diagnostics: &'a mut Vec<Diagnostic>,
    /// 本函数无法静态定型的信号（异构赋值、需要装箱的字面量等）。
    pub(crate) soft_demote: bool,
    /// 调用点实参与静态签名不符时要降级的被调函数。
    pub(crate) demote_callees: Vec<ryupy_hir::FuncId>,
}

impl FunctionChecker<'_> {
    fn check_function(&mut self) {
        for (i, p) in self.func.params.iter().enumerate() {
            let param_ty = self.sig.params[i].clone();
            self.local_vars.insert(p.local, InferTy::Known(param_ty));
        }

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

        for (loc_id, _loc) in self.func.body.locals.iter() {
            let infer_ty = self.local_vars[loc_id].clone();
            let resolved = match self.infer.resolve(&infer_ty) {
                Some(t) => t,
                // 声明了却从未赋值：Python 在读取时才抛 UnboundLocalError，
                // 这里不阻断编译，交给 MIR 的确定赋值分析与运行时。
                None => Ty::Dynamic,
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
                        // 异构赋值（`x = 1; x = "a"`）在 Python 里合法。静态推导拿不到
                        // 单一类型，就把变量改绑成 Dynamic 并让整函数降级。
                        if self
                            .infer
                            .unify(&loc_infer, &InferTy::Known(val_ty))
                            .is_err()
                        {
                            self.soft_demote = true;
                            self.infer.force(&loc_infer, Ty::Dynamic);
                        }
                    }
                    AssignTarget::Attribute { base, name } => {
                        let base_ty = self.check_expr(*base);
                        self.check_attribute_assign(
                            base_ty,
                            *base,
                            name.clone(),
                            val_ty,
                            stmt.range,
                        );
                    }
                }
            }
            StmtKind::Return(opt_val) => {
                let ret_ty = match opt_val {
                    Some(e) => self.check_expr(*e),
                    None => Ty::None,
                };
                // 返回类型与标注不一致不再是错误：标注只是优化线索，整函数降级即可
                if self
                    .infer
                    .unify(
                        &InferTy::Known(self.sig.ret.clone()),
                        &InferTy::Known(ret_ty),
                    )
                    .is_err()
                {
                    self.soft_demote = true;
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

                self.expect_range_int(*start, &start_ty, "start");
                self.expect_range_int(*stop, &stop_ty, "stop");
                if let Some(s) = step {
                    self.expect_range_int(*s, &step_ty, "step");
                }

                let var_ty = self.local_vars[*var].clone();
                let _ = self
                    .infer
                    .unify(&var_ty, &crate::infer::InferTy::Known(Ty::Int));

                for &s in body {
                    self.check_stmt(s);
                }
            }
            StmtKind::Raise { value, cause } => {
                if let Some(v) = value {
                    self.check_expr(*v);
                }
                if let Some(c) = cause {
                    self.check_expr(*c);
                }
            }
            StmtKind::Try {
                body,
                handlers,
                orelse,
                finalbody,
            } => {
                for &s in body {
                    self.check_stmt(s);
                }
                for &handler_id in handlers {
                    let (name, handler_body) = {
                        let handler = &self.func.body.handlers[handler_id];
                        (handler.name, handler.body.clone())
                    };
                    // 异常对象是动态值：`as e` 的局部变量按 Dynamic 处理，
                    // 同时让整函数降级——异常对象上的属性（`args` / `__cause__`）
                    // 需要 M2.5 的属性协议，静态槽位放不下。
                    if let Some(local) = name {
                        let loc_infer = self.local_vars[local].clone();
                        if self
                            .infer
                            .unify(&loc_infer, &InferTy::Known(Ty::Dynamic))
                            .is_err()
                        {
                            self.soft_demote = true;
                            self.infer.force(&loc_infer, Ty::Dynamic);
                        }
                    }
                    for s in handler_body {
                        self.check_stmt(s);
                    }
                }
                for &s in orelse {
                    self.check_stmt(s);
                }
                for &s in finalbody {
                    self.check_stmt(s);
                }
            }
            StmtKind::Break | StmtKind::Continue => {}
        }
    }

    /// `for x in range(...)` 的参数必须是整数；动态值交给运行时判定。
    fn expect_range_int(&mut self, expr: ExprId, ty: &Ty, label: &str) {
        if matches!(ty, Ty::Int | Ty::Bool | Ty::Dynamic | Ty::Error) {
            return;
        }
        self.diagnostics.push(
            Diagnostic::error(
                Span::new(self.file, self.func.body.exprs[expr].range),
                format!("range() 的 {label} 必须为 int，得到 `{ty:?}`"),
            )
            .with_code("E0203")
            .with_primary_label("期望 int 类型"),
        );
    }

    /// 真值判定接受一切类型（Python 里任何对象都有真值），因此不做检查。
    fn check_cond(&mut self, cond: ExprId) {
        let _ = self.check_expr(cond);
    }

    pub(crate) fn check_expr(&mut self, expr_id: ExprId) -> Ty {
        let expr = &self.func.body.exprs[expr_id];
        let ty = match &expr.kind {
            ExprKind::Literal(lit) => match lit {
                Literal::Int(v) => {
                    if i64::try_from(*v).is_err() {
                        // 需要 BigInteger 装箱，静态路径放不下
                        self.soft_demote = true;
                    }
                    Ty::Int
                }
                Literal::Float(_) => Ty::Float,
                Literal::Bool(_) => Ty::Bool,
                Literal::Str(_) => Ty::Str,
                Literal::None => Ty::None,
            },
            ExprKind::Local(loc_id) => {
                let infer_ty = self.local_vars[*loc_id].clone();
                self.infer.resolve(&infer_ty).unwrap_or(Ty::Error)
            }
            ExprKind::Func(_) => Ty::None,
            ExprKind::Builtin(builtin) => match builtin {
                ryupy_hir::Builtin::Str => Ty::Str,
                ryupy_hir::Builtin::Type => Ty::Dynamic,
                _ => Ty::None,
            },
            ExprKind::Binary { op, lhs, rhs } => {
                let lhs_ty = self.check_expr(*lhs);
                let rhs_ty = self.check_expr(*rhs);
                if lhs_ty.is_dynamic() || rhs_ty.is_dynamic() {
                    Ty::Dynamic
                } else {
                    self.check_binary_op(*op, lhs_ty, rhs_ty)
                }
            }
            ExprKind::Unary { op, operand } => {
                let operand_ty = self.check_expr(*operand);
                if operand_ty.is_dynamic() {
                    Ty::Dynamic
                } else {
                    match crate::ops::check_unary_op(*op, &operand_ty) {
                        Some(t) => t,
                        None => {
                            // Python 写得出来、运行时才抛 TypeError
                            self.soft_demote = true;
                            Ty::Dynamic
                        }
                    }
                }
            }
            ExprKind::Compare { op, lhs, rhs } => {
                let lhs_ty = self.check_expr(*lhs);
                let rhs_ty = self.check_expr(*rhs);
                if lhs_ty.is_dynamic() || rhs_ty.is_dynamic() {
                    Ty::Dynamic
                } else {
                    match crate::ops::check_cmp_op(*op, &lhs_ty, &rhs_ty) {
                        Some(t) => t,
                        None => {
                            self.soft_demote = true;
                            Ty::Dynamic
                        }
                    }
                }
            }
            ExprKind::BoolOp { op: _, lhs, rhs } => {
                let lhs_ty = self.check_expr(*lhs);
                let rhs_ty = self.check_expr(*rhs);
                // `and` / `or` 返回操作数本身，两端类型不同也合法
                if lhs_ty == rhs_ty {
                    lhs_ty
                } else {
                    Ty::Dynamic
                }
            }
            ExprKind::IfExp { cond, then_, else_ } => {
                self.check_cond(*cond);
                let then_ty = self.check_expr(*then_);
                let else_ty = self.check_expr(*else_);
                if then_ty == else_ty {
                    then_ty
                } else {
                    Ty::Dynamic
                }
            }
            ExprKind::Class(cls_id) => Ty::Class(*cls_id),
            ExprKind::Attribute { base, name } => {
                let base_ty = self.check_expr(*base);
                self.check_attribute_read(&base_ty, expr.range, name)
            }
            ExprKind::Call { callee, args } => self.check_call(*callee, args, expr.range),
            ExprKind::Missing => Ty::Error,
        };

        self.results.exprs.insert(expr_id, ty.clone());
        ty
    }

    fn check_binary_op(&mut self, op: BinOp, lhs: Ty, rhs: Ty) -> Ty {
        match crate::ops::check_binary_op(op, &lhs, &rhs) {
            Some(t) => t,
            None => {
                // 静态能看出不相容，但 Python 只在运行时抛 TypeError：
                // 降级到动态路径，由 PyOps 给出准确消息。
                self.soft_demote = true;
                Ty::Dynamic
            }
        }
    }

    fn check_attribute_read(&mut self, base_ty: &Ty, range: TextRange, name: &Name) -> Ty {
        match base_ty {
            Ty::Class(cls_id) => {
                let cls = &self.module.classes[*cls_id];
                if let Some(field) = cls.fields.iter().find(|f| f.name == *name) {
                    type_ref_to_ty(&field.annotation)
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!("类 `{}` 没有名为 `{}` 的属性", cls.name, name),
                        )
                        .with_code("E0203")
                        .with_primary_label("未定义的属性"),
                    );
                    Ty::Error
                }
            }
            Ty::Dynamic | Ty::Error => Ty::Dynamic,
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, range),
                        format!("类型 `{base_ty:?}` 不支持属性访问 `{name}`"),
                    )
                    .with_code("E0203")
                    .with_primary_label("类型不支持属性访问"),
                );
                Ty::Error
            }
        }
    }

    fn check_attribute_assign(
        &mut self,
        base_ty: Ty,
        base: ExprId,
        name: Name,
        val_ty: Ty,
        range: TextRange,
    ) {
        match base_ty {
            Ty::Class(cls_id) => {
                let cls = &self.module.classes[cls_id];
                if cls.frozen {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, self.func.body.exprs[base].range),
                            format!(
                                "不能修改被 @dataclass(frozen=True) 修饰的 `{}` 实例属性",
                                cls.name
                            ),
                        )
                        .with_code("E0203")
                        .with_primary_label("frozen dataclass 实例不可变"),
                    );
                } else if let Some(field) = cls.fields.iter().find(|f| f.name == name) {
                    let expected_ty = type_ref_to_ty(&field.annotation);
                    if expected_ty != Ty::Error
                        && val_ty != Ty::Error
                        && expected_ty != val_ty
                        && !(expected_ty == Ty::Float && val_ty == Ty::Int)
                    {
                        // 字段类型不符：与实参一样交给运行时
                        self.soft_demote = true;
                    }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, range),
                            format!("类 `{}` 没有名为 `{}` 的属性", cls.name, name),
                        )
                        .with_code("E0203")
                        .with_primary_label("未定义的属性"),
                    );
                }
            }
            Ty::Dynamic | Ty::Error => self.soft_demote = true,
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, range),
                        format!("类型 `{base_ty:?}` 不支持属性赋值"),
                    )
                    .with_code("E0203")
                    .with_primary_label("不支持属性赋值"),
                );
            }
        }
    }
}
