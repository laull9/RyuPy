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
    /// 所有参数默认值表达式；它们的 `ExprId` 都位于 `main` 的 body 中。
    ///
    /// Python 在模块作用域、`def` 执行时求值默认值，因此它们不是任何用户函数体的一部分。
    /// 单独列出是为了让类型检查（在 main 上下文）覆盖它们。
    pub default_exprs: Vec<ExprId>,
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

/// 参数在调用绑定中的角色（对应 Python 的 `/` 与 `*` 分隔）。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum ParamKind {
    /// `/` 之前：只能按位置传入。
    PositionalOnly,
    /// 普通参数：位置或关键字都可以。
    PositionalOrKeyword,
    /// `*args`：收集多出来的位置实参。
    VarPositional,
    /// `*` 之后：只能按关键字传入。
    KeywordOnly,
    /// `**kwargs`：收集未知的关键字实参。
    VarKeyword,
}

#[derive(Debug)]
pub struct Param {
    pub local: LocalId,
    pub annotation: Option<TypeRef>,
    /// 默认值表达式。**该 `ExprId` 属于模块入口函数（main）的 body**：
    /// Python 在 `def` 执行时（模块作用域）求值默认值，而不是每次调用时求值。
    pub default: Option<ExprId>,
    pub kind: ParamKind,
    pub range: TextRange,
}

/// 调用实参：位置、`*args`、关键字、`**kwargs` 四种形态。
#[derive(Clone, Debug)]
pub enum CallArg {
    Positional(ExprId),
    Starred(ExprId),
    Keyword(Name, ExprId),
    DoubleStarred(ExprId),
}

impl CallArg {
    /// 该实参对应的表达式（关键字实参取右侧的值）。
    pub fn expr(&self) -> ExprId {
        match self {
            CallArg::Positional(id)
            | CallArg::Starred(id)
            | CallArg::Keyword(_, id)
            | CallArg::DoubleStarred(id) => *id,
        }
    }
}

/// 一个类定义。
///
/// 两类类共享同一结构：
/// - `@dataclass`：`fields` 有内容，构造走静态 `NewObject`，类体只允许字段标注；
/// - 普通 class（M2.5）：运行时由 `PyClass` / `PyInstance` 承载，类体按源码顺序
///   求值属性并登记方法。
#[derive(Debug)]
pub struct Class {
    pub name: Name,
    /// 单继承的基类；`None` 表示 `object`。
    pub base: Option<ClassBase>,
    pub fields: Vec<Field>,
    /// 类体按源码顺序执行：属性、方法、表达式语句。
    pub body: Vec<ClassItem>,
    /// `@dataclass(frozen=True)`：字段写入在编译期拒绝。
    pub frozen: bool,
    /// `@dataclass` 修饰的类。
    pub is_dataclass: bool,
    /// 是否为 `BaseException` 家族（内建或用户定义）——决定能否出现在 `except` 中。
    pub is_exception: bool,
    pub range: TextRange,
}

/// 单继承的基类表示。用户类指向已解析的 `ClassId`，内建基类保留异常类型名。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum ClassBase {
    User(ClassId),
    Builtin(&'static str),
}

#[derive(Debug)]
pub struct Field {
    pub name: Name,
    pub annotation: TypeRef,
    pub range: TextRange,
}

/// 类体里的一项，保留源码顺序（Python 的类体是顺序执行的）。
#[derive(Clone, Debug)]
pub struct ClassItem {
    pub kind: ClassItemKind,
    pub range: TextRange,
}

#[derive(Clone, Debug)]
pub enum ClassItemKind {
    /// `name = value` / `name: T = value`：写入类命名空间。
    Attr { name: Name, value: ExprId },
    /// 方法定义；`@property` / `@staticmethod` / `@classmethod` 在运行时应用。
    Method {
        func: FuncId,
        decorators: Vec<Decorator>,
    },
    /// 表达式语句（通常是 docstring 或类体里的副作用）。
    Expr(ExprId),
}

