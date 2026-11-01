//! MIR 数据结构定义。

use la_arena::{Arena, Idx};

pub type FnId = Idx<Function>;
pub type ClassDefId = Idx<ClassDef>;
pub type LocalId = Idx<LocalDecl>;
pub type BlockId = Idx<BasicBlock>;
pub type GlobalId = Idx<GlobalDecl>;
pub type ModuleId = Idx<ModuleDef>;

/// 模块级静态槽位。
///
/// 目前只用于函数对象、参数默认值数组与参数元数据：它们必须在多次调用之间保持同一份
/// 引用（`def f(x=[])` 的列表要在调用间共享）。M2.7 的模块变量会复用同一套机制。
#[derive(Debug)]
pub struct GlobalDecl {
    pub name: String,
    pub ty: MirType,
}

/// 单个模块的编译结果。
///
/// 多文件程序由 driver 逐模块调用 [`crate::build::build_program`]，再用
/// [`crate::link::link`] 合并成 [`LinkedProgram`]。合并时重新分配 ID 与名字，
/// 因此各模块内部的 arena 编号互不影响。
#[derive(Debug)]
pub struct Program {
    pub functions: Arena<Function>,
    pub classes: Arena<ClassDef>,
    /// 模块级静态字段（函数对象、默认值数组、模块变量）。
    pub globals: Arena<GlobalDecl>,
    /// 模块入口函数（顶层语句）。
    pub entry: FnId,
    /// 源文件名，入口包装器用它设置 traceback 的文件名。
    pub source_name: String,
}

/// 合并后的程序：所有可达模块 + 入口模块。
#[derive(Debug)]
pub struct LinkedProgram {
    pub functions: Arena<Function>,
    pub classes: Arena<ClassDef>,
    pub globals: Arena<GlobalDecl>,
    pub modules: Arena<ModuleDef>,
    pub entry: ModuleId,
}

/// 一个已编译模块在运行时需要的信息。
#[derive(Debug)]
pub struct ModuleDef {
    /// 跨模块属性写入必须拒绝的静态异常导入别名。
    pub protected_exports: Vec<String>,
    /// 点分绝对模块名；入口模块是 `__main__`。
    pub name: String,
    /// `__package__`。
    pub package: String,
    /// `__file__`。
    pub file: String,
    /// 模块初始化函数（顶层语句）。
    pub init: FnId,
    /// 模块对象暴露的名字 → 静态槽位（模块变量、顶层函数、顶层类）。
    pub exports: Vec<ModuleExport>,
}

/// 导出读取的显式绑定状态协议；后端不得依据 Python 名称或 CLR 默认值猜测。
#[derive(Debug, Clone)]
pub enum ExportGuard {
    None,
    ValueSentinel,
    BoundFlag(GlobalId),
}

/// 一个公开名字及其存储、绑定状态。
#[derive(Debug, Clone)]
pub struct ModuleExport {
    pub name: String,
    pub value: GlobalId,
    pub guard: ExportGuard,
}

#[derive(Debug)]
pub struct ClassDef {
    /// Python 可见的类名。
    pub name: String,
    /// 生成的 CLR 类型名（多模块合并时加前缀，避免同名类冲突）。
    pub codegen_name: String,
    pub fields: Vec<(String, MirType)>,
    /// `@dataclass` 走静态字段布局；普通 class 由运行时 `PyClass` 承载，不生成 CLR 类型。
    ///
    /// ADR-0009 之后 `frozen=True` 也不再使用 .NET 值类型：值拷贝会破坏 Python 的
    /// 对象身份，因此 `is_value_type` 保留字段但当前恒为 `false`。
    pub is_value_type: bool,
    pub is_dataclass: bool,
    /// 用户类继承链（单继承）；`None` 表示直接继承 `object`。
    pub base: Option<ClassDefId>,
    /// 继承的内建异常名（如 `ValueError`）；与 [`Self::is_exception`] 一起决定
    /// codegen 生成的 CLR 基类。
    pub builtin_base: Option<String>,
    pub is_exception: bool,
    /// 异常类的 CLR 工厂委托所在静态槽位。
    pub factory_global: Option<GlobalId>,
}

