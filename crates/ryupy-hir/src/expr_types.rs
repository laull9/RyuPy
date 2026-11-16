//! HIR表达式与运算符的数据定义；只保存解析后的语义节点，不执行降级或类型推断。
use crate::hir::{CallArg, ClassId, ExprId, FuncId, LocalId, ModuleVarId, Name};
use ryupy_base::TextRange;

#[derive(Clone, Debug)]
pub struct Expr {
    pub kind: ExprKind,
    pub range: TextRange,
}

#[derive(Clone, Debug)]
pub enum ExprKind {
    /// 内层类体读取外层定义的隐式cell，不是当前类namespace或模块槽。
    ClassCellRead(ClassId),
    /// 类体读取定义者普通局部cell，未绑定按free的NameError而非LOAD_FAST。
    ClassFreeLocal(LocalId),
    /// 类体定义位置读取先前类命名空间绑定；不对descriptor做实例绑定。
    ClassLocal(Name),
    /// 条件可能绑定的类名字；仅命名空间缺值时求 fallback。
    ClassLookup {
        name: Name,
        fallback: ExprId,
    },
    /// 暂停并在恢复后得到 send 的值；next 对应 None。
    Yield(Option<ExprId>),
    YieldFrom(ExprId),
    /// await 明确保留，不能按普通调用或 yield-from 源码语义处理。
    Await {
        value: ExprId,
        context: crate::AwaitContext,
    },
    /// 合成上下文退出参数读取真实异常传播链，跳过用户属性覆盖。
    ExceptionTraceback(ExprId),
    /// 异步迭代的中性协议操作；入口同步求值，下一项随后由显式await恢复。
    AsyncIteration {
        value: ExprId,
        op: AsyncIterationOp,
    },
    /// 错误恢复占位：对应的错误已经报过，下游静默跳过。
    Missing,
    /// 降级期临时名称，模块完成前必须统一登记并替换为 ModuleVar。
    UnresolvedGlobal(Name),
    /// 编译器生成的模块初始化读取，不是可遮蔽的Python调用。
    ModuleFile(Name),
    /// 本次导入的初始元数据，只在模块前言读取一次。
    ModuleMetadata(Name, Name),
    Literal(Literal),
    Local(LocalId),
    /// 读取模块级变量（M2.7）。
    ModuleVar(ModuleVarId),
    /// 读取外层函数的 cell（M3.0 闭包）：下标对应当前函数 `ClosureInfo::free`。
    Free(usize),
    /// lambda 表达式（M3.0）：在求值位置创建匿名函数对象。
    Lambda(FuncId),
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
        /// 来自增量赋值 `x op= y`：可变对象（list / bytearray / set / dict / 带 `__iop__` 的实例）
        /// 要原地修改并保留身份，不可变值与普通二元运算相同。
        inplace: bool,
    },
    /// 单个富比较；返回值由运行时对象协议决定。
    Compare {
        op: CmpOp,
        lhs: ExprId,
        rhs: ExprId,
    },
    /// 每个操作数只求值一次；中间比较按真值短路，最终保留原始比较结果。
    CompareChain {
        operands: Vec<ExprId>,
        ops: Vec<CmpOp>,
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
    /// 实参会暂停：调用目标必须先绑定并保存。
    PreparedCall {
        callee: ExprId,
        args: Vec<CallArg>,
    },
    /// 隐式上下文协议：按类型绑定方法，绕过实例属性。
    ContextMethod {
        base: ExprId,
        name: Name,
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
    /// 的字面量本身超出 `i64`）。当前Small路径来自Ruff的u64表示。
    Int(i128),
    /// 超u64的正整数字面量，已在容量内正规化为有效十进制字符串。
    BigInt(Box<str>),
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

pub use crate::comparisons::CmpOp;

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum BoolOp {
    And,
    Or,
}

/// 槽查找和内建耗尽身份不依赖源码名字绑定。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum AsyncIterationOp {
    Start,
    Next,
    IsStop,
}
