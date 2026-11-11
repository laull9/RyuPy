//! MIR 数据结构定义。
pub use crate::operations::{BinOp, CastKind, DynOp, UnaryOp};
use crate::{GlobalStorage, ModuleExecutionKind};

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
#[derive(Debug, Clone, PartialEq)]
pub struct GlobalDecl {
    /// 存储身份来自前端能力，不由名字前缀推断。
    pub storage: GlobalStorage,
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
    /// 仅冷注册消费；候选不进入活动导入表，不能成为跨模块绑定证明。
    pub candidate_modules: Vec<String>,
    pub optional_sources: Option<String>,
    pub metadata_unavailable: Option<String>,
    /// driver签发的封闭分发静态载荷；后端只传递，不解析元数据。
    pub distribution_metadata: Option<String>,
    /// 仅整产物准入与实际link核验可签发；发射前仍逐字段校验，不能以存在性当作授权。
    pub trace_product: Option<crate::generator_product::Certificate>,
    pub functions: Arena<Function>,
    pub classes: Arena<ClassDef>,
    pub globals: Arena<GlobalDecl>,
    pub modules: Arena<ModuleDef>,
    pub entry: ModuleId,
}

/// 一个已编译模块在运行时需要的信息。
#[derive(Debug, Clone, PartialEq)]
pub struct ModuleDef {
    pub instance_capabilities: ryupy_base::InstanceCapabilities,
    pub execution: ModuleExecutionKind,
    /// 编译计划的审计原因；不作为运行时语义开关。
    pub environment_reason: String,
    /// 模块运行位置与诊断file分离，codegen不得从名字猜模式。
    pub location: crate::ModuleLocation,
    /// 公开__file__读写走初始化的真实模块槽，不在runtime重置初值。
    pub file_backed_by_global: bool,
    /// 实际源码解析分支证明的包身份，不从文件名或公开属性推断。
    pub is_package: bool,
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
#[derive(Debug, Clone, PartialEq)]
pub enum ExportGuard {
    None,
    ValueSentinel,
    BoundFlag(GlobalId),
}

/// 一个公开名字及其存储、绑定状态。
#[derive(Debug, Clone, PartialEq)]
pub struct ModuleExport {
    pub name: String,
    pub value: GlobalId,
    pub guard: ExportGuard,
}

#[derive(Debug, Clone, PartialEq)]
pub struct ClassDef {
    /// 仅实例类隐式词法 cell 的来源计划；代码后端不据名字重建此身份。
    pub class_cell: Option<crate::ClassCellDefinition>,
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
#[derive(Debug, Clone, PartialEq)]
pub struct ParamMeta {
    pub name: String,
    pub kind: ParamKind,
    pub has_default: bool,
    pub ty: MirType,
}

/// Dynamic core 的真实函数引用；私有槽仅用于普通定义的静态已知直调。
#[derive(Debug, Clone, Copy, PartialEq)]
pub struct DynamicFunctionOrigin {
    pub owner: LocalId,
    pub source_global: Option<GlobalId>,
}

#[derive(Debug)]
pub struct Function {
    /// 实例入口的显式state参数；Python参数元数据不包含此槽。
    pub module_state: Option<LocalId>,
    /// 环境local的可信来源，与module_state同时存在。
    pub module_state_source: Option<crate::InstanceStateSource>,
    pub instance_capabilities: ryupy_base::InstanceCapabilities,
    /// 不可变 Python 定义来源；后端不得从方法符号推测。
    pub definition: Option<crate::DefinitionInfo>,
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
    /// 特化身份锚点：MakeFunction保存本次原函数对象，不导出且禁止普通Python/MIR写入。
    pub specialization_identity_global: Option<GlobalId>,
    /// 默认值数组（`object[]`）的槽位；没有默认值时为 `None`。
    pub defaults_global: Option<GlobalId>,
    /// `PySignature` 的槽位，供生成的调用入口（thunk）使用。
    pub signature_global: Option<GlobalId>,
    /// 闭包环境形参（M3.0）：引用外层 cell 的函数多一个 `object[]` 参数，
    /// 每个元素是外层作用域的一个 cell（`object[1]`）。非闭包为 `None`。
    pub env: Option<LocalId>,
    /// 已收敛为Dynamic的Python函数才有尾owner；不属于Python参数元数据。
    pub dynamic_origin: Option<DynamicFunctionOrigin>,
}

#[derive(Debug)]
pub struct Body {
    /// 源码实例异常链要求；独立于EH形状，pure bare也不能遗失。
    pub source_handler_contracts: Vec<crate::SourceHandlerContract>,
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
#[derive(Debug, Clone, PartialEq)]
pub struct Region {
    /// 本函数源码异常链ID；仅实际分派外层区域携带，with/cleanup没有。
    pub source_handler_chain: Option<usize>,
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
#[derive(Debug, Clone, PartialEq)]
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

#[derive(Debug, Clone, PartialEq)]
pub struct LocalDecl {
    pub ty: MirType,
    /// 用户变量保留名字（调试与快照用），编译器临时变量为 `None`。
    pub name: Option<String>,
    /// cell 变量（M3.0）：槽位里存的是 `object[1]`（PyOps.CellNew 创建），
    /// 读写经过 `CellGet` / `CellSet`，多个闭包共享同一个数组。
    pub is_cell: bool,
}

#[derive(Debug, Clone, PartialEq)]
pub struct BasicBlock {
    pub stmts: Vec<Statement>,
    pub terminator: Terminator,
}

#[derive(Debug, Clone, PartialEq)]
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

#[derive(Clone, Debug, PartialEq)]
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

#[derive(Debug, Clone, PartialEq)]
pub enum Rvalue {
    /// 生成器降级前的委托推进；降级时补帧参数，返回是否产出。
    GeneratorDelegateStep {
        delegate: Operand,
    },
    /// 创建持有帧与静态恢复委托的生成器，构造时不执行函数体。
    MakeGenerator {
        resume: FnId,
        frame: Vec<Operand>,
        /// 保存函数对象引用，恢复时读取其当前Module，不能快照Module值。
        origin: Option<Operand>,
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
    /// 实例普通函数的零实参值调用；惰性 frame 来源仅供真实 canonical super 消费。
    InstanceZeroCall {
        callee: Operand,
        frame: crate::ZeroCallFrame,
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
        /// 独立模块捕获标志；None保持原方法PIC。
        captured: Option<Operand>,
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
        /// 已在真实定义位置捕获的环境；旧函数为None。
        instance_context: Option<Operand>,
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

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Callee {
    Fn(FnId),
    Runtime(RuntimeFn),
}

pub use crate::runtime_fn::{EXCEPTION_TYPE_NAMES, RuntimeFn};

#[derive(Debug, Clone, PartialEq)]
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
