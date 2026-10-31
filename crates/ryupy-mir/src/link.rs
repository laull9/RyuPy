//! 多模块合并（M2.7）。
//!
//! driver 逐模块调用 `build_program`：每个模块有自己的 arena，`FnId` / `ClassDefId` /
//! `GlobalId` 都从 0 开始，因此不能直接塞进同一个 [`LinkedProgram`]。本模块做一次
//! 「重新分配 + 改写引用」的合并：
//!
//! - 所有 ID 按模块顺序重新分配；
//! - 静态槽位名与 CLR 类型名加 `m<模块序号>_` 前缀，避免不同模块重名；
//! - 方法名（`Function::codegen_name`）同样加前缀，Python 可见名保持原样；
//! - 模块导出表里的槽位 ID 也跟着改写。
//!
//! 函数体内部的局部变量 ID 属于函数自己的 arena，不需要改写。

use la_arena::Arena;
use rustc_hash::FxHashMap;

use crate::mir::{
    BasicBlock, Body, Callee, ClassDef, ClassDefId, FnId, Function, GlobalDecl, GlobalId,
    LinkedProgram, LocalDecl, MirType, ModuleDef, ModuleId, Operand, Program, Rvalue, Statement,
    Terminator,
};

/// 一个模块的元信息（由 driver 从模块图提供）。
#[derive(Debug, Clone)]
pub struct UnitMeta {
    pub name: String,
    pub package: String,
    pub file: String,
    /// 模块导出：Python 名字 → 模块内部槽位。
    pub exports: Vec<(String, GlobalId)>,
}

/// 把各模块的 `Program` 合并成一个程序集级别的 IR。
///
/// `entry` 是入口模块在 `units` 里的下标（`__main__`）。
pub fn link(units: Vec<(Program, UnitMeta)>, entry: usize) -> LinkedProgram {
    let mut functions: Arena<Function> = Arena::default();
    let mut classes: Arena<ClassDef> = Arena::default();
    let mut globals: Arena<GlobalDecl> = Arena::default();
    let mut modules: Arena<ModuleDef> = Arena::default();

    let mut module_ids: Vec<ModuleId> = Vec::with_capacity(units.len());
    let mut entry_init: Option<FnId> = None;

    for (unit_index, (program, meta)) in units.into_iter().enumerate() {
        let prefix = format!("m{unit_index}_");

        // 1. 先建立三张 ID 映射（只分配，不读内容）。
        let mut class_map: FxHashMap<ClassDefId, ClassDefId> = FxHashMap::default();
        for (old, _) in program.classes.iter() {
            class_map.insert(old, classes.alloc(ClassDef::placeholder()));
        }
        let mut global_map: FxHashMap<GlobalId, GlobalId> = FxHashMap::default();
        for (old, decl) in program.globals.iter() {
            global_map.insert(
                old,
                globals.alloc(GlobalDecl {
                    name: format!("{prefix}{}", decl.name),
                    ty: remap_type(&decl.ty, &class_map),
                }),
            );
        }
        let mut fn_map: FxHashMap<FnId, FnId> = FxHashMap::default();
        for (old, func) in program.functions.iter() {
            let new_id = functions.alloc(Function {
                name: func.name.clone(),
                codegen_name: format!("{prefix}{}", func.codegen_name),
                source_file: func.source_file.clone(),
                params: func.params.clone(),
                ret: remap_type(&func.ret, &class_map),
                body: Body {
                    locals: Arena::default(),
                    blocks: Arena::default(),
                    entry: la_arena::Idx::from_raw(0.into()),
                    regions: Vec::new(),
                },
                param_meta: func
                    .param_meta
                    .iter()
                    .map(|meta| crate::mir::ParamMeta {
                        name: meta.name.clone(),
                        kind: meta.kind,
                        has_default: meta.has_default,
                        ty: remap_type(&meta.ty, &class_map),
                    })
                    .collect(),
                function_global: func.function_global.map(|g| global_map[&g]),
                defaults_global: func.defaults_global.map(|g| global_map[&g]),
                signature_global: func.signature_global.map(|g| global_map[&g]),
            });
            fn_map.insert(old, new_id);
        }

        // 2. 类定义：字段 / 基类 / 工厂槽位都要改写。
        for (old, cls) in program.classes.iter() {
            let new_id = class_map[&old];
            classes[new_id] = ClassDef {
                name: cls.name.clone(),
                codegen_name: format!("{prefix}{}", cls.codegen_name),
                fields: cls
                    .fields
                    .iter()
                    .map(|(name, ty)| (name.clone(), remap_type(ty, &class_map)))
                    .collect(),
                is_value_type: cls.is_value_type,
                is_dataclass: cls.is_dataclass,
                base: cls.base.map(|b| class_map[&b]),
                builtin_base: cls.builtin_base.clone(),
                is_exception: cls.is_exception,
                factory_global: cls.factory_global.map(|g| global_map[&g]),
            };
        }

        // 3. 函数体：改写操作数、调用目标与类型。
        for (old, func) in program.functions.iter() {
            let new_id = fn_map[&old];
            let mut locals: Arena<LocalDecl> = Arena::default();
            for (_, decl) in func.body.locals.iter() {
                locals.alloc(LocalDecl {
                    ty: remap_type(&decl.ty, &class_map),
                    name: decl.name.clone(),
                });
            }
            let body = Body {
                locals,
                blocks: remap_blocks(&func.body.blocks, &fn_map, &class_map, &global_map),
                entry: func.body.entry,
                regions: func
                    .body
                    .regions
                    .iter()
                    .map(|region| crate::mir::Region {
                        kind: match &region.kind {
                            crate::mir::RegionKind::Catch { type_names } => {
                                crate::mir::RegionKind::Catch {
                                    type_names: type_names.clone(),
                                }
                            }
                            crate::mir::RegionKind::CatchAll => crate::mir::RegionKind::CatchAll,
                            crate::mir::RegionKind::FinallyGuard => {
                                crate::mir::RegionKind::FinallyGuard
                            }
                        },
                        try_start: region.try_start,
                        try_len: region.try_len,
                        handler: region.handler,
                        exc_local: region.exc_local,
                    })
                    .collect(),
            };
            let init = fn_map[&program.entry];
            functions[new_id].body = body;
            if unit_index == entry {
                entry_init = Some(init);
            }
        }

        // 4. 模块导出表。
        let exports = meta
            .exports
            .iter()
            .map(|(name, gid)| (name.clone(), global_map[gid]))
            .collect();
        module_ids.push(modules.alloc(ModuleDef {
            name: meta.name,
            package: meta.package,
            file: meta.file,
            init: fn_map[&program.entry],
            exports,
        }));
    }

    let entry_module = module_ids.get(entry).copied().unwrap_or_else(|| {
        // 空程序（理论上不会出现）：补一个空入口模块，避免 panic。
        modules.alloc(ModuleDef {
            name: "__main__".to_owned(),
            package: String::new(),
            file: String::new(),
            init: entry_init.unwrap_or_else(|| functions.alloc(Function::placeholder())),
            exports: Vec::new(),
        })
    });

    LinkedProgram {
        functions,
        classes,
        globals,
        modules,
        entry: entry_module,
    }
}

