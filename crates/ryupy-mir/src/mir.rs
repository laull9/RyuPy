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
    // Phase 2：Dynamic —— RyuPy.Runtime.PyValue
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
    // Phase 2：
    // DynamicBinary { site: u32, op: BinOp, lhs, rhs } —— PyOps.Add(ref site, a, b)
    // BoxToPyValue { src, src_ty } / UnboxFromPyValue { src, target_ty }
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
