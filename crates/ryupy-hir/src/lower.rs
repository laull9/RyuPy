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
use rustc_hash::FxHashMap;
use ryupy_base::{Diagnostic, FileId, Ranged, Span};
use ryupy_syntax::ParsedModule;
use ryupy_syntax::ast;

use crate::hir::{
    AssignTarget, Body, Expr, ExprId, ExprKind, Function, FunctionKind, Literal, Local, Module,
    Name, Param, Stmt, StmtKind,
};
use crate::scope::{
    FunctionScope, GlobalScope, Resolution, collect_assigned_names, resolve_builtin,
};
use crate::type_ref::{lower_type_ref, map_bin_op};

/// 降级整个模块。即使有错误也返回一个结构完整的 `Module`。
pub fn lower_module(parsed: &ParsedModule) -> (Module, Vec<Diagnostic>) {
    let mut diagnostics = Vec::new();
    let mut functions = Arena::default();
    let mut global_scope = GlobalScope::default();

    // 第一遍：收集所有顶层 def，支持前向引用
    let mut user_funcs: Vec<(ast::StmtFunctionDef, crate::hir::FuncId)> = Vec::new();
    for stmt in &parsed.module.body {
        if let ast::Stmt::FunctionDef(fn_def) = stmt {
            let name = Name::from(fn_def.name.as_str());
            if global_scope.names.contains_key(&name) {
                diagnostics.push(
                    Diagnostic::error(
                        Span::new(parsed.file, fn_def.name.range()),
                        format!("重复定义函数 `{name}`"),
                    )
                    .with_code("E0102")
                    .with_primary_label("该名字已在模块中定义"),
                );
            }
            let func_id = functions.alloc(Function {
                name: name.clone(),
                kind: FunctionKind::User,
                params: Vec::new(),
                ret: None,
                body: Body::default(),
                range: fn_def.range(),
            });
            global_scope.names.insert(name, Resolution::Func(func_id));
            user_funcs.push((fn_def.clone(), func_id));
        }
    }

    // 分配 main 入口函数
    let main_id = functions.alloc(Function {
        name: "main".into(),
        kind: FunctionKind::ModuleMain,
        params: Vec::new(),
        ret: None,
        body: Body::default(),
        range: parsed.module.range,
    });

    // 预扫描模块顶层被赋值的名字（作为 main 的局部变量）
    let top_assigned = collect_assigned_names(&parsed.module.body);
    let mut module_locals = FxHashMap::default();
    let mut main_body = Body::default();
    for (name, range) in top_assigned {
        let local_id = main_body.locals.alloc(Local {
            name: name.clone(),
            annotation: None,
            is_param: false,
            range,
        });
        module_locals.insert(name, local_id);
    }

    // 第二遍：降级各个用户函数体
    for (fn_def, func_id) in user_funcs {
        let mut body = Body::default();
        let mut func_scope = FunctionScope::default();
        let mut params = Vec::new();

        // 1. 参数处理
        for param_def in fn_def.parameters.iter_non_variadic_params() {
            let param_name = Name::from(param_def.parameter.name.as_str());
            let ann = param_def
                .parameter
                .annotation
                .as_deref()
                .map(|e| lower_type_ref(e, parsed.file, &global_scope, &mut diagnostics));
            let local_id = body.locals.alloc(Local {
                name: param_name.clone(),
                annotation: ann.clone(),
                is_param: true,
                range: param_def.parameter.range,
            });
            func_scope.locals.insert(param_name, local_id);
            params.push(Param {
                local: local_id,
                annotation: ann,
                range: param_def.parameter.range,
            });
        }

        // 2. 返回值标注
        let ret = fn_def
            .returns
            .as_deref()
            .map(|e| lower_type_ref(e, parsed.file, &global_scope, &mut diagnostics));

        // 3. 预扫描函数内被赋值的局部变量（赋值即局部）
        for (name, range) in collect_assigned_names(&fn_def.body) {
            if let std::collections::hash_map::Entry::Vacant(e) =
                func_scope.locals.entry(name.clone())
            {
                let local_id = body.locals.alloc(Local {
                    name,
                    annotation: None,
                    is_param: false,
                    range,
                });
                e.insert(local_id);
            }
        }

        // 4. 降级函数体语句
        let mut lower_ctx = StmtLowerCtx {
            file: parsed.file,
            diagnostics: &mut diagnostics,
            global_scope: &global_scope,
            func_scope: &mut func_scope,
            module_locals: &module_locals,
            body: &mut body,
            is_in_func: true,
        };
        for stmt in &fn_def.body {
            lower_ctx.lower_stmt(stmt);
        }

        functions[func_id].params = params;
        functions[func_id].ret = ret;
        functions[func_id].body = body;
    }

    // 降级 main 函数（模块顶层非 FunctionDef 语句）
    let mut main_scope = FunctionScope {
        locals: module_locals,
    };
    let mut main_lower_ctx = StmtLowerCtx {
        file: parsed.file,
        diagnostics: &mut diagnostics,
        global_scope: &global_scope,
        func_scope: &mut main_scope,
        module_locals: &FxHashMap::default(),
        body: &mut main_body,
        is_in_func: false,
    };
    for stmt in &parsed.module.body {
        if !matches!(stmt, ast::Stmt::FunctionDef(_)) {
            main_lower_ctx.lower_stmt(stmt);
        }
    }
    functions[main_id].body = main_body;

    let module = Module {
        file: parsed.file,
        functions,
        classes: Arena::default(),
        main: main_id,
    };

    (module, diagnostics)
}

