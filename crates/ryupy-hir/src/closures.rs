//! 闭包作用域分析（M3.0）。
//!
//! 在 lowering 之前对整棵 AST 做一遍 Python 作用域分析，回答三个问题：
//!
//! 1. **嵌套函数发现**：函数体（或模块顶层）里出现的 `def` / lambda，为它们分配
//!    [`FuncId`] 并登记 `AST 位置 → FuncId`，供 lowering 与 lambda 表达式引用。
//! 2. **free 变量**：嵌套函数读取（或 `nonlocal` 写入）的名字属于哪一层外层函数。
//!    名字沿作用域链向上找到「最近的把它当局部变量绑定」的函数——沿途每一层都把
//!    该名字记进自己的 `free` 列表，最外层函数把它记进 `cells`（装箱成 cell）。
//! 3. **cell 变量**：被嵌套函数捕获的局部变量需要堆上共享（同一个 `object[1]`），
//!    读写都要经过 cell 而不是普通槽位。
//!
//! 模块作用域不需要 cell：模块变量是静态槽位，任何函数都能直接访问，因此主作用域
//! 只负责发现嵌套 def / lambda，不参与 free / cell 计算。
//!
//! 与 CPython 的对应关系：
//! - 赋值即局部（含 `def` 名、`for` 目标、`with as`）；
//! - `nonlocal` 必须能在某个外层**函数**作用域里找到绑定，否则报诊断
//!   （CPython 是 SyntaxError，这里按「继续编译 + 明确报错」处理）；
//! - late binding：cell 是共享对象，闭包捕获的是 cell 而不是值快照。

use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Diagnostic, Span, TextRange};
use ryupy_syntax::ast;

use crate::hir::{ClassId, ClosureInfo, FuncId, Function, Name};
use la_arena::Arena;

// 复用 re-export 的 AST 类型名，保持与其它模块一致的写法。
use ast::Stmt;

/// 一个作用域在分析期间的中间数据。
pub(crate) struct ScopeData {
    pub(crate) postponed_annotations: bool,
    pub(crate) func_id: FuncId,
    /// 作用域种类：函数 / lambda 为 `false`，模块顶层为 `true`。
    pub(crate) is_module: bool,
    /// 特殊类定义作用域不对应可调用函数，只提供隐式 __class__ 绑定。
    pub(crate) class: Option<ClassId>,
    /// 本函数实际捕获的隐式类 cell 来源；普通局部同名绑定仍是 None。
    pub(crate) class_cell: Option<ClassId>,
    /// def 语句 / lambda 表达式的源码范围。
    pub(crate) range: TextRange,
    pub(crate) parent: Option<usize>,
    /// 本作用域绑定的名字（参数、赋值目标、def 名、`with as` 等），按发现顺序。
    pub(crate) assigned: Vec<Name>,
    pub(crate) globals: FxHashSet<Name>,
    /// `nonlocal` 声明：名字与声明位置（诊断用）。
    pub(crate) nonlocals: Vec<(Name, TextRange)>,
    /// `Load` 上下文读取的名字，按发现顺序。
    pub(crate) reads: Vec<(Name, TextRange)>,
    /// 直接嵌套的作用域：def 语句或 lambda 表达式的 AST 位置。
    pub(crate) nested: Vec<TextRange>,
    /// 分析结果：free 列表（env 下标顺序）。
    pub(crate) free: Vec<Name>,
    /// 分析结果：被捕获的局部名（cell）。
    pub(crate) cells: Vec<Name>,
}

impl ScopeData {
    pub(crate) fn assigned_contains(&self, name: &Name) -> bool {
        self.assigned.iter().any(|n| n == name)
    }

    pub(crate) fn free_contains(&self, name: &Name) -> bool {
        self.free.iter().any(|n| n == name)
    }

    pub(crate) fn cells_contains(&self, name: &Name) -> bool {
        self.cells.iter().any(|n| n == name)
    }
}

