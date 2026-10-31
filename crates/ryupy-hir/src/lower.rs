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
    AssignTarget, Binding, Body, Expr, ExprId, ExprKind, Function, FunctionKind, Local, Module,
    ModuleInfo, ModuleVar, ModuleVarId, Name, Param, Stmt, StmtId, StmtKind,
};
use crate::operator::lower_type_ref;
use crate::scope;
use crate::scope::{FunctionScope, GlobalScope, Resolution, collect_assigned_names};

/// 降级整个模块。即使有错误也返回一个结构完整的 `Module`。
///
/// `info` 由 driver 提供（模块名 / 包名 / 文件名），HIR 不读文件也不搜索模块；
/// 它只影响 `__name__` 等特殊变量、相对导入的绝对化以及诊断展示。
pub fn lower_module(
    parsed: &ParsedModule,
    info: &ModuleInfo,
    source: &str,
) -> (Module, Vec<Diagnostic>) {
    let mut diagnostics = Vec::new();
    let line_starts = compute_line_starts(source);
    let mut functions = Arena::default();
    let mut classes = Arena::default();
    let mut global_scope = GlobalScope::default();

    // 模块级变量与导入绑定（M2.7）：见 module_vars.rs。
    let (mut vars, module_var_ids) = crate::module_vars::collect_module_vars(parsed, info);

    // 第一遍：收集所有顶层 class 与 def，支持前向引用。
    //
    // 类体的方法也是普通函数：这里先把它们全部分配 FuncId，函数体内可以互相前向
    // 引用（通过 `self.other()` 在运行时查找），也保证类体属性表达式与方法体在
    // 同一轮之后统一降级。
    let mut raw_classes = Vec::new();
    let mut user_funcs: Vec<(ast::StmtFunctionDef, crate::hir::FuncId)> = Vec::new();
    let mut top_classes: Vec<crate::hir::ClassId> = Vec::new();
    let mut top_functions: Vec<crate::hir::FuncId> = Vec::new();
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
                    base: None,
                    fields: Vec::new(),
                    body: Vec::new(),
                    frozen: false,
                    is_dataclass: false,
                    is_exception: false,
                    range: cls_def.range(),
                });
                global_scope.names.insert(name, Resolution::Class(cls_id));
                top_classes.push(cls_id);

                let mut methods = Vec::new();
                for item in &cls_def.body {
                    let ast::Stmt::FunctionDef(fn_def) = item else {
                        continue;
                    };
                    let method_name = Name::from(fn_def.name.as_str());
                    let func_id = functions.alloc(crate::hir::Function {
                        name: method_name.clone(),
                        kind: crate::hir::FunctionKind::User,
                        params: Vec::new(),
                        ret: None,
                        body: Body::default(),
                        range: fn_def.range(),
                    });
                    methods.push(crate::class::MethodSlot {
                        name: method_name,
                        func: func_id,
                        range: fn_def.range(),
                    });
                    user_funcs.push((fn_def.clone(), func_id));
                }
                raw_classes.push((cls_def, cls_id, methods));
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
                top_functions.push(func_id);
                user_funcs.push((fn_def.clone(), func_id));
            }
            _ => {}
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

    let mut main_body = Body::default();

    // 解析所有类：基类必须已经解析过，因此这里保持源码顺序。
    // 类体属性表达式降级到 main_body，由 MIR 在 `class` 语句位置求值。
    for (cls_def, cls_id, methods) in raw_classes {
        if let Some(parsed_cls) = crate::class::parse_class_def(
            cls_def,
            parsed.file,
            &global_scope,
            &classes,
            &methods,
            &module_var_ids,
            &mut main_body,
            &mut diagnostics,
        ) {
            classes[cls_id] = parsed_cls;
        }
    }

    // 第二遍：降级各个用户函数体（含类方法）
    let mut all_default_exprs = Vec::new();
    for (fn_def, func_id) in user_funcs {
        let mut body = Body::default();
        let mut func_scope = FunctionScope::default();
        // 先把 `global` 声明记进作用域：这些名字既不能登记成局部变量，
        // 赋值/读取也要落到模块命名空间。
        for name in scope::collect_global_names(&fn_def.body) {
            func_scope.globals.insert(name);
        }
        let mut params = Vec::new();

        // 1. 参数处理：按 Python 的绑定顺序（位置-only → 普通 → *args → 关键字-only → **kwargs）。
        //
        // 默认值表达式降级到 **main 的 body**：Python 在 def 执行时于模块作用域求值，
        // 不是每次调用时求值（`def f(x=[])` 的列表在多次调用间共享）。
        let mut default_exprs = Vec::new();
        let mut push_param = |params: &mut Vec<Param>,
                              func_scope: &mut FunctionScope,
                              body: &mut Body,
                              diagnostics: &mut Vec<Diagnostic>,
                              parameter: &ast::Parameter,
                              kind: crate::hir::ParamKind,
                              default: Option<&ast::Expr>| {
            let param_name = Name::from(parameter.name.as_str());
            let ann = parameter
                .annotation
                .as_deref()
                .map(|e| lower_type_ref(e, parsed.file, &global_scope, diagnostics));
            let local_id = body.locals.alloc(Local {
                name: param_name.clone(),
                annotation: ann.clone(),
                is_param: true,
                range: parameter.range,
            });
            func_scope.locals.insert(param_name, local_id);
            let default = default.map(|expr| {
                let id = lower_default_expr(
                    parsed.file,
                    &global_scope,
                    &module_var_ids,
                    &mut main_body,
                    diagnostics,
                    expr,
                );
                default_exprs.push(id);
                id
            });
            params.push(Param {
                annotation: ann,
                local: local_id,
                default,
                kind,
                range: parameter.range,
            });
        };

        for param_def in &fn_def.parameters.posonlyargs {
            push_param(
                &mut params,
                &mut func_scope,
                &mut body,
                &mut diagnostics,
                &param_def.parameter,
                crate::hir::ParamKind::PositionalOnly,
                param_def.default.as_deref(),
            );
        }
        for param_def in &fn_def.parameters.args {
            push_param(
                &mut params,
                &mut func_scope,
                &mut body,
                &mut diagnostics,
                &param_def.parameter,
                crate::hir::ParamKind::PositionalOrKeyword,
                param_def.default.as_deref(),
            );
        }
        if let Some(vararg) = &fn_def.parameters.vararg {
            push_param(
                &mut params,
                &mut func_scope,
                &mut body,
                &mut diagnostics,
                vararg,
                crate::hir::ParamKind::VarPositional,
                None,
            );
        }
        for param_def in &fn_def.parameters.kwonlyargs {
            push_param(
                &mut params,
                &mut func_scope,
                &mut body,
                &mut diagnostics,
                &param_def.parameter,
                crate::hir::ParamKind::KeywordOnly,
                param_def.default.as_deref(),
            );
        }
        if let Some(kwarg) = &fn_def.parameters.kwarg {
            push_param(
                &mut params,
                &mut func_scope,
                &mut body,
                &mut diagnostics,
                kwarg,
                crate::hir::ParamKind::VarKeyword,
                None,
            );
        }

        // 2. 返回值标注
        let ret = fn_def
            .returns
            .as_deref()
            .map(|e| lower_type_ref(e, parsed.file, &global_scope, &mut diagnostics));

        // 3. 预扫描函数内被赋值的局部变量（赋值即局部）
        for (name, range) in collect_assigned_names(&fn_def.body) {
            if func_scope.is_global(&name) {
                continue;
            }
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
            module_vars: &module_var_ids,
            vars: &mut vars,
            module_scope: false,
            package: &info.package,
            classes: &classes,
            body: &mut body,
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
        all_default_exprs.extend(default_exprs);
    }

    // 降级 main 函数（模块顶层非 FunctionDef 语句）
    let mut main_scope = FunctionScope::default();
    let mut main_lower_ctx = StmtLowerCtx {
        file: parsed.file,
        diagnostics: &mut diagnostics,
        global_scope: &global_scope,
        func_scope: &mut main_scope,
        module_vars: &module_var_ids,
        vars: &mut vars,
        module_scope: true,
        package: &info.package,
        classes: &classes,
        body: &mut main_body,
        loop_depth: 0,
        handler_depth: 0,
        line_starts: &line_starts,
    };
    // `__name__` / `__package__` / `__file__` 在模块代码执行前写入。
    main_lower_ctx.emit_special_module_vars(info, parsed.module.range);
    for stmt in &parsed.module.body {
        match stmt {
            // `def` 不产生普通语句，但它的默认值必须在**定义位置**求值
            ast::Stmt::FunctionDef(fn_def) => {
                let name = Name::from(fn_def.name.as_str());
                if let Some(Resolution::Func(func_id)) = global_scope.names.get(&name) {
                    main_lower_ctx.lower_function_defaults(*func_id, fn_def.range());
                }
            }
            ast::Stmt::ClassDef(cls_def) => {
                let name = Name::from(cls_def.name.as_str());
                if let Some(Resolution::Class(cls_id)) = global_scope.names.get(&name) {
                    main_lower_ctx.lower_class_def(*cls_id, cls_def.range());
                }
            }
            ast::Stmt::Import(_) | ast::Stmt::ImportFrom(_) => {
                main_lower_ctx.lower_import_to_root(stmt);
            }
            _ => main_lower_ctx.lower_stmt_to_root(stmt),
        }
    }
    functions[main_id].body = main_body;

    let module = Module {
        file: parsed.file,
        source_name: info.file.clone(),
        info: info.clone(),
        functions,
        classes,
        vars,
        top_functions,
        top_classes,
        main: main_id,
        default_exprs: all_default_exprs,
    };

    (module, diagnostics)
}

