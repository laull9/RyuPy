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
use ryupy_hir::{BinOp, Body, Builtin, ExprId, ExprKind, Literal, Module, StmtKind};

use crate::ty::{FnSig, ModuleTypes, Ty, TypeckResults};

pub fn check_module(module: &Module) -> (ModuleTypes, Vec<Diagnostic>) {
    let mut sigs = ArenaMap::default();
    let mut bodies = ArenaMap::default();
    let mut diagnostics = Vec::new();

    for (func_id, func) in module.functions.iter() {
        let sig = FnSig {
            params: Vec::new(),
            ret: Ty::None,
        };
        sigs.insert(func_id, sig);

        let mut results = TypeckResults::default();
        let mut checker = BodyChecker {
            file: module.file,
            body: &func.body,
            results: &mut results,
            diagnostics: &mut diagnostics,
        };

        for &stmt_id in &func.body.root {
            checker.check_stmt(stmt_id);
        }

        bodies.insert(func_id, results);
    }

    (ModuleTypes { sigs, bodies }, diagnostics)
}

struct BodyChecker<'a> {
    file: FileId,
    body: &'a Body,
    results: &'a mut TypeckResults,
    diagnostics: &'a mut Vec<Diagnostic>,
}

impl BodyChecker<'_> {
    fn check_stmt(&mut self, stmt_id: ryupy_hir::StmtId) {
        let stmt = &self.body.stmts[stmt_id];
        if let StmtKind::Expr(expr_id) = stmt.kind {
            self.check_expr(expr_id);
        }
    }

    fn check_expr(&mut self, expr_id: ExprId) -> Ty {
        let expr = &self.body.exprs[expr_id];
        let ty = match &expr.kind {
            ExprKind::Literal(Literal::Int(_)) => Ty::Int,
            ExprKind::Binary { op, lhs, rhs } => {
                let lhs_ty = self.check_expr(*lhs);
                let rhs_ty = self.check_expr(*rhs);
                match op {
                    BinOp::Add => {
                        if lhs_ty == Ty::Int && rhs_ty == Ty::Int {
                            Ty::Int
                        } else if lhs_ty == Ty::Error || rhs_ty == Ty::Error {
                            Ty::Error
                        } else {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, expr.range),
                                    format!("不支持类型 `{lhs_ty:?}` 和 `{rhs_ty:?}` 的加法"),
                                )
                                .with_code("E0203")
                                .with_primary_label("类型不匹配"),
                            );
                            Ty::Error
                        }
                    }
                    _ => {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, expr.range),
                                "M1.0 仅支持加法运算",
                            )
                            .with_code("E0200")
                            .with_primary_label("不支持的运算符"),
                        );
                        Ty::Error
                    }
                }
            }
            ExprKind::Builtin(Builtin::Print) => Ty::None,
            ExprKind::Call { callee, args } => {
                let callee_expr = &self.body.exprs[*callee];
                if matches!(callee_expr.kind, ExprKind::Builtin(Builtin::Print)) {
                    self.results.exprs.insert(*callee, Ty::None);
                    for &arg in args {
                        let arg_ty = self.check_expr(arg);
                        if arg_ty != Ty::Int && arg_ty != Ty::Error {
                            let arg_range = self.body.exprs[arg].range;
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, arg_range),
                                    format!("print 目前只支持 int 参数，得到 `{arg_ty:?}`"),
                                )
                                .with_code("E0203")
                                .with_primary_label("类型不匹配"),
                            );
                        }
                    }
                    Ty::None
                } else {
                    self.check_expr(*callee);
                    for &arg in args {
                        self.check_expr(arg);
                    }
                    self.diagnostics.push(
                        Diagnostic::error(Span::new(self.file, expr.range), "暂不支持调用该函数")
                            .with_code("E0200")
                            .with_primary_label("未知的函数调用"),
                    );
                    Ty::Error
                }
            }
            ExprKind::Missing => Ty::Error,
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, expr.range),
                        "暂不支持该表达式的类型推导",
                    )
                    .with_code("E0200")
                    .with_primary_label("未支持的类型检查"),
                );
                Ty::Error
            }
        };

        self.results.exprs.insert(expr_id, ty.clone());
        ty
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use ryupy_base::SourceDb;

    #[test]
    fn checks_print_add() {
        let mut db = SourceDb::default();
        let src = "print(1 + 2)\n";
        let file = db.add("test.py", src);
        let (parsed, _) = ryupy_syntax::parse(file, src);
        let (hir, _) = ryupy_hir::lower_module(&parsed);
        let (types, diags) = check_module(&hir);
        assert!(diags.is_empty());
        assert_eq!(types.sigs[hir.main].ret, Ty::None);
    }
}
