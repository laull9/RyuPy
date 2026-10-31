//! MIR 数据结构定义。

use la_arena::{Arena, Idx};

pub type FnId = Idx<Function>;
pub type ClassDefId = Idx<ClassDef>;
pub type LocalId = Idx<LocalDecl>;
pub type BlockId = Idx<BasicBlock>;

#[derive(Debug)]
pub struct Program {
    pub functions: Arena<Function>,
    pub classes: Arena<ClassDef>,
    /// 编译为 CIL `Main` 的入口函数。
    pub entry: FnId,
    /// 源文件名，入口包装器用它设置 traceback 的文件名。
    pub source_name: String,
}

#[derive(Debug)]
pub struct ClassDef {
    pub name: String,
    pub fields: Vec<(String, MirType)>,
    /// true → .NET 值类型（struct），false → 引用类型（class）。
    pub is_value_type: bool,
}

#[derive(Debug)]
pub struct Function {
    pub name: String,
    /// 参数也是局部变量，按顺序对应 CIL 的 `ldarg.N`。
    pub params: Vec<LocalId>,
    pub ret: MirType,
    pub body: Body,
}

#[derive(Debug)]
pub struct Body {
    pub locals: Arena<LocalDecl>,
    pub blocks: Arena<BasicBlock>,
    pub entry: BlockId,
    /// 异常区域（M2.2）。区域彼此嵌套或不相交，靠「块下标区间」描述受保护代码。
    pub regions: Vec<Region>,
}

/// 一个受保护的代码区域，对应 CIL 异常表里的一条或多条记录。
///
/// 为什么用「块下标区间」而不是块集合：codegen 按 arena 顺序发射指令，构建器保证
/// 区域内的块是**连续分配**的，于是「块区间」与「指令区间」一一对应，异常表只需要
/// 首尾两条指令的位置。
#[derive(Debug)]
pub struct Region {
    pub kind: RegionKind,
    /// 受保护区间首块。
    pub try_start: BlockId,
    /// 受保护区间块数。
    pub try_len: usize,
    /// 处理入口块（catch 分支或 finally 的异常通道）。
    pub handler: BlockId,
    /// 处理入口里保存异常对象的局部变量；入口首条语句从求值栈上取下异常对象。
    pub exc_local: Option<LocalId>,
}

/// 异常区域的种类。
#[derive(Debug)]
pub enum RegionKind {
    /// `except TypeError:` / `except (TypeError, ValueError):`。
    ///
    /// 一种类型一条 CIL catch 记录；CLR 按「类型及其基类」匹配，正好等于 Python 的
    /// `except` 语义（前提是 CLR 继承树与 Python 继承树同构，见 `PyExceptionTypes`）。
    Catch { type_names: Vec<String> },
    /// 裸 `except:`：捕获所有 `PyBaseException`。
    CatchAll,
    /// `finally` 的异常通道：捕获所有 `PyBaseException`，把异常记进状态后跳进 finally 体。
    ///
    /// Python 的 `finally` 必须覆盖「return / break / continue / 异常」四类离开方式，
    /// 而 CIL 的 finally 处理器不能抑制异常，也不能带状态离开。所以这里不用 CLR 的
    /// finally，改成「catch-all + 状态机 + 分派块」：所有离开路径先写状态再跳进 finally 体，
    /// finally 体执行完由分派块决定下一步。
    FinallyGuard,
}

/// 动静双轨的类型表示：决定生成原生 CIL 指令还是 `PyValue` 操作。
#[derive(Clone, Debug, PartialEq, Eq)]
pub enum MirType {
    I64,
    F64,
    Bool,
    Str,
    /// `None` / 无返回值，对应 CIL `void`（作为局部变量类型时不分配槽位）。
    Unit,
    Class(ClassDefId),
    /// 动态值：CIL `object`，承载装箱的 `int` / `float` / `bool` / `str` / `None`
    /// （[ADR-0010](../../../docs/adr/0010-dynamic-value-representation.md)）。
    Dynamic,
    // Phase 4：NdArrayView { elem: Box<MirType>, ndim: u8 }
}