pub(crate) struct StmtLowerCtx<'a> {
    pub(crate) file: FileId,
    pub(crate) diagnostics: &'a mut Vec<Diagnostic>,
    pub(crate) global_scope: &'a GlobalScope,
    pub(crate) func_scope: &'a mut FunctionScope,
    /// 模块级变量表：模块作用域里的名字直接落到这里。
    pub(crate) module_vars: &'a FxHashMap<Name, ModuleVarId>,
    /// 模块级变量 arena；`x: T = ...` 要在 lowering 时补上标注。
    pub(crate) vars: &'a mut Arena<ModuleVar>,
    /// 是否位于模块顶层（只有模块顶层允许 import 语句）。
    pub(crate) module_scope: bool,
    /// 当前模块的 `__package__`，用于把相对导入换算成绝对模块名。
    pub(crate) package: &'a str,
    /// 已解析的类（`except` 需要判断用户类是否属于异常家族）。
    pub(crate) classes: &'a Arena<crate::hir::Class>,
    pub(crate) body: &'a mut Body,
    pub(crate) loop_depth: usize,
    /// 当前所处的 `except` 体层数。只有 > 0 时裸 `raise` 才有意义。
    pub(crate) handler_depth: usize,
    /// 每一行的起始字节偏移，用于把 `TextRange` 换算成行号。
    pub(crate) line_starts: &'a [usize],
}

