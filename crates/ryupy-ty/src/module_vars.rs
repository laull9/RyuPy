//! 模块级变量的类型提示（M2.7）。
//!
//! 模块级变量在 MIR 里是静态槽位，所以要先给每个槽位一个类型。完整方案需要跨函数
//! 推导，这里用一条保守规则：
//!
//! 1. 有标注：以标注为准；若模块顶层的赋值提示明显冲突，则降级为 `Dynamic`；
//! 2. 没有标注：只看模块顶层赋值表达式的**字面量形状**（字面量、同类运算、
//!    dataclass 构造、引用另一个已知模块变量）。任何一个赋值提示不出来，或提示互相
//!    冲突，就降级为 `Dynamic`；
//! 3. 被任何函数用 `global` 声明过的名字一律 `Dynamic`：函数体里的读写走动态路径，
//!    避免出现「函数改了类型、模块顶层仍按旧类型读」的静默错误。
//!
//! 这里不调用类型检查器，只看 HIR 结构，因此不受函数检查顺序影响；宁可保守地
//! 退到 `Dynamic`，也不产生错误拒绝。

use la_arena::ArenaMap;
use ryupy_hir::{ExprId, ExprKind, Literal, Module, ModuleVarId, StmtKind};

use crate::ty::Ty;

/// 估算每个模块级变量的槽位类型。
pub fn infer_module_var_types(module: &Module) -> ArenaMap<ModuleVarId, Ty> {
    // 每个变量的原始提示（`None` 表示暂时无法确定）。
    let mut hints: ArenaMap<ModuleVarId, Option<Ty>> = ArenaMap::default();
    for (id, var) in module.vars.iter() {
        hints.insert(id, annotation_ty(module, var));
    }

    // 收集模块顶层的赋值：变量 → 右值表达式。
    let mut assignments: Vec<(ModuleVarId, ExprId)> = Vec::new();
    let body = &module.functions[module.main].body;
    collect_assignments(body, &body.root, &mut assignments);
    // `for i in range(...)` 的模块级循环变量一定是 int；先写入提示，
    // 避免被「无法从赋值推断」兜底成 Dynamic 而让静态槽位装箱。
    for var in collect_range_vars(module) {
        if !module.vars[var].is_global && hints[var].as_ref() != Some(&Ty::Dynamic) {
            hints.insert(var, Some(Ty::Int));
        }
    }

    // 迭代若干轮，让 `y = x + 1` 这种依赖前一个变量的提示传播过来。
    // 轮数上限等于变量数即可；存在环时仍会停在 `None`，最后按 Dynamic 处理。
    let rounds = module.vars.len().max(1);
    for _ in 0..rounds {
        let mut changed = false;
        for &(var, value) in &assignments {
            if module.vars[var].is_global {
                continue;
            }
            let Some(hint) = expr_hint(module, body, value, &hints) else {
                continue;
            };
            match &hints[var] {
                Some(existing) if *existing != hint => {
                    // 冲突：改成 Dynamic 并标记收敛信号。
                    if *existing != Ty::Dynamic {
                        hints.insert(var, Some(Ty::Dynamic));
                        changed = true;
                    }
                }
                Some(_) => {}
                None => {
                    hints.insert(var, Some(hint));
                    changed = true;
                }
            }
        }
        if !changed {
            break;
        }
    }

    // 兜底：仍然未知或声明为 global 的变量走动态路径。
    for (id, var) in module.vars.iter() {
        if var.is_global || hints[id].is_none() {
            hints.insert(id, Some(Ty::Dynamic));
        }
    }

    let mut out = ArenaMap::default();
    for (id, _) in module.vars.iter() {
        out.insert(id, hints[id].clone().unwrap_or(Ty::Dynamic));
    }
    out
}

