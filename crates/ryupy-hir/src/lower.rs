//! AST → HIR 降级。
//!
//! 流程：
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

use crate::expr::ExprLowerCtx;
use crate::hir::{
    AssignTarget, Body, Expr, ExprId, ExprKind, Function, FunctionKind, Local, Module, Name, Param,
    Stmt, StmtId, StmtKind,
};
use crate::operator::{lower_type_ref, map_bin_op};
use crate::scope::{FunctionScope, GlobalScope, Resolution, collect_assigned_names};

/// 降级整个模块。即使有错误也返回一个结构完整的 `Module`。
///
/// `source_name` 是源文件路径，只用于 traceback 与诊断展示；它不参与名称解析。
pub fn lower_module(
    parsed: &ParsedModule,
    source_name: &str,
    source: &str,
) -> (Module, Vec<Diagnostic>) {
    let mut diagnostics = Vec::new();
    let line_starts = compute_line_starts(source);
    let mut functions = Arena::default();
    let mut classes = Arena::default();
    let mut global_scope = GlobalScope::default();

    // 第一遍：收集所有顶层 class 与 def，支持前向引用
    let mut raw_classes = Vec::new();
    let mut user_funcs: Vec<(ast::StmtFunctionDef, crate::hir::FuncId)> = Vec::new();
    for stmt in &parsed.module.body {
        match stmt {
            ast::Stmt::ClassDef(cls_def) => {
                let name = Name::from(cls_def.name.as_str());
                if global_scope.names.contains_key(&name) {
                    diagnostics.push(
                        Diagnostic::error(
                            Span::new(parsed.file, cls_def.name.range()),
                            format!("重复定义名字 `{name}`"),
                        )
                        .with_code("E0102")
                        .with_primary_label("该名字已在模块中定义"),
                    );
                }
                let cls_id = classes.alloc(crate::hir::Class {
                    name: name.clone(),
                    fields: Vec::new(),
                    frozen: false,
                    range: cls_def.range(),
                });
                global_scope.names.insert(name, Resolution::Class(cls_id));
                raw_classes.push((cls_def, cls_id));
            }
            ast::Stmt::FunctionDef(fn_def) => {
                let name = Name::from(fn_def.name.as_str());
                if global_scope.names.contains_key(&name) {
                    diagnostics.push(
                        Diagnostic::error(
                            Span::new(parsed.file, fn_def.name.range()),
                            format!("重复定义名字 `{name}`"),
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
            ast::Stmt::ImportFrom(import_from) => {
                if !crate::class::is_allowed_dataclass_import(import_from) {
                    diagnostics.push(
                        Diagnostic::error(
                            Span::new(parsed.file, import_from.range()),
                            "Phase 1 仅支持 `from dataclasses import dataclass`",
                        )
                        .with_code("E0100")
                        .with_primary_label("不支持该导入模块"),
                    );
                }
            }
            ast::Stmt::Import(import_stmt) => {
                diagnostics.push(
                    Diagnostic::error(
                        Span::new(parsed.file, import_stmt.range()),
                        "Phase 1 不支持普通 import，仅支持 `from dataclasses import dataclass`",
                    )
                    .with_code("E0100")
                    .with_primary_label("暂不支持普通 import 语法"),
                );
            }
            _ => {}
        }
    }

    // 解析所有 dataclass 类体字段
    for (cls_def, cls_id) in raw_classes {
        if let Some(parsed_cls) =
            crate::class::parse_class_def(cls_def, parsed.file, &global_scope, &mut diagnostics)
        {
            classes[cls_id] = parsed_cls;
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
                annotation: ann,
                local: local_id,
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
            loop_depth: 0,
            handler_depth: 0,
            line_starts: &line_starts,
        };
        for stmt in &fn_def.body {
            lower_ctx.lower_stmt_to_root(stmt);
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
        loop_depth: 0,
        handler_depth: 0,
        line_starts: &line_starts,
    };
    for stmt in &parsed.module.body {
        if !matches!(
            stmt,
            ast::Stmt::FunctionDef(_)
                | ast::Stmt::ClassDef(_)
                | ast::Stmt::ImportFrom(_)
                | ast::Stmt::Import(_)
        ) {
            main_lower_ctx.lower_stmt_to_root(stmt);
        }
    }
    functions[main_id].body = main_body;

    let module = Module {
        file: parsed.file,
        source_name: source_name.to_string(),
        functions,
        classes,
        main: main_id,
    };

    (module, diagnostics)
}

pub(crate) struct StmtLowerCtx<'a> {
    pub(crate) file: FileId,
    pub(crate) diagnostics: &'a mut Vec<Diagnostic>,
    pub(crate) global_scope: &'a GlobalScope,
    pub(crate) func_scope: &'a mut FunctionScope,
    pub(crate) module_locals: &'a FxHashMap<Name, crate::hir::LocalId>,
    pub(crate) body: &'a mut Body,
    pub(crate) is_in_func: bool,
    pub(crate) loop_depth: usize,
    /// 当前所处的 `except` 体层数。只有 > 0 时裸 `raise` 才有意义。
    pub(crate) handler_depth: usize,
    /// 每一行的起始字节偏移，用于把 `TextRange` 换算成行号。
    pub(crate) line_starts: &'a [usize],
}

/// 预先算出每一行的起始字节偏移，供 `TextRange` → 行号换算。
fn compute_line_starts(source: &str) -> Vec<usize> {
    let mut starts = vec![0usize];
    for (idx, byte) in source.bytes().enumerate() {
        if byte == b'\n' {
            starts.push(idx + 1);
        }
    }
    starts
}

impl StmtLowerCtx<'_> {
    /// 字节偏移 → 1 起行号。
    pub(crate) fn line_of(&self, offset: usize) -> u32 {
        match self.line_starts.binary_search(&offset) {
            Ok(idx) => idx as u32 + 1,
            Err(idx) => idx as u32,
        }
    }

    pub(crate) fn lower_expr(&mut self, expr: &ast::Expr) -> ExprId {
        let mut expr_ctx = ExprLowerCtx {
            file: self.file,
            global_scope: self.global_scope,
            func_scope: self.func_scope,
            module_locals: self.module_locals,
            is_in_func: self.is_in_func,
            body: self.body,
            diagnostics: self.diagnostics,
        };
        expr_ctx.lower_expr(expr)
    }

    fn lower_stmt_to_root(&mut self, stmt: &ast::Stmt) {
        if let Some(stmt_id) = self.lower_stmt(stmt) {
            self.body.root.push(stmt_id);
        }
    }

    pub(crate) fn lower_suite(&mut self, stmts: &[ast::Stmt]) -> Vec<StmtId> {
        let mut out = Vec::new();
        for s in stmts {
            if let Some(id) = self.lower_stmt(s) {
                out.push(id);
            }
        }
        out
    }

    /// `try` 语句：四段结构原样进入 HIR，交给 MIR 划分受保护区域。
    fn lower_try(&mut self, try_stmt: &ast::StmtTry) -> StmtId {
        if try_stmt.is_star {
            self.diagnostics.push(
                Diagnostic::error(
                    Span::new(self.file, try_stmt.range),
                    "暂不支持 except* 异常组语法",
                )
                .with_code("E0100")
                .with_primary_label("异常组属于暂缓能力"),
            );
        }

        let body = self.lower_suite(&try_stmt.body);

        let mut handlers = Vec::new();
        for handler in &try_stmt.handlers {
            let ast::ExceptHandler::ExceptHandler(except) = handler;
            let types = match &except.type_ {
                None => None,
                Some(type_expr) => {
                    let names = self.exception_type_names(type_expr);
                    if names.is_empty() {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, type_expr.range()),
                                "except 目前只支持内建异常类型名（用户自定义异常在 M2.6 接续）",
                            )
                            .with_code("E0100")
                            .with_primary_label("不支持该异常类型表达式"),
                        );
                    }
                    Some(names)
                }
            };

            let name = except.name.as_ref().and_then(|ident| {
                let ident_name = Name::from(ident.as_str());
                self.func_scope
                    .locals
                    .get(&ident_name)
                    .copied()
                    .or_else(|| {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, ident.range),
                                format!("`{ident_name}` 未登记为局部变量"),
                            )
                            .with_code("E0101")
                            .with_primary_label("内部错误：异常变量预扫描遗漏"),
                        );
                        None
                    })
            });

            // except 体里允许裸 `raise`
            self.handler_depth += 1;
            let handler_body = self.lower_suite(&except.body);
            self.handler_depth -= 1;

            let id = self.body.handlers.alloc(crate::hir::Handler {
                types,
                name,
                body: handler_body,
                range: except.range,
            });
            handlers.push(id);
        }

        let orelse = self.lower_suite(&try_stmt.orelse);
        let finalbody = self.lower_suite(&try_stmt.finalbody);

        self.body.stmts.alloc(Stmt {
            kind: StmtKind::Try {
                body,
                handlers,
                orelse,
                finalbody,
            },
            range: try_stmt.range,
            line: self.line_of(try_stmt.range.start().to_usize()),
        })
    }

    /// 把 `except` 的类型表达式解析成异常类型名列表。
    ///
    /// 支持 `except E:` 与 `except (E1, E2):`，其中每个名字必须是已登记的内建异常类。
    /// 返回空列表表示无法解析（调用方报错）。
    fn exception_type_names(&self, expr: &ast::Expr) -> Vec<&'static str> {
        match expr {
            ast::Expr::Name(name) => {
                let ident = Name::from(name.id.as_str());
                match self.global_scope.names.get(&ident) {
                    // 用户定义了同名类时优先用局部/模块定义，M2.6 再接自定义异常
                    Some(_) => Vec::new(),
                    None => match crate::scope::resolve_builtin(name.id.as_str()) {
                        Some(crate::hir::Builtin::Exception(type_name)) => vec![type_name],
                        _ => Vec::new(),
                    },
                }
            }
            ast::Expr::Tuple(tuple) => {
                let mut out = Vec::new();
                for elt in &tuple.elts {
                    out.extend(self.exception_type_names(elt));
                }
                out
            }
            _ => Vec::new(),
        }
    }

    fn lower_stmt(&mut self, stmt: &ast::Stmt) -> Option<StmtId> {
        let kind = match stmt {
            ast::Stmt::Expr(expr_stmt) => {
                let expr_id = self.lower_expr(&expr_stmt.value);
                StmtKind::Expr(expr_id)
            }
            ast::Stmt::Assign(assign_stmt) => {
                let val = self.lower_expr(&assign_stmt.value);
                if assign_stmt.targets.len() == 1 {
                    let target = &assign_stmt.targets[0];
                    match target {
                        ast::Expr::Name(name_expr) => {
                            let name = Name::from(name_expr.id.as_str());
                            if let Some(&loc_id) = self.func_scope.locals.get(&name) {
                                StmtKind::Assign {
                                    target: AssignTarget::Local(loc_id),
                                    value: val,
                                }
                            } else {
                                return None;
                            }
                        }
                        ast::Expr::Attribute(attr_expr) => {
                            let base = self.lower_expr(&attr_expr.value);
                            let name = Name::from(attr_expr.attr.as_str());
                            StmtKind::Assign {
                                target: AssignTarget::Attribute { base, name },
                                value: val,
                            }
                        }
                        _ => {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, target.range()),
                                    "暂不支持非简单变量或属性访问的赋值目标",
                                )
                                .with_code("E0100")
                                .with_primary_label("仅支持赋值给局部变量或属性"),
                            );
                            return None;
                        }
                    }
                } else {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, assign_stmt.range()),
                            "暂不支持多元链式赋值",
                        )
                        .with_code("E0100")
                        .with_primary_label("不支持形如 a = b = 1 的链式赋值"),
                    );
                    return None;
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
                            StmtKind::Assign {
                                target: AssignTarget::Local(loc_id),
                                value: val,
                            }
                        } else {
                            return None;
                        }
                    } else {
                        return None;
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
                    return None;
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
                            StmtKind::Assign {
                                target: AssignTarget::Local(loc_id),
                                value: bin,
                            }
                        } else {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, aug_stmt.range),
                                    "暂不支持该增量运算符",
                                )
                                .with_code("E0100")
                                .with_primary_label("暂不支持的操作符"),
                            );
                            return None;
                        }
                    } else {
                        return None;
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
                    return None;
                }
            }
            ast::Stmt::Return(ret_stmt) => {
                let val = ret_stmt.value.as_deref().map(|v| self.lower_expr(v));
                StmtKind::Return(val)
            }
            ast::Stmt::Pass(_) => StmtKind::Pass,
            ast::Stmt::Assert(assert_stmt) => {
                // `assert cond, msg` 去糖为 `if not cond: raise AssertionError(msg)`
                let cond = self.lower_expr(&assert_stmt.test);
                let not_cond = self.body.exprs.alloc(Expr {
                    kind: ExprKind::Unary {
                        op: crate::hir::UnaryOp::Not,
                        operand: cond,
                    },
                    range: assert_stmt.test.range(),
                });
                let raise_args = match &assert_stmt.msg {
                    Some(msg) => vec![self.lower_expr(msg)],
                    None => Vec::new(),
                };
                let callee = self.body.exprs.alloc(Expr {
                    kind: ExprKind::Builtin(crate::hir::Builtin::Exception("AssertionError")),
                    range: assert_stmt.range,
                });
                let call = self.body.exprs.alloc(Expr {
                    kind: ExprKind::Call {
                        callee,
                        args: raise_args,
                    },
                    range: assert_stmt.range,
                });
                let raise_stmt = self.body.stmts.alloc(Stmt {
                    kind: StmtKind::Raise {
                        value: Some(call),
                        cause: None,
                    },
                    range: assert_stmt.range,
                    line: self.line_of(assert_stmt.range.start().to_usize()),
                });
                StmtKind::If {
                    cond: not_cond,
                    then_: vec![raise_stmt],
                    else_: Vec::new(),
                }
            }
            ast::Stmt::Raise(raise_stmt) => {
                let value = raise_stmt.exc.as_deref().map(|e| self.lower_expr(e));
                let cause = raise_stmt.cause.as_deref().map(|e| self.lower_expr(e));
                // 裸 `raise` 不在 lowering 阶段报错：Python 允许它出现在任何位置，
                // 没有正在处理的异常时在运行时抛 RuntimeError("No active exception to reraise")。
                // 编译期拒绝会误伤「异常处理函数里的裸 raise」这类常见写法。
                StmtKind::Raise { value, cause }
            }
            ast::Stmt::Try(try_stmt) => return Some(self.lower_try(try_stmt)),
            ast::Stmt::If(if_stmt) => return Some(self.lower_if(if_stmt)),
            ast::Stmt::While(while_stmt) => {
                if !while_stmt.orelse.is_empty() {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, while_stmt.range),
                            "Phase 1 暂不支持 while ... else 语法",
                        )
                        .with_code("E0100")
                        .with_primary_label("不支持 while else"),
                    );
                }
                let cond = self.lower_expr(&while_stmt.test);
                self.loop_depth += 1;
                let body = self.lower_suite(&while_stmt.body);
                self.loop_depth -= 1;
                StmtKind::While { cond, body }
            }
            ast::Stmt::For(for_stmt) => {
                let kind = self.lower_for(for_stmt)?;
                let line = self.line_of(for_stmt.range.start().to_usize());
                return Some(self.body.stmts.alloc(Stmt {
                    kind,
                    range: for_stmt.range,
                    line,
                }));
            }
            ast::Stmt::Break(b) => {
                if self.loop_depth == 0 {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, b.range),
                            "'break' 只能在循环体内部使用",
                        )
                        .with_code("E0107")
                        .with_primary_label("循环外的 break"),
                    );
                }
                StmtKind::Break
            }
            ast::Stmt::Continue(c) => {
                if self.loop_depth == 0 {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, c.range),
                            "'continue' 只能在循环体内部使用",
                        )
                        .with_code("E0107")
                        .with_primary_label("循环外的 continue"),
                    );
                }
                StmtKind::Continue
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
                return None;
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
                return None;
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
                return None;
            }
            _ => {
                self.diagnostics.push(
                    Diagnostic::error(Span::new(self.file, stmt.range()), "暂不支持该语句")
                        .with_code("E0100")
                        .with_primary_label("暂不支持此语法结构"),
                );
                return None;
            }
        };

        let range = stmt.range();
        let line = self.line_of(range.start().to_usize());
        Some(self.body.stmts.alloc(Stmt { kind, range, line }))
    }
}
