//! HIR + 类型 → MIR。
//!
//! 核心是一个「当前基本块」游标的 builder：
//! - 表达式降级返回 `Operand`，复杂子表达式先存进临时局部变量；
//! - `if` / `while` / `for range` / 短路 `and` `or` 需要新建基本块并连接 terminator；
//! - `break` / `continue` 需要一个「循环目标栈」记录 (continue_bb, break_bb)；
//! - `return` 之后的代码落在一个新的不可达块里，最后由清理 Pass 删除；
//! - 函数末尾没有显式 `return` 时补 `Return(None)`。
//!
//! Python 语义改写在这里完成：
//! - `a // b`、`a % b`、`a ** b` → `Callee::Runtime(..)`；
//! - `int` 与 `float` 混合 → 先插入 `Cast { I64ToF64 }`；
//! - `print(a, b)` → 按各参数类型依次 `WriteI64` / `WriteF64` / ...，中间 `WriteSep`，末尾 `WriteNewline`；
//! - `str + str` → `StrConcat`；
//! - `for i in range(a, b, s)` → 计数循环（注意 `s` 为负数时比较方向相反，`s == 0` 要抛 `ValueError`）。
//!
//! 构建流程：
//! 1. 预分配所有函数的 MIR [`FnId`]，支持函数间相互调用与前向引用；
//! 2. 逐函数构建基本块、局部变量槽与语句序列；
//! 3. Python 语义改写（print 展开为运行时 Write 调用）；
//! 4. 确定赋值分析：若局部变量在赋值前被读取，作为编译器错误报出（语义陷阱 #10）。

use la_arena::Arena;
use rustc_hash::{FxHashMap, FxHashSet};
use ryupy_base::{Diagnostic, Span};
use ryupy_hir::{
    AssignTarget, Binding, Builtin, ExprId, ExprKind, LocalId as HirLocalId, Module, ModuleVarId,
    StmtId, StmtKind,
};
use ryupy_ty::{ModuleTypes, Ty, type_ref_to_ty};

use crate::expr_builder::ExprBuilder;
use crate::mir::{
    BasicBlock, BlockId, Callee, Constant, FnId, Function, GlobalDecl, LocalDecl,
    LocalId as MirLocalId, MirType, Operand, ParamKind, ParamMeta, Program, Region, RuntimeFn,
    Rvalue, Statement, Terminator,
};