/// 收集模块顶层的 `for ... in range(...)` 循环变量（只收集模块级绑定）。
fn collect_range_vars(module: &Module) -> Vec<ModuleVarId> {
    use ryupy_hir::Binding;
    let mut out = Vec::new();
    fn walk(body: &ryupy_hir::Body, stmts: &[ryupy_hir::StmtId], out: &mut Vec<ModuleVarId>) {
        for &stmt_id in stmts {
            match &body.stmts[stmt_id].kind {
                StmtKind::ForRange {
                    var, body: inner, ..
                } => {
                    if let Binding::ModuleVar(var) = var {
                        out.push(*var);
                    }
                    walk(body, inner, out);
                }
                StmtKind::ForEach {
                    body: inner,
                    orelse,
                    ..
                } => {
                    walk(body, inner, out);
                    walk(body, orelse, out);
                }
                StmtKind::If { then_, else_, .. } => {
                    walk(body, then_, out);
                    walk(body, else_, out);
                }
                StmtKind::While {
                    body: inner,
                    orelse,
                    ..
                } => {
                    walk(body, inner, out);
                    walk(body, orelse, out);
                }
                StmtKind::Try {
                    body: try_body,
                    handlers,
                    orelse,
                    finalbody,
                } => {
                    walk(body, try_body, out);
                    for handler in handlers {
                        walk(body, &body.handlers[*handler].body, out);
                    }
                    walk(body, orelse, out);
                    walk(body, finalbody, out);
                }
                _ => {}
            }
        }
    }
    let body = &module.functions[module.main].body;
    walk(body, &body.root, &mut out);
    out
}

fn annotation_ty(module: &Module, var: &ryupy_hir::ModuleVar) -> Option<Ty> {
    // 没有标注时先留空，交给赋值提示推断；直接给 Dynamic 会与提示冲突。
    let ty = crate::check::type_ref_to_ty(var.annotation.as_ref()?);
    // `TypeRef::Class` 只有 dataclass 有静态布局；普通类实例是动态值。
    let ty = match ty {
        Ty::Class(id)
            if !module.classes[id].is_dataclass
                || (module.classes[id].decorated_binding.is_some()
                    || module.classes[id].runtime_dataclass) =>
        {
            Ty::Dynamic
        }
        other => other,
    };
    Some(ty)
}

/// 遍历模块顶层语句（含控制流块），收集 `模块变量 = 表达式`。
fn collect_assignments(
    body: &ryupy_hir::Body,
    stmts: &[ryupy_hir::StmtId],
    out: &mut Vec<(ModuleVarId, ExprId)>,
) {
    for &stmt_id in stmts {
        match &body.stmts[stmt_id].kind {
            // 序列解包的元素来自迭代协议，提示不可靠；留给 Dynamic。
            StmtKind::Assign {
                target: ryupy_hir::AssignTarget::ModuleVar(var),
                value,
            } => out.push((*var, *value)),
            StmtKind::If { then_, else_, .. } => {
                collect_assignments(body, then_, out);
                collect_assignments(body, else_, out);
            }
            StmtKind::While {
                body: inner,
                orelse,
                ..
            } => {
                collect_assignments(body, inner, out);
                collect_assignments(body, orelse, out);
            }
            StmtKind::ForEach {
                body: inner,
                orelse,
                ..
            } => {
                collect_assignments(body, inner, out);
                collect_assignments(body, orelse, out);
            }
            StmtKind::ForRange {
                body: inner,
                orelse,
                ..
            } => {
                // `for i in range(...)` 的循环变量一定是 int，但这里不放提示：
                // 变量可能同时标注成别的类型，交给标注与保守兜底处理。
                collect_assignments(body, inner, out);
                collect_assignments(body, orelse, out);
            }
            StmtKind::Try {
                body: try_body,
                handlers,
                orelse,
                finalbody,
            } => {
                collect_assignments(body, try_body, out);
                for handler in handlers {
                    collect_assignments(body, &body.handlers[*handler].body, out);
                }
                collect_assignments(body, orelse, out);
                collect_assignments(body, finalbody, out);
            }
            _ => {}
        }
    }
}

