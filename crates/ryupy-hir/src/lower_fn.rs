//! 单个用户函数体的降级（顶层 def / 方法 / 嵌套 def / lambda 共用一条路径）。
//!
//! [`lower_function_body`] 从 [`LowerArgs`] 取模块级上下文：参数与标注降级、
//! `global` / `nonlocal` / free 变量的作用域准备、cell / env 的登记，最后把
//! `ClosureInfo` 写回 HIR。参数默认值：顶层函数在 main 作用域求值并登记
//! `default_exprs`；嵌套函数留给外层 def 位置（见 `closures_lower.rs`）。

use la_arena::Arena;
use rustc_hash::FxHashMap;
use ryupy_base::{Diagnostic, FileId};
use ryupy_syntax::ast;

use crate::hir::ModuleVarId;
use crate::hir::{Body, ExprId, Function, Local, ModuleVar, Name, Param, Stmt, StmtKind};
use crate::lower::StmtLowerCtx;
use crate::operator::lower_type_ref;
use crate::scope::{FunctionScope, GlobalScope, collect_assigned_names};

/// 一个待降级函数的 AST 来源：def 语句或 lambda 表达式。
pub(crate) enum FnSource<'a> {
    Def(&'a ast::StmtFunctionDef),
    Lambda(&'a ast::ExprLambda),
    Comprehension(crate::comprehension_ast::Comprehension<'a>),
}

/// 降级单个函数体所需的全部上下文（lower_module 的局部状态打包）。
#[allow(clippy::too_many_arguments)]
pub(crate) struct LowerArgs<'a, 'b> {
    pub(crate) class_names: Option<&'a rustc_hash::FxHashSet<Name>>,
    pub(crate) file: FileId,
    pub(crate) package: &'a str,
    pub(crate) source: FnSource<'b>,
    pub(crate) func_id: crate::hir::FuncId,
    pub(crate) functions: &'a mut Arena<Function>,
    pub(crate) global_scope: &'a GlobalScope,
    pub(crate) module_vars: &'a FxHashMap<Name, ModuleVarId>,
    pub(crate) vars: &'a mut Arena<ModuleVar>,
    pub(crate) classes: &'a Arena<crate::hir::Class>,
    pub(crate) analysis: &'a crate::closures::ClosureAnalysis,
    pub(crate) nested_defaults: &'a mut crate::definition_metadata::DefinitionMetadata,
    pub(crate) all_default_exprs: &'a mut Vec<ExprId>,
    /// 顶层函数参数默认值的降级目标（嵌套函数的默认值在 def 位置由
    /// `lower_nested_def` 降级进外层函数体，不走这里）。
    pub(crate) main_body: &'a mut Body,
    pub(crate) line_starts: &'a [usize],
    pub(crate) diagnostics: &'a mut Vec<Diagnostic>,
}

/// 降级一个用户函数（顶层 def / 方法 / 嵌套 def / lambda）的函数体。
pub(crate) fn lower_function_body(args: LowerArgs<'_, '_>) {
    let LowerArgs {
        class_names,
        file,
        package,
        source,
        func_id,
        functions,
        global_scope,
        module_vars,
        vars,
        classes,
        analysis,
        nested_defaults,
        all_default_exprs,
        main_body,
        line_starts,
        diagnostics,
    } = args;

    if let FnSource::Def(def) = &source {
        crate::closures_decl::validate_declarations(file, def, diagnostics);
    }
    let is_generator = match &source {
        FnSource::Def(def) => crate::generator_scan::analyze(file, &def.body, diagnostics),
        FnSource::Comprehension(comp) => {
            let generator = comp.kind == crate::comprehension_ast::ComprehensionKind::Generator;
            crate::generator_scan::validate_expression(file, *comp, diagnostics);
            generator
        }
        FnSource::Lambda(lambda) => {
            crate::generator_scan::analyze_lambda(file, &lambda.body, diagnostics)
        }
    };
    let is_lambda = matches!(source, FnSource::Lambda(_));
    let info = analysis.infos.get(&func_id).cloned().unwrap_or_default();
    // 嵌套函数（def / lambda）的默认值不在 main 作用域求值：
    // 它们由 lower_nested_def 在外层作用域的 def 位置求值。
    let is_nested = analysis.nested_order.iter().any(|(id, _)| *id == func_id);

    let mut body = Body::default();
    let mut func_scope = FunctionScope::default();
    // `global` / `nonlocal` 声明先行登记：这些名字既不能登记成局部变量，
    // 读写也要分别落到模块命名空间 / 外层 cell。
    let body_stmts: &[ast::Stmt] = match &source {
        FnSource::Def(fn_def) => &fn_def.body,
        FnSource::Lambda(_) | FnSource::Comprehension(_) => &[],
    };
    for name in crate::scope::collect_global_names(body_stmts) {
        func_scope.globals.insert(name);
    }
    for name in crate::closures_find::collect_nonlocal_names(body_stmts) {
        func_scope.nonlocals.insert(name);
    }
    // 外层 cell 名 → free 下标；被捕获的局部名 → cell 集合。
    for (index, name) in info.free.iter().enumerate() {
        func_scope.free.insert(name.clone(), index);
    }
    for name in &info.cells {
        func_scope.cells.insert(name.clone());
    }

    let docstring = match body_stmts.first() {
        Some(ast::Stmt::Expr(statement)) => match statement.value.as_ref() {
            ast::Expr::StringLiteral(text) => Some(text.value.to_str().to_owned()),
            _ => None,
        },
        _ => None,
    };
    let mut params = Vec::new();
    // 默认值按「最终参数顺序」对齐：顶层函数在 main 作用域求值（共享同一份），
    // 嵌套函数在此记录对齐表、由 lower_nested_def 在外层 def 位置求值。
    let mut aligned_defaults: Vec<Option<ExprId>> = Vec::new();

    let mut push_param = |params: &mut Vec<Param>,
                          func_scope: &mut FunctionScope,
                          body: &mut Body,
                          parameter: &ast::Parameter,
                          kind: crate::hir::ParamKind,
                          default: Option<&ast::Expr>| {
        let param_name = Name::from(parameter.name.as_str());
        let ann = parameter
            .annotation
            .as_deref()
            .map(|e| lower_type_ref(e, file, global_scope, diagnostics));
        let local_id = body.locals.alloc(Local {
            name: param_name.clone(),
            annotation: ann.clone(),
            is_param: true,
            range: parameter.range,
        });
        func_scope.locals.insert(param_name, local_id);
        let default = if is_nested {
            // 嵌套函数：默认值由外层作用域在 def 位置求值（lower_nested_def），
            // 这里先留空，循环结束后从 nested_defaults 回填。
            None
        } else {
            default.map(|expr| {
                // 顶层函数：默认值在模块作用域求值（`def f(x=[])` 的列表在调用间共享）。
                let id = crate::lower::lower_default_expr(
                    class_names,
                    file,
                    global_scope,
                    module_vars,
                    &analysis.nested_funcs,
                    nested_defaults,
                    main_body,
                    diagnostics,
                    expr,
                );
                all_default_exprs.push(id);
                id
            })
        };
        aligned_defaults.push(default);
        params.push(Param {
            annotation: ann,
            local: local_id,
            default,
            kind,
            range: parameter.range,
        });
    };

    match &source {
        FnSource::Comprehension(comp) => {
            crate::comprehensions::prepare_scope(*comp, &mut params, &mut func_scope, &mut body);
        }
        FnSource::Def(fn_def) => {
            for param_def in &fn_def.parameters.posonlyargs {
                push_param(
                    &mut params,
                    &mut func_scope,
                    &mut body,
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
                    kwarg,
                    crate::hir::ParamKind::VarKeyword,
                    None,
                );
            }
        }
        FnSource::Lambda(lambda) => {
            if let Some(parameters) = &lambda.parameters {
                for param_def in &parameters.posonlyargs {
                    push_param(
                        &mut params,
                        &mut func_scope,
                        &mut body,
                        &param_def.parameter,
                        crate::hir::ParamKind::PositionalOnly,
                        None,
                    );
                }
                for param_def in &parameters.args {
                    push_param(
                        &mut params,
                        &mut func_scope,
                        &mut body,
                        &param_def.parameter,
                        crate::hir::ParamKind::PositionalOrKeyword,
                        None,
                    );
                }
                if let Some(vararg) = &parameters.vararg {
                    push_param(
                        &mut params,
                        &mut func_scope,
                        &mut body,
                        vararg,
                        crate::hir::ParamKind::VarPositional,
                        None,
                    );
                }
                for param_def in &parameters.kwonlyargs {
                    push_param(
                        &mut params,
                        &mut func_scope,
                        &mut body,
                        &param_def.parameter,
                        crate::hir::ParamKind::KeywordOnly,
                        None,
                    );
                }
                if let Some(kwarg) = &parameters.kwarg {
                    push_param(
                        &mut params,
                        &mut func_scope,
                        &mut body,
                        kwarg,
                        crate::hir::ParamKind::VarKeyword,
                        None,
                    );
                }
            }
        }
    }

    // 返回值标注（lambda 没有标注）
    let ret = match &source {
        FnSource::Def(fn_def) => fn_def
            .returns
            .as_deref()
            .map(|e| lower_type_ref(e, file, global_scope, diagnostics)),
        FnSource::Lambda(_) | FnSource::Comprehension(_) => None,
    };

    // 预扫描本作用域绑定的局部变量（赋值即局部；嵌套 def 名也是局部）。
    // `nonlocal` 声明的名字跳过：它们指向外层 cell，不是本函数的局部。
    if !is_lambda {
        let mut bound: Vec<(Name, ryupy_base::TextRange)> = collect_assigned_names(body_stmts);
        for name in crate::closures_find::collect_def_names(body_stmts) {
            bound.push((name.clone(), ryupy_base::TextRange::default()));
        }
        for (name, range) in bound {
            if func_scope.is_global(&name) || func_scope.nonlocals.contains(&name) {
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
    }

    if !is_nested && let FnSource::Def(def) = &source {
        let mut annotations = Vec::new();
        for parameter in def.parameters.iter() {
            if let Some(expr) = parameter.annotation() {
                let value = crate::lower::lower_default_expr(
                    class_names,
                    file,
                    global_scope,
                    module_vars,
                    &analysis.nested_funcs,
                    nested_defaults,
                    main_body,
                    diagnostics,
                    expr,
                );
                all_default_exprs.push(value);
                annotations.push((Name::from(parameter.name().as_str()), value));
            }
        }
        if let Some(expr) = &def.returns {
            let value = crate::lower::lower_default_expr(
                class_names,
                file,
                global_scope,
                module_vars,
                &analysis.nested_funcs,
                nested_defaults,
                main_body,
                diagnostics,
                expr,
            );
            all_default_exprs.push(value);
            annotations.push((Name::from("return"), value));
        }
        nested_defaults.annotations.insert(func_id, annotations);
    }

    // 降级函数体
    let mut lower_ctx = StmtLowerCtx {
        file,
        diagnostics,
        global_scope,
        func_scope: &mut func_scope,
        module_vars,
        vars,
        module_scope: false,
        package,
        classes,
        nested_funcs: &analysis.nested_funcs,
        nested_defaults,
        body: &mut body,
        loop_depth: 0,
        handler_depth: 0,
        line_starts,
    };
    match source {
        FnSource::Comprehension(comp) => lower_ctx.lower_comprehension_body(comp),
        FnSource::Def(fn_def) => {
            for stmt in &fn_def.body {
                lower_ctx.lower_stmt_to_root(stmt);
            }
        }
        FnSource::Lambda(lambda) => {
            // lambda 体去糖为一条 `return <表达式>`。
            let line = lower_ctx.line_of(lambda.range.start().to_usize());
            let value = lower_ctx.lower_expr(&lambda.body);
            let stmt = body.stmts.alloc(Stmt {
                kind: StmtKind::Return(Some(value)),
                range: lambda.range,
                line,
            });
            body.root.push(stmt);
        }
    }

    functions[func_id].docstring = docstring;
    functions[func_id].is_generator = is_generator;
    functions[func_id].params = params;
    functions[func_id].ret = ret;
    functions[func_id].body = body;
    functions[func_id].closure = info;
}