/// 参数在调用绑定中的角色，与 HIR 的 `ParamKind` 一一对应。
///
/// codegen 只依赖 MIR，因此不能直接复用 HIR 的枚举；这里的判别值会写进运行时
/// `PySignature`，**修改必须与 C# 侧 `PyParamKind` 同步**。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum ParamKind {
    PositionalOnly = 0,
    PositionalOrKeyword = 1,
    VarPositional = 2,
    KeywordOnly = 3,
    VarKeyword = 4,
}

/// 一个 Python 参数的元数据（名字、绑定种类、默认值、槽位类型）。
#[derive(Debug, Clone)]
pub struct ParamMeta {
    pub name: String,
    pub kind: ParamKind,
    pub has_default: bool,
    pub ty: MirType,
}

#[derive(Debug)]
pub struct Function {
    /// Python 可见的函数名（`PyFunction.__name__`）。
    pub name: String,
    /// 生成到程序集里的唯一方法名（多模块合并时由 link 加前缀）。
    pub codegen_name: String,
    /// 该函数所属源文件，用于 traceback 的 `File "..."`。
    pub source_file: String,
    /// 参数也是局部变量，按顺序对应 CIL 的 `ldarg.N`。
    ///
    /// 闭包函数（M3.0）在此之外还有 `env` 形参（见 [`Function::env`]），
    /// 排在所有 Python 参数之后。
    pub params: Vec<LocalId>,
    pub ret: MirType,
    pub body: Body,
    /// 参数元数据，顺序与 `params` 一致。
    pub param_meta: Vec<ParamMeta>,
    /// 该函数的 `PyFunction` 对象所在的模块静态槽位（`ModuleMain` 为 `None`）。
    pub function_global: Option<GlobalId>,
    /// 特化身份锚点：初始化时保存原函数对象，不导出且禁止Python/MIR写入。
    pub specialization_identity_global: Option<GlobalId>,
    /// 默认值数组（`object[]`）的槽位；没有默认值时为 `None`。
    pub defaults_global: Option<GlobalId>,
    /// `PySignature` 的槽位，供生成的调用入口（thunk）使用。
    pub signature_global: Option<GlobalId>,
    /// 闭包环境形参（M3.0）：引用外层 cell 的函数多一个 `object[]` 参数，
    /// 每个元素是外层作用域的一个 cell（`object[1]`）。非闭包为 `None`。
    pub env: Option<LocalId>,
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
    /// 仅编译器私有局部使用的双通道浮点向量，不进入Python值ABI。
    V128F64,
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
    /// cell 变量（M3.0）：槽位里存的是 `object[1]`（PyOps.CellNew 创建），
    /// 读写经过 `CellGet` / `CellSet`，多个闭包共享同一个数组。
    pub is_cell: bool,
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
    /// 写模块级静态槽位（类对象在 `class` 语句位置创建后登记）。
    SetGlobal {
        dest: GlobalId,
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
    /// 模块级静态字段。
    Global(GlobalId),
    /// 内联构造的 `object[]`（调用实参、关键字名字与值）。
    Array(Vec<Operand>),
}

#[derive(Clone, Debug, PartialEq)]
pub enum Constant {
    I64(i64),
    F64(f64),
    Bool(bool),
    Str(String),
    /// 超出 i64 的整数字面量，以十进制字符串携带，构造时由运行时转 BigInteger。
    BigInt(String),
    /// CIL `null` 引用：未绑定槽/内部缓存哨兵/占位；不是Python None单例。
    Null,
    /// Python `None`（运行时单例）。
    None,
}

#[derive(Debug)]
pub enum Rvalue {
    /// 生成器降级前的委托推进；降级时补帧参数，返回是否产出。
    GeneratorDelegateStep {
        delegate: Operand,
    },
    /// 创建持有帧与静态恢复委托的生成器，构造时不执行函数体。
    MakeGenerator {
        resume: FnId,
        frame: Vec<Operand>,
    },
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
    /// 运行平台是否支持硬件128位向量；只用于选择保留原顺序的快路。
    IsVector128Supported,
    /// 按低/高通道打包，不改变通道内部算术顺序。
    VectorPackF64 {
        low: Operand,
        high: Operand,
    },
    /// 双通道独立运算；仅Add/Sub/Mul，不含归约或融合乘加。
    VectorBinaryF64 {
        op: BinOp,
        lhs: Operand,
        rhs: Operand,
    },
    /// 固定通道读取（0或1），结果仍为原生F64。
    VectorLaneF64 {
        value: Operand,
        lane: u8,
    },
    /// 纯CLR精确boxed Double测试；不包含整数/bool或用户数值转换。
    IsBoxedF64 {
        value: Operand,
    },
    /// 已被精确Double事实证明的动态局部拆箱；不调用PyCast。
    UnboxExactF64 {
        value: Operand,
    },
    /// 纯CLR精确boxed Int64测试；不包含Python bool/大整数转换。
    IsBoxedI64 {
        value: Operand,
    },
    /// 仅在迭代类型事实证明为boxed Int64的边上拆箱。
    UnboxExactI64 {
        value: Operand,
    },
    /// 动态二元运算：`PyPic.Binary(site, op, a, b)`。
    DynBinary {
        op: DynOp,
        lhs: Operand,
        rhs: Operand,
        /// 增量赋值（`x op= y`）：运行时对可变对象原地修改，见 [`DynOp::INPLACE_FLAG`]。
        inplace: bool,
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
    /// 动态位置调用：`callee(arg0, arg1, ...)`，没有关键字、`*`、`**`。
    ///
    /// 参数个数在编译期已知且较少（≤ 3），codegen 可以走 `PyPic.CallValueN`，
    /// 由运行时复用调用点数组，避免每次调用分配 `object[]`。语义与
    /// `CallValue(callee, [args], [], [])` 完全相同。
    DynCallValue {
        callee: Operand,
        args: Vec<Operand>,
    },
    /// 动态方法位置调用：`obj.name(arg0, ...)`，没有关键字与展开实参。
    DynCallMethod {
        obj: Operand,
        name: String,
        args: Vec<Operand>,
    },
    /// 创建闭包函数对象（M3.0）：`new PyFunction(...)`，携带 env（外层 cell 数组）
    /// 与此时求值的默认值数组。结果是装箱的 `PyFunction` 对象。
    ///
    /// 嵌套函数 / lambda 都由它创建：每次执行 def / lambda 表达式都会产生新的
    /// 函数对象，默认值也重新求值（与 CPython 一致）。
    MakeFunction {
        /// 被创建的函数。
        func: FnId,
        /// 闭包环境：被创建函数 `ClosureInfo::free` 顺序的 cell 对象。
        env: Vec<Operand>,
        /// 默认值数组内容（顺序与参数一致，无默认值的槽位不会生成元素）。
        defaults: Vec<Operand>,
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
    /// 增量赋值的操作码标记位：`code() | INPLACE_FLAG`。**与 `PyOps.InPlaceFlag` 同步。**
    pub const INPLACE_FLAG: i32 = 0x100;

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
    /// i64模2^64加法；溢出判定/动态回退必须由MIR显式构造。
    WrappingAdd,
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

pub use crate::runtime_fn::{EXCEPTION_TYPE_NAMES, RuntimeFn};

#[derive(Debug)]
pub enum Terminator {
    /// 暂停边，仅在生成器降级前存在；恢复时从 resume 继续。
    Suspend {
        value: Operand,
        resume: BlockId,
        /// 恢复后接收 send 的局部；语句型 yield 可忽略。
        input: Option<LocalId>,
        /// 委托恢复由DelegateStep消费send/throw，不能预先GeneratorInput。
        delegated: bool,
    },
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
