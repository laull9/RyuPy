//! HIR 数据结构定义。只有数据，不含逻辑。

use la_arena::{Arena, Idx};
use ryupy_base::{FileId, TextRange};

pub type FuncId = Idx<Function>;
pub type ModuleVarId = Idx<ModuleVar>;
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

/// 模块的身份信息，全部由 driver 提供（HIR 不读文件）。
#[derive(Debug, Clone, Default, PartialEq, Eq)]
pub struct ModuleInfo {
    /// 模块真实名字绑定到每个模块对象独立的运行时命名空间。
    pub instance_globals: bool,
    /// 仅实际构建开启：前言从本次导入对象获取冷位置与四个元数据初值。
    pub resolve_file_at_runtime: bool,
    /// 点分绝对模块名；入口文件固定为 `__main__`。
    pub name: String,
    /// `__package__`：包内的模块是所在包名，包的 `__init__.py` 是自身名字，入口为空串。
    pub package: String,
    /// `__file__`：traceback 与模块属性展示用的文件名。
    pub file: String,
}

/// 模块级变量（M2.7）。
///
/// 模块顶层赋值、导入绑定、`global` 声明以及 `__name__` 等特殊名字都落在这里。
/// 它们在 MIR 里是静态槽位，模块对象通过生成的 getter/setter 暴露给导入方。
#[derive(Debug)]
pub struct ModuleVar {
    pub name: Name,
    pub range: TextRange,
    /// 模块级 `x: int = ...` 的标注，供类型推导取提示。
    pub annotation: Option<TypeRef>,
    /// 被函数里的 `global` 声明过。这类名字的读写走动态路径。
    pub is_global: bool,
    /// 源码可能删除此模块绑定；独立于值类型，普通数值仍保typed槽。
    pub may_delete: bool,
}

#[derive(Debug)]
pub struct Module {
    /// 被静态模块异常 handler 使用的稳定导入别名；外部重绑定明确拒绝。
    pub protected_exports: Vec<String>,
    pub file: FileId,
    /// 源文件名。traceback 的 `File "..."` 与诊断都要用它，由 driver 提供。
    pub source_name: String,
    pub info: ModuleInfo,
    pub functions: Arena<Function>,
    pub classes: Arena<Class>,
    /// 模块级变量。
    pub vars: Arena<ModuleVar>,
    /// 模块顶层定义的函数（不含类方法），按源码顺序。
    pub top_functions: Vec<FuncId>,
    /// 模块顶层定义的类，按源码顺序。
    pub top_classes: Vec<ClassId>,
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
    /// 真实源码定义来源；隐式函数保持未知。
    pub definition: Option<crate::DefinitionInfo>,
    pub name: Name,
    pub kind: FunctionKind,
    pub params: Vec<Param>,
    pub ret: Option<TypeRef>,
    pub body: Body,
    pub range: TextRange,
    /// 闭包信息（M3.0）：free 变量、被捕获的 cell 局部与嵌套函数标记。
    ///
    /// 由 `closures.rs` 在 lowering 之前分析整个 AST 填写；对顶层函数
    /// `free` / `cells` 通常为空，`has_nested` 标记体内是否有嵌套 def / lambda。
    pub closure: ClosureInfo,
    /// 当前函数体含 yield（包括不可达分支），调用先返回生成器而非执行体。
    pub is_generator: bool,
    /// 首条字符串语句的文档内容；没有文档时为 None。
    pub docstring: Option<String>,
    /// 定义位置求值的真实注解对象，与静态类型线索分开保存。
    pub annotation_values: Vec<(Name, ExprId)>,
}

/// 一个函数作用域的闭包分析结果。
#[derive(Debug, Default, Clone)]
pub struct ClosureInfo {
    /// 真实隐式 __class__ 自由绑定的词法类来源；局部同名 cell 不使用此标记。
    pub class_cell: Option<ClassId>,
    /// 本函数引用的外层 cell（含 `nonlocal`），顺序即 env 数组下标。
    ///
    /// 创建本函数对象时，外层按此顺序把对应的 cell 装进 env；
    /// 函数体内通过 `ExprKind::Free(idx)` / `AssignTarget::Free(idx)` 访问。
    pub free: Vec<Name>,
    /// 本函数内被嵌套函数捕获的局部名（需要装箱成 cell），按发现顺序。
    pub cells: Vec<Name>,
    /// 体内存在嵌套 `def` / lambda：本函数降级为动态路径并在 def 位置创建函数对象。
    pub has_nested: bool,
}

#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum FunctionKind {
    /// 用户写的 `def`。
    User,
    /// 嵌套 `def` / lambda（M3.0）：在 def 位置通过 `MakeFunction` 创建运行时对象，
    /// 没有模块级静态槽位；调用一律走运行时 `call_value`。
    Nested,
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

pub use crate::class_hir::*;

