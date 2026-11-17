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
//! - print 的全位置调用先准备实参、固定输出目标，再按静态类型直接 Write；关键字/星号走动态绑定。
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
    LocalId as MirLocalId, MirType, Operand, Program, Region, RuntimeFn, Rvalue, Statement,
};

/// 编译一个模块，返回（模块程序，模块导出表，诊断）。
///
/// 导出表是「Python 可见名字 → 静态槽位」，codegen 用它生成模块对象的属性读写。
pub fn build_program(
    hir: &Module,
    types: &ModuleTypes,
) -> (Program, Vec<crate::mir::ModuleExport>, Vec<Diagnostic>) {
    crate::generator_state::build_program(hir, types)
}

/// 跨模块闭图显式启用缺省参数通用伴生体；普通单模块接口保持原发射。
pub fn build_program_with_generic(
    hir: &Module,
    types: &ModuleTypes,
) -> (Program, Vec<crate::mir::ModuleExport>, Vec<Diagnostic>) {
    crate::generator_state::build_program_with_generic(hir, types, true)
}

pub(crate) fn build_program_with_origins(
    hir: &Module,
    types: &ModuleTypes,
    origins: Option<&mut crate::trace_lowering::LoweringOrigins>,
) -> (Program, Vec<crate::mir::ModuleExport>, Vec<Diagnostic>) {
    build_program_mode(hir, types, origins, None, false)
}

pub(crate) fn build_program_mode(
    hir: &Module,
    types: &ModuleTypes,
    origins: Option<&mut crate::trace_lowering::LoweringOrigins>,
    deferred: Option<&mut crate::generator_product::DeferredGenerators>,
    trace_product: bool,
) -> (Program, Vec<crate::mir::ModuleExport>, Vec<Diagnostic>) {
    build_program_cores(hir, types, origins, deferred, trace_product, false)
}

