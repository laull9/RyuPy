//! HIR 数据结构定义。只有数据，不含逻辑。

use la_arena::{Arena, Idx};
use ryupy_base::{FileId, TextRange};

pub type FuncId = Idx<Function>;
pub type ClassId = Idx<Class>;
pub type LocalId = Idx<Local>;
pub type ExprId = Idx<Expr>;
pub type StmtId = Idx<Stmt>;
pub type HandlerId = Idx<Handler>;

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
    /// 源文件名。traceback 的 `File "..."` 与诊断都要用它，由 driver 提供。
    pub source_name: String,
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
    /// `except` 分支：与语句分开存放，避免 `StmtKind` 过大。
    pub handlers: Arena<Handler>,
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
    /// `str(x)`：动态路径走 `PyRepr.StrValue`。
    Str,
    /// `type(x)`：返回类型对象（最小实现，只有 `__name__`）。
    Type,
    /// Python 异常类（`ValueError` 等）。构造调用走运行时的 `PyOps.MakeException`。
    Exception(&'static str),
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
    /// 语句起始行号（1 起）。traceback 直接用它，MIR 不需要再回查源码。
    pub line: u32,
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
    /// `raise [exc [from cause]]`；两者都为 `None` 表示裸重抛。
    Raise {
        value: Option<ExprId>,
        cause: Option<ExprId>,
    },
    /// `try` 语句：`body` / `handlers` / `else` / `finally` 四部分，任意一部分可以为空。
    ///
    /// HIR 保留 Python 的四段结构，不提前去糖成「嵌套 try」：MIR 需要知道
    /// 「哪些块属于受保护区域」「哪些块属于 else」「finally 要包住哪些部分」，
    /// 这些信息一旦被去糖就失去了。
    Try {
        body: Vec<StmtId>,
        handlers: Vec<HandlerId>,
        orelse: Vec<StmtId>,
        finalbody: Vec<StmtId>,
    },
}

/// `except` 分支。独立分配在 [`Body::handlers`] 里，`Try` 只持有 ID。
#[derive(Debug)]
pub struct Handler {
    /// 匹配的异常类型名；`None` 表示裸 `except:`。
    ///
    /// 之所以在 HIR 就把 `except ValueError` 解析成名字而不是保留表达式：
    /// 匹配交给 CIL 的类型化 catch，需要的是**类型**而不是运行时可调用对象。
    /// `except (TypeError, ValueError)` 展开成多个名字。用户自定义异常类
    /// （以及任何非字面量类型表达式）留到 M2.6，届时改成按运行时类型匹配。
    pub types: Option<Vec<&'static str>>,
    /// `as name` 绑定的局部变量。
    pub name: Option<LocalId>,
    pub body: Vec<StmtId>,
    pub range: TextRange,
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
