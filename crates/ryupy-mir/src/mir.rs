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
    FloorDivF64,
    ModF64,
    PowI64,
    PowF64,
    F64ToI64,
    AbsI64,
    AbsF64,
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
    /// `PyCast.RangeStep`：动态步长拆箱为 i64，且为 0 时报 ValueError。
    RangeStep,
    /// `PyCast.*`：动态值拆箱到静态槽位。
    CastToI64,
    CastToF64,
    CastToBool,
    CastToStr,
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
    /// 构建期间的占位，验证阶段不允许残留。
    Unreachable,
}