pub(crate) fn build_program_cores(
    hir: &Module,
    types: &ModuleTypes,
    mut origins: Option<&mut crate::trace_lowering::LoweringOrigins>,
    deferred: Option<&mut crate::generator_product::DeferredGenerators>,
    trace_product: bool,
    generic: bool,
) -> (Program, Vec<crate::mir::ModuleExport>, Vec<Diagnostic>) {
    let mut functions = Arena::default();
    let mut classes = Arena::default();
    let mut globals = Arena::default();
    let mut diagnostics = Vec::new();
    #[cfg(feature = "trace-adapter-test")]
    diagnostics.extend(crate::trace_adapter_test::admission(hir));

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
                storage: crate::GlobalStorage::LegacyStatic,
                name: format!("__exc_factory_{}", hir_cls_id.into_raw().into_u32()),
                ty: MirType::Dynamic,
            })
        });
        let mir_id = classes.alloc(crate::mir::ClassDef {
            class_cell: None,
            name: cls.name.as_str().to_string(),
            codegen_name: cls.name.as_str().to_string(),
            fields,
            is_value_type: false,
            is_generator_state: false,
            is_dataclass: cls.is_dataclass
                && (cls.decorated_binding.is_none() && !cls.runtime_dataclass),
            base: None,
            builtin_base: match cls.base {
                Some(ryupy_hir::ClassBase::Builtin(name)) => Some(name.to_string()),
                _ => None,
            },
            is_exception: cls.is_exception,
            factory_global,
        });
        class_map.insert(hir_cls_id, mir_id);
        if !hir.info.instance_globals && cls.lexical.is_none() {
            let global = globals.alloc(GlobalDecl {
                storage: crate::GlobalStorage::LegacyStatic,
                name: format!("__class_{}", hir_cls_id.into_raw().into_u32()),
                ty: MirType::Dynamic,
            });
            hir_class_globals.insert(hir_cls_id, global);
        }
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
            storage: if hir.info.instance_globals {
                crate::GlobalStorage::InstancePublic {
                    name: var.name.to_string(),
                }
            } else {
                crate::GlobalStorage::LegacyStatic
            },
            name: format!("__mv{}_{}", var_id.into_raw().into_u32(), var.name),
            ty: ty.clone(),
        });
        module_var_ids.insert(var_id, gid);
        module_var_tys.insert(var_id, ty);
    }

    let module_bound_flags = crate::module_bindings::allocate(&mut globals, &module_var_ids);

    // 2. 预先为每个 HIR 函数分配 MIR FnId，并登记函数对象 / 默认值数组 / 签名所需的
    //    模块静态槽位。槽位在函数体构建之前分配，调用方才能在表达式里引用它们。
    let mut fn_map: FxHashMap<ryupy_hir::FuncId, FnId> = FxHashMap::default();
    let mut global_seq = 0usize;
    let mut fn_sources = FxHashMap::default();
    let plain_defs = crate::dynamic_origin_build::plain_definitions(hir);
    for (hir_fn_id, hir_fn) in hir.functions.iter() {
        let ret_ty = ty_to_mir(&types.sigs[hir_fn_id].ret);
        let param_meta = crate::build_params::build_param_meta(hir, types, hir_fn_id);

        let mut alloc_global = |globals: &mut Arena<GlobalDecl>, prefix: &str| {
            let id = globals.alloc(GlobalDecl {
                storage: crate::GlobalStorage::LegacyStatic,
                name: format!("{prefix}_{global_seq}"),
                ty: MirType::Dynamic,
            });
            global_seq += 1;
            id
        };

        let is_main = hir_fn.kind == ryupy_hir::FunctionKind::ModuleMain;
        let is_top_user =
            !hir.info.instance_globals && hir_fn.kind == ryupy_hir::FunctionKind::User;
        let function_global = is_top_user.then(|| alloc_global(&mut globals, "__fn"));
        // 每个顶层用户函数都分配默认值数组：thunk 的绑定入口需要一个固定长度的数组，
        // 没有默认值的槽位留空即可（PySignature.HasDefault 为 false 时不会被读取）。
        let defaults_global = is_top_user.then(|| alloc_global(&mut globals, "__defaults"));
        // 签名是所有实例共享的元数据：嵌套函数（M3.0）没有函数对象槽位，
        // 但 MakeFunction 在 def 位置创建 PyFunction 时同样需要签名。
        let signature_global = (!is_main && !hir.info.stock_thin_functions)
            .then(|| alloc_global(&mut globals, "__sig"));
        if !hir.info.instance_globals
            && types.mode(hir_fn_id) == ryupy_ty::FuncMode::Dynamic
            && plain_defs.contains(&hir_fn_id)
        {
            fn_sources.insert(hir_fn_id, alloc_global(&mut globals, "__origin"));
        }

        let codegen_name = if is_main {
            "__module_main".to_string()
        } else {
            hir_fn.name.as_str().to_string()
        };
        let mir_fn = Function {
            core: Default::default(),
            is_comprehension: hir_fn.is_comprehension && !hir_fn.is_generator,
            module_state: None,
            instance_capabilities: Default::default(),
            module_state_source: None,
            name: hir_fn.name.as_str().to_string(),
            codegen_name,
            source_file: hir.info.file.clone(),
            definition: hir_fn
                .definition
                .as_ref()
                .map(|info| crate::DefinitionInfo {
                    lexical_qualname: info.lexical_qualname.clone(),
                    firstlineno: info.firstlineno,
                    introspection_kind: hir.info.introspection_profile.then_some(
                        if hir_fn.is_coroutine {
                            crate::PythonFunctionKind::Coroutine
                        } else if hir_fn.is_generator {
                            crate::PythonFunctionKind::Generator
                        } else if hir.top_functions.contains(&hir_fn_id) {
                            crate::PythonFunctionKind::OrdinaryModule
                        } else {
                            crate::PythonFunctionKind::OrdinaryNonModule
                        },
                    ),
                }),
            params: Vec::new(),
            ret: ret_ty,
            body: crate::mir::Body {
                direct_globals_contracts: Vec::new(),
                source_handler_contracts: Vec::new(),
                locals: Arena::default(),
                blocks: Arena::default(),
                entry: la_arena::Idx::from_raw(0.into()),
                regions: Vec::new(),
            },
            param_meta,
            function_global,
            specialization_identity_global: None,
            defaults_global,
            signature_global,
            env: None,
            dynamic_origin: None,
        };
        let mir_fn_id = functions.alloc(mir_fn);
        fn_map.insert(hir_fn_id, mir_fn_id);
    }

    let work = crate::generic_cores::allocate(hir, types, &fn_map, &mut functions, generic);
    let source = crate::source_provenance::allocate(
        hir,
        &fn_map,
        &module_var_ids,
        &functions,
        &mut globals,
        &mut diagnostics,
    );
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
    for (hir_fn_id, mir_fn_id, is_generic) in work {
        let hir_fn = &hir.functions[hir_fn_id];
        let mut builder = FunctionBuilder::new(
            hir,
            types,
            &fn_map,
            &fn_globals,
            &fn_defaults,
            &fn_sources,
            &class_map,
            &hir_class_globals,
            &class_factories,
            &module_var_ids,
            &module_bound_flags,
            &globals,
            hir_fn_id,
        );

        if is_generic {
            builder.types.generic = Some(hir_fn_id);
            builder.ret_ty = MirType::Dynamic;
        }
        if origins.is_some() && !is_generic {
            builder.trace_expressions = Some(Vec::new());
        }
        builder.trace_product = trace_product;
        // 建立 HIR 局部变量 → MIR 局部变量映射。
        // 被 nested 函数捕获的名字（cell，M3.0）在槽位里存 `object[1]`，
        // 必须用 Dynamic 表示；读写都经过运行时 cell 协议。
        let captured: rustc_hash::FxHashSet<&str> =
            hir_fn.closure.cells.iter().map(|n| n.as_str()).collect();
        for (hir_loc_id, hir_loc) in hir_fn.body.locals.iter() {
            let hir_ty = builder
                .types
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

        builder.add_module_state(&mut params);
        builder.add_dynamic_owner(&mut params);

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
        if hir_fn.kind == ryupy_hir::FunctionKind::ModuleMain && !hir.info.instance_globals {
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
        if !hir.info.stock_thin_functions || hir_fn_id == hir.main {
            builder.emit_frame_push(source, functions[mir_fn_id].signature_global);
        }

        // 构建语句
        for &stmt_id in &hir_fn.body.root {
            builder.build_stmt(stmt_id);
        }

        builder.finish_function();

        diagnostics.extend(builder.diagnostics);

        let entry = builder.entry;
        functions[mir_fn_id].instance_capabilities = hir.info.instance_capabilities;
        functions[mir_fn_id].module_state = builder.module_state;
        functions[mir_fn_id].module_state_source = builder.module_state.map(|_| {
            if hir.info.stock_thin_functions && hir_fn_id != hir.main {
                return crate::InstanceStateSource::StockThinLease;
            }
            crate::InstanceStateSource::parameter(
                hir_fn.kind == ryupy_hir::FunctionKind::ModuleMain,
            )
        });
        functions[mir_fn_id].params = params;
        functions[mir_fn_id].env = builder.env;
        functions[mir_fn_id].dynamic_origin =
            builder
                .dynamic_owner
                .map(|owner| crate::mir::DynamicFunctionOrigin {
                    owner,
                    source_global: (!is_generic)
                        .then(|| fn_sources.get(&hir_fn_id).copied())
                        .flatten(),
                });
        for (class, definition) in std::mem::take(&mut builder.class_cells) {
            classes[class].class_cell = Some(definition);
        }
        if let Some(origins) = origins.as_deref_mut().filter(|_| !is_generic) {
            origins.bodies.insert(
                hir_fn_id,
                crate::trace_lowering::FunctionOrigin {
                    locals: builder.local_map.clone(),
                    expressions: builder.trace_expressions.take().unwrap_or_default(),
                },
            );
        }
        functions[mir_fn_id].body = crate::mir::Body {
            direct_globals_contracts: std::mem::take(&mut builder.direct_globals_contracts),
            source_handler_contracts: std::mem::take(&mut builder.source_handler_contracts),
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

    crate::module_bindings::instrument(&mut program, &module_bound_flags);
    let generator_layouts = crate::generator_product::finish_build(
        &mut program,
        hir,
        &fn_map,
        &hir_class_globals,
        deferred,
    );

    let exports = crate::module_bindings::exports(
        hir,
        &program,
        &module_var_ids,
        &module_bound_flags,
        &fn_map,
        &hir_class_globals,
    );

    if let Some(origins) = origins {
        origins.functions = fn_map;
        origins.classes = hir_class_globals;
        origins.globals = module_var_ids;
        origins.collect_generators(&program);
        origins.generator_layouts = generator_layouts;
    }
    (program, exports, diagnostics)
}

/// `PyOps.ArgError` 的错误种类：未知关键字。
pub const ARG_ERROR_UNEXPECTED_KEYWORD: i64 = 3;

pub use crate::generic_cores::ty_to_mir;

#[path = "build_state.rs"]
mod context;
pub(crate) use context::FunctionBuilder;
