//! AST → HIR lowering。
//!
//! 流程（两遍）：
//! 1. **收集**：遍历模块顶层，为每个 `def` / `class` 分配 `FuncId` / `ClassId`，填入 [`GlobalScope`]。
//!    同时创建隐式 `main` 函数。
//! 2. **降级**：逐个函数体
//!    - 用 [`collect_assigned_names`] 预扫描，建立 [`FunctionScope`]；
//!    - 把参数分配为 `Local { is_param: true }`；
//!    - 递归降级语句与表达式，同时完成去糖（见 crate 文档）。
//!
//! 不支持的语法一律报诊断（错误码 `E01xx`）并产出 `ExprKind::Missing` / 跳过语句，
//! **不要 panic**——这样一次编译能报出尽可能多的错误。

use la_arena::Arena;
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ParsedModule;
use ryupy_syntax::ast;

use crate::hir::{
    BinOp, Body, Builtin, Expr, ExprId, ExprKind, Function, FunctionKind, Literal, Module, Stmt,
    StmtKind,
};
#[allow(unused_imports)]
use crate::scope::{FunctionScope, GlobalScope, collect_assigned_names};

/// 降级整个模块。即使有错误也返回一个结构完整的 `Module`。
pub fn lower_module(parsed: &ParsedModule) -> (Module, Vec<Diagnostic>) {
    let mut ctx = LowerCtx {
        file: parsed.file,
        diagnostics: Vec::new(),
        body: Body::default(),
    };

    for stmt in &parsed.module.body {
        ctx.lower_stmt(stmt);
    }

    let mut functions = Arena::default();
    let main_fn = Function {
        name: "main".into(),
        kind: FunctionKind::ModuleMain,
        params: Vec::new(),
        ret: None,
        body: ctx.body,
        range: parsed.module.range,
    };
    let main = functions.alloc(main_fn);

    let module = Module {
        file: parsed.file,
        functions,
        classes: Arena::default(),
        main,
    };

    (module, ctx.diagnostics)
}

struct LowerCtx {
    file: FileId,
    diagnostics: Vec<Diagnostic>,
    body: Body,
}

impl LowerCtx {
    fn lower_stmt(&mut self, stmt: &ast::Stmt) {
        match stmt {
            ast::Stmt::Expr(expr_stmt) => {
                let expr_id = self.lower_expr(&expr_stmt.value);
                let stmt_id = self.body.stmts.alloc(Stmt {
                    kind: StmtKind::Expr(expr_id),
                    range: expr_stmt.range,
                });
                self.body.root.push(stmt_id);
            }
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, stmt.range()), "暂不支持该语句")
                        .with_code("E0100")
                        .with_primary_label("暂不支持此语法结构"),
                );
            }
        }
    }

    fn lower_expr(&mut self, expr: &ast::Expr) -> ExprId {
        let range = expr.range();
        let kind = match expr {
            ast::Expr::NumberLiteral(num) => match &num.value {
                ast::Number::Int(int_val) => {
                    if let Some(val) = int_val.as_i64() {
                        ExprKind::Literal(Literal::Int(val))
                    } else {
                        self.diagnostics.push(
                            Diagnostic::error(Span::new(self.file, num.range), "整数超出 i64 范围")
                                .with_code("E0100")
                                .with_primary_label("数值过大，Phase 1 仅支持 64 位有符号整数"),
                        );
                        ExprKind::Missing
                    }
                }
                _ => {
                    self.diagnostics.push(
                        Diagnostic::error(Span::new(self.file, num.range), "暂不支持非整数字面量")
                            .with_code("E0100")
                            .with_primary_label("M1.0 仅支持整数字面量"),
                    );
                    ExprKind::Missing
                }
            },
            ast::Expr::BinOp(bin) => {
                if bin.op == ast::Operator::Add {
                    let lhs = self.lower_expr(&bin.left);
                    let rhs = self.lower_expr(&bin.right);
                    ExprKind::Binary {
                        op: BinOp::Add,
                        lhs,
                        rhs,
                    }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(Span::new(self.file, bin.range), "暂不支持该运算符")
                            .with_code("E0100")
                            .with_primary_label("M1.0 仅支持加法 `+` 运算"),
                    );
                    ExprKind::Missing
                }
            }
            ast::Expr::Call(call) => {
                if let ast::Expr::Name(name) = &*call.func {
                    if name.id.as_str() == "print" {
                        let callee = self.body.exprs.alloc(Expr {
                            kind: ExprKind::Builtin(Builtin::Print),
                            range: name.range,
                        });
                        if !call.arguments.keywords.is_empty() {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, call.arguments.range),
                                    "暂不支持 print 关键字参数",
                                )
                                .with_code("E0100")
                                .with_primary_label("暂不支持 sep/end 等关键字参数"),
                            );
                        }
                        let mut args = Vec::new();
                        for arg in &*call.arguments.args {
                            args.push(self.lower_expr(arg));
                        }
                        ExprKind::Call { callee, args }
                    } else {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, call.range()),
                                "暂不支持调用除 print 以外的函数",
                            )
                            .with_code("E0100")
                            .with_primary_label("未定义的函数或不支持的内建函数"),
                        );
                        ExprKind::Missing
                    }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, call.range()),
                            "暂不支持复杂函数调用",
                        )
                        .with_code("E0100")
                        .with_primary_label("暂不支持调用非直接名称表达式"),
                    );
                    ExprKind::Missing
                }
            }
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, range), "暂不支持该表达式")
                        .with_code("E0100")
                        .with_primary_label("M1.0 暂不支持该语法"),
                );
                ExprKind::Missing
            }
        };

        self.body.exprs.alloc(Expr { kind, range })
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use ryupy_base::SourceDb;

    #[test]
    fn lowers_print_add() {
        let mut db = SourceDb::default();
        let src = "print(1 + 2)\n";
        let file = db.add("test.py", src);
        let (parsed, diags) = ryupy_syntax::parse(file, src);
        assert!(diags.is_empty());

        let (hir, diags) = lower_module(&parsed);
        assert!(diags.is_empty());
        let main = &hir.functions[hir.main];
        assert_eq!(main.name.as_str(), "main");
        assert_eq!(main.body.root.len(), 1);
        let stmt = &main.body.stmts[main.body.root[0]];
        let StmtKind::Expr(call_expr) = stmt.kind else {
            panic!("expected expr stmt");
        };
        let ExprKind::Call { callee, ref args } = main.body.exprs[call_expr].kind else {
            panic!("expected call expr");
        };
        assert!(matches!(
            main.body.exprs[callee].kind,
            ExprKind::Builtin(Builtin::Print)
        ));
        assert_eq!(args.len(), 1);
    }

    #[test]
    fn reports_unsupported_syntax_gracefully() {
        let mut db = SourceDb::default();
        let src = "x = 1\n";
        let file = db.add("test.py", src);
        let (parsed, _) = ryupy_syntax::parse(file, src);

        let (_, diags) = lower_module(&parsed);
        assert_eq!(diags.len(), 1);
        assert_eq!(diags[0].code, Some("E0100"));
    }
}
