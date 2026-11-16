use la_arena::ArenaMap;
use ryupy_hir::{ClassId, ExprId, FuncId, LocalId};

/// 一个函数最终走哪条执行路径（M2.1）。
///
/// - `Static`：标注齐全、调用点一致、函数体内一切都能静态定型，走 Phase 1 的强类型路径；
/// - `Dynamic`：缺少标注、异构赋值、多返回类型或调用点实参不符——按 Python 语义走通用路径，
///   局部变量与参数都是 `object`（[ADR-0010](../../../docs/adr/0010-dynamic-value-representation.md)）。
///
/// 降级判定是**单调**的：静态检查一旦发现无法证明，就永久降级，不会回升。这样不动点迭代
/// 一定收敛，也避免「这次能证明、下次不能」的不稳定行为。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum FuncMode {
    Static,
    Dynamic,
}

impl FuncMode {
    pub fn is_dynamic(self) -> bool {
        self == FuncMode::Dynamic
    }
}

/// 已解析的类型。
#[derive(Clone, Debug, PartialEq, Eq)]
pub enum Ty {
    /// Python `int`。静态路径映射为 `int64`，动态路径在 i64 溢出时升级 BigInteger。
    Int,
    /// Python `float`，即 IEEE 754 `float64`。
    Float,
    Bool,
    Str,
    /// str注解的任意对象槽；不是exact字符串事实，也不独自触发整函数降级。
    OpaqueStr,
    /// `None` 的类型；也用作「无返回值」。
    None,
    Class(ClassId),
    /// 函数对象本身的类型（Phase 1 不支持一等函数值）。
    Func(FuncId),
    /// 无法静态确定，运行时用装箱的 CLR 值表示。
    Dynamic,
    /// 毒化类型：已经报过错，与任何类型统一都成功，用于抑制连锁报错。
    Error,
}

impl Ty {
    /// 是否触发原Dynamic推断；OpaqueStr同为object表示，但不独自使函数降级。
    pub fn is_dynamic(&self) -> bool {
        matches!(self, Ty::Dynamic)
    }
}

#[derive(Clone, Debug)]
pub struct FnSig {
    pub params: Vec<Ty>,
    pub ret: Ty,
}

/// 一个函数体的推导结果。动态函数不填任何条目，下游按 `Dynamic` 兜底。
#[derive(Debug, Default)]
pub struct TypeckResults {
    pub exprs: ArenaMap<ExprId, Ty>,
    pub locals: ArenaMap<LocalId, Ty>,
}

impl TypeckResults {
    /// 取表达式类型；动态函数没有记录，统一返回 `Dynamic`。
    pub fn expr_ty(&self, id: ExprId) -> Ty {
        self.exprs.get(id).cloned().unwrap_or(Ty::Dynamic)
    }

    /// 取局部变量类型；动态函数没有记录，统一返回 `Dynamic`。
    pub fn local_ty(&self, id: LocalId) -> Ty {
        self.locals.get(id).cloned().unwrap_or(Ty::Dynamic)
    }
}

/// 整个模块的类型信息。
#[derive(Debug, Default)]
pub struct ModuleTypes {
    pub sigs: ArenaMap<FuncId, FnSig>,
    pub bodies: ArenaMap<FuncId, TypeckResults>,
    pub modes: ArenaMap<FuncId, FuncMode>,
    /// 模块级变量的槽位类型（M2.7）。
    pub module_vars: ArenaMap<ryupy_hir::ModuleVarId, Ty>,
}

impl ModuleTypes {
    pub fn mode(&self, id: FuncId) -> FuncMode {
        self.modes.get(id).copied().unwrap_or(FuncMode::Static)
    }

    pub fn is_dynamic(&self, id: FuncId) -> bool {
        self.mode(id).is_dynamic()
    }

    pub fn body(&self, id: FuncId) -> Option<&TypeckResults> {
        self.bodies.get(id)
    }

    pub fn dump(&self) -> String {
        use std::fmt::Write;
        let mut out = String::new();
        for (var_id, ty) in self.module_vars.iter() {
            let _ = writeln!(out, "module var {var_id:?}: {ty:?}");
        }
        for (func_id, sig) in self.sigs.iter() {
            let mode = self.mode(func_id);
            let _ = writeln!(
                out,
                "func {:?} [{mode:?}]: {:?} -> {:?}",
                func_id, sig.params, sig.ret
            );
            if let Some(body) = self.bodies.get(func_id) {
                for (loc_id, ty) in body.locals.iter() {
                    let _ = writeln!(out, "  local {:?}: {:?}", loc_id, ty);
                }
            }
        }
        out
    }
}
