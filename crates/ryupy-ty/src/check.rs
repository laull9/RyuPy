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
use ryupy_base::{Diagnostic, FileId};
use ryupy_hir::{
    BinOp, ExprId, ExprKind, Function, FunctionKind, Literal, LocalId, Module, TypeRef,
};

use crate::infer::{InferCtx, InferTy};
use crate::ty::{FnSig, FuncMode, ModuleTypes, Ty, TypeckResults};

/// 不动点迭代上限。正常情况下 2~3 轮即收敛；上限只是防止将来引入新的降级来源时死循环。
const MAX_ROUNDS: usize = 16;

pub fn check_module(module: &Module) -> (ModuleTypes, Vec<Diagnostic>) {
    let mut modes: ArenaMap<ryupy_hir::FuncId, FuncMode> = ArenaMap::default();
    for (func_id, func) in module.functions.iter() {
        modes.insert(func_id, initial_mode(module, func));
    }

    // 模块级变量的槽位类型只依赖 HIR 结构，先算好供所有轮次复用。
    let module_vars = crate::module_vars::infer_module_var_types(module);
    crate::default_modes::apply(module, &module_vars, &mut modes);

    // ── 不动点：只降不升 ──────────────────────────────────────────────
    for _ in 0..MAX_ROUNDS {
        let sigs = compute_sigs(module, &modes);
        let mut changed = false;
        for (func_id, _func) in module.functions.iter() {
            if modes[func_id].is_dynamic() {
                continue;
            }
            let outcome = try_static(module, &sigs, &modes, &module_vars, func_id);
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

    // 模块入口最终要走动态路径时，模块级槽位也统一装箱：
    // 动态执行可能产生 BigInteger 等静态槽位放不下的值，写回静态槽位会溢出或截断。
    let mut module_vars = module_vars;
    if modes[module.main].is_dynamic() {
        for (var_id, _) in module.vars.iter() {
            module_vars.insert(var_id, Ty::Dynamic);
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
            module_vars: &module_vars,
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
        module_vars,
    };
    (types, diagnostics)
}

/// 标注齐全才可能走静态路径；模块入口没有标注，但从静态开始（Phase 1 行为）。
fn initial_mode(module: &Module, func: &Function) -> FuncMode {
    // M3.0：闭包能力一律走动态路径——
    // - 含嵌套 def / lambda 的函数需要创建函数对象与 cell 装箱；
    // - 引用外层 cell（free 非空）的函数需要 env 参数与动态读取。
    // 两者都不影响未捕获函数保留静态快路径。
    if func.is_generator || func.closure.has_nested || !func.closure.free.is_empty() {
        return FuncMode::Dynamic;
    }
    // 装饰后的类标注只指向公开动态值，不能据原 ClassId 生成静态布局。
    if func.params.iter().filter_map(|p| p.annotation.as_ref())
        .chain(func.ret.iter())
        .chain(func.body.locals.iter().filter_map(|(_, l)| l.annotation.as_ref()))
        .any(|ann| matches!(ann, TypeRef::Class(id) if (module.classes[*id].decorated_binding.is_some() || module.classes[*id].runtime_dataclass)))
    {
        return FuncMode::Dynamic;
    }
    if func.kind == FunctionKind::ModuleMain {
        return FuncMode::Static;
    }
    // `*args` / `**kwargs` 的槽位在运行时是 tuple / dict，标注不能改变这一点；
    // 这类函数一律走动态路径（参数是装箱值），由运行时完成绑定。
    let has_variadic = func.params.iter().any(|p| {
        matches!(
            p.kind,
            ryupy_hir::ParamKind::VarPositional | ryupy_hir::ParamKind::VarKeyword
        )
    });
    let params_annotated = func.params.iter().all(|p| p.annotation.is_some());
    if !has_variadic && params_annotated && func.ret.is_some() {
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
                            .map(|ann| module_type_ref_to_ty(module, ann))
                            .unwrap_or(Ty::Dynamic)
                    })
                    .collect(),
                ret: func
                    .ret
                    .as_ref()
                    .map(|ann| module_type_ref_to_ty(module, ann))
                    .unwrap_or(Ty::Dynamic),
            }
        };
        sigs.insert(func_id, sig);
    }
    sigs
}

