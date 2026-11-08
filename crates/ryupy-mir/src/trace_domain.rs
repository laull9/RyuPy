//! 生产 traceback 资格分析的有限值域与单调求解结果。
//!
//! 此结果只是 HIR 事实，不是生产协议证书。只有后续对最终 MIR 的来源、
//! 实参、恢复槽和运行时失败边全部验证后才能签发证书；本模块不选择协议。
use ryupy_hir::{ClassId, ExprId, FuncId, LocalId, ModuleVarId};
use rustc_hash::FxHashMap;
use std::collections::BTreeSet;

/// 每个候选集合的硬上限。溢出只变为未知，不能截断后继续证明。
pub const SET_LIMIT: usize = 32;

/// 分配点由函数与表达式身份组成，不使用源码名字、路径或行号。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub struct AllocationSite {
    pub function: FuncId,
    pub expression: ExprId,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Primitive {
    None,
    Bool,
    Integer,
    Float,
    String,
}

/// 内建能力来自已解析 HIR Builtin；字符串仅是该内建的固定 ABI 标识。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Atom {
    Primitive(Primitive),
    Function(FuncId),
    Class(ClassId),
    Instance(ClassId, AllocationSite),
    Generator(FuncId, AllocationSite),
    /// 编译器模块前言的协议中立元数据；不能作为可调用值或用户属性接收者。
    ModuleMetadata,
    Print,
    Next,
    ExceptionClass(&'static str),
    ExceptionInstance(&'static str),
}

/// 空集合是 Bottom（尚无正常返回），Unknown 是吸收元，二者不能混同。
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct Values {
    pub(crate) atoms: Vec<Atom>,
    pub(crate) unknown: bool,
}

impl Values {
    pub fn atoms(&self) -> &[Atom] {
        &self.atoms
    }

    pub fn is_unknown(&self) -> bool {
        self.unknown
    }

    pub(crate) fn one(atom: Atom) -> Self {
        Self { atoms: vec![atom], unknown: false }
    }

    pub(crate) fn unknown() -> Self {
        Self { atoms: Vec::new(), unknown: true }
    }

    /// 返回是否真正增长；未知传播后不保留可被误当作完整集合的前缀。
    pub(crate) fn join(&mut self, other: &Self) -> bool {
        if self.unknown {
            return false;
        }
        if other.unknown {
            *self = Self::unknown();
            return true;
        }
        let before = self.atoms.len();
        for atom in &other.atoms {
            if !self.atoms.contains(atom) {
                self.atoms.push(*atom);
            }
        }
        if self.atoms.len() > SET_LIMIT {
            *self = Self::unknown();
            return true;
        }
        self.atoms.len() != before
    }
}

#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct Summary {
    pub parameters: FxHashMap<LocalId, Values>,
    pub returns: Values,
    pub yields: Values,
    /// 包括源码 raise 和调用传播；暂不消去被 handler 捕获的错误，宁可拒绝。
    pub errors: BTreeSet<&'static str>,
    pub calls: Vec<FuncId>,
}

/// 尚未签发的分析事实。字段没有“可启用”布尔值，避免把 HIR 扫描当成证书。
#[derive(Clone, Debug, Default, PartialEq, Eq)]
pub struct Facts {
    pub functions: FxHashMap<FuncId, Summary>,
    pub expressions: FxHashMap<(FuncId, ExprId), Values>,
    pub locals: FxHashMap<(FuncId, LocalId), Values>,
    pub globals: FxHashMap<ModuleVarId, Values>,
    pub refusals: BTreeSet<String>,
    pub converged: bool,
}

/// 纯分析入口，不执行源码、不访问文件、不改变 Legacy/default 路由。
pub fn analyze(module: &ryupy_hir::Module) -> Facts {
    super::trace_domain_transfer::analyze(module)
}