fn expr_hint(
    module: &Module,
    body: &ryupy_hir::Body,
    expr: ExprId,
    hints: &ArenaMap<ModuleVarId, Option<Ty>>,
) -> Option<Ty> {
    // 占位表达式越界时按未知处理。
    if expr.into_raw().into_u32() as usize >= body.exprs.len() {
        return None;
    }
    match &body.exprs[expr].kind {
        ExprKind::Literal(lit) => Some(match lit {
            // 超出 i64 的整数必须装箱（BigInteger），槽位类型按 Dynamic 处理。
            Literal::Int(v) if i64::try_from(*v).is_err() => Ty::Dynamic,
            Literal::Int(_) => Ty::Int,
            Literal::Float(_) => Ty::Float,
            Literal::Bool(_) => Ty::Bool,
            Literal::Str(_) => Ty::Str,
            Literal::None => Ty::None,
        }),
        ExprKind::ModuleVar(var) => hints.get(*var).cloned().flatten(),
        ExprKind::Unary { op, operand } => {
            use ryupy_hir::UnaryOp;
            let inner = expr_hint(module, body, *operand, hints)?;
            match op {
                UnaryOp::Not => Some(Ty::Bool),
                UnaryOp::Neg | UnaryOp::Pos | UnaryOp::Invert => match inner {
                    Ty::Int | Ty::Float => Some(inner),
                    _ => None,
                },
            }
        }
        ExprKind::Binary { op, lhs, rhs, .. } => {
            use ryupy_hir::BinOp;
            let a = expr_hint(module, body, *lhs, hints)?;
            let b = expr_hint(module, body, *rhs, hints)?;
            match op {
                BinOp::Add => match (&a, &b) {
                    (Ty::Int, Ty::Int) => Some(Ty::Int),
                    (Ty::Str, Ty::Str) => Some(Ty::Str),
                    (Ty::Int | Ty::Float, Ty::Int | Ty::Float) => Some(Ty::Float),
                    _ => None,
                },
                BinOp::Sub => numeric(&a, &b),
                BinOp::Mul => match (&a, &b) {
                    (Ty::Str, Ty::Int) | (Ty::Int, Ty::Str) => Some(Ty::Str),
                    _ => numeric(&a, &b),
                },
                BinOp::TrueDiv => match (&a, &b) {
                    (Ty::Int | Ty::Float, Ty::Int | Ty::Float) => Some(Ty::Float),
                    _ => None,
                },
                BinOp::FloorDiv | BinOp::Mod | BinOp::Pow => match (&a, &b) {
                    (Ty::Int, Ty::Int) => Some(Ty::Int),
                    (Ty::Int | Ty::Float, Ty::Int | Ty::Float) => Some(Ty::Float),
                    _ => None,
                },
                BinOp::BitAnd | BinOp::BitOr | BinOp::BitXor | BinOp::LShift | BinOp::RShift => {
                    match (&a, &b) {
                        (Ty::Int, Ty::Int) => Some(Ty::Int),
                        _ => None,
                    }
                }
            }
        }
        ExprKind::Compare { .. } => Some(Ty::Bool),
        ExprKind::BoolOp { lhs, rhs, .. } => {
            let a = expr_hint(module, body, *lhs, hints)?;
            let b = expr_hint(module, body, *rhs, hints)?;
            (a == b).then_some(a)
        }
        ExprKind::IfExp { then_, else_, .. } => {
            let a = expr_hint(module, body, *then_, hints)?;
            let b = expr_hint(module, body, *else_, hints)?;
            (a == b).then_some(a)
        }
        ExprKind::Call { callee, .. } | ExprKind::PreparedCall { callee, .. } => {
            let ExprKind::Class(id) = &body.exprs[*callee].kind else {
                return None;
            };
            (module.classes[*id].is_dataclass
                && (module.classes[*id].decorated_binding.is_none()
                    && !module.classes[*id].runtime_dataclass))
                .then_some(Ty::Class(*id))
        }
        _ => None,
    }
}

fn numeric(a: &Ty, b: &Ty) -> Option<Ty> {
    match (a, b) {
        (Ty::Int, Ty::Int) => Some(Ty::Int),
        (Ty::Int | Ty::Float, Ty::Int | Ty::Float) => Some(Ty::Float),
        _ => None,
    }
}
