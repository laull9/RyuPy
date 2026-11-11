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
    Body, ExprId, Function, FunctionKind, Module, ModuleInfo, ModuleVar, ModuleVarId, Name, Stmt,
    StmtKind,
};
use crate::lower_fn::{FnSource, LowerArgs, lower_function_body};
use crate::scope::{FunctionScope, GlobalScope, Resolution};

/// 降级整个模块。即使有错误也返回一个结构完整的 `Module`。
///
/// `info` 由 driver 提供（模块名 / 包名 / 文件名），HIR 不读文件也不搜索模块；
/// 它只影响 `__name__` 等特殊变量、相对导入的绝对化以及诊断展示。
pub fn lower_module(
    parsed: &ParsedModule,
    info: &ModuleInfo,
    source: &str,
) -> (Module, Vec<Diagnostic>) {
    lower_module_with_native_exceptions(parsed, info, source, &[])
}

/// driver 提供未被本地源码遮蔽的原生模块名；HIR 不做文件系统探测。
pub fn lower_module_with_native_exceptions(
    parsed: &ParsedModule,
    info: &ModuleInfo,
    source: &str,
    native_exceptions: &[&str],
) -> (Module, Vec<Diagnostic>) {
    if info.instance_globals {
        let diagnostics = crate::instance_globals::validate(
            &parsed.module.body,
            parsed.file,
            info.instance_capabilities,
        );
        if !diagnostics.is_empty() {
            // 预检失败不继续降级：保留结构完整的空模块供诊断调用方使用。
            let mut empty = ParsedModule {
                file: parsed.file,
                module: parsed.module.clone(),
            };
            empty.module.body.clear();
            let (module, _) =
                lower_module_with_native_exceptions(&empty, info, source, native_exceptions);
            return (module, diagnostics);
        }
    }
    let mut diagnostics = crate::lexical_private::validate(parsed.file, &parsed.module.body);
    crate::future_imports::validate(&parsed.module.body, parsed.file, &mut diagnostics);
    let line_starts = crate::source_lines::line_starts(source);
    let mut functions = Arena::default();
    let mut classes = Arena::default();
    let mut global_scope = GlobalScope {
        instance_globals: info.instance_globals,
        deleted_names: crate::module_deletions::collect(&parsed.module.body),
        postponed_annotations: crate::future_imports::annotations(&parsed.module.body),
        native_exception_modules: native_exceptions
            .iter()
            .map(|name| (*name).to_owned())
            .collect(),
        exception_imports: crate::exception_imports::collect(
            &parsed.module.body,
            native_exceptions,
        ),
        dataclass_markers: crate::decorators::dataclass_markers(&parsed.module.body),
        ..GlobalScope::default()
    };

    crate::decorators::validate_mutable_markers(
        &parsed.module.body,
        &global_scope.dataclass_markers,
        parsed.file,
        &mut diagnostics,
    );

    // 模块级变量与导入绑定（M2.7）：见 module_vars.rs。
    let (mut vars, mut module_var_ids) =
        crate::module_vars::collect_module_vars(parsed, info, &global_scope.deleted_names);
    crate::module_deletions::validate_default_builtins(
        &vars,
        info.instance_globals,
        parsed.file,
        &mut diagnostics,
    );

    crate::typed_dict::register(parsed, info, native_exceptions, &mut global_scope);

    // 第一遍：收集所有顶层 class 与 def，支持前向引用。
    //
    // 类体的方法也是普通函数：这里先把它们全部分配 FuncId，函数体内可以互相前向
    // 引用（通过 `self.other()` 在运行时查找），也保证类体属性表达式与方法体在
    // 同一轮之后统一降级。
    // 在任何函数体/default降级前统一公开绑定；内部FuncId登记并不等于执行def。
    crate::module_definition_bindings::register(
        parsed,
        &mut global_scope,
        &mut vars,
        &mut module_var_ids,
    );
    let mut definition_functions = FxHashMap::default();
    let mut definition_classes = FxHashMap::default();
    let mut raw_classes = Vec::new();
    let mut user_funcs: Vec<(&ast::StmtFunctionDef, crate::hir::FuncId)> = Vec::new();
    let mut top_classes: Vec<crate::hir::ClassId> = Vec::new();
    let mut top_functions: Vec<crate::hir::FuncId> = Vec::new();
    for stmt in crate::module_definitions::definitions(&parsed.module.body) {
        match stmt {
            ast::Stmt::ClassDef(cls_def) => {
                let name = Name::from(cls_def.name.as_str());
                if global_scope.names.contains_key(&name)
                    && (!global_scope.dynamic_names.contains(&name)
                        || global_scope.delete_only_definitions.contains(&name))
                {
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
                    lexical: None,
                    has_class_cell: false,
                    name: name.clone(),
                    base: None,
                    fields: Vec::new(),
                    has_annotations: false,
                    declared_fields: Vec::new(),
                    enum_members: None,
                    body: Vec::new(),
                    frozen: false,
                    is_dataclass: false,
                    runtime_dataclass: false,
                    is_exception: false,
                    range: cls_def.range(),
                    decorators: Vec::new(),
                    decorated_binding: global_scope
                        .dynamic_names
                        .contains(&name)
                        .then(|| module_var_ids.get(&name).copied())
                        .flatten(),
                });
                if crate::decorators::has_runtime_class_decorators(
                    cls_def,
                    &global_scope.dataclass_markers,
                ) {
                    global_scope.dynamic_names.insert(name.clone());
                }
                global_scope.names.insert(name, Resolution::Class(cls_id));
                definition_classes.insert(cls_def.range, cls_id);
                top_classes.push(cls_id);

                let mut methods = Vec::new();
                for fn_def in crate::class_body::methods(&cls_def.body) {
                    let method_name = Name::from(fn_def.name.as_str());
                    let func_id = functions.alloc(crate::hir::Function {
                        definition: None,
                        name: method_name.clone(),
                        kind: crate::hir::FunctionKind::User,
                        params: Vec::new(),
                        ret: None,
                        body: Body::default(),
                        range: fn_def.range(),
                        closure: Default::default(),
                        is_generator: false,
                        docstring: None,
                        annotation_values: Vec::new(),
                    });
                    methods.push(crate::class::MethodSlot {
                        name: method_name,
                        func: func_id,
                        range: fn_def.range(),
                    });
                    user_funcs.push((fn_def, func_id));
                }
                raw_classes.push((cls_def, cls_id, methods));
            }
            ast::Stmt::FunctionDef(fn_def) => {
                let name = Name::from(fn_def.name.as_str());
                if global_scope.names.contains_key(&name)
                    && (!global_scope.dynamic_names.contains(&name)
                        || global_scope.delete_only_definitions.contains(&name))
                {
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
                    definition: None,
                    name: name.clone(),
                    kind: FunctionKind::User,
                    params: Vec::new(),
                    ret: None,
                    body: Body::default(),
                    range: fn_def.range(),
                    closure: Default::default(),
                    is_generator: false,
                    docstring: None,
                    annotation_values: Vec::new(),
                });
                if !fn_def.decorator_list.is_empty() {
                    global_scope.dynamic_names.insert(name.clone());
                }
                global_scope.names.insert(name, Resolution::Func(func_id));
                definition_functions.insert(fn_def.range, func_id);
                top_functions.push(func_id);
                user_funcs.push((fn_def, func_id));
            }
            _ => {}
        }
    }

    // 分配 main 入口函数
    let main_id = functions.alloc(Function {
        definition: None,
        name: "main".into(),
        kind: FunctionKind::ModuleMain,
        params: Vec::new(),
        ret: None,
        body: Body::default(),
        range: parsed.module.range,
        closure: Default::default(),
        is_generator: false,
        docstring: None,
        annotation_values: Vec::new(),
    });

    // M3.0：闭包作用域分析——发现嵌套 def / lambda 并计算 free / cell 变量。
    // 必须在降级任何函数体之前完成：名字解析（Free / cell）和 lambda 表达式
    // 都依赖它的登记表；新类模板同时明确每个表达式arena的真实owner。
    let analysis = crate::lexical_class_registry::analyze(
        parsed,
        &mut functions,
        &mut classes,
        main_id,
        info.instance_globals.then_some(&definition_classes),
        &line_starts,
        &mut diagnostics,
    );
    let mut nested_defaults = crate::definition_metadata::DefinitionMetadata::default();

    let mut main_body = Body::default();

    let mut class_scopes = FxHashMap::default();

    // 解析所有类：基类必须已经解析过，因此这里保持源码顺序。
    // 类体属性表达式降级到 main_body，由 MIR 在 `class` 语句位置求值。
    for (cls_def, cls_id, methods) in raw_classes {
        if let Some(mut parsed_cls) = crate::class::parse_class_def(
            cls_def,
            parsed.file,
            &global_scope,
            &classes,
            &methods,
            &module_var_ids,
            &analysis.nested_funcs,
            &mut nested_defaults,
            &mut class_scopes,
            &mut main_body,
            &mut diagnostics,
        ) {
            parsed_cls.has_class_cell = analysis
                .infos
                .values()
                .any(|info| info.class_cell == Some(cls_id));
            classes[cls_id] = parsed_cls;
        }
    }

    // 第二遍：降级各个用户函数体（含类方法与嵌套函数 / lambda）。
    // 顶层函数先降级、嵌套函数按发现顺序降级——保证父作用域的嵌套 def 语句
    // （在父函数体降级时触发默认值求值）先于子函数体处理。
    let mut all_default_exprs = Vec::new();
    let mut lower_one = |func_id: crate::hir::FuncId,
                         source: FnSource<'_>,
                         functions: &mut Arena<Function>,
                         diagnostics: &mut Vec<Diagnostic>| {
        lower_function_body(LowerArgs {
            file: parsed.file,
            package: &info.package,
            source,
            func_id,
            class_names: class_scopes.get(&func_id),
            functions,
            global_scope: &global_scope,
            module_vars: &module_var_ids,
            vars: &mut vars,
            classes: &classes,
            analysis: &analysis,
            nested_defaults: &mut nested_defaults,
            all_default_exprs: &mut all_default_exprs,
            main_body: &mut main_body,
            line_starts: &line_starts,
            diagnostics,
        });
    };
    for (fn_def, func_id) in &user_funcs {
        lower_one(
            *func_id,
            FnSource::Def(fn_def),
            &mut functions,
            &mut diagnostics,
        );
    }
    for (func_id, range) in &analysis.nested_order {
        let Some(node) = crate::closures_find::find_nested_ast(&parsed.module.body, *range) else {
            diagnostics.push(
                Diagnostic::error(Span::new(parsed.file, *range), "内部错误：嵌套函数节点丢失")
                    .with_code("E0101")
                    .with_primary_label("闭包分析登记的 AST 位置在降级时找不到"),
            );
            continue;
        };
        match node {
            crate::closures_find::NestedAst::Comprehension(comp) => {
                lower_one(
                    *func_id,
                    FnSource::Comprehension(comp),
                    &mut functions,
                    &mut diagnostics,
                );
            }
            crate::closures_find::NestedAst::Def(fn_def) => {
                lower_one(
                    *func_id,
                    FnSource::Def(fn_def),
                    &mut functions,
                    &mut diagnostics,
                );
            }
            crate::closures_find::NestedAst::Lambda(lambda) => {
                lower_one(
                    *func_id,
                    FnSource::Lambda(lambda),
                    &mut functions,
                    &mut diagnostics,
                );
            }
        }
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
        forbid_lexical_class: false,
        direct_stmt_index: 0,
        package: &info.package,
        classes: &classes,
        nested_funcs: &analysis.nested_funcs,
        nested_defaults: &mut nested_defaults,
        body: &mut main_body,
        loop_depth: 0,
        handler_depth: 0,
        line_starts: &line_starts,
    };
    // `__name__` / `__package__` / `__file__` 在模块代码执行前写入。
    main_lower_ctx.emit_special_module_vars(info, parsed.module.range);
    if global_scope.postponed_annotations
        && crate::annotation_stmts::has_module_annotations(&parsed.module.body)
    {
        main_lower_ctx.setup_annotations(parsed.module.range);
    }
    for (index, stmt) in parsed.module.body.iter().enumerate() {
        if index == 0 && main_lower_ctx.lower_instance_docstring(stmt) {
            continue;
        }
        match stmt {
            // 静态def只初始化默认值；公开动态def必须在定义成功后才发布新函数对象。
            ast::Stmt::FunctionDef(fn_def) => {
                let name = Name::from(fn_def.name.as_str());
                if let Some(func_id) = definition_functions.get(&fn_def.range) {
                    if !global_scope.dynamic_names.contains(&name) {
                        main_lower_ctx.lower_function_defaults(*func_id, fn_def.range());
                    } else if let Some(&var) = module_var_ids.get(&name) {
                        let decorators = fn_def
                            .decorator_list
                            .iter()
                            .map(|decorator| main_lower_ctx.lower_expr(&decorator.expression))
                            .collect();
                        let stmt = main_lower_ctx.body.stmts.alloc(Stmt {
                            kind: StmtKind::DecoratedDef {
                                func: *func_id,
                                target: crate::hir::AssignTarget::ModuleVar(var),
                                decorators,
                            },
                            range: fn_def.range(),
                            line: main_lower_ctx.line_of(fn_def.range.start().to_usize()),
                        });
                        main_lower_ctx.body.root.push(stmt);
                    }
                }
            }
            ast::Stmt::ClassDef(cls_def) => {
                if let Some(cls_id) = definition_classes.get(&cls_def.range) {
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
    functions[main_id].closure = analysis.infos[&main_id].clone();

    // 回填嵌套函数（def / lambda）的默认值：它们在外层作用域的定义位置求值
    //（见 lower_nested_def 与 lower_lambda）。main 降级也会产生新的登记
    //（模块顶层的 lambda），因此回填必须在 main 降级之后。
    for (func_id, defaults) in &nested_defaults.defaults {
        for (index, expr) in defaults.iter().enumerate() {
            if let Some(expr) = expr {
                functions[*func_id].params[index].default = Some(*expr);
            }
        }
    }

    crate::lexical_class_lower::apply(&mut classes, &mut nested_defaults);
    for (func_id, annotations) in nested_defaults.annotations {
        functions[func_id].annotation_values = annotations;
    }

    let mut module = Module {
        protected_exports: crate::exception_imports::protected_names(
            &global_scope.exception_imports,
        ),
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

    crate::unresolved_globals::resolve(&mut module);
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
    /// 当前函数直属语句索引；嵌套suite不更新，用于局部导入的结构化支配证明。
    pub(crate) direct_stmt_index: usize,
    /// 是否位于模块顶层；函数导入仍在原语句位置执行。
    pub(crate) module_scope: bool,
    pub(crate) forbid_lexical_class: bool,
    /// 当前模块的 `__package__`，用于把相对导入换算成绝对模块名。
    pub(crate) package: &'a str,
    /// 已解析的类（`except` 需要判断用户类是否属于异常家族）。
    pub(crate) classes: &'a Arena<crate::hir::Class>,
    /// 嵌套 def / lambda 的 AST 位置 → FuncId（M3.0，见 closures.rs）。
    pub(crate) nested_funcs: &'a FxHashMap<ryupy_base::TextRange, crate::hir::FuncId>,
    /// 嵌套函数的默认值：在**外层作用域** def 位置求值，先暂存、循环结束后
    /// 回填进对应函数的 `Param::default`。
    pub(crate) nested_defaults: &'a mut crate::definition_metadata::DefinitionMetadata,
    pub(crate) body: &'a mut Body,
    pub(crate) loop_depth: usize,
    /// 当前所处的 `except` 体层数。只有 > 0 时裸 `raise` 才有意义。
    pub(crate) handler_depth: usize,
    /// 每一行的起始字节偏移，用于把 `TextRange` 换算成行号。
    pub(crate) line_starts: &'a [usize],
}

/// 把参数默认值表达式降级到模块入口函数的 body。
///
/// 默认值在模块作用域求值，因此这里用空函数作用域 + 模块变量表解析名字；
/// 默认值里若还有 lambda（带参数默认值），同样登记进 `nested_defaults`。
#[allow(clippy::too_many_arguments)]
pub(crate) fn lower_default_expr(
    class_names: Option<&crate::class_scope::ClassScope>,
    file: FileId,
    global_scope: &GlobalScope,
    module_vars: &FxHashMap<Name, ModuleVarId>,
    nested_funcs: &FxHashMap<ryupy_base::TextRange, crate::hir::FuncId>,
    nested_defaults: &mut crate::definition_metadata::DefinitionMetadata,
    body: &mut Body,
    diagnostics: &mut Vec<Diagnostic>,
    expr: &ast::Expr,
) -> ExprId {
    let scope = FunctionScope::default();
    let mut ctx = ExprLowerCtx {
        class_names,
        allow_yield: false,
        file,
        global_scope,
        func_scope: &scope,
        module_vars,
        nested_funcs,
        nested_defaults,
        body,
        diagnostics,
    };
    ctx.lower_expr(expr)
}

impl StmtLowerCtx<'_> {
    /// 语句降级复用：把下标位置（可能是切片）降级成表达式。
    pub(crate) fn lower_subscript_index(&mut self, slice: &ast::Expr) -> ExprId {
        let mut expr_ctx = ExprLowerCtx {
            class_names: None,
            allow_yield: !self.module_scope,
            file: self.file,
            global_scope: self.global_scope,
            func_scope: self.func_scope,
            module_vars: self.module_vars,
            nested_funcs: self.nested_funcs,
            nested_defaults: self.nested_defaults,
            body: self.body,
            diagnostics: self.diagnostics,
        };
        expr_ctx.lower_subscript_index(slice)
    }

    pub(crate) fn lower_expr(&mut self, expr: &ast::Expr) -> ExprId {
        let mut expr_ctx = ExprLowerCtx {
            class_names: None,
            allow_yield: !self.module_scope,
            file: self.file,
            global_scope: self.global_scope,
            func_scope: self.func_scope,
            module_vars: self.module_vars,
            nested_funcs: self.nested_funcs,
            nested_defaults: self.nested_defaults,
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
}
