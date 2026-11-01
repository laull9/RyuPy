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
use ryupy_base::Diagnostic;
use ryupy_hir::{LocalId as HirLocalId, Module, ModuleVarId};
use ryupy_ty::{ModuleTypes, Ty, type_ref_to_ty};

use crate::build_flow::FinallyCtx;
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
            is_dataclass: cls.is_dataclass && cls.decorated_binding.is_none(),
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

        let is_main = hir_fn.kind == ryupy_hir::FunctionKind::ModuleMain;
        let is_top_user = hir_fn.kind == ryupy_hir::FunctionKind::User;
        let function_global = is_top_user.then(|| alloc_global(&mut globals, "__fn"));
        // 每个顶层用户函数都分配默认值数组：thunk 的绑定入口需要一个固定长度的数组，
        // 没有默认值的槽位留空即可（PySignature.HasDefault 为 false 时不会被读取）。
        let defaults_global = is_top_user.then(|| alloc_global(&mut globals, "__defaults"));
        // 签名是所有实例共享的元数据：嵌套函数（M3.0）没有函数对象槽位，
        // 但 MakeFunction 在 def 位置创建 PyFunction 时同样需要签名。
        let signature_global = (!is_main).then(|| alloc_global(&mut globals, "__sig"));

        let codegen_name = if is_main {
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
            env: None,
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

        // 建立 HIR 局部变量 → MIR 局部变量映射。
        // 被 nested 函数捕获的名字（cell，M3.0）在槽位里存 `object[1]`，
        // 必须用 Dynamic 表示；读写都经过运行时 cell 协议。
        let captured: rustc_hash::FxHashSet<&str> =
            hir_fn.closure.cells.iter().map(|n| n.as_str()).collect();
        for (hir_loc_id, hir_loc) in hir_fn.body.locals.iter() {
            let hir_ty = types
                .body(hir_fn_id)
                .map(|b| b.local_ty(hir_loc_id))
                .unwrap_or(Ty::Dynamic);
            let is_cell = captured.contains(hir_loc.name.as_str());
            // 局部变量里的 `None` 用运行时的 None 单例（装箱 object）表示：
            // `Unit` 没有 CIL 表示、也无法分配槽位，若直接使用，赋值会把值留在求值栈上，
            // 生成的方法被 CLR 判为 invalid program；而 i64 占位值又会把 `None` 打印成 0。
            let ty = if is_cell {
                MirType::Dynamic
            } else {
                match ty_to_mir(&hir_ty) {
                    MirType::Unit => MirType::Dynamic,
                    ty => ty,
                }
            };
            let mir_loc = builder.locals.alloc(LocalDecl {
                ty,
                name: Some(hir_loc.name.to_string()),
                is_cell,
            });
            builder.local_map.insert(hir_loc_id, mir_loc);
            if is_cell {
                // cell 的「未赋值」由运行时哨兵检查，编译期视为已赋值。
                builder.assigned_locals.insert(hir_loc_id);
            }
        }

        // 收集参数 ID，并标记为已赋值
        let mut params = Vec::new();
        for p in &hir_fn.params {
            let mir_loc = builder.local_map[&p.local];
            params.push(mir_loc);
            builder.assigned_locals.insert(p.local);
        }

        // 闭包环境（M3.0）：引用外层 cell 的函数多一个 `object[]` 形参，
        // 由 thunk 从 PyFunction.Env 传入。排在所有 Python 参数之后。
        if !hir_fn.closure.free.is_empty() {
            let env_local = builder.locals.alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: Some("__env".to_string()),
                is_cell: false,
            });
            builder.env = Some(env_local);
            params.push(env_local);
        }

        // 函数入口：为每个 cell 局部分配 cell 对象。参数 cell 先把实参装箱进
        // 临时量（此刻参数槽位还是原始实参），再让槽位指向新 cell。
        // 闭包函数一律是动态模式，参数槽位都是装箱值，无需再 Box。
        for (hir_loc_id, hir_loc) in hir_fn.body.locals.iter() {
            let Some(&slot) = builder.local_map.get(&hir_loc_id) else {
                continue;
            };
            if !builder.locals[slot].is_cell {
                continue;
            }
            if hir_loc.is_param {
                let boxed = builder.alloc_temp(MirType::Dynamic);
                builder.push_stmt(Statement::Assign {
                    dest: boxed,
                    value: Rvalue::Use(Operand::Local(slot)),
                });
                builder.push_stmt(Statement::Assign {
                    dest: slot,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::CellNew),
                        args: Vec::new(),
                    },
                });
                builder.push_stmt(Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::CellSet),
                    args: vec![Operand::Local(slot), Operand::Local(boxed)],
                }));
            } else {
                builder.push_stmt(Statement::Assign {
                    dest: slot,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(RuntimeFn::CellNew),
                        args: Vec::new(),
                    },
                });
            }
        }

        // 模块入口开头把动态模块变量重置为未绑定的 CLR null：失败后重试等同于重新执行模块，
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
                    value: Operand::Const(Constant::Null),
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
        functions[mir_fn_id].env = builder.env;
        functions[mir_fn_id].body = crate::mir::Body {
            locals: builder.locals,
            blocks: builder.blocks,
            entry,
            regions: std::mem::take(&mut builder.regions),
        };
    }

    let entry = fn_map[&hir.main];
    let mut program = Program {
        functions,
        classes,
        globals,
        entry,
        source_name: hir.source_name.clone(),
    };

    let generators = hir
        .functions
        .iter()
        .filter(|(_, f)| f.is_generator)
        .map(|(id, _)| fn_map[&id])
        .collect::<Vec<_>>();
    crate::generators::lower(&mut program, &generators);

    // 模块导出表：模块变量 + 顶层函数 + 顶层类。
    let mut exports: Vec<(String, crate::mir::GlobalId)> = Vec::new();
    for (var_id, var) in hir.vars.iter() {
        if let Some(&gid) = module_var_ids.get(&var_id) {
            exports.push((var.name.as_str().to_owned(), gid));
        }
    }
    for fn_id in &hir.top_functions {
        if exports
            .iter()
            .any(|(name, _)| name == hir.functions[*fn_id].name.as_str())
        {
            continue;
        }
        if let Some(&mir_id) = fn_map.get(fn_id)
            && let Some(gid) = program.functions[mir_id].function_global
        {
            exports.push((hir.functions[*fn_id].name.as_str().to_owned(), gid));
        }
    }
    for cls_id in &hir.top_classes {
        if exports
            .iter()
            .any(|(name, _)| name == hir.classes[*cls_id].name.as_str())
        {
            continue;
        }
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
    /// 闭包环境形参（M3.0）：`Free` 读取 / `nonlocal` 写入都要经过它。
    pub(crate) env: Option<MirLocalId>,
    /// 当前正在执行的类体原始命名空间。
    pub(crate) class_namespace: Option<Operand>,
}

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
            env: None,
            class_namespace: None,
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
}