/// 编译一个模块，返回（模块程序，模块导出表，诊断）。
///
/// 导出表是「Python 可见名字 → 静态槽位」，codegen 用它生成模块对象的属性读写。
pub fn build_program(
    hir: &Module,
    types: &ModuleTypes,
) -> (
    Program,
    Vec<(String, crate::mir::GlobalId)>,
    Vec<Diagnostic>,
) {
    let mut functions = Arena::default();
    let mut classes = Arena::default();
    let mut globals = Arena::default();
    let mut diagnostics = Vec::new();

    // 0. 构建 MIR 类定义。
    //
    // 普通 class 不生成 CLR 类型：它由运行时 `PyClass` / `PyInstance` 承载。
    // dataclass 仍保留静态字段布局，但 ADR-0009 之后统一使用引用类型以保留身份。
    let mut class_map: FxHashMap<ryupy_hir::ClassId, crate::mir::ClassDefId> = FxHashMap::default();
    let mut hir_class_globals: FxHashMap<ryupy_hir::ClassId, crate::mir::GlobalId> =
        FxHashMap::default();
    for (hir_cls_id, cls) in hir.classes.iter() {
        let fields = cls
            .fields
            .iter()
            .map(|f| {
                let ty = type_ref_to_ty(&f.annotation);
                (f.name.as_str().to_string(), ty_to_mir(&ty))
            })
            .collect();
        let factory_global = cls.is_exception.then(|| {
            globals.alloc(GlobalDecl {
                name: format!("__exc_factory_{}", hir_cls_id.into_raw().into_u32()),
                ty: MirType::Dynamic,
            })
        });
        let mir_id = classes.alloc(crate::mir::ClassDef {
            name: cls.name.as_str().to_string(),
            codegen_name: cls.name.as_str().to_string(),
            fields,
            is_value_type: false,
            is_dataclass: cls.is_dataclass,
            base: None,
            builtin_base: match cls.base {
                Some(ryupy_hir::ClassBase::Builtin(name)) => Some(name.to_string()),
                _ => None,
            },
            is_exception: cls.is_exception,
            factory_global,
        });
        let class_global = globals.alloc(GlobalDecl {
            name: format!("__class_{}", hir_cls_id.into_raw().into_u32()),
            ty: MirType::Dynamic,
        });
        class_map.insert(hir_cls_id, mir_id);
        hir_class_globals.insert(hir_cls_id, class_global);
    }
    // 基类映射放在第二遍：此时所有 ClassDefId 都已分配。
    for (hir_cls_id, cls) in hir.classes.iter() {
        if let Some(ryupy_hir::ClassBase::User(base_id)) = cls.base
            && let Some(&base_mir) = class_map.get(&base_id)
        {
            classes[class_map[&hir_cls_id]].base = Some(base_mir);
        }
    }

    // 异常类的工厂委托槽位（普通类没有）。
    let class_factories: FxHashMap<crate::mir::ClassDefId, crate::mir::GlobalId> = classes
        .iter()
        .filter_map(|(id, cls)| cls.factory_global.map(|global| (id, global)))
        .collect();

    // 1. 模块级变量：每个变量一个静态槽位，类型来自 ty 的提示。
    //    名字带序号，link 阶段还会加模块前缀，保证合并后不重名。
    let mut module_var_ids: FxHashMap<ModuleVarId, crate::mir::GlobalId> = FxHashMap::default();
    let mut module_var_tys: FxHashMap<ModuleVarId, MirType> = FxHashMap::default();
    for (var_id, var) in hir.vars.iter() {
        let hint = types
            .module_vars
            .get(var_id)
            .cloned()
            .unwrap_or(Ty::Dynamic);
        let ty = match ty_to_mir(&hint) {
            MirType::Unit => MirType::Dynamic,
            ty => ty,
        };
        let gid = globals.alloc(GlobalDecl {
            name: format!("__mv{}_{}", var_id.into_raw().into_u32(), var.name),
            ty: ty.clone(),
        });
        module_var_ids.insert(var_id, gid);
        module_var_tys.insert(var_id, ty);
    }

    // 2. 预先为每个 HIR 函数分配 MIR FnId，并登记函数对象 / 默认值数组 / 签名所需的
    //    模块静态槽位。槽位在函数体构建之前分配，调用方才能在表达式里引用它们。
    let mut fn_map: FxHashMap<ryupy_hir::FuncId, FnId> = FxHashMap::default();
    let mut global_seq = 0usize;
    for (hir_fn_id, hir_fn) in hir.functions.iter() {
        let ret_ty = ty_to_mir(&types.sigs[hir_fn_id].ret);
        let param_meta = build_param_meta(hir, types, hir_fn_id);

        let mut alloc_global = |globals: &mut Arena<GlobalDecl>, prefix: &str| {
            let id = globals.alloc(GlobalDecl {
                name: format!("{prefix}_{global_seq}"),
                ty: MirType::Dynamic,
            });
            global_seq += 1;
            id
        };

        let is_user = hir_fn.kind == ryupy_hir::FunctionKind::User;
        let function_global = is_user.then(|| alloc_global(&mut globals, "__fn"));
        // 每个用户函数都分配默认值数组：thunk 的绑定入口需要一个固定长度的数组，
        // 没有默认值的槽位留空即可（PySignature.HasDefault 为 false 时不会被读取）。
        let defaults_global = is_user.then(|| alloc_global(&mut globals, "__defaults"));
        let signature_global = is_user.then(|| alloc_global(&mut globals, "__sig"));

        let codegen_name = if hir_fn.kind == ryupy_hir::FunctionKind::ModuleMain {
            "__module_main".to_string()
        } else {
            hir_fn.name.as_str().to_string()
        };
        let mir_fn = Function {
            name: hir_fn.name.as_str().to_string(),
            codegen_name,
            source_file: hir.info.file.clone(),
            params: Vec::new(),
            ret: ret_ty,
            body: crate::mir::Body {
                locals: Arena::default(),
                blocks: Arena::default(),
                entry: la_arena::Idx::from_raw(0.into()),
                regions: Vec::new(),
            },
            param_meta,
            function_global,
            defaults_global,
            signature_global,
        };
        let mir_fn_id = functions.alloc(mir_fn);
        fn_map.insert(hir_fn_id, mir_fn_id);
    }

    let mut fn_globals: FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId> = FxHashMap::default();
    let mut fn_defaults: FxHashMap<ryupy_hir::FuncId, Option<crate::mir::GlobalId>> =
        FxHashMap::default();
    for (hir_fn_id, mir_fn_id) in fn_map.iter() {
        let function = &functions[*mir_fn_id];
        if let Some(global) = function.function_global {
            fn_globals.insert(*hir_fn_id, global);
        }
        fn_defaults.insert(*hir_fn_id, function.defaults_global);
    }

    // 2. 逐函数构建函数体
    for (hir_fn_id, hir_fn) in hir.functions.iter() {
        let mir_fn_id = fn_map[&hir_fn_id];
        let mut builder = FunctionBuilder::new(
            hir,
            types,
            &fn_map,
            &fn_globals,
            &fn_defaults,
            &class_map,
            &hir_class_globals,
            &class_factories,
            &module_var_ids,
            &globals,
            hir_fn_id,
        );

        // 建立 HIR 局部变量 → MIR 局部变量映射
        for (hir_loc_id, hir_loc) in hir_fn.body.locals.iter() {
            let hir_ty = types
                .body(hir_fn_id)
                .map(|b| b.local_ty(hir_loc_id))
                .unwrap_or(Ty::Dynamic);
            // 局部变量里的 `None` 用运行时的 None 单例（装箱 object）表示：
            // `Unit` 没有 CIL 表示、也无法分配槽位，若直接使用，赋值会把值留在求值栈上，
            // 生成的方法被 CLR 判为 invalid program；而 i64 占位值又会把 `None` 打印成 0。
            let ty = match ty_to_mir(&hir_ty) {
                MirType::Unit => MirType::Dynamic,
                ty => ty,
            };
            let mir_loc = builder.locals.alloc(LocalDecl {
                ty,
                name: Some(hir_loc.name.to_string()),
            });
            builder.local_map.insert(hir_loc_id, mir_loc);
        }

        // 收集参数 ID，并标记为已赋值
        let mut params = Vec::new();
        for p in &hir_fn.params {
            let mir_loc = builder.local_map[&p.local];
            params.push(mir_loc);
            builder.assigned_locals.insert(p.local);
        }

        // 模块入口开头把动态模块变量重置为 `None`：失败后重试等同于重新执行模块，
        // 不应该读到上一次失败留下的值（CPython 会丢弃旧模块对象）。
        if hir_fn.kind == ryupy_hir::FunctionKind::ModuleMain {
            for (var_id, _) in hir.vars.iter() {
                let Some(&gid) = module_var_ids.get(&var_id) else {
                    continue;
                };
                if module_var_tys[&var_id] != MirType::Dynamic {
                    continue;
                }
                builder.push_stmt(Statement::SetGlobal {
                    dest: gid,
                    value: Operand::Const(Constant::None),
                });
            }
        }

        // 帧记录：traceback 需要知道「当前在哪个函数、第几行」
        builder.emit_frame_push();

        // 构建语句
        for &stmt_id in &hir_fn.body.root {
            builder.build_stmt(stmt_id);
        }

        builder.finish_function();

        diagnostics.extend(builder.diagnostics);

        let entry = builder.entry;
        functions[mir_fn_id].params = params;
        functions[mir_fn_id].body = crate::mir::Body {
            locals: builder.locals,
            blocks: builder.blocks,
            entry,
            regions: std::mem::take(&mut builder.regions),
        };
    }

    let entry = fn_map[&hir.main];
    let program = Program {
        functions,
        classes,
        globals,
        entry,
        source_name: hir.source_name.clone(),
    };

    // 模块导出表：模块变量 + 顶层函数 + 顶层类。
    let mut exports: Vec<(String, crate::mir::GlobalId)> = Vec::new();
    for (var_id, var) in hir.vars.iter() {
        if let Some(&gid) = module_var_ids.get(&var_id) {
            exports.push((var.name.as_str().to_owned(), gid));
        }
    }
    for fn_id in &hir.top_functions {
        if let Some(&mir_id) = fn_map.get(fn_id)
            && let Some(gid) = program.functions[mir_id].function_global
        {
            exports.push((hir.functions[*fn_id].name.as_str().to_owned(), gid));
        }
    }
    for cls_id in &hir.top_classes {
        if let Some(&gid) = hir_class_globals.get(cls_id) {
            exports.push((hir.classes[*cls_id].name.as_str().to_owned(), gid));
        }
    }

    (program, exports, diagnostics)
}

