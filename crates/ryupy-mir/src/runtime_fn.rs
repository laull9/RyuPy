//! 运行时 ABI：内建异常名称与静态调用入口。新增入口须同步 codegen 引用及 C# 实现。

/// 编译器认识的 Python 内建及已注册原生模块异常类型名。
///
/// codegen 的异常类型引用表按此与运行时 CLR 类对齐。模块异常不能因此成为
/// 全局内建名字；HIR 必须先验证导入身份，再使用这里的运行时类型名。
pub const EXCEPTION_TYPE_NAMES: &[&str] = &[
    "BaseException",
    "Exception",
    "Warning",
    "UserWarning",
    "DeprecationWarning",
    "PendingDeprecationWarning",
    "RuntimeWarning",
    "SyntaxWarning",
    "FutureWarning",
    "ImportWarning",
    "UnicodeWarning",
    "BytesWarning",
    "ResourceWarning",
    "SystemExit",
    "KeyboardInterrupt",
    "GeneratorExit",
    "ArithmeticError",
    "BufferError",
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
    "IsADirectoryError",
    "NotADirectoryError",
    "StopIteration",
    "StopAsyncIteration",
    "EOFError",
    "json.JSONDecodeError",
    "zipfile.BadZipFile",
    "getopt.GetoptError",
    "optparse.OptParseError",
    "optparse.OptionError",
    "optparse.OptionConflictError",
    "optparse.OptionValueError",
    "optparse.BadOptionError",
    "optparse.AmbiguousOptionError",
    "dataclasses.FrozenInstanceError",
    "re.error",
    "io.UnsupportedOperation",
];