/// 把参数默认值表达式降级到模块入口函数的 body。
///
/// 默认值在模块作用域求值，因此这里用空函数作用域 + 模块变量表解析名字。
fn lower_default_expr(
    file: FileId,
    global_scope: &GlobalScope,
    module_vars: &FxHashMap<Name, ModuleVarId>,
    body: &mut Body,
    diagnostics: &mut Vec<Diagnostic>,
    expr: &ast::Expr,
) -> ExprId {
    let scope = FunctionScope::default();
    let mut ctx = ExprLowerCtx {
        file,
        global_scope,
        func_scope: &scope,
        module_vars,
        body,
        diagnostics,
    };
    ctx.lower_expr(expr)
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

    /// 语句降级复用：把下标位置（可能是切片）降级成表达式。
    pub(crate) fn lower_subscript_index(&mut self, slice: &ast::Expr) -> ExprId {
        let mut expr_ctx = ExprLowerCtx {
            file: self.file,
            global_scope: self.global_scope,
            func_scope: self.func_scope,
            module_vars: self.module_vars,
            body: self.body,
            diagnostics: self.diagnostics,
        };
        expr_ctx.lower_subscript_index(slice)
    }

    pub(crate) fn lower_expr(&mut self, expr: &ast::Expr) -> ExprId {
        let mut expr_ctx = ExprLowerCtx {
            file: self.file,
            global_scope: self.global_scope,
            func_scope: self.func_scope,
            module_vars: self.module_vars,
            body: self.body,
            diagnostics: self.diagnostics,
        };
        expr_ctx.lower_expr(expr)
    }

    /// 在当前位置插入一条「求值某函数默认值」的内部语句。
    pub(crate) fn lower_function_defaults(
        &mut self,
        func_id: crate::hir::FuncId,
        range: ryupy_base::TextRange,
    ) {
        let line = self.line_of(range.start().to_usize());
        let stmt_id = self.body.stmts.alloc(Stmt {
            kind: StmtKind::FunctionDefaults(func_id),
            range,
            line,
        });
        self.body.root.push(stmt_id);
    }

    /// 在当前位置插入一条类定义语句（类体属性与方法对象由 MIR 按顺序执行）。
    pub(crate) fn lower_class_def(
        &mut self,
        cls_id: crate::hir::ClassId,
        range: ryupy_base::TextRange,
    ) {
        let line = self.line_of(range.start().to_usize());
        let stmt_id = self.body.stmts.alloc(Stmt {
            kind: StmtKind::ClassDef(cls_id),
            range,
            line,
        });
        self.body.root.push(stmt_id);
    }

    fn lower_stmt_to_root(&mut self, stmt: &ast::Stmt) {
        if matches!(stmt, ast::Stmt::Import(_) | ast::Stmt::ImportFrom(_)) {
            self.lower_import_to_root(stmt);
            return;
        }
        if let Some(stmt_id) = self.lower_stmt(stmt) {
            self.body.root.push(stmt_id);
        }
    }

    pub(crate) fn lower_suite(&mut self, stmts: &[ast::Stmt]) -> Vec<StmtId> {
        let mut out = Vec::new();
        for s in stmts {
            if matches!(s, ast::Stmt::Import(_) | ast::Stmt::ImportFrom(_)) {
                self.lower_import_into(s, &mut out);
                continue;
            }
            if let Some(id) = self.lower_stmt(s) {
                out.push(id);
            }
        }
        out
    }

    /// 序列解包目标：`a, b` / `first, *rest, last`。
    pub(crate) fn lower_sequence_target(
        &mut self,
        elements: &[ast::Expr],
        range: ryupy_base::TextRange,
    ) -> Option<Vec<crate::hir::SequenceItem>> {
        let mut items = Vec::new();
        for element in elements {
            match element {
                ast::Expr::Name(name_expr) => {
                    let name = Name::from(name_expr.id.as_str());
                    let binding = self.resolve_binding(&name)?;
                    items.push(crate::hir::SequenceItem::One(binding));
                }
                ast::Expr::Starred(starred) => {
                    let ast::Expr::Name(name_expr) = starred.value.as_ref() else {
                        self.diagnostics.push(
                            Diagnostic::error(
                                Span::new(self.file, starred.range),
                                "星号解包的目标必须是简单变量",
                            )
                            .with_code("E0100")
                            .with_primary_label("暂不支持嵌套解包目标"),
                        );
                        return None;
                    };
                    let name = Name::from(name_expr.id.as_str());
                    let binding = self.resolve_binding(&name)?;
                    items.push(crate::hir::SequenceItem::Rest(binding));
                }
                _ => {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, element.range()),
                            "暂不支持嵌套的解包目标",
                        )
                        .with_code("E0100")
                        .with_primary_label("只支持变量与星号变量"),
                    );
                    return None;
                }
            }
        }
        if items
            .iter()
            .filter(|i| matches!(i, crate::hir::SequenceItem::Rest(_)))
            .count()
            > 1
        {
            self.diagnostics.push(
                Diagnostic::error(Span::new(self.file, range), "解包目标中只能有一个星号变量")
                    .with_code("E0100")
                    .with_primary_label("多个星号解包"),
            );
            return None;
        }
        Some(items)
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
                self.resolve_binding(&ident_name).or_else(|| {
                    self.diagnostics.push(
                        Diagnostic::error(
                            Span::new(self.file, ident.range),
                            format!("`{ident_name}` 未登记为变量"),
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
    /// 支持 `except E:` 与 `except (E1, E2):`，其中每个名字可以是内建异常类，
    /// 也可以是**继承自 BaseException 的用户类**（M2.6）。返回空列表表示无法解析，
    /// 调用方报诊断。
    fn exception_type_names(&self, expr: &ast::Expr) -> Vec<String> {
        match expr {
            ast::Expr::Name(name) => {
                let ident = Name::from(name.id.as_str());
                match self.global_scope.names.get(&ident) {
                    // 用户类：必须位于 BaseException 家族，否则 `except X` 在 CPython
                    // 里是 TypeError，这里作为暂不支持的能力明确拒绝。
                    Some(Resolution::Class(cls_id)) => {
                        let cls = &self.classes[*cls_id];
                        if cls.is_exception {
                            vec![cls.name.as_str().to_owned()]
                        } else {
                            Vec::new()
                        }
                    }
                    Some(_) => Vec::new(),
                    None => match crate::scope::resolve_builtin(name.id.as_str()) {
                        Some(crate::hir::Builtin::Exception(type_name)) => {
                            vec![type_name.to_owned()]
                        }
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
                            let target = match self.resolve_binding(&name) {
                                Some(Binding::Local(local)) => AssignTarget::Local(local),
                                Some(Binding::ModuleVar(var)) => AssignTarget::ModuleVar(var),
                                None => {
                                    self.diagnostics.push(
                                        Diagnostic::error(
                                            Span::new(self.file, name_expr.range),
                                            format!("未定义的赋值目标 `{name}`"),
                                        )
                                        .with_code("E0106")
                                        .with_primary_label("名字未登记为局部变量或模块变量"),
                                    );
                                    return None;
                                }
                            };
                            StmtKind::Assign { target, value: val }
                        }
                        ast::Expr::Attribute(attr_expr) => {
                            let base = self.lower_expr(&attr_expr.value);
                            let name = Name::from(attr_expr.attr.as_str());
                            StmtKind::Assign {
                                target: AssignTarget::Attribute { base, name },
                                value: val,
                            }
                        }
                        ast::Expr::Subscript(sub) => {
                            let base = self.lower_expr(&sub.value);
                            let index = self.lower_subscript_index(&sub.slice);
                            StmtKind::Assign {
                                target: AssignTarget::Subscript { base, index },
                                value: val,
                            }
                        }
                        ast::Expr::Tuple(tuple) => {
                            let items = self.lower_sequence_target(&tuple.elts, tuple.range)?;
                            StmtKind::Assign {
                                target: AssignTarget::Sequence(items),
                                value: val,
                            }
                        }
                        ast::Expr::List(list) => {
                            let items = self.lower_sequence_target(&list.elts, list.range)?;
                            StmtKind::Assign {
                                target: AssignTarget::Sequence(items),
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
                    let target = match self.resolve_binding(&name) {
                        Some(Binding::Local(local)) => {
                            self.body.locals[local].annotation = Some(ann);
                            AssignTarget::Local(local)
                        }
                        Some(Binding::ModuleVar(var)) => {
                            self.vars[var].annotation = Some(ann);
                            AssignTarget::ModuleVar(var)
                        }
                        None => return None,
                    };
                    let Some(val_expr) = &ann_stmt.value else {
                        return None;
                    };
                    let val = self.lower_expr(val_expr);
                    StmtKind::Assign { target, value: val }
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
            // 增量赋值的去糖与「目标只求值一次」见 aug_assign.rs
            ast::Stmt::AugAssign(aug_stmt) => return self.lower_aug_assign(aug_stmt),
            ast::Stmt::Return(ret_stmt) => {
                let val = ret_stmt.value.as_deref().map(|v| self.lower_expr(v));
                StmtKind::Return(val)
            }
            ast::Stmt::Pass(_) => StmtKind::Pass,
            ast::Stmt::Delete(del_stmt) => {
                let mut targets = Vec::new();
                for target in &del_stmt.targets {
                    match target {
                        // 删除名字需要模块命名空间（M2.7），这里只处理容器下标与属性。
                        ast::Expr::Subscript(sub) => {
                            let base = self.lower_expr(&sub.value);
                            let index = self.lower_subscript_index(&sub.slice);
                            let id = self.body.exprs.alloc(Expr {
                                kind: ExprKind::Subscript { base, index },
                                range: sub.range,
                            });
                            targets.push(id);
                        }
                        ast::Expr::Attribute(attr) => {
                            let base = self.lower_expr(&attr.value);
                            let name = Name::from(attr.attr.as_str());
                            let id = self.body.exprs.alloc(Expr {
                                kind: ExprKind::Attribute { base, name },
                                range: attr.range,
                            });
                            targets.push(id);
                        }
                        _ => {
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(self.file, target.range()),
                                    "暂不支持该 del 目标",
                                )
                                .with_code("E0100")
                                .with_primary_label("目前只支持 del obj[...] 与 del obj.attr"),
                            );
                        }
                    }
                }
                StmtKind::Delete(targets)
            }
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
                        args: raise_args
                            .into_iter()
                            .map(crate::hir::CallArg::Positional)
                            .collect(),
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
            ast::Stmt::With(with_stmt) => return self.lower_with(with_stmt),
            ast::Stmt::If(if_stmt) => return Some(self.lower_if(if_stmt)),
            ast::Stmt::While(while_stmt) => {
                let cond = self.lower_expr(&while_stmt.test);
                self.loop_depth += 1;
                let body = self.lower_suite(&while_stmt.body);
                self.loop_depth -= 1;
                // `else` 不属于循环体：按当前 loop_depth 降级
                let orelse = self.lower_suite(&while_stmt.orelse);
                StmtKind::While { cond, body, orelse }
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
            // `global` 在预扫描阶段已经登记为模块变量，这里不需要运行时动作。
            ast::Stmt::Global(_) => return None,
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