/// `PyOps.ArgError` 的错误种类：未知关键字。
pub const ARG_ERROR_UNEXPECTED_KEYWORD: i64 = 3;

pub fn ty_to_mir(ty: &Ty) -> MirType {
    match ty {
        Ty::Int => MirType::I64,
        Ty::Float => MirType::F64,
        Ty::Bool => MirType::Bool,
        Ty::Str => MirType::Str,
        Ty::None => MirType::Unit,
        Ty::Class(id) => MirType::Class(la_arena::Idx::from_raw(id.into_raw())),
        Ty::Dynamic => MirType::Dynamic,
        // 函数对象与毒化类型都不该走到 codegen；用 Dynamic 兜底而不是 Unit，
        // 避免把「有值」错当成 void。
        Ty::Func(_) | Ty::Error => MirType::Dynamic,
    }
}

/// 从 HIR 参数与类型签名构造 MIR 参数元数据。
///
/// `*args` / `**kwargs` 的槽位在运行时是 tuple / dict，即使写了标注也不能当成普通值，
/// 因此强制为 `Dynamic`。
fn build_param_meta(
    hir: &Module,
    types: &ModuleTypes,
    hir_fn_id: ryupy_hir::FuncId,
) -> Vec<ParamMeta> {
    let hir_fn = &hir.functions[hir_fn_id];
    let sig = &types.sigs[hir_fn_id];
    hir_fn
        .params
        .iter()
        .enumerate()
        .map(|(index, param)| {
            let kind = match param.kind {
                ryupy_hir::ParamKind::PositionalOnly => ParamKind::PositionalOnly,
                ryupy_hir::ParamKind::PositionalOrKeyword => ParamKind::PositionalOrKeyword,
                ryupy_hir::ParamKind::VarPositional => ParamKind::VarPositional,
                ryupy_hir::ParamKind::KeywordOnly => ParamKind::KeywordOnly,
                ryupy_hir::ParamKind::VarKeyword => ParamKind::VarKeyword,
            };
            let ty = match kind {
                ParamKind::VarPositional | ParamKind::VarKeyword => MirType::Dynamic,
                _ => ty_to_mir(sig.params.get(index).unwrap_or(&Ty::Dynamic)),
            };
            ParamMeta {
                name: hir_fn.body.locals[param.local].name.to_string(),
                kind,
                has_default: param.default.is_some(),
                ty,
            }
        })
        .collect()
}

/// 单个函数的 MIR 构建器。
///
/// 字段对外开放到 crate 内，是为了把「循环」「异常区域」这些小节的构建逻辑拆到
/// 独立文件（`build_loops.rs` / `build_try.rs`），避免单文件过长。
pub(crate) struct FunctionBuilder<'a> {
    pub(crate) hir: &'a Module,
    pub(crate) types: &'a ModuleTypes,
    pub(crate) fn_map: &'a FxHashMap<ryupy_hir::FuncId, FnId>,
    /// 每个用户函数的函数对象静态槽位（调用一等函数值 / 运行时绑定用）。
    pub(crate) fn_globals: &'a FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId>,
    /// 每个函数默认值数组的静态槽位（没有默认值为 `None`）。
    pub(crate) fn_defaults: &'a FxHashMap<ryupy_hir::FuncId, Option<crate::mir::GlobalId>>,
    /// HIR 类 → MIR 类定义。
    pub(crate) class_map: &'a FxHashMap<ryupy_hir::ClassId, crate::mir::ClassDefId>,
    /// HIR 类 → 类对象静态槽位（表达式构建直接使用）。
    pub(crate) hir_class_globals: &'a FxHashMap<ryupy_hir::ClassId, crate::mir::GlobalId>,
    /// 异常类 → CLR 工厂委托的静态槽位。
    pub(crate) class_factories: &'a FxHashMap<crate::mir::ClassDefId, crate::mir::GlobalId>,
    /// HIR 模块变量 → 静态槽位。
    pub(crate) module_vars: &'a FxHashMap<ModuleVarId, crate::mir::GlobalId>,
    /// 全部静态槽位（读取槽位类型做装箱/拆箱）。
    pub(crate) globals: &'a Arena<GlobalDecl>,
    pub(crate) current_hir_fn: ryupy_hir::FuncId,
    pub(crate) locals: Arena<LocalDecl>,
    pub(crate) blocks: Arena<BasicBlock>,
    pub(crate) entry: BlockId,
    pub(crate) current_bb: BlockId,
    pub(crate) local_map: FxHashMap<HirLocalId, MirLocalId>,
    pub(crate) assigned_locals: FxHashSet<HirLocalId>,
    pub(crate) reported_unbound: FxHashSet<HirLocalId>,
    pub(crate) loop_stack: Vec<(BlockId, BlockId)>, // (continue_bb, break_bb)
    pub(crate) diagnostics: Vec<Diagnostic>,
    /// 已构建的异常区域。
    pub(crate) regions: Vec<Region>,
    /// 当前嵌套的受保护区域起点（块下标），最内层在末尾。
    pub(crate) protected_stack: Vec<usize>,
    /// 当前正在构建的 finally 上下文，最内层在末尾。
    pub(crate) finally_stack: Vec<FinallyCtx>,
    /// `finally` 状态机的当前动作码；只有函数里出现过 finally 时才分配。
    pub(crate) flow_kind: Option<MirLocalId>,
    /// 函数级动作表：动作码 → 跳出目标。0/1/2 是内建动作（正常/异常/返回）。
    pub(crate) actions: Vec<BlockId>,
    /// 统一返回块。
    pub(crate) epilogue: Option<BlockId>,
    /// 需要跳到统一返回块的块。
    pub(crate) epilogue_jumps: Vec<BlockId>,
    /// 本函数保存返回值的局部变量。
    pub(crate) retval: Option<MirLocalId>,
    /// 返回类型。
    pub(crate) ret_ty: MirType,
    /// traceback 帧深度。
    pub(crate) frame_depth: Option<MirLocalId>,
}