/// `RyuPy.Runtime` 中的静态方法。新增条目时同步修改 C# 侧与 codegen 的引用表。
#[derive(Clone, Copy, Debug, PartialEq, Eq, Hash)]
pub enum RuntimeFn {
    /// 真实实例字典名称读取、写入与执行来源。
    InstanceLoadName,
    InstanceStoreName,
    /// 显式frame的唯一globals删除；不读取或修改捕获的builtins。
    InstanceDeleteName,
    InstanceExecutionSource,
    InstanceCaptureDefinitionContext,
    InstanceDefinitionModule,
    InstanceClassGuard,
    /// 类体/普通终结之后，词法class cell填充之前的冷能力检查。
    InstanceClassCellGuard,
    InstanceMakeClass,
    /// 每次真实import消费前检查该frame捕获的导入函数。
    InstanceImportGuard,
    /// 两阶段运行期相对导入；仅显式实例能力可调用。
    InstanceRelativeImport,
    InstanceRelativeImportAttr,
    /// 数值背板仍是真list；读预检不触发用户协议，最终新鲜F64可不先装箱。
    NumericListCreate,
    NumericListAppendF64,
    NumericListPeekList,
    NumericListCanReadF64,
    NumericListReadF64,
    /// 仅同一冻结(list,index)的CanRead真边且无潜在写入后调用。
    NumericListReadCheckedF64,
    /// 无逃逸循环累加器：一次holder分配、受守卫数值累加、出口物化。
    NumericAccumulatorCreate,
    NumericAccumulatorAdd,
    NumericAccumulatorValue,
    /// 本次print的输出目标；实参准备完成后捕获一次，所有Write入口首参使用它。
    PrintTarget,
    // print(a,b) → target=PrintTarget(); WriteI64(target,a); WriteSep(target); ...
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
    /// `PyErrors.MatchDynamicException(object, object) -> bool`：真实动态except目标校验与匹配。
    ExceptionMatch,
    /// `PyErrors.Reraise() -> object`：裸 `raise`。
    Reraise,
    /// `PyErrors.PrepareRaise(object) -> object`：抛前准备（挂上下文 + 记栈帧）。
    RaisePrepare,
    /// 隔离S2a源raise入口：归一化后发行真实Explicit请求。
    #[cfg(feature = "trace-adapter-test")]
    TraceAdapterExplicit,
    /// 隔离S2a静态handler：必须在恢复帧之前接收。
    #[cfg(feature = "trace-adapter-test")]
    TraceAdapterHandle,
    /// 隔离S2a自动cleanup：同Q转发，不重复Begin。
    #[cfg(feature = "trace-adapter-test")]
    TraceAdapterForward,
    /// `PyRepr.StrValue(object) -> string`：`str(x)`。
    DynStr,
    /// `PyObject.TypeOf(object) -> object`：`type(x)`。
    DynType,
    /// `PyObject.GetAttr(object, string) -> object`：动态属性读取。
    DynGetAttr,
    /// `PyOps.PrepareMethod(object, string) -> object`：实参暂停前绑定方法。
    PrepareMethod,
    /// 历史ABI：有界接收者在实参前冻结属性目标，其余原样返回。
    CaptureModuleTarget,
    /// 历史ABI：RequiresEarlyMethodCapture判据；不是Python模块类型判断，无用户回调。
    IsModule,
    /// 显式捕获标志与目标的数组调用；参数数组不可共享。
    CallCapturedMethod,
    /// `PyErrors.Load(object, string) -> object`：读取可能未赋值的局部变量。
    LoadChecked,
    /// `PyErrors.Unbound() -> object`：把变量标记为未赋值。
    MakeUnbound,
    /// `PyErrors.LoadName(object, string) -> object`：读取可能未赋值的模块级名字。
    LoadName,
    /// `PyOps.CheckGlobalBound(bool, string) -> void`：静态槽读取前检查绑定位。
    CheckGlobalBound,
    /// 删除专用存在性检查；与读取的builtin回退严格分离。
    CheckModuleDelete,
    CheckModuleDeleteValue,
    /// 仅可删除且有真实builtin对象的名字使用此读取冷能力。
    LoadDeletedModuleName,
    /// `PyTraceback.PushFrame(string, string) -> long`：压入栈帧并返回深度。
    TracePush,
    /// `PyTraceback.PushFrameWithOrigin(string, string, object) -> long`。
    TracePushWithOrigin,
    /// `PyTraceback.CreateModuleExecutionSource(object) -> object`，每次模块执行新建。
    ModuleExecutionSource,
    /// 与TracePush相同的递归深度检查，但不压入无回调叶快路的帧。
    TraceCheckDepth,
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
    /// 普通函数locals：先完整绑定，再逐槽同步，不构造快照数组。
    FunctionLocalsValidate,
    FunctionLocalsEnsure,
    FunctionLocalsSync,
    /// `PyOps.CallMethod(object, string, object[])`。
    CallMethod,
    /// `PyOps.ListAppend(list, item)`：星号字面量的增量构建。
    ListAppend,
    /// `PyOps.SetAdd(object, object)` / `SetExtend(object, object)`：按求值顺序修改集合。
    SetAdd,
    SetExtend,
    /// `PyOps.ListExtend(list, seq)`。
    ListExtend,
    /// `PyOps.UnpackCheck(long, long, bool)`：解包数量不符时抛 ValueError（bool 表示有星号）。
    UnpackCheck,
    /// `PyOps.UnpackSequence(object, long) -> object`：非星号解包并冻结为精确 tuple。
    UnpackSequence,
    /// `PyOps.UnpackEx(object, long, long) -> object`：星号解包冻结为前项、新 list、后项 tuple。
    UnpackEx,
    /// `PyOps.IterStart(object) -> object`：建立迭代器。
    IterStart,
    /// `PyOps.IterNextOk(object) -> bool`：推进迭代器。
    IterNextOk,
    /// `PyOps.IterValue(object) -> object`：取当前元素。
    IterValue,
    /// `PyOps.ArraySet(object, long, object)`：写数组元素。
    ArraySet,
    /// `PyOps.ArrayGet(object, long) -> object`：读数组元素。
    ArrayGet,
    /// 读取发送值或在真实yield恢复点抛注入异常。
    GeneratorInput,
    /// 内部异常分派完成，要求运行时重新进入resume而非暴露yield。
    GeneratorRetry,
    GeneratorDelegateStart,
    GeneratorDelegateStep,
    GeneratorDelegateCurrent,
    GeneratorDelegateResult,
    /// `PyOps.CallValue(object, object[], object[], object[]) -> object`：调用一等函数对象。
    CallValue,
    // ── 闭包 cell（M3.0）──
    /// `PyOps.CellNew() -> object`：创建 `object[1]`，槽位放「未赋值」哨兵。
    CellNew,
    /// `PyOps.CellGet(object, string) -> object`：读 cell，未赋值抛 `UnboundLocalError`。
    CellGet,
    /// `PyOps.CellSet(object, object)`：写 cell。
    CellSet,
    /// `PyOps.CellGetFree(object[], long, string) -> object`：读外层 cell
    /// （env + 下标一步完成），未赋值抛 free variable 版 `NameError`。
    CellGetFree,
    /// `PyOps.TypeObject(string) -> object`：内建构造名作为值使用时的类型对象。
    TypeObject,
    BuiltinFunction,
    /// `PyOps.FunctionModule(object, object)`：定义点写入已冻结的真实模块成员。
    FunctionModule,
    /// 读取真实函数实例的Defaults；只在成功的静态def发布处更新直调默认值槽。
    FunctionDefaults,
    FunctionMetadata,
    ClassNamespaceGet,
    /// 当前类自身原始字典是否包含名字，不执行描述符或继承查找。
    ClassNamespaceContains,
    DeclareDataclass,
    /// 普通类最终own namespace冷处理；固定(object)->void。
    FinalizeOrdinaryClass,
    /// 完成普通Enum成员规范化，成功后才允许公开类绑定。
    EnumFinalize,
    /// 只有HIR已验证的原生Enum基类走此构造入口，普通类创建不能绕过成员转换。
    EnumCreate,
    /// 创建TypedDict私有类体命名空间，并验证真实基值。
    TypedDictBegin,
    /// 合并TypedDict字段元数据，返回可公开的真实类型。
    TypedDictFinish,
    /// `PyRuntimeBases.Validate(object) -> object`：检查真实普通类与整个祖先链。
    ValidateOrdinaryBase,
    /// 大字典逐对插入：先拒绝非精确标量键，不调用用户hash/eq。
    DictSetScalarKey,
    ApplyDataclass,
    /// `PyOps.ArgError(int, string) -> object`：绑定失败时抛 `TypeError`（永不返回）。
    ArgError,
    /// `PyOps.StarArg(object) -> object`：给 `*iterable` 实参打标记，由运行时展开。
    StarArg,
    /// `PyOps.FormatValue(object, string, int) -> string`：f-string 的单段格式化。
    FormatValue,
    // ── 类与属性协议（M2.5 / M2.6）──
    /// `PyOps.MakeClass(string, object[], object) -> object`：创建类对象。
    /// 第三个参数是异常类的工厂委托（普通类传 `null`）。
    MakeClass,
    /// `PyOps.SetAttr(object, string, object)`：实例 / 类属性写入。
    SetAttr,
    /// `PyOps.DelAttr(object, string)`：实例 / 类属性删除。
    DelAttr,
    /// `PyOps.Decorate(int, object) -> object`：应用 property / staticmethod / classmethod。
    Decorate,
    // ── 模块系统（M2.7）──
    /// `PyModules.Import(string) -> object`：导入并返回模块对象（带缓存与循环处理）。
    ImportModule,
    /// 读取已缓存模块的初始运行位置；不会触发import或用户属性协议。
    ModuleFile,
    /// 本次导入元数据快照：(str, str) -> Dynamic。
    ModuleMetadata,
    /// `PyModules.ImportAttr(string, string) -> object`：`from m import name` 的取值。
    ImportAttr,
}