fn remap_type(ty: &MirType, class_map: &FxHashMap<ClassDefId, ClassDefId>) -> MirType {
    match ty {
        MirType::Class(id) => MirType::Class(class_map.get(id).copied().unwrap_or(*id)),
        other => other.clone(),
    }
}

fn remap_blocks(
    blocks: &Arena<BasicBlock>,
    fn_map: &FxHashMap<FnId, FnId>,
    class_map: &FxHashMap<ClassDefId, ClassDefId>,
    global_map: &FxHashMap<GlobalId, GlobalId>,
) -> Arena<BasicBlock> {
    let mut out: Arena<BasicBlock> = Arena::default();
    for (_, block) in blocks.iter() {
        out.alloc(BasicBlock {
            stmts: block
                .stmts
                .iter()
                .map(|stmt| remap_stmt(stmt, fn_map, class_map, global_map))
                .collect(),
            terminator: remap_terminator(&block.terminator, global_map),
        });
    }
    out
}

fn remap_stmt(
    stmt: &Statement,
    fn_map: &FxHashMap<FnId, FnId>,
    class_map: &FxHashMap<ClassDefId, ClassDefId>,
    global_map: &FxHashMap<GlobalId, GlobalId>,
) -> Statement {
    match stmt {
        Statement::Assign { dest, value } => Statement::Assign {
            dest: *dest,
            value: remap_rvalue(value, fn_map, class_map, global_map),
        },
        Statement::Eval(value) => {
            Statement::Eval(remap_rvalue(value, fn_map, class_map, global_map))
        }
        Statement::SetField { obj, field, value } => Statement::SetField {
            obj: remap_operand(obj, global_map),
            field: *field,
            value: remap_operand(value, global_map),
        },
        Statement::SetGlobal { dest, value } => Statement::SetGlobal {
            dest: global_map[dest],
            value: remap_operand(value, global_map),
        },
        Statement::Line(line) => Statement::Line(*line),
    }
}