/// 分析产物：嵌套函数登记表 + 每个函数的闭包信息。
pub(crate) struct ClosureAnalysis {
    /// def 语句 / lambda 表达式的 AST 位置 → 分配的 [`FuncId`]。
    pub nested_funcs: FxHashMap<TextRange, FuncId>,
    /// 每个函数（含顶层与嵌套）的闭包信息。
    pub infos: FxHashMap<FuncId, ClosureInfo>,
    /// 嵌套 def 的绑定名：AST 位置 → 名字。
    ///
    /// 降级阶段靠 `resolve_binding` 解析绑定目标，本表主要供调试与后续
    /// 竖切（M3.1 推导式作用域）复用。
    #[allow(dead_code)]
    pub nested_def_names: FxHashMap<TextRange, Name>,
    /// 嵌套函数的发现顺序（FuncId 分配顺序一致）。HashMap 遍历无序，
    /// lowering 按这个顺序处理嵌套函数体，保证诊断与快照确定。
    pub nested_order: Vec<(FuncId, TextRange)>,
}

/// 对整个模块做闭包作用域分析。
///
/// 必须在「第一遍收集顶层 def / class」之后调用：`functions` 里已经有顶层函数与
/// `main` 的占位条目，嵌套函数的 [`FuncId`] 在这里追加分配。
pub(crate) fn analyze_closures(
    parsed: &ryupy_syntax::ParsedModule,
    functions: &mut Arena<Function>,
    main_id: FuncId,
    instance_classes: Option<&FxHashMap<TextRange, ClassId>>,
    diagnostics: &mut Vec<Diagnostic>,
) -> ClosureAnalysis {
    let mut scopes: Vec<ScopeData> = Vec::new();
    let mut nested_funcs: FxHashMap<TextRange, FuncId> = FxHashMap::default();
    let mut nested_def_names: FxHashMap<TextRange, Name> = FxHashMap::default();
    // 第一遍之后分配的 FuncId 都是嵌套函数；顶层 def / 方法不是。
    let first_nested = functions.len() as u32;

    // 模块顶层是根作用域：只发现嵌套 def / lambda（含顶层 def 参数默认值里的 lambda），
    // 不收集 assigned / reads（模块变量由 module_vars 单独处理）。
    let main_scope = scopes.len();
    scopes.push(ScopeData {
        postponed_annotations: crate::future_imports::annotations(&parsed.module.body),
        func_id: main_id,
        is_module: true,
        class: None,
        class_cell: None,
        range: parsed.module.range,
        parent: None,
        assigned: Vec::new(),
        globals: FxHashSet::default(),
        nonlocals: Vec::new(),
        reads: Vec::new(),
        nested: Vec::new(),
        free: Vec::new(),
        cells: Vec::new(),
    });
    collect_module_nested(
        &parsed.module.body,
        main_scope,
        instance_classes.is_some(),
        functions,
        &mut scopes,
        &mut nested_funcs,
        &mut nested_def_names,
    );

    // 每个顶层 def / 方法是独立的函数作用域（父作用域 = 模块）。
    for stmt in crate::module_definitions::definitions(&parsed.module.body) {
        match stmt {
            Stmt::FunctionDef(fn_def) => {
                let func_id = top_level_func_by_range(functions, fn_def.range);
                walk_function_scope(
                    &fn_def.body,
                    &fn_def.parameters,
                    func_id,
                    fn_def.range,
                    main_scope,
                    functions,
                    &mut scopes,
                    &mut nested_funcs,
                    &mut nested_def_names,
                );
            }
            Stmt::ClassDef(cls_def) => {
                let parent = instance_classes
                    .and_then(|classes| classes.get(&cls_def.range))
                    .map_or(main_scope, |class| {
                        crate::closures_walk::walk_class_scope(
                            cls_def,
                            *class,
                            main_scope,
                            functions,
                            &mut scopes,
                            &mut nested_funcs,
                            &mut nested_def_names,
                        )
                    });
                for fn_def in crate::class_body::methods(&cls_def.body) {
                    let func_id = top_level_func_by_range(functions, fn_def.range);
                    walk_function_scope(
                        &fn_def.body,
                        &fn_def.parameters,
                        func_id,
                        fn_def.range,
                        parent,
                        functions,
                        &mut scopes,
                        &mut nested_funcs,
                        &mut nested_def_names,
                    );
                }
            }
            _ => {}
        }
    }

    // CP 的 super 名字读取隐式请求 __class__，即使 super 被参数/local/global 遮蔽。
    // 仍走下面的普通词法解析：__class__ 自身的遮蔽不能被这份隐式需求覆盖。
    if instance_classes.is_some() {
        for scope in &mut scopes {
            if !scope.is_module
                && scope.class.is_none()
                && let Some((_, range)) = scope
                    .reads
                    .iter()
                    .find(|(name, _)| name.as_str() == "super")
            {
                scope.reads.push((Name::from("__class__"), *range));
            }
        }
    }

    // 每次解析沿完整父链传播 free，中间层无需再迭代。global 声明截断查找；
    // nonlocal 已从 assigned 移除，因此可穿过只负责转发 cell 的中间层。
    for s in 1..scopes.len() {
        let names: Vec<_> = scopes[s]
            .reads
            .iter()
            .chain(&scopes[s].nonlocals)
            .cloned()
            .collect();
        let mut seen = FxHashSet::default();
        for (name, _) in names {
            if !seen.insert(name.clone())
                || scopes[s].globals.contains(&name)
                || scopes[s].assigned_contains(&name)
                || scopes[s].free_contains(&name)
            {
                continue;
            }
            let mut cur = scopes[s].parent;
            let mut chain = vec![s];
            let mut found = None;
            while let Some(a) = cur {
                if scopes[a].is_module || scopes[a].globals.contains(&name) {
                    break;
                }
                if scopes[a].assigned_contains(&name) {
                    found = Some(a);
                    break;
                }
                chain.push(a);
                cur = scopes[a].parent;
            }
            if let Some(ancestor) = found {
                if !scopes[ancestor].cells_contains(&name) {
                    scopes[ancestor].cells.push(name.clone());
                }
                let class_cell = scopes[ancestor].class;
                for t in chain {
                    if class_cell.is_some() {
                        scopes[t].class_cell = class_cell;
                    }
                    if !scopes[t].free_contains(&name) {
                        scopes[t].free.push(name.clone());
                    }
                }
            } else if scopes[s].nonlocals.iter().any(|(n, _)| n == &name) {
                diagnostics.push(
                    Diagnostic::error(
                        Span::new(parsed.file, nonlocal_range(&scopes, s, &name)),
                        format!("没有找到 `{name}` 可供 nonlocal 绑定的外层函数变量"),
                    )
                    .with_code("E0103")
                    .with_primary_label("nonlocal 只能指向外层函数的局部变量"),
                );
            }
        }
    }

    // ── 产出每个函数的 ClosureInfo ────────────────────────────────────────
    let mut infos: FxHashMap<FuncId, ClosureInfo> = FxHashMap::default();
    for scope in scopes.iter().filter(|scope| scope.class.is_none()) {
        infos.insert(
            scope.func_id,
            ClosureInfo {
                class_cell: scope.class_cell,
                free: scope.free.clone(),
                cells: scope.cells.clone(),
                has_nested: !scope.nested.is_empty(),
            },
        );
    }

    ClosureAnalysis {
        nested_funcs,
        infos,
        nested_def_names,
        // scopes 按发现顺序排列；只取分析期间新分配的（嵌套）作用域，
        // 顶层 def / 方法在第一遍已登记，由 lower_module 按 user_funcs 降级。
        nested_order: scopes
            .iter()
            .filter(|s| {
                !s.is_module && s.class.is_none() && s.func_id.into_raw().into_u32() >= first_nested
            })
            .map(|s| (s.func_id, s.range))
            .collect(),
    }
}

/// 顶层 def / 方法在第一遍已经分配过 FuncId：按 AST 位置找回
///（不能按名字——不同类里的同名方法会撞车）。
pub(crate) fn top_level_func_by_range(functions: &Arena<Function>, range: TextRange) -> FuncId {
    functions
        .iter()
        .find(|(_, f)| f.range == range)
        .map(|(id, _)| id)
        .unwrap_or_else(|| panic!("顶层函数未在第一遍分配 FuncId（位置 {range:?}）"))
}

/// nonlocal 声明的诊断位置：取该名字在本作用域声明处的范围。
fn nonlocal_range(scopes: &[ScopeData], s: usize, name: &Name) -> TextRange {
    scopes[s]
        .nonlocals
        .iter()
        .find(|(n, _)| n == name)
        .map(|(_, r)| *r)
        .unwrap_or(TextRange::default())
}

/// 模块顶层：只收集嵌套 def / lambda（以及顶层 def / 方法参数默认值里的 lambda）。
///
/// 顶层的直接 `def` / `class` 语句已在第一遍登记，这里跳过它们的函数体，
/// 但**不**跳过参数默认值：默认值在模块作用域求值，里面的 lambda 属于模块作用域。
use crate::closures_walk::{collect_module_nested, walk_function_scope};