#[derive(Debug)]
pub struct LocalDecl {
    pub ty: MirType,
    /// 用户变量保留名字（调试与快照用），编译器临时变量为 `None`。
    pub name: Option<String>,
    // Phase 3：is_captured —— 由闭包转换 Pass 提升到 DisplayClass 字段。
}

#[derive(Debug)]
pub struct BasicBlock {
    pub stmts: Vec<Statement>,
    pub terminator: Terminator,
}

#[derive(Debug)]
pub enum Statement {
    Assign {
        dest: LocalId,
        value: Rvalue,
    },
    /// 只为副作用求值（例如调用 `print`）。
    Eval(Rvalue),
    SetField {
        obj: Operand,
        field: u32,
        value: Operand,
    },
    /// 记录当前行号：traceback 用它定位抛出点。
    ///
    /// 只写一个静态字段，不产生值；放在语句流里而不是终结符上，是因为同一块里
    /// 连续多条语句要能各自报告自己的行号。
    Line(u32),
}

#[derive(Clone, Debug)]
pub enum Operand {
    Local(LocalId),
    Const(Constant),
}

#[derive(Clone, Debug, PartialEq)]
pub enum Constant {
    I64(i64),
    F64(f64),
    Bool(bool),
    Str(String),
    /// 超出 i64 的整数字面量，以十进制字符串携带，构造时由运行时转 BigInteger。
    BigInt(String),
    /// Python `None`（运行时单例）。
    None,
}

#[derive(Debug)]
pub enum Rvalue {
    Use(Operand),
    /// 原生无装箱运算，操作数类型必须相同（构建时已插入 `Cast`）。
    Binary {
        op: BinOp,
        lhs: Operand,
        rhs: Operand,
    },
    Unary {
        op: UnaryOp,
        operand: Operand,
    },
    Cast {
        kind: CastKind,
        operand: Operand,
    },
    Call {
        callee: Callee,
        args: Vec<Operand>,
    },
    NewObject {
        class: ClassDefId,
        args: Vec<Operand>,
    },
    GetField {
        obj: Operand,
        field: u32,
    },
    /// 静态值装箱成动态值（CIL `box`）。
    Box {
        value: Operand,
        from: MirType,
    },
    /// 动态值拆箱到静态槽位（调用 `PyCast.*`，类型不符抛 Python `TypeError`）。
    Unbox {
        value: Operand,
        to: MirType,
    },
    /// 动态二元运算：`PyOps.Binary(op, a, b)`。
    DynBinary {
        op: DynOp,
        lhs: Operand,
        rhs: Operand,
    },
    /// 动态比较：`PyOps.Compare(op, a, b)`，结果是 CIL `bool`。
    DynCompare {
        op: DynOp,
        lhs: Operand,
        rhs: Operand,
    },
    /// 动态一元运算：`PyOps.Unary(op, a)`。
    DynUnary {
        op: DynOp,
        operand: Operand,
    },
    /// 动态真值判定：`PyOps.Truthy(value)`，结果是 CIL `bool`。
    DynTruthy {
        value: Operand,
    },
    /// 带「前缀操作数 + 数组实参」的运行时调用。
    ///
    /// 容器构造、方法调用、内建调用都长这样：若干固定位置的标量参数 + 一个 `object[]`。
    /// 单独一个变体而不是给每种调用加一条指令，是为了让 codegen 的数组发射只写一份。
    CallWithArgs {
        callee: RuntimeFn,
        prefix: Vec<Operand>,
        args: Vec<Operand>,
    },
    /// 异常处理入口处的「捕获到的异常对象」。
    ///
    /// CIL 在把控制权交给 catch 子句时会把异常对象压栈，所以这条 rvalue 本身不发射
    /// 任何取值指令，只承接 `stloc`——它必须出现在处理入口块的**第一条**语句。
    CatchException,
}

/// 动态运算的操作码。数值必须与 C# 侧 `PyOps` 的常量一致（ADR-0010 的 ABI）。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum DynOp {
    Add,
    Sub,
    Mul,
    TrueDiv,
    FloorDiv,
    Mod,
    Pow,
    BitAnd,
    BitOr,
    BitXor,
    LShift,
    RShift,
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,
    Neg,
    Pos,
    Not,
    Invert,
}