/// 类装饰器可替换类对象；原静态布局不再是可信类型事实。
pub(crate) fn module_type_ref_to_ty(module: &Module, annotation: &TypeRef) -> Ty {
    match annotation {
        TypeRef::Class(id)
            if (module.classes[*id].decorated_binding.is_some()
                || module.classes[*id].runtime_dataclass) =>
        {
            Ty::Dynamic
        }
        _ => type_ref_to_ty(annotation),
    }
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
    module_vars: &ArenaMap<ryupy_hir::ModuleVarId, Ty>,
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
            module_vars,
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

/// 取表达式的直接子表达式（用于「容器字面量整体降级但仍要检查元素」）。
fn expr_children(kind: &ExprKind) -> Vec<ExprId> {
    match kind {
        ExprKind::List(items) | ExprKind::Tuple(items) | ExprKind::Set(items) => items.clone(),
        ExprKind::Dict(pairs) => pairs.iter().flat_map(|(k, v)| [*k, *v]).collect(),
        ExprKind::Subscript { base, index } => vec![*base, *index],
        ExprKind::Slice { start, stop, step } => {
            [*start, *stop, *step].into_iter().flatten().collect()
        }
        ExprKind::Starred(inner) | ExprKind::YieldFrom(inner) => vec![*inner],
        ExprKind::Yield(value) => value.iter().copied().collect(),
        _ => Vec::new(),
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
        TypeRef::Runtime => Ty::Dynamic,
    }
}

pub(crate) struct FunctionChecker<'a> {
    pub(crate) file: FileId,
    pub(crate) module: &'a Module,
    pub(crate) func: &'a Function,
    pub(crate) sig: &'a FnSig,
    pub(crate) all_sigs: &'a ArenaMap<ryupy_hir::FuncId, FnSig>,
    pub(crate) all_modes: &'a ArenaMap<ryupy_hir::FuncId, FuncMode>,
    /// 模块级变量的槽位类型。
    pub(crate) module_vars: &'a ArenaMap<ryupy_hir::ModuleVarId, Ty>,
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
                    let ann_ty = module_type_ref_to_ty(self.module, ann);
                    let _ = self.infer.unify(&InferTy::Var(v), &InferTy::Known(ann_ty));
                }
                self.local_vars.insert(loc_id, InferTy::Var(v));
            }
        }

        for &stmt_id in &self.func.body.root {
            self.check_stmt(stmt_id);
        }

        // 参数默认值在模块作用域、def 执行时求值：放在语句之后检查，
        // 这样默认值引用的模块变量已经完成类型推导（类型与求值顺序无关）。
        // 它们仍然让 `has_dynamic` 能据此决定 main 是否降级。
        if self.func.kind == FunctionKind::ModuleMain {
            for &expr in &self.module.default_exprs {
                self.check_expr(expr);
            }
            // 类体属性表达式也在模块作用域求值，必须补齐类型旁表，
            // 否则 MIR 会把它们当成都态值而丢失静态特化。
            for (_, class) in self.module.classes.iter() {
                self.soft_demote |= class.decorated_binding.is_some() || class.runtime_dataclass;
                for decorator in &class.decorators {
                    if let ryupy_hir::Decorator::Expr(expr) = decorator {
                        self.check_expr(*expr);
                    }
                }
                if let Some(
                    ryupy_hir::ClassBase::Expr(expr)
                    | ryupy_hir::ClassBase::Runtime(expr)
                    | ryupy_hir::ClassBase::Native { value: expr, .. }
                    | ryupy_hir::ClassBase::TypedDict { value: expr, .. },
                ) = &class.base
                {
                    self.check_expr(*expr);
                }
                self.check_class_items(&class.body);
            }
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

    fn check_class_items(&mut self, items: &[ryupy_hir::ClassItem]) {
        for item in items {
            match &item.kind {
                ryupy_hir::ClassItemKind::If { test, body, orelse } => {
                    self.check_expr(*test);
                    self.check_class_items(body);
                    self.check_class_items(orelse);
                }
                ryupy_hir::ClassItemKind::Annotation { value, .. }
                | ryupy_hir::ClassItemKind::Attr { value, .. }
                | ryupy_hir::ClassItemKind::Expr(value) => {
                    self.check_expr(*value);
                }
                ryupy_hir::ClassItemKind::Method { decorators, .. } => {
                    for decorator in decorators {
                        if let ryupy_hir::Decorator::Expr(expr) = decorator {
                            self.check_expr(*expr);
                        }
                    }
                }
            }
        }
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
                Literal::BigInt(_) => {
                    self.soft_demote = true;
                    Ty::Dynamic
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
            ExprKind::ModuleVar(var) => self.module_vars.get(*var).cloned().unwrap_or(Ty::Dynamic),
            // 闭包自由变量与 lambda 对象都是动态值（M3.0）
            ExprKind::ClassLookup { fallback, .. } => {
                self.check_expr(*fallback);
                Ty::Dynamic
            }
            ExprKind::Free(_) | ExprKind::Lambda(_) | ExprKind::ClassLocal(_) => Ty::Dynamic,
            // 函数名作为值使用时是一等函数对象；调用位置由 check_call 单独处理。
            ExprKind::Func(id) => Ty::Func(*id),
            ExprKind::Builtin(builtin) => match builtin {
                ryupy_hir::Builtin::Str => Ty::Str,
                ryupy_hir::Builtin::Type => Ty::Dynamic,
                _ => Ty::None,
            },
            ExprKind::Binary { op, lhs, rhs, .. } => {
                let lhs_ty = self.check_expr(*lhs);
                let rhs_ty = self.check_expr(*rhs);
                if lhs_ty.is_dynamic() || rhs_ty.is_dynamic() {
                    Ty::Dynamic
                } else if *op == BinOp::Pow
                    && lhs_ty == Ty::Int
                    && rhs_ty == Ty::Int
                    && !self.is_nonneg_int_literal(*rhs)
                {
                    // Python 的 `int ** int` 在指数为负时返回 float（`2 ** -1 == 0.5`），
                    // 结果类型取决于运行时的指数值。只有指数是确定的非负字面量时，
                    // 结果才稳定是 int，可以走静态快速幂；其余情况让整个表达式动态化，
                    // 由运行时按真实指数选择 int 或 float。
                    self.soft_demote = true;
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
                // `in` / `is` 走运行时容器协议，静态路径不展开
                if matches!(
                    op,
                    ryupy_hir::CmpOp::In
                        | ryupy_hir::CmpOp::NotIn
                        | ryupy_hir::CmpOp::Is
                        | ryupy_hir::CmpOp::IsNot
                ) {
                    self.soft_demote = true;
                    Ty::Dynamic
                } else if lhs_ty.is_dynamic() || rhs_ty.is_dynamic() {
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
            ExprKind::Class(cls_id) => {
                if self.module.classes[*cls_id].is_dataclass
                    && (self.module.classes[*cls_id].decorated_binding.is_none()
                        && !self.module.classes[*cls_id].runtime_dataclass)
                {
                    Ty::Class(*cls_id)
                } else {
                    // 普通 class 的类对象与实例都是运行时 `PyClass` / `PyInstance`，
                    // 静态槽位放不下；整函数走动态路径（M2.5）。
                    self.soft_demote = true;
                    Ty::Dynamic
                }
            }
            ExprKind::Attribute { base, name } => {
                let base_ty = self.check_expr(*base);
                self.check_attribute_read(&base_ty, expr.range, name)
            }
            ExprKind::Call { callee, args } | ExprKind::PreparedCall { callee, args } => {
                self.check_call(*callee, args, expr.range)
            }
            ExprKind::FString(parts) => {
                for part in parts {
                    if let ryupy_hir::FStringPart::Value { value, .. } = part {
                        self.check_expr(*value);
                    }
                }
                Ty::Str
            }
            // 容器字面量、下标、切片与星号：M2.3 起统一走动态路径，
            // 静态容器类型留到优化阶段（见 optimization.md）。
            ExprKind::List(_)
            | ExprKind::Tuple(_)
            | ExprKind::Set(_)
            | ExprKind::Dict(_)
            | ExprKind::Bytes(_)
            | ExprKind::Subscript { .. }
            | ExprKind::Slice { .. }
            | ExprKind::Starred(_)
            | ExprKind::Yield(_)
            | ExprKind::YieldFrom(_) => {
                for child in expr_children(&expr.kind) {
                    self.check_expr(child);
                }
                self.soft_demote = true;
                Ty::Dynamic
            }
            ExprKind::Missing | ExprKind::UnresolvedGlobal(_) => Ty::Error,
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

    /// 表达式是否为确定的非负整数字面量。
    ///
    /// 只有这种指数能保证 `int ** int` 的结果仍是 `int`（见 `ExprKind::Binary` 的处理），
    /// 因而可以安全地发射静态快速幂。
    fn is_nonneg_int_literal(&self, expr: ExprId) -> bool {
        match &self.func.body.exprs[expr].kind {
            ExprKind::Literal(Literal::Int(value)) => *value >= 0,
            _ => false,
        }
    }
}
