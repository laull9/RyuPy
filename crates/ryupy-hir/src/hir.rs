//! HIR 数据结构定义。只有数据，不含逻辑。

use la_arena::{Arena, Idx};
use ryupy_base::{FileId, TextRange};

pub type FuncId = Idx<Function>;
pub type ClassId = Idx<Class>;
pub type LocalId = Idx<Local>;
pub type ExprId = Idx<Expr>;
pub type StmtId = Idx<Stmt>;

/// 标识符。先用最朴素的 `Box<str>`，等 profile 证明有必要再换成字符串驻留（lasso）。
#[derive(Clone, Debug, PartialEq, Eq, Hash)]
pub struct Name(pub Box<str>);

impl Name {
    pub fn as_str(&self) -> &str {
        &self.0
    }
}

impl From<&str> for Name {
    fn from(s: &str) -> Self {
        Name(s.into())
    }
}

impl std::fmt::Display for Name {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        f.write_str(self.as_str())
    }
}

// ───────────────────────────── 模块级 ─────────────────────────────

#[derive(Debug)]
pub struct Module {
    pub file: FileId,
    pub functions: Arena<Function>,
    pub classes: Arena<Class>,
    /// 顶层语句组成的隐式入口函数。
    pub main: FuncId,
}

#[derive(Debug)]
pub struct Function {
    pub name: Name,
    pub kind: FunctionKind,
    pub params: Vec<Param>,
    pub ret: Option<TypeRef>,
    pub body: Body,
    pub range: TextRange,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum FunctionKind {
    /// 用户写的 `def`。
    User,
    /// 由顶层语句合成的入口。
    ModuleMain,
}

#[derive(Debug)]
pub struct Param {
    pub local: LocalId,
    pub annotation: Option<TypeRef>,
    pub range: TextRange,
}

/// Phase 1 只支持 `@dataclass`：字段固定、无方法继承。
#[derive(Debug)]
pub struct Class {
    pub name: Name,
    pub fields: Vec<Field>,
    /// `@dataclass(frozen=True)` → 编译为 .NET 值类型 struct。
    pub frozen: bool,
    pub range: TextRange,
}

#[derive(Debug)]
pub struct Field {
    pub name: Name,
    pub annotation: TypeRef,
    pub range: TextRange,
}

/// 源码中写出来的类型标注（尚未检查）。
#[derive(Clone, Debug, PartialEq)]
pub enum TypeRef {
    Int,
    Float,
    Bool,
    Str,
    None,
    /// 用户定义的类，名字已解析。
    Class(ClassId),
    /// 无法识别的标注；已经报过错，下游当作 `Error` 类型处理。
    Error,
}

// ───────────────────────────── 函数体 ─────────────────────────────

#[derive(Debug, Default)]
pub struct Body {
    pub locals: Arena<Local>,
    pub exprs: Arena<Expr>,
    pub stmts: Arena<Stmt>,
    /// 函数体顶层语句序列。
    pub root: Vec<StmtId>,
}

#[derive(Debug)]
pub struct Local {
    pub name: Name,
    /// `x: int = ...` 这种带标注的声明会填在这里。
    pub annotation: Option<TypeRef>,
    pub is_param: bool,
    /// 第一次出现的位置，用于诊断。
    pub range: TextRange,
}

#[derive(Clone, Debug)]
pub struct Expr {
    pub kind: ExprKind,
    pub range: TextRange,
}

#[derive(Clone, Debug)]
pub enum ExprKind {
    /// 错误恢复占位：对应的错误已经报过，下游静默跳过。
    Missing,
    Literal(Literal),
    Local(LocalId),
    Func(FuncId),
    Class(ClassId),
    Builtin(Builtin),
    Unary {
        op: UnaryOp,
        operand: ExprId,
    },
    Binary {
        op: BinOp,
        lhs: ExprId,
        rhs: ExprId,
    },
    /// 链式比较 `a < b < c` 在 lowering 时展开为 `a < b and b < c`
    /// （要求中间项无副作用，否则报「暂不支持」）。
    Compare {
        op: CmpOp,
        lhs: ExprId,
        rhs: ExprId,
    },
    /// 短路逻辑。注意 Python 的 `and/or` 返回操作数本身而不是 bool。
    BoolOp {
        op: BoolOp,
        lhs: ExprId,
        rhs: ExprId,
    },
    IfExp {
        cond: ExprId,
        then_: ExprId,
        else_: ExprId,
    },
    Call {
        callee: ExprId,
        args: Vec<ExprId>,
    },
    Attribute {
        base: ExprId,
        name: Name,
    },
}

#[derive(Clone, Debug, PartialEq)]
pub enum Literal {
    /// Python 整数。`i128` 是为了容纳 `-9223372036854775808`（`Neg(9223372036854775808)`
    /// 的字面量本身超出 `i64`）。超出 `i128` 的字面量仍然报错。
    Int(i128),
    Float(f64),
    Bool(bool),
    Str(Box<str>),
    None,
}

/// 编译器内建识别的名字。`range` 不在这里：它只能出现在 `for` 头部，lowering 时直接变成 `ForRange`。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Builtin {
    Print,
    Int,
    Float,
    Abs,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum UnaryOp {
    Neg,
    Pos,
    Not,
    Invert,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum BinOp {
    Add,
    Sub,
    Mul,
    /// `/`：结果总是 float。
    TrueDiv,
    /// `//`：向负无穷取整，与 CIL `div`（向零取整）不同！
    FloorDiv,
    /// `%`：结果符号跟随除数，与 CIL `rem` 不同！
    Mod,
    Pow,
    BitAnd,
    BitOr,
    BitXor,
    LShift,
    RShift,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum CmpOp {
    Eq,
    NotEq,
    Lt,
    LtE,
    Gt,
    GtE,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum BoolOp {
    And,
    Or,
}

#[derive(Debug)]
pub struct Stmt {
    pub kind: StmtKind,
    pub range: TextRange,
}

#[derive(Debug)]
pub enum StmtKind {
    Expr(ExprId),
    /// 普通赋值、带标注赋值、增量赋值（已展开）都落到这里。
    Assign {
        target: AssignTarget,
        value: ExprId,
    },
    Return(Option<ExprId>),
    If {
        cond: ExprId,
        then_: Vec<StmtId>,
        else_: Vec<StmtId>,
    },
    While {
        cond: ExprId,
        body: Vec<StmtId>,
    },
    /// `for var in range(start, stop, step)` 的专用形式，编译为原生计数循环，不产生迭代器对象。
    ForRange {
        var: LocalId,
        start: ExprId,
        stop: ExprId,
        step: Option<ExprId>,
        body: Vec<StmtId>,
    },
    Break,
    Continue,
    Pass,
}

#[derive(Debug)]
pub enum AssignTarget {
    Local(LocalId),
    /// `obj.field = value`（dataclass 字段写入）。
    Attribute {
        base: ExprId,
        name: Name,
    },
}
