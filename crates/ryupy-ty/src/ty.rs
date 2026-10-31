use la_arena::ArenaMap;
use ryupy_hir::{ClassId, ExprId, FuncId, LocalId};

/// 已解析的类型。
#[derive(Clone, Debug, PartialEq, Eq)]
pub enum Ty {
    /// Python `int`，Phase 1 映射为 `int64`（溢出策略见 ADR-0004）。
    Int,
    /// Python `float`，即 IEEE 754 `float64`。
    Float,
    Bool,
    Str,
    /// `None` 的类型；也用作「无返回值」。
    None,
    Class(ClassId),
    /// 函数对象本身的类型（只在被调用位置出现，Phase 1 不支持一等函数值）。
    Func(FuncId),
    /// Phase 2：无法静态确定，运行时用 `PyValue` 表示。
    Dynamic,
    /// 毒化类型：已经报过错，与任何类型统一都成功，用于抑制连锁报错。
    Error,
}

#[derive(Clone, Debug)]
pub struct FnSig {
    pub params: Vec<Ty>,
    pub ret: Ty,
}

/// 一个函数体的推导结果。
#[derive(Debug, Default)]
pub struct TypeckResults {
    pub exprs: ArenaMap<ExprId, Ty>,
    pub locals: ArenaMap<LocalId, Ty>,
}

/// 整个模块的类型信息。
#[derive(Debug, Default)]
pub struct ModuleTypes {
    pub sigs: ArenaMap<FuncId, FnSig>,
    pub bodies: ArenaMap<FuncId, TypeckResults>,
}

impl ModuleTypes {
    pub fn dump(&self) -> String {
        use std::fmt::Write;
        let mut out = String::new();
        for (func_id, sig) in self.sigs.iter() {
            let _ = writeln!(out, "func {:?}: {:?} -> {:?}", func_id, sig.params, sig.ret);
            if let Some(body) = self.bodies.get(func_id) {
                for (loc_id, ty) in body.locals.iter() {
                    let _ = writeln!(out, "  local {:?}: {:?}", loc_id, ty);
                }
            }
        }
        out
    }
}