/// 源码中写出来的类型标注（尚未检查）。
#[derive(Clone, Debug, PartialEq)]
pub enum TypeRef {
    Int,
    Float,
    Bool,
    Str,
    None,
    /// 合法运行时注解，不据此限制实参或推断存储布局。
    Runtime,
    /// 用户定义的类，名字已解析。
    Class(ClassId),
    /// 无法识别的标注；已经报过错，下游当作 `Error` 类型处理。
    Error,
}

// ───────────────────────────── 函数体 ─────────────────────────────

#[derive(Debug, Default)]
pub struct Body {
    /// 可支持直接 locals() 的普通函数；源码可见槽按 CPython 编译访问顺序排列。
    pub function_locals: Option<Vec<LocalId>>,
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
    /// 实参会暂停：调用目标必须先绑定并保存。
    PreparedCall {
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
    /// 语句型暂停；生成器表达式与 send 的结果值另行接入。
    Yield(Option<ExprId>),
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
        var: Binding,
        iter: ExprId,
        body: Vec<StmtId>,
        /// `for ... else:`：迭代耗尽时执行；`break` 跳过。
        orelse: Vec<StmtId>,
    },
    /// `for var in range(start, stop, step)` 的专用形式，编译为原生计数循环，不产生迭代器对象。
    ForRange {
        var: Binding,
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
    /// 内部节点：普通静态绑定def的执行点，准备默认值/注解并创建、发布新函数。
    ///
    /// Python 在 `def` 执行时求值默认值；把它保留成一条语句（而不是统一提到开头），
    /// 默认值才能引用此前的模块变量；节点不把该函数降级为动态公开绑定。
    FunctionDefaults(FuncId),
    /// 嵌套 `def`（M3.0）：在执行位置创建函数对象并写入绑定名。
    ///
    /// 参数默认值也在此时于**外层作用域**求值（每次执行 def 都会产生新的默认值数组）。
    /// `target` 是函数对象写入的位置：外层函数里是局部变量，模块顶层是模块变量。
    NestedDef {
        func: FuncId,
        target: AssignTarget,
    },
    /// 装饰定义：表达式属于定义位置的外层 body，源码顺序求值、逆序应用。
    /// defaults / 原函数对象在装饰表达式之后创建，全部成功后才更新 target。
    DecoratedDef {
        func: FuncId,
        target: AssignTarget,
        decorators: Vec<ExprId>,
    },
    /// `raise [exc [from cause]]`；两者都为 `None` 表示裸重抛。
    Raise {
        value: Option<ExprId>,
        cause: Option<ExprId>,
    },
    /// 顺序删除模块绑定、已支持局部或属性/下标；目标本身不执行普通名字读取。
    Delete(Vec<ExprId>),
    /// 顶层 `class` 定义。MIR 在**定义位置**创建类对象并顺序执行类体，
    /// 保证类属性求值、方法对象创建与模块语句的先后关系正确。
    ClassDef(ClassId),
    /// `import x` / `import x as y` / `import a.b`（M2.7）。
    ///
    /// `bind_root` 表示 `import a.b` 没有别名：Python 只把顶层包 `a` 绑定到命名空间。
    Import {
        /// 绝对模块名。
        module: String,
        bind: Binding,
        bind_root: bool,
    },
    /// `from x import a, b as c`（M2.7）。星号导入明确拒绝，由 lower 报诊断。
    ImportFrom {
        /// 绝对模块名。
        module: String,
        /// 依次绑定：导入方看到的名字 → 目标模块里的名字。
        names: Vec<(String, Binding)>,
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
    /// `except (TypeError, ValueError)` 展开成多个名字。用户自定义异常类在 M2.6
    /// 接入：类名与内建异常名一样作为 catch 的类型标识（codegen 会生成对应的
    /// CLR 异常子类，保持 CLR 与 Python 继承树同构）。
    pub types: Option<Vec<String>>,
    /// 动态链的真实目标表达式；与types互斥，两者皆None仅表示裸except。
    /// 仅mixed链保存全部非bare目标，静态链不增加表达式arena或改变旧EH。
    pub type_expr: Option<ExprId>,
    /// 动态目标开始求值时使用的except头行；静态/裸handler不发射此行。
    pub type_line: u32,
    /// `as name` 绑定的目标（函数内是局部变量，模块顶层是模块变量）。
    pub name: Option<Binding>,
    pub body: Vec<StmtId>,
    pub range: TextRange,
}

/// 一个可赋值目标。函数内是局部变量，模块顶层是模块变量（M2.7）；
/// `nonlocal` 声明的名字是外层 cell（M3.0）。
#[derive(Clone, Copy, Debug, PartialEq, Eq)]
pub enum Binding {
    Local(LocalId),
    ModuleVar(ModuleVarId),
    /// 外层函数的 cell（`nonlocal`）。
    Free(usize),
}

pub use crate::assign_target::{AssignTarget, SequenceItem, SequenceTarget};