impl DynOp {
    /// 运行时操作码编号。**修改必须与 `runtime/RyuPy.Runtime/PyOps.cs` 同步。**
    pub fn code(self) -> i32 {
        match self {
            DynOp::Add => 1,
            DynOp::Sub => 2,
            DynOp::Mul => 3,
            DynOp::TrueDiv => 4,
            DynOp::FloorDiv => 5,
            DynOp::Mod => 6,
            DynOp::Pow => 7,
            DynOp::BitAnd => 8,
            DynOp::BitOr => 9,
            DynOp::BitXor => 10,
            DynOp::LShift => 11,
            DynOp::RShift => 12,
            // 比较与一元运算的编号各自从 1 开始，由调用的运行时方法区分
            DynOp::Eq => 1,
            DynOp::Ne => 2,
            DynOp::Lt => 3,
            DynOp::Le => 4,
            DynOp::Gt => 5,
            DynOp::Ge => 6,
            DynOp::Neg => 1,
            DynOp::Pos => 2,
            DynOp::Not => 3,
            DynOp::Invert => 4,
        }
    }
}

/// 已经是 CIL 层面的语义：`Div` 对整数是向零截断。Python 的 `//` 和 `%` 在构建 MIR 时
/// 就已改写成 [`RuntimeFn`] 调用，不会出现在这里。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum BinOp {
    Add,
    Sub,
    Mul,
    Div,
    BitAnd,
    BitOr,
    BitXor,
    Shl,
    Shr,
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum UnaryOp {
    Neg,
    Not,
    BitNot,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum CastKind {
    I64ToF64,
    /// `int(x)`：向零截断，NaN/Inf 需抛 Python 异常（由运行时处理）。
    F64ToI64,
    BoolToI64,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Callee {
    Fn(FnId),
    Runtime(RuntimeFn),
}

/// 编译器认识的 Python 内建异常类型名。
///
/// 这张表是三方约定的单一来源：HIR 的名字解析、运行时的 `PyExceptionTypes`、
/// codegen 的异常类型引用表都按它对齐。新增异常类型时三处必须同时修改。
pub const EXCEPTION_TYPE_NAMES: &[&str] = &[
    "BaseException",
    "Exception",
    "SystemExit",
    "KeyboardInterrupt",
    "ArithmeticError",
    "ZeroDivisionError",
    "OverflowError",
    "LookupError",
    "IndexError",
    "KeyError",
    "ValueError",
    "UnicodeError",
    "UnicodeDecodeError",
    "UnicodeEncodeError",
    "TypeError",
    "NameError",
    "UnboundLocalError",
    "AttributeError",
    "RuntimeError",
    "NotImplementedError",
    "RecursionError",
    "AssertionError",
    "ImportError",
    "ModuleNotFoundError",
    "OSError",
    "FileNotFoundError",
    "FileExistsError",
    "PermissionError",
    "StopIteration",
    "StopAsyncIteration",
    "EOFError",
];

/// `RyuPy.Runtime` 中的静态方法。新增条目时同步修改 C# 侧与 codegen 的引用表。
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub enum RuntimeFn {
    // print(a, b) → WriteI64(a); WriteSep(); WriteF64(b); WriteNewline()
    WriteI64,
    WriteF64,
    WriteBool,
    WriteStr,
    WriteSep,
    WriteNewline,
    StrConcat,
    FloorDivI64,
    ModI64,
    TrueDivF64,
    FloorDivF64,
    ModF64,
    PowI64,
    PowF64,
    F64ToI64,
    AbsI64,
    AbsF64,
    /// `PyMath.ShiftLeftI64(long, long)`：溢出转 `OverflowError`（CIL 的 shl 会取模）。
    ShlI64,
    /// `PyMath.ShiftRightI64(long, long)`：处理移位量 ≥ 64。
    ShrI64,
    /// `print` 动态值（等价 `print(str(v))`）。
    WriteObject,
    /// `PyOps.Binary(int, object, object)`。
    DynBinary,
    /// `PyOps.Compare(int, object, object) -> bool`。
    DynCompare,
    /// `PyOps.Unary(int, object)`。
    DynUnary,
    /// `PyOps.Truthy(object) -> bool`。
    DynTruthy,
    /// `PyOps.None()`。
    DynNone,
    /// `PyOps.FromDecimalString(string)`。
    DynFromDecimal,
    /// `PyOps.ToIntValue` / `ToFloatValue` / `AbsValue`。
    DynToInt,
    DynToFloat,
    DynAbs,
    /// `PyOps.MakeException(string, object)`：构造异常对象。
    MakeException,
    /// `PyOps.WithCause(object, object)`：给异常挂显式原因。
    WithCause,
    /// `PyCast.RangeStep`：动态步长拆箱为 i64，且为 0 时报 ValueError。
    RangeStep,
    /// `PyCast.*`：动态值拆箱到静态槽位。
    CastToI64,
    CastToF64,
    CastToBool,
    CastToStr,
    /// `PyErrors.EnterHandler(object) -> object`：进入 except 体，返回上一层当前异常。
    HandlerEnter,
    /// `PyErrors.LeaveHandler(object)`：离开 except 体，恢复上一层当前异常。
    HandlerLeave,
    /// `PyErrors.Reraise() -> object`：裸 `raise`。
    Reraise,
    /// `PyErrors.PrepareRaise(object) -> object`：抛前准备（挂上下文 + 记栈帧）。
    RaisePrepare,
    /// `PyRepr.StrValue(object) -> string`：`str(x)`。
    DynStr,
    /// `PyObject.TypeOf(object) -> object`：`type(x)`。
    DynType,
    /// `PyObject.GetAttr(object, string) -> object`：动态属性读取。
    DynGetAttr,
    /// `PyErrors.Load(object, string) -> object`：读取可能未赋值的局部变量。
    LoadChecked,
    /// `PyErrors.Unbound() -> object`：把变量标记为未赋值。
    MakeUnbound,
    /// `PyTraceback.PushFrame(string, string) -> long`：压入栈帧并返回深度。
    TracePush,
    /// `PyTraceback.SetLine(long)`：更新当前行号。
    TraceLine,
    /// `PyTraceback.PopTo(long)`：弹栈到指定深度。
    TracePopTo,
    /// `PyTraceback.Attach(object)`：把当前栈快照挂到异常上。
    TraceAttach,
    // ── 容器协议（M2.3）──
    /// `PyOps.MakeList(object[])` / `MakeTuple` / `MakeSet`。
    MakeList,
    MakeTuple,
    MakeSet,
    /// `PyOps.MakeDict()`。
    MakeDict,
    /// `PyOps.MakeSlice(object, object, object)`。
    MakeSlice,
    /// `PyOps.MakeBytes(string)`。
    MakeBytes,
    /// `PyOps.GetItem(object, object)`。
    GetItem,
    /// `PyOps.SetItem(object, object, object)`。
    SetItem,
    /// `PyOps.DelItem(object, object)`。
    DelItem,
    /// `PyOps.Contains(object, object) -> bool`。
    Contains,
    /// `PyOps.Len(object) -> long`。
    Len,
    /// `PyOps.Is(object, object) -> bool`。
    IsSame,
    /// `PyOps.ReprValue(object) -> string`。
    ReprValue,
    /// `PyOps.CallBuiltin(string, object[])`。
    DynBuiltin,
    /// `PyOps.CallMethod(object, string, object[])`。
    CallMethod,
    /// `PyOps.ListAppend(list, item)`：星号字面量的增量构建。
    ListAppend,
    /// `PyOps.ListExtend(list, seq)`。
    ListExtend,
    /// `PyOps.UnpackCheck(long, long, bool)`：解包数量不符时抛 ValueError（bool 表示有星号）。
    UnpackCheck,
    /// `PyOps.IterStart(object) -> object`：建立迭代器。
    IterStart,
    /// `PyOps.IterNextOk(object) -> bool`：推进迭代器。
    IterNextOk,
    /// `PyOps.IterValue(object) -> object`：取当前元素。
    IterValue,
}

#[derive(Debug)]
pub enum Terminator {
    Goto(BlockId),
    Branch {
        cond: Operand,
        then_: BlockId,
        else_: BlockId,
    },
    Return(Option<Operand>),
    /// `raise exc [from cause]`：抛出后当前块结束。
    Raise {
        value: Option<Operand>,
        cause: Option<Operand>,
    },
    /// 构建期间的占位，验证阶段不允许残留。
    Unreachable,
}