/// `def` 上方支持的类内修饰器。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Decorator {
    Property,
    StaticMethod,
    ClassMethod,
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
        args: Vec<CallArg>,
    },
    Attribute {
        base: ExprId,
        name: Name,
    },
    /// `[a, b]`；元素可以是 [`ExprKind::Starred`]。
    List(Vec<ExprId>),
    /// `(a, b)`；元素可以是 [`ExprKind::Starred`]。
    Tuple(Vec<ExprId>),
    /// `{a, b}`。
    Set(Vec<ExprId>),
    /// `{k: v}`；`**mapping` 暂不支持。
    Dict(Vec<(ExprId, ExprId)>),
    /// `b"..."`：源码里的字节串已经是字节序列，不需要再做编码。
    Bytes(Box<[u8]>),
    /// f-string：字面量与插值段交替。
    FString(Vec<FStringPart>),
    /// `obj[key]`，`key` 可以是 [`ExprKind::Slice`]。
    Subscript {
        base: ExprId,
        index: ExprId,
    },
    /// `a[start:stop:step]`，三段都可以省略（`None`）。
    Slice {
        start: Option<ExprId>,
        stop: Option<ExprId>,
        step: Option<ExprId>,
    },
    /// `*seq`：只出现在容器字面量与赋值目标里。
    Starred(ExprId),
}

/// f-string 的一段。
#[derive(Clone, Debug, PartialEq)]
pub enum FStringPart {
    /// 原样输出的字面量（`{{` / `}}` 已由解析器还原成单个花括号）。
    Literal(Box<str>),
    /// `{expr}` / `{expr!r}` / `{expr:spec}`。
    Value {
        value: ExprId,
        /// `!s` / `!r` / `!a` 的字节；没有转换时为 0。
        conversion: u8,
        /// 格式说明符；目前只支持纯字面量（含嵌套插值的规格暂不支持）。
        spec: Option<Box<str>>,
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
    /// 走运行时统一入口的内建函数（`len` / `sorted` / `list` …）。
    ///
    /// 它们的语义完全由运行时决定（容器协议），编译期不需要知道返回类型；
    /// 名字本身是编译期常量，运行时的 `PyOps.CallBuiltin` 按名字分派。
    Dynamic(&'static str),
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
    /// `in`：成员判断，走容器的 `__contains__`。
    In,
    /// `not in`。
    NotIn,
    /// `is`：引用身份。
    Is,
    /// `is not`。
    IsNot,
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
        /// `while ... else:`：循环**正常结束**（条件变假）时执行；`break` 跳过。
        orelse: Vec<StmtId>,
    },
    /// `for var in <可迭代对象>`：通用迭代协议。
    ForEach {
        var: LocalId,
        iter: ExprId,
        body: Vec<StmtId>,
        /// `for ... else:`：迭代耗尽时执行；`break` 跳过。
        orelse: Vec<StmtId>,
    },
    /// `for var in range(start, stop, step)` 的专用形式，编译为原生计数循环，不产生迭代器对象。
    ForRange {
        var: LocalId,
        start: ExprId,
        stop: ExprId,
        step: Option<ExprId>,
        body: Vec<StmtId>,
        /// `for ... else:`：迭代耗尽时执行；`break` 跳过。
        orelse: Vec<StmtId>,
    },
    Break,
    Continue,
    Pass,
    /// 内部节点：在模块入口的**定义位置**求值该函数的参数默认值。
    ///
    /// Python 在 `def` 执行时求值默认值；把它保留成一条语句（而不是统一提到开头），
    /// 默认值才能引用此前的模块变量。
    FunctionDefaults(FuncId),
    /// `raise [exc [from cause]]`；两者都为 `None` 表示裸重抛。
    Raise {
        value: Option<ExprId>,
        cause: Option<ExprId>,
    },
    /// `del a[i]` / `del a[x:y]`；目标是下标表达式。
    Delete(Vec<ExprId>),
    /// 顶层 `class` 定义。MIR 在**定义位置**创建类对象并顺序执行类体，
    /// 保证类属性求值、方法对象创建与模块语句的先后关系正确。
    ClassDef(ClassId),
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
    /// `except (TypeError, ValueError)` 展开成多个名字。用户自定义异常类在 M2.6
    /// 接入：类名与内建异常名一样作为 catch 的类型标识（codegen 会生成对应的
    /// CLR 异常子类，保持 CLR 与 Python 继承树同构）。
    pub types: Option<Vec<String>>,
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
    /// `obj[key] = value`。
    Subscript {
        base: ExprId,
        index: ExprId,
    },
    /// `a, b = ...` / `first, *middle, last = ...`。
    Sequence(Vec<SequenceItem>),
}

/// 序列解包的一个位置。
#[derive(Clone, Copy, Debug)]
pub enum SequenceItem {
    /// 普通目标：按位置取一个元素。
    One(LocalId),
    /// 星号目标：把中间剩余的元素打包成列表。
    Rest(LocalId),
}