fn remap_terminator(term: &Terminator, global_map: &FxHashMap<GlobalId, GlobalId>) -> Terminator {
    match term {
        Terminator::Goto(bb) => Terminator::Goto(*bb),
        Terminator::Branch { cond, then_, else_ } => Terminator::Branch {
            cond: remap_operand(cond, global_map),
            then_: *then_,
            else_: *else_,
        },
        Terminator::Return(value) => {
            Terminator::Return(value.as_ref().map(|v| remap_operand(v, global_map)))
        }
        Terminator::Raise { value, cause } => Terminator::Raise {
            value: value.as_ref().map(|v| remap_operand(v, global_map)),
            cause: cause.as_ref().map(|v| remap_operand(v, global_map)),
        },
        Terminator::Unreachable => Terminator::Unreachable,
    }
}

fn remap_operand(op: &Operand, global_map: &FxHashMap<GlobalId, GlobalId>) -> Operand {
    match op {
        Operand::Local(_) | Operand::Const(_) => op.clone(),
        Operand::Global(gid) => Operand::Global(global_map[gid]),
        Operand::Array(items) => Operand::Array(
            items
                .iter()
                .map(|item| remap_operand(item, global_map))
                .collect(),
        ),
    }
}

fn remap_rvalue(
    value: &Rvalue,
    fn_map: &FxHashMap<FnId, FnId>,
    class_map: &FxHashMap<ClassDefId, ClassDefId>,
    global_map: &FxHashMap<GlobalId, GlobalId>,
) -> Rvalue {
    match value {
        Rvalue::Use(op) => Rvalue::Use(remap_operand(op, global_map)),
        Rvalue::Binary { op, lhs, rhs } => Rvalue::Binary {
            op: *op,
            lhs: remap_operand(lhs, global_map),
            rhs: remap_operand(rhs, global_map),
        },
        Rvalue::Unary { op, operand } => Rvalue::Unary {
            op: *op,
            operand: remap_operand(operand, global_map),
        },
        Rvalue::Cast { kind, operand } => Rvalue::Cast {
            kind: *kind,
            operand: remap_operand(operand, global_map),
        },
        Rvalue::Call { callee, args } => Rvalue::Call {
            callee: match callee {
                Callee::Fn(id) => Callee::Fn(fn_map[id]),
                Callee::Runtime(rt) => Callee::Runtime(*rt),
            },
            args: args.iter().map(|a| remap_operand(a, global_map)).collect(),
        },
        Rvalue::NewObject { class, args } => Rvalue::NewObject {
            class: class_map[class],
            args: args.iter().map(|a| remap_operand(a, global_map)).collect(),
        },
        Rvalue::GetField { obj, field } => Rvalue::GetField {
            obj: remap_operand(obj, global_map),
            field: *field,
        },
        Rvalue::Box { value, from } => Rvalue::Box {
            value: remap_operand(value, global_map),
            from: remap_type(from, class_map),
        },
        Rvalue::Unbox { value, to } => Rvalue::Unbox {
            value: remap_operand(value, global_map),
            to: remap_type(to, class_map),
        },
        Rvalue::DynBinary { op, lhs, rhs } => Rvalue::DynBinary {
            op: *op,
            lhs: remap_operand(lhs, global_map),
            rhs: remap_operand(rhs, global_map),
        },
        Rvalue::DynCompare { op, lhs, rhs } => Rvalue::DynCompare {
            op: *op,
            lhs: remap_operand(lhs, global_map),
            rhs: remap_operand(rhs, global_map),
        },
        Rvalue::DynUnary { op, operand } => Rvalue::DynUnary {
            op: *op,
            operand: remap_operand(operand, global_map),
        },
        Rvalue::DynTruthy { value } => Rvalue::DynTruthy {
            value: remap_operand(value, global_map),
        },
        Rvalue::CallWithArgs {
            callee,
            prefix,
            args,
        } => Rvalue::CallWithArgs {
            callee: *callee,
            prefix: prefix
                .iter()
                .map(|a| remap_operand(a, global_map))
                .collect(),
            args: args.iter().map(|a| remap_operand(a, global_map)).collect(),
        },
        Rvalue::CatchException => Rvalue::CatchException,
    }
}

impl ClassDef {
    /// link 阶段先占位，随后整体覆盖。
    fn placeholder() -> Self {
        Self {
            name: String::new(),
            codegen_name: String::new(),
            fields: Vec::new(),
            is_value_type: false,
            is_dataclass: false,
            base: None,
            builtin_base: None,
            is_exception: false,
            factory_global: None,
        }
    }
}

impl Function {
    /// 空程序兜底用；正常流程不会使用。
    fn placeholder() -> Self {
        Self {
            name: String::new(),
            codegen_name: String::new(),
            source_file: String::new(),
            params: Vec::new(),
            ret: MirType::Unit,
            body: Body {
                locals: Arena::default(),
                blocks: Arena::default(),
                entry: la_arena::Idx::from_raw(0.into()),
                regions: Vec::new(),
            },
            param_meta: Vec::new(),
            function_global: None,
            defaults_global: None,
            signature_global: None,
        }
    }
}