/// 一个正在构建的 `finally` 区域。
pub(crate) struct FinallyCtx {
    /// 需要在 finally 体执行完后接续的块。
    ///
    /// 存「待回填的源块」而不是「finally 入口块」，是因为入口块只能在区域窗口关闭之后
    /// 分配（窗口内必须是受保护的代码），而构建期间还不知道它。
    pub(crate) pending: Vec<BlockId>,
    /// 区域起始块下标，用于判断跳出目标是否离开了本区域。
    pub(crate) start: usize,
}

/// `finally` 分派块里的内建动作码。
pub(crate) const FLOW_NORMAL: i64 = 0;
pub(crate) const FLOW_EXCEPTION: i64 = 1;
pub(crate) const FLOW_RETURN: i64 = 2;
/// 自定义动作（break / continue / 跳出）从 3 开始。
pub(crate) const FLOW_FIRST_CUSTOM: i64 = 3;

impl<'a> FunctionBuilder<'a> {
    #[allow(clippy::too_many_arguments)]
    fn new(
        hir: &'a Module,
        types: &'a ModuleTypes,
        fn_map: &'a FxHashMap<ryupy_hir::FuncId, FnId>,
        fn_globals: &'a FxHashMap<ryupy_hir::FuncId, crate::mir::GlobalId>,
        fn_defaults: &'a FxHashMap<ryupy_hir::FuncId, Option<crate::mir::GlobalId>>,
        class_map: &'a FxHashMap<ryupy_hir::ClassId, crate::mir::ClassDefId>,
        hir_class_globals: &'a FxHashMap<ryupy_hir::ClassId, crate::mir::GlobalId>,
        class_factories: &'a FxHashMap<crate::mir::ClassDefId, crate::mir::GlobalId>,
        module_vars: &'a FxHashMap<ModuleVarId, crate::mir::GlobalId>,
        globals: &'a Arena<GlobalDecl>,
        current_hir_fn: ryupy_hir::FuncId,
    ) -> Self {
        let mut blocks = Arena::default();
        let entry = blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        });
        Self {
            hir,
            types,
            fn_map,
            fn_globals,
            fn_defaults,
            class_map,
            hir_class_globals,
            class_factories,
            module_vars,
            globals,
            current_hir_fn,
            locals: Arena::default(),
            blocks,
            entry,
            current_bb: entry,
            local_map: FxHashMap::default(),
            assigned_locals: FxHashSet::default(),
            reported_unbound: FxHashSet::default(),
            loop_stack: Vec::new(),
            diagnostics: Vec::new(),
            regions: Vec::new(),
            protected_stack: Vec::new(),
            finally_stack: Vec::new(),
            flow_kind: None,
            retval: None,
            actions: Vec::new(),
            epilogue: None,
            epilogue_jumps: Vec::new(),
            ret_ty: ty_to_mir(&types.sigs[current_hir_fn].ret),
            frame_depth: None,
        }
    }

    pub(crate) fn new_block(&mut self) -> BlockId {
        self.blocks.alloc(BasicBlock {
            stmts: Vec::new(),
            terminator: Terminator::Unreachable,
        })
    }

    pub(crate) fn push_stmt(&mut self, stmt: Statement) {
        self.blocks[self.current_bb].stmts.push(stmt);
    }

    pub(crate) fn is_current_unterminated(&self) -> bool {
        matches!(
            self.blocks[self.current_bb].terminator,
            Terminator::Unreachable
        )
    }

    pub(crate) fn terminate_current(&mut self, term: Terminator) {
        if self.is_current_unterminated() {
            self.blocks[self.current_bb].terminator = term;
        }
    }

    // ───────────────────── 帧记录 / 行号 / 统一返回 ─────────────────────

    /// 函数入口压入 traceback 栈帧，深度存进局部变量。
    ///
    /// 深度用于两个地方：正常返回时 `PopTo(depth)` 弹掉自己的帧；异常被兜住时
    /// 同样用它把被展开的调用者帧弹掉，避免栈错误地一直增长。
    pub(crate) fn emit_frame_push(&mut self) {
        // 模块顶层函数在 traceback 里显示为 `<module>`，与 CPython 一致
        let func = &self.hir.functions[self.current_hir_fn];
        let fn_name = if matches!(func.kind, ryupy_hir::FunctionKind::ModuleMain) {
            "<module>".to_string()
        } else {
            func.name.as_str().to_string()
        };
        let depth = self.alloc_temp(MirType::I64);
        self.push_stmt(Statement::Assign {
            dest: depth,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TracePush),
                args: vec![
                    Operand::Const(Constant::Str(self.hir.info.file.clone())),
                    Operand::Const(Constant::Str(fn_name)),
                ],
            },
        });
        self.frame_depth = Some(depth);
    }

    /// 把 traceback 栈弹回本函数的深度。
    ///
    /// 异常穿过调用链时，被穿过的函数不会执行自己的返回路径，帧会残留在栈里。
    /// 处理入口（catch / finally 通道）必须补一次弹栈，否则后续的递归深度判断会失真。
    pub(crate) fn emit_frame_restore(&mut self) {
        let Some(depth) = self.frame_depth else {
            return;
        };
        self.push_stmt(Statement::Eval(Rvalue::Call {
            callee: Callee::Runtime(RuntimeFn::TracePopTo),
            args: vec![Operand::Local(depth)],
        }));
    }

    /// 记录语句行号（只在有帧记录时有效，但总是发射，便于诊断调试产物）。
    fn emit_line(&mut self, line: u32) {
        self.push_stmt(Statement::Line(line));
    }

    /// 分配（或复用）`finally` 状态变量。
    pub(crate) fn flow_kind_local(&mut self) -> MirLocalId {
        if let Some(local) = self.flow_kind {
            return local;
        }
        let local = self.alloc_temp(MirType::I64);
        self.flow_kind = Some(local);
        local
    }

    /// 写状态码。
    pub(crate) fn set_flow(&mut self, code: i64) {
        let Some(flow) = self.flow_kind else { return };
        self.push_stmt(Statement::Assign {
            dest: flow,
            value: Rvalue::Use(Operand::Const(Constant::I64(code))),
        });
    }

    /// 把跳出目标登记进函数级动作表，返回状态码（相同目标复用同一个码）。
    pub(crate) fn intern_action(&mut self, target: BlockId) -> i64 {
        if let Some(idx) = self.actions.iter().position(|t| *t == target) {
            return FLOW_FIRST_CUSTOM + idx as i64;
        }
        self.actions.push(target);
        FLOW_FIRST_CUSTOM + (self.actions.len() - 1) as i64
    }

    /// 分配临时局部变量。
    pub(crate) fn alloc_temp(&mut self, ty: MirType) -> MirLocalId {
        self.locals.alloc(LocalDecl { ty, name: None })
    }

    /// 分配 `break` / `continue` / 普通跳出用的目标，并在需要时穿过 finally。
    pub(crate) fn route_jump(&mut self, target: BlockId) {
        if let Some(ctx) = self.finally_stack.last() {
            if self.block_index(target) < ctx.start {
                let code = self.intern_action(target);
                self.set_flow(code);
                let bb = self.current_bb;
                self.finally_stack
                    .last_mut()
                    .expect("刚检查过非空")
                    .pending
                    .push(bb);
                self.start_dead_block();
                return;
            }
        }
        self.terminate_current(Terminator::Goto(target));
    }

    /// 「目标待回填」之后必须换一个块继续构建。
    ///
    /// 为什么不能留原地：`build_stmt` 用「当前块是否已终结」判断上一条语句是否正常落下，
    /// 待回填的块此刻还没有终结符，后续语句会被错误地接到它后面。新块没有前驱，
    /// 会被 `simplify_cfg` 当成死代码清掉。
    fn start_dead_block(&mut self) {
        self.current_bb = self.new_block();
    }

    /// 把一批「目标待定」的块回填成 `Goto(target)`。
    pub(crate) fn resolve_jumps(&mut self, pending: Vec<BlockId>, target: BlockId) {
        for bb in pending {
            if matches!(self.blocks[bb].terminator, Terminator::Unreachable) {
                self.blocks[bb].terminator = Terminator::Goto(target);
            }
        }
    }

    /// 当前块「正常结束」：有 finally 时写 NORMAL 状态并交给它，否则记进 `pending`。
    pub(crate) fn route_normal(&mut self, pending: &mut Vec<BlockId>) {
        if self.finally_stack.is_empty() {
            pending.push(self.current_bb);
        } else {
            self.set_flow(FLOW_NORMAL);
            let bb = self.current_bb;
            self.finally_stack
                .last_mut()
                .expect("刚检查过非空")
                .pending
                .push(bb);
        }
    }

    /// `return`：写返回值到统一槽位，再走 finally 或直接去尾块。
    pub(crate) fn build_return(&mut self, value: Option<ExprId>) {
        let ret_ty = ty_to_mir(&self.types.sigs[self.current_hir_fn].ret);
        let retval = self.retval_local();
        if let Some(dest) = retval {
            let op = match value {
                Some(expr) => {
                    let value_ty = self.expr_builder().ty_of(expr);
                    let raw = self.expr_builder().build_expr(expr);
                    self.expr_builder().coerce(raw, &value_ty, &ret_ty)
                }
                None => Operand::Const(Constant::I64(0)),
            };
            self.push_stmt(Statement::Assign {
                dest,
                value: Rvalue::Use(op),
            });
        }
        if !self.finally_stack.is_empty() {
            self.set_flow(FLOW_RETURN);
            let bb = self.current_bb;
            self.finally_stack
                .last_mut()
                .expect("刚检查过非空")
                .pending
                .push(bb);
        } else {
            let bb = self.current_bb;
            self.epilogue_jumps.push(bb);
        }
        self.start_dead_block();
    }

    /// 在定义位置求值一个函数的参数默认值，写进它的默认值数组。
    ///
    /// 默认值数组由 codegen 的 `__init_functions` 预先分配，这里只填元素，
    /// 因此 `PyFunction` 持有的是同一份数组，多次调用共享可变默认值。
    pub(crate) fn emit_function_defaults(&mut self, fn_id: ryupy_hir::FuncId) {
        let Some(defaults_global) = self.fn_defaults.get(&fn_id).copied().flatten() else {
            return;
        };
        let defaults: Vec<(usize, ExprId)> = self.hir.functions[fn_id]
            .params
            .iter()
            .enumerate()
            .filter_map(|(index, param)| param.default.map(|expr| (index, expr)))
            .collect();
        for (index, expr) in defaults {
            let op = self.expr_builder().build_expr(expr);
            let ty = self.expr_builder().ty_of(expr);
            let value = self.expr_builder().coerce(op, &ty, &MirType::Dynamic);
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ArraySet),
                args: vec![
                    Operand::Global(defaults_global),
                    Operand::Const(Constant::I64(index as i64)),
                    value,
                ],
            }));
        }
    }

    /// 返回槽位；返回类型是 `None` 时不需要槽位。
    pub(crate) fn retval_local(&mut self) -> Option<MirLocalId> {
        if matches!(self.ret_ty, MirType::Unit) {
            return None;
        }
        if self.retval.is_none() {
            let local = self.alloc_temp(self.ret_ty.clone());
            self.retval = Some(local);
        }
        self.retval
    }

    /// 函数收尾：隐式返回 + 统一返回块（负责弹帧与 `ret`）。
    ///
    /// 所有 `return` 都汇聚到一个尾块，好处是「弹帧」只有一个出口，
    /// 不用担心某条返回路径漏掉 `PopTo`。
    pub(crate) fn finish_function(&mut self) {
        // 有返回值的函数必须有一个返回槽位，哪怕所有路径都只是 `raise`：
        // 尾块要读它，而尾块可能因为不可达被跳过，所以这里只负责「存在」。
        if !matches!(self.ret_ty, MirType::Unit) && self.retval.is_none() {
            let ty = self.ret_ty.clone();
            self.retval = Some(self.alloc_temp(ty));
        }
        if self.is_current_unterminated() {
            // 隐式 `return None`：非 Unit 函数要把默认值写进返回槽位
            if let Some(dest) = self.retval {
                let value = if matches!(self.ret_ty, MirType::Dynamic) {
                    Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::DynNone),
                        args: Vec::new(),
                    }
                } else {
                    Rvalue::Use(Operand::Const(Constant::I64(0)))
                };
                self.push_stmt(Statement::Assign { dest, value });
            }
            let current = self.current_bb;
            self.epilogue_jumps.push(current);
        }
        let epilogue = self.new_block();
        self.current_bb = epilogue;

        if let Some(depth) = self.frame_depth {
            self.push_stmt(Statement::Eval(Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::TracePopTo),
                args: vec![Operand::Local(depth)],
            }));
        }
        let ret_op = self.retval.map(Operand::Local);
        self.blocks[epilogue].terminator = Terminator::Return(ret_op);
        self.epilogue = Some(epilogue);

        for bb in std::mem::take(&mut self.epilogue_jumps) {
            if matches!(self.blocks[bb].terminator, Terminator::Unreachable) {
                self.blocks[bb].terminator = Terminator::Goto(epilogue);
            }
        }
    }

    /// 块下标。用于比较「目标块是否在当前区域之前分配」，也就是是否离开了区域。
    pub(crate) fn block_index(&self, bb: BlockId) -> usize {
        bb.into_raw().into_u32() as usize
    }

    /// 下一个将要分配的块下标（等于当前已分配块数）。
    pub(crate) fn block_index_of_next_block(&self) -> usize {
        self.blocks.len()
    }

    /// 当前块若未终结则返回它（表示「正常落下」）。
    pub(crate) fn if_unterminated_take(&mut self) -> Option<BlockId> {
        if self.is_current_unterminated() {
            Some(self.current_bb)
        } else {
            None
        }
    }

    pub(crate) fn expr_builder(&mut self) -> ExprBuilder<'_> {
        ExprBuilder {
            hir: self.hir,
            types: self.types,
            fn_map: self.fn_map,
            fn_globals: self.fn_globals,
            fn_defaults: self.fn_defaults,
            class_globals: self.hir_class_globals,
            module_vars: self.module_vars,
            globals: self.globals,
            current_hir_fn: self.current_hir_fn,
            locals: &mut self.locals,
            blocks: &mut self.blocks,
            current_bb: &mut self.current_bb,
            local_map: &self.local_map,
            assigned_locals: &mut self.assigned_locals,
            reported_unbound: &mut self.reported_unbound,
            diagnostics: &mut self.diagnostics,
        }
    }

    /// 把值写进绑定目标（局部变量或模块槽位），必要时补装箱/拆箱。
    pub(crate) fn store_binding(&mut self, binding: Binding, value: Operand, from: &MirType) {
        match binding {
            Binding::Local(local) => {
                let dest = self.local_map[&local];
                self.assigned_locals.insert(local);
                self.push_stmt(Statement::Assign {
                    dest,
                    value: Rvalue::Use(value),
                });
            }
            Binding::ModuleVar(var) => {
                let gid = self.module_vars[&var];
                let to = self.globals[gid].ty.clone();
                let value = self.coerce_mir(value, from, &to);
                self.push_stmt(Statement::SetGlobal { dest: gid, value });
            }
        }
    }

    /// 把一个操作数从 `from` 表示转换到 `to` 表示（Dynamic 与静态槽位之间）。
    fn coerce_mir(&mut self, value: Operand, from: &MirType, to: &MirType) -> Operand {
        if from == to {
            return value;
        }
        let dest = self.alloc_temp(to.clone());
        let rvalue = if *to == MirType::Dynamic {
            Rvalue::Box {
                value,
                from: from.clone(),
            }
        } else if *from == MirType::Dynamic {
            Rvalue::Unbox {
                value,
                to: to.clone(),
            }
        } else {
            // 两个不同的静态表示：先装箱再拆箱，约定由运行时转换（如 i64 → f64）。
            let boxed = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: boxed,
                value: Rvalue::Box {
                    value,
                    from: from.clone(),
                },
            });
            Rvalue::Unbox {
                value: Operand::Local(boxed),
                to: to.clone(),
            }
        };
        self.push_stmt(Statement::Assign {
            dest,
            value: rvalue,
        });
        Operand::Local(dest)
    }

    /// 调用运行时导入一个模块，返回动态值。
    fn import_value(&mut self, module: &str) -> Operand {
        let temp = self.alloc_temp(MirType::Dynamic);
        self.push_stmt(Statement::Assign {
            dest: temp,
            value: Rvalue::Call {
                callee: Callee::Runtime(RuntimeFn::ImportModule),
                args: vec![Operand::Const(Constant::Str(module.to_owned()))],
            },
        });
        Operand::Local(temp)
    }

    /// `import x` / `import a.b` / `import x as y`。
    fn build_import(&mut self, module: &str, bind: ModuleVarId, bind_root: bool) {
        let gid = self.module_vars[&bind];
        let value = if bind_root {
            // `import a.b`：先把 a.b 初始化，再把顶层包 a 绑定到命名空间。
            let _ = self.import_value(module);
            let root = module.split('.').next().unwrap_or(module);
            self.import_value(root)
        } else {
            self.import_value(module)
        };
        self.push_stmt(Statement::SetGlobal { dest: gid, value });
    }

    /// `from m import a, b as c`：逐个取属性，缺属性时运行时尝试导入子模块。
    fn build_import_from(&mut self, module: &str, names: &[(String, ModuleVarId)]) {
        for (name, bind) in names {
            let gid = self.module_vars[bind];
            let temp = self.alloc_temp(MirType::Dynamic);
            self.push_stmt(Statement::Assign {
                dest: temp,
                value: Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::ImportAttr),
                    args: vec![
                        Operand::Const(Constant::Str(module.to_owned())),
                        Operand::Const(Constant::Str(name.clone())),
                    ],
                },
            });
            self.push_stmt(Statement::SetGlobal {
                dest: gid,
                value: Operand::Local(temp),
            });
        }
    }

    pub(crate) fn build_stmt(&mut self, stmt_id: StmtId) {
        if !self.is_current_unterminated() {
            // 当前基本块已终结（例如前一条语句是 return/break/continue）。
            // 只有当存在后续不可达语句时，才分配死代码块承载它。
            self.current_bb = self.new_block();
        }
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let stmt = &hir_body.stmts[stmt_id];
        let line = stmt.line;
        self.emit_line(line);
        match &stmt.kind {
            StmtKind::Expr(expr_id) => {
                self.build_expr_stmt(*expr_id);
            }
            StmtKind::Import {
                module,
                bind,
                bind_root,
            } => {
                let module = module.clone();
                let bind = *bind;
                let bind_root = *bind_root;
                self.build_import(&module, bind, bind_root);
            }
            StmtKind::ImportFrom { module, names } => {
                let module = module.clone();
                let names = names.clone();
                self.build_import_from(&module, &names);
            }
            StmtKind::Assign { target, value } => match target {
                AssignTarget::Subscript { base, index } => {
                    self.build_subscript_assign(*base, *index, *value);
                }
                AssignTarget::Sequence(items) => {
                    let items = items.clone();
                    self.build_sequence_assign(&items, *value);
                    self.mark_sequence_targets(&items);
                }
                _ => {
                    let val_ty = self.expr_builder().ty_of(*value);
                    let val_op = self.expr_builder().build_expr(*value);
                    match target {
                        AssignTarget::Local(loc) => {
                            let dest = self.local_map[loc];
                            self.expr_builder().assign_value(dest, val_op, &val_ty);
                            self.assigned_locals.insert(*loc);
                        }
                        AssignTarget::ModuleVar(var) => {
                            let gid = self.module_vars[var];
                            let to = self.globals[gid].ty.clone();
                            let value = self.expr_builder().coerce(val_op, &val_ty, &to);
                            self.push_stmt(Statement::SetGlobal { dest: gid, value });
                        }
                        AssignTarget::Attribute { base, name } => {
                            let base_ty = self.expr_builder().ty_of(*base);
                            let base_op = self.expr_builder().build_expr(*base);
                            let base_op = if matches!(base_ty, Ty::Class(_)) {
                                self.expr_builder().as_local(base_op)
                            } else {
                                base_op
                            };
                            if let Ty::Class(cls_id) = base_ty {
                                let cls = &self.hir.classes[cls_id];
                                if let Some((field_idx, field)) =
                                    cls.fields.iter().enumerate().find(|(_, f)| f.name == *name)
                                {
                                    let field_ty = ty_to_mir(&type_ref_to_ty(&field.annotation));
                                    let value =
                                        self.expr_builder().coerce(val_op, &val_ty, &field_ty);
                                    self.push_stmt(Statement::SetField {
                                        obj: base_op,
                                        field: field_idx as u32,
                                        value,
                                    });
                                }
                            } else {
                                // 普通实例 / 类的动态属性写入（M2.5）：统一交给运行时，
                                // 它负责实例字典、property setter 与 AttributeError。
                                let value =
                                    self.expr_builder()
                                        .coerce(val_op, &val_ty, &MirType::Dynamic);
                                self.push_stmt(Statement::Eval(Rvalue::Call {
                                    callee: Callee::Runtime(RuntimeFn::SetAttr),
                                    args: vec![
                                        base_op,
                                        Operand::Const(Constant::Str(name.as_str().to_owned())),
                                        value,
                                    ],
                                }));
                            }
                        }
                        AssignTarget::Subscript { .. } | AssignTarget::Sequence(_) => {}
                    }
                }
            },
            StmtKind::Return(opt_expr) => {
                self.build_return(*opt_expr);
            }
            StmtKind::If { cond, then_, else_ } => {
                let cond_raw = self.expr_builder().build_expr(*cond);
                let cond_ty = self.expr_builder().ty_of(*cond);
                let cond_bool = self.expr_builder().to_bool_operand(cond_raw, &cond_ty);

                let then_bb = self.new_block();
                let else_bb = self.new_block();
                let join_bb = self.new_block();

                self.terminate_current(Terminator::Branch {
                    cond: cond_bool,
                    then_: then_bb,
                    else_: else_bb,
                });

                // then 分支
                self.current_bb = then_bb;
                for &s in then_ {
                    self.build_stmt(s);
                }
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(join_bb));
                }

                // else 分支
                self.current_bb = else_bb;
                for &s in else_ {
                    self.build_stmt(s);
                }
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(join_bb));
                }

                self.current_bb = join_bb;
            }
            StmtKind::While { cond, body, orelse } => {
                let cond_bb = self.new_block();
                let body_bb = self.new_block();
                let else_bb = self.new_block();
                let exit_bb = self.new_block();

                self.terminate_current(Terminator::Goto(cond_bb));

                // 循环条件块：条件变假 → else（正常结束）
                self.current_bb = cond_bb;
                let cond_raw = self.expr_builder().build_expr(*cond);
                let cond_ty = self.expr_builder().ty_of(*cond);
                let cond_bool = self.expr_builder().to_bool_operand(cond_raw, &cond_ty);
                self.terminate_current(Terminator::Branch {
                    cond: cond_bool,
                    then_: body_bb,
                    else_: else_bb,
                });

                // 循环体：break 直接跳 exit，跳过 else
                self.current_bb = body_bb;
                self.loop_stack.push((cond_bb, exit_bb));
                for &s in body {
                    self.build_stmt(s);
                }
                self.loop_stack.pop();
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(cond_bb));
                }

                // else 体
                self.current_bb = else_bb;
                for &s in orelse {
                    self.build_stmt(s);
                }
                if self.is_current_unterminated() {
                    self.terminate_current(Terminator::Goto(exit_bb));
                }
                self.current_bb = exit_bb;
            }
            StmtKind::Break => {
                let (_, break_bb) = *self
                    .loop_stack
                    .last()
                    .expect("break 语句在 HIR 降级阶段已做循环作用域检查");
                // 跳出可能要穿过 finally：由 route_jump 决定直接跳还是走状态机
                self.route_jump(break_bb);
            }
            StmtKind::Continue => {
                let (continue_bb, _) = *self
                    .loop_stack
                    .last()
                    .expect("continue 语句在 HIR 降级阶段已做循环作用域检查");
                self.route_jump(continue_bb);
            }
            StmtKind::FunctionDefaults(fn_id) => {
                self.emit_function_defaults(*fn_id);
            }
            StmtKind::ForRange {
                var,
                start,
                stop,
                step,
                body,
                orelse,
            } => {
                self.build_for_range(*var, *start, *stop, *step, body, orelse);
            }
            StmtKind::ForEach {
                var,
                iter,
                body,
                orelse,
            } => {
                self.build_for_each(*var, *iter, body, orelse);
            }
            StmtKind::Raise { value, cause } => {
                // 裸 raise 取运行时「当前异常」；显式 raise 先准备（挂上下文 + 记栈帧）。
                let value_op = match value {
                    Some(expr) => {
                        let ty = self.expr_builder().ty_of(*expr);
                        let raw = self.expr_builder().build_expr(*expr);
                        let boxed = self.expr_builder().coerce(raw, &ty, &MirType::Dynamic);
                        let dest = self.alloc_temp(MirType::Dynamic);
                        self.push_stmt(Statement::Assign {
                            dest,
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::RaisePrepare),
                                args: vec![boxed],
                            },
                        });
                        Operand::Local(dest)
                    }
                    None => {
                        let dest = self.alloc_temp(MirType::Dynamic);
                        self.push_stmt(Statement::Assign {
                            dest,
                            value: Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::Reraise),
                                args: Vec::new(),
                            },
                        });
                        Operand::Local(dest)
                    }
                };
                let cause_op = cause.map(|e| {
                    let ty = self.expr_builder().ty_of(e);
                    let raw = self.expr_builder().build_expr(e);
                    self.expr_builder().coerce(raw, &ty, &MirType::Dynamic)
                });
                self.terminate_current(Terminator::Raise {
                    value: Some(value_op),
                    cause: cause_op,
                });
            }
            StmtKind::Try {
                body,
                handlers,
                orelse,
                finalbody,
            } => {
                self.build_try(body, handlers, orelse, finalbody);
            }
            StmtKind::Delete(targets) => {
                for &target in targets {
                    self.build_delete(target);
                }
            }
            StmtKind::ClassDef(cls_id) => {
                self.build_class_def(*cls_id);
            }
            StmtKind::Pass => {}
        }
    }

    fn build_expr_stmt(&mut self, expr_id: ExprId) {
        let hir_body = &self.hir.functions[self.current_hir_fn].body;
        let expr = &hir_body.exprs[expr_id];
        if let ExprKind::Call { callee, args } = &expr.kind {
            let callee_expr = &hir_body.exprs[*callee];
            let has_star = args.iter().any(|arg| {
                matches!(
                    arg,
                    ryupy_hir::CallArg::Starred(_) | ryupy_hir::CallArg::DoubleStarred(_)
                )
            });
            if matches!(callee_expr.kind, ExprKind::Builtin(Builtin::Print)) && !has_star {
                // `print` 太低频地需要通用分派，单独展开：
                // - 位置实参先全部求值（Python 的实参求值顺序），再统一输出；
                // - `sep` / `end` 关键字决定分隔符与结尾，缺省仍是空格与换行；
                // - `file` / `flush` 依赖文件流（M3.4），现在明确报 TypeError。
                let dynamic = self.types.is_dynamic(self.current_hir_fn);
                let mut written = Vec::with_capacity(args.len());
                let mut sep: Option<Operand> = None;
                let mut end_op: Option<Operand> = None;

                for arg in args {
                    match arg {
                        ryupy_hir::CallArg::Positional(expr) => {
                            let op = self.expr_builder().build_expr(*expr);
                            let arg_ty = self.expr_builder().ty_of(*expr);
                            let (op, rt_fn) = if dynamic {
                                (
                                    self.expr_builder().coerce(op, &arg_ty, &MirType::Dynamic),
                                    RuntimeFn::WriteObject,
                                )
                            } else {
                                match arg_ty {
                                    Ty::Int => (op, RuntimeFn::WriteI64),
                                    Ty::Float => (op, RuntimeFn::WriteF64),
                                    Ty::Bool => (op, RuntimeFn::WriteBool),
                                    Ty::Str => (op, RuntimeFn::WriteStr),
                                    Ty::None => {
                                        let boxed = self.expr_builder().coerce(
                                            op,
                                            &arg_ty,
                                            &MirType::Dynamic,
                                        );
                                        (boxed, RuntimeFn::WriteObject)
                                    }
                                    _ => (op, RuntimeFn::WriteI64),
                                }
                            };
                            written.push((op, rt_fn));
                        }
                        ryupy_hir::CallArg::Starred(_) => {
                            // `print(*xs)` 需要运行时展开，留到需要真实用例时再实现
                            self.diagnostics.push(
                                Diagnostic::error(
                                    Span::new(
                                        self.hir.file,
                                        self.hir.functions[self.current_hir_fn].body.exprs
                                            [arg.expr()]
                                        .range,
                                    ),
                                    "print(*args) 暂不支持",
                                )
                                .with_code("E0304")
                                .with_primary_label("暂不支持展开实参"),
                            );
                        }
                        ryupy_hir::CallArg::Keyword(name, expr) => {
                            let op = self.expr_builder().build_expr(*expr);
                            let ty = self.expr_builder().ty_of(*expr);
                            match name.as_str() {
                                "sep" => {
                                    sep = Some(self.expr_builder().coerce(op, &ty, &MirType::Str));
                                }
                                "end" => {
                                    end_op =
                                        Some(self.expr_builder().coerce(op, &ty, &MirType::Str));
                                }
                                other => {
                                    // 求值后抛 TypeError：Python 的未知关键字也在调用时失败
                                    self.push_stmt(Statement::Eval(Rvalue::Call {
                                        callee: Callee::Runtime(RuntimeFn::ArgError),
                                        args: vec![
                                            Operand::Const(Constant::I64(
                                                ARG_ERROR_UNEXPECTED_KEYWORD,
                                            )),
                                            Operand::Const(Constant::Str(format!(
                                                "print() got an unexpected keyword argument '{other}'"
                                            ))),
                                        ],
                                    }));
                                }
                            }
                        }
                        ryupy_hir::CallArg::DoubleStarred(_) => {}
                    }
                }

                for (i, (op, rt_fn)) in written.into_iter().enumerate() {
                    if i > 0 {
                        match &sep {
                            Some(sep_op) => self.push_stmt(Statement::Eval(Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::WriteStr),
                                args: vec![sep_op.clone()],
                            })),
                            None => self.push_stmt(Statement::Eval(Rvalue::Call {
                                callee: Callee::Runtime(RuntimeFn::WriteSep),
                                args: Vec::new(),
                            })),
                        }
                    }
                    self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(rt_fn),
                        args: vec![op],
                    }));
                }
                match end_op {
                    Some(end_op) => self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::WriteStr),
                        args: vec![end_op],
                    })),
                    None => self.push_stmt(Statement::Eval(Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::WriteNewline),
                        args: Vec::new(),
                    })),
                }
                return;
            }
        }
        let _ = self.expr_builder().build_expr(expr_id);
    }
}