struct StmtLowerCtx<'a> {
    file: FileId,
    diagnostics: &'a mut Vec<Diagnostic>,
    global_scope: &'a GlobalScope,
    func_scope: &'a mut FunctionScope,
    module_locals: &'a FxHashMap<Name, crate::hir::LocalId>,
    body: &'a mut Body,
    is_in_func: bool,
}

impl StmtLowerCtx<'_> {
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
            ast::Stmt::Assign(assign_stmt) => {
                let val = self.lower_expr(&assign_stmt.value);
                for target in &assign_stmt.targets {
                    if let ast::Expr::Name(name_expr) = target {
                        let name = Name::from(name_expr.id.as_str());
                        if let Some(&loc_id) = self.func_scope.locals.get(&name) {
                            let stmt_id = self.body.stmts.alloc(Stmt {
                                kind: StmtKind::Assign {
                                    target: AssignTarget::Local(loc_id),
                                    value: val,
                                },
                                range: assign_stmt.range,
                            });
                            self.body.root.push(stmt_id);
                        }
                    } else {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, target.range()),
                                "暂不支持非简单变量的赋值目标",
                            )
                            .with_code("E0100")
                            .with_primary_label("仅支持赋值给单个局部变量"),
                        );
                    }
                }
            }
            ast::Stmt::AnnAssign(ann_stmt) => {
                if let ast::Expr::Name(name_expr) = &*ann_stmt.target {
                    let name = Name::from(name_expr.id.as_str());
                    let ann = lower_type_ref(
                        &ann_stmt.annotation,
                        self.file,
                        self.global_scope,
                        self.diagnostics,
                    );
                    if let Some(&loc_id) = self.func_scope.locals.get(&name) {
                        self.body.locals[loc_id].annotation = Some(ann);
                        if let Some(val_expr) = &ann_stmt.value {
                            let val = self.lower_expr(val_expr);
                            let stmt_id = self.body.stmts.alloc(Stmt {
                                kind: StmtKind::Assign {
                                    target: AssignTarget::Local(loc_id),
                                    value: val,
                                },
                                range: ann_stmt.range,
                            });
                            self.body.root.push(stmt_id);
                        }
                    }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, ann_stmt.target.range()),
                            "暂不支持带标注的非变量赋值",
                        )
                        .with_code("E0100")
                        .with_primary_label("目标必须是变量"),
                    );
                }
            }
            ast::Stmt::AugAssign(aug_stmt) => {
                if let ast::Expr::Name(name_expr) = &*aug_stmt.target {
                    let name = Name::from(name_expr.id.as_str());
                    if let Some(&loc_id) = self.func_scope.locals.get(&name) {
                        let lhs = self.body.exprs.alloc(Expr {
                            kind: ExprKind::Local(loc_id),
                            range: name_expr.range,
                        });
                        let rhs = self.lower_expr(&aug_stmt.value);
                        if let Some(op) = map_bin_op(aug_stmt.op) {
                            let bin = self.body.exprs.alloc(Expr {
                                kind: ExprKind::Binary { op, lhs, rhs },
                                range: aug_stmt.range,
                            });
                            let stmt_id = self.body.stmts.alloc(Stmt {
                                kind: StmtKind::Assign {
                                    target: AssignTarget::Local(loc_id),
                                    value: bin,
                                },
                                range: aug_stmt.range,
                            });
                            self.body.root.push(stmt_id);
                        } else {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, aug_stmt.range),
                                    "暂不支持该增量运算符",
                                )
                                .with_code("E0100")
                                .with_primary_label("暂不支持的操作符"),
                            );
                        }
                    }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, aug_stmt.target.range()),
                            "暂不支持非变量增量赋值",
                        )
                        .with_code("E0100")
                        .with_primary_label("目标必须是简单变量"),
                    );
                }
            }
            ast::Stmt::Return(ret_stmt) => {
                let val = ret_stmt.value.as_deref().map(|v| self.lower_expr(v));
                let stmt_id = self.body.stmts.alloc(Stmt {
                    kind: StmtKind::Return(val),
                    range: ret_stmt.range,
                });
                self.body.root.push(stmt_id);
            }
            ast::Stmt::Pass(p) => {
                let stmt_id = self.body.stmts.alloc(Stmt {
                    kind: StmtKind::Pass,
                    range: p.range,
                });
                self.body.root.push(stmt_id);
            }
            ast::Stmt::Global(g) => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, g.range),
                        "Phase 1 暂不支持 global 声明",
                    )
                    .with_code("E0103")
                    .with_primary_label("不支持 global"),
                );
            }
            ast::Stmt::Nonlocal(n) => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, n.range),
                        "Phase 1 暂不支持 nonlocal 声明",
                    )
                    .with_code("E0103")
                    .with_primary_label("不支持 nonlocal"),
                );
            }
            ast::Stmt::FunctionDef(f) => {
                self.diagnostics.push(
                    Diagnostic::error(
                        Span::new(self.file, f.range),
                        "Phase 1 暂不支持嵌套函数定义",
                    )
                    .with_code("E0104")
                    .with_primary_label("不支持嵌套 def"),
                );
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
                ast::Number::Float(f) => ExprKind::Literal(Literal::Float(*f)),
                _ => {
                    self.diagnostics.push(
                        Diagnostic::error(Span::new(self.file, num.range), "暂不支持该数值字面量")
                            .with_code("E0100")
                            .with_primary_label("暂不支持此类数值"),
                    );
                    ExprKind::Missing
                }
            },
            ast::Expr::BooleanLiteral(b) => ExprKind::Literal(Literal::Bool(b.value)),
            ast::Expr::NoneLiteral(_) => ExprKind::Literal(Literal::None),
            ast::Expr::StringLiteral(s) => ExprKind::Literal(Literal::Str(s.value.to_str().into())),
            ast::Expr::Name(name_expr) => {
                let name = Name::from(name_expr.id.as_str());
                // LEGB 查找：1. Local
                if let Some(&loc_id) = self.func_scope.locals.get(&name) {
                    ExprKind::Local(loc_id)
                } else if let Some(&res) = self.global_scope.names.get(&name) {
                    // 2. Global
                    match res {
                        Resolution::Func(fn_id) => ExprKind::Func(fn_id),
                        Resolution::Class(cls_id) => ExprKind::Class(cls_id),
                        Resolution::Local(_) | Resolution::Builtin(_) => ExprKind::Missing,
                    }
                } else if self.is_in_func && self.module_locals.contains_key(&name) {
                    // 函数体内禁止读取模块级变量（语义陷阱）
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, name_expr.range),
                            format!("函数体内不能访问模块级变量 `{name}`"),
                        )
                        .with_code("E0105")
                        .with_primary_label("Phase 1 函数体内只允许访问局部变量或顶层函数/类"),
                    );
                    ExprKind::Missing
                } else if let Some(builtin) = resolve_builtin(name.as_str()) {
                    // 3. Builtin
                    ExprKind::Builtin(builtin)
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, name_expr.range),
                            format!("未定义的变量或名称 `{name}`"),
                        )
                        .with_code("E0106")
                        .with_primary_label("未解析的名字"),
                    );
                    ExprKind::Missing
                }
            }
            ast::Expr::BinOp(bin) => {
                if let Some(op) = map_bin_op(bin.op) {
                    let lhs = self.lower_expr(&bin.left);
                    let rhs = self.lower_expr(&bin.right);
                    ExprKind::Binary { op, lhs, rhs }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(Span::new(self.file, bin.range), "暂不支持该运算符")
                            .with_code("E0100")
                            .with_primary_label("暂不支持此运算"),
                    );
                    ExprKind::Missing
                }
            }
            ast::Expr::Call(call) => {
                let callee = self.lower_expr(&call.func);
                let mut args = Vec::new();
                for arg in &*call.arguments.args {
                    args.push(self.lower_expr(arg));
                }
                if !call.arguments.keywords.is_empty() {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, call.arguments.range),
                            "Phase 1 暂不支持关键字参数",
                        )
                        .with_code("E0100")
                        .with_primary_label("不支持关键字参数"),
                    );
                }
                ExprKind::Call { callee, args }
            }
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, range), "暂不支持该表达式")
                        .with_code("E0100")
                        .with_primary_label("M1.1 暂不支持该语法"),
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
    fn lowers_function_definitions_and_forward_reference() {
        let mut db = SourceDb::default();
        let src = r#"
def f(x: int) -> int:
    return g(x)

def g(y: int) -> int:
    return y + 1

print(f(10))
"#;
        let file = db.add("test.py", src);
        let (parsed, diags) = ryupy_syntax::parse(file, src);
        assert!(diags.is_empty());

        let (hir, diags) = lower_module(&parsed);
        assert!(diags.is_empty(), "diags: {diags:?}");
        assert_eq!(hir.functions.len(), 3); // f, g, main
    }
}
