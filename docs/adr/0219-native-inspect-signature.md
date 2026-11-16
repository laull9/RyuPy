# ADR-0219：原生签名值对象、实时元数据与真实框架消费者

- 日期：2026-11-16
- 状态：设计及手写 pending 输入；尚未实现、未运行 oracle、未构建、未验收
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0212](0212-live-function-defaults.md)、[ADR-0214](0214-generic-default-function-companions.md)
- 实施条件：主代理完成当前提交并明确启动下一竖切；本文不授权提前修改已有实现

## 1. 首切决定与完成对象

交付一组可独立使用的原生 `inspect.signature` 值对象及绑定协议，同时完成实际 CIL
函数被固定 attrs、Pydantic、FastAPI 消费的桥接合同。首切以现有非延迟注解、真实
defaults/kwdefaults、普通函数/绑定方法/partial 为基础，包含明确支持的类及 callable。
普通完整供参 typed ABI、PyFunction 字段布局及静态热路保持现状。

首切完成必须同时满足以下三组门，不能只完成格式化字符串或挂一个 `__signature__`：

1. 原生值对象：Parameter、Signature、kind、empty、BoundArguments 的构造、读取、替换、
   验证、比较/哈希、只读集合和绑定闭环。
2. 元数据：当前默认值/注解、装饰器解包、用户显式签名、方法绑定与 partial 变换。
3. 实际框架：真实包直接读取编译函数签名、推导转换器/验证器/依赖参数，并实际调用该 CIL 函数。

框架在本切仍可属于既有 CPython provider 域；被测应用回调必须实际编译为 CIL。
产品兼容通过数、CIL 函数执行数和框架自身源码执行归属分别记录。纯原生框架闭包仍归 C4。

## 2. 本次只读核对的实际消费者

| 固定消费者 | 已读取的实际路径 | 本切必须解除的签名依赖 | 独立阻断 |
| --- | --- | --- | --- |
| attrs 23.2.0 | `src/attr/_compat.py::_AnnotationExtractor`；`_make.py` 的 pre-init、cached-property、converter 路径 | 参数映射、首参注解、return_annotation、Parameter.empty/Signature.empty 的同一身份、方法接收者处理 | 固定原项目还读取 `sys.modules[cls.__module__].__dict__`，并通过 compile/exec 生成方法 |
| Pydantic 2.14.3 / core 2.50.4 | `_internal/_signature.py`、`_decorators.py`、`_typing_extra.py::signature_no_eval`、`_generate_schema.py::_arguments_schema` | Parameter.replace、kind 的身份/相等/哈希、参数有序映射、empty、模型签名生成及 validator 形参判定 | `validate_call` 对 FunctionType/MethodType/partial 的独立类型准入；完整类型求值与扩展 ABI |
| FastAPI 0.158.1 | `dependencies/utils.py::_get_signature/get_typed_signature/get_dependant`；`dependencies/models.py` | signature、unwrap、Parameter/Signature 重建、当前默认值与真实注解、实际函数 globals | callable 分类/协程协议、完整服务生命周期；字符串注解与 3.14 FORWARDREF 分支 |

attrs 原始源码来自现有固定项目源码树，版本为 23.2.0；本机 uv 缓存另有 attrs 26.1.0，
不能用后者替代固定样本。FastAPI/Pydantic 的实际验收依赖以
`tests/general_projects/requirements.txt` 为准，其版本为上述 0.158.1/2.14.3/2.50.4。
项目自身旧 uv.lock 的 Pydantic 2.12.5/core 2.41.5 是另一套版本输入，另列结果。

本轮只读取源码和元数据。新增输入没有运行，也没有据此改变旧项目账目。

## 3. 原生公开 API 族

| API | 首切完整合同 |
| --- | --- |
| `inspect.signature` | 函数及下述准入 callable；follow_wrapped；3.12 基线的 globals/locals/eval_str 入口，字符串求值域见 §7 |
| `inspect.unwrap` | 实际 `__wrapped__` 链、可调用 stop、身份循环检测；用户属性/stop 异常原样传播 |
| `Parameter` | name/kind/default/annotation；五个 kind 常量；empty；replace；str/repr；值比较/哈希；不可变 |
| `Signature` | 参数序列验证、只读有序 parameters、return_annotation、empty；replace、from_callable、str/repr、值比较/哈希 |
| `Signature.bind/bind_partial` | 位置/仅位置/仅关键字/变参绑定；重复/未知/缺参错误；不执行被检查函数 |
| `BoundArguments` | signature、真实可变 dict arguments、实时 args/kwargs 派生值、apply_defaults；参数和值身份保持 |

`Parameter.empty`、`Signature.empty` 和 `inspect._empty` 指向同一个原生标记**类对象**。
它与 None、内部 replace 的“未提供”哨兵不同。五个 kind 是稳定的 `_ParameterKind`
成员，值为 0–4，保留整数相等/排序/哈希/int 转换、成员身份、name/value/description。
Pydantic 直接导入私有 `_ParameterKind`，本切登记其受控真实类型；不能用普通 long 或
CLR enum 冒充。现有 Enum 支持不含完整 IntEnum，因此该有限整数派生载体属于必需工作。

Signature 的 parameters 是新建私有有序映射的 mappingproxy，不能让用户替换参数。
构造与 replace 验证名字、重复、kind 顺序、位置参数必填/默认顺序、variadic 默认值。
映射保定义顺序；Signature 的值比较/哈希按 CP 规则处理 keyword-only 参数的顺序等价，
不能直接比较映射迭代序列。BoundArguments.arguments 可替换为实际 dict，派生值读取其当前内容。
不可哈希的 default/annotation 会令 Parameter/Signature 的 hash 抛出 TypeError。
不为方便缓存调用用户 hash、repr 或 equality；这些协议仅在用户实际请求时执行。

Parameter/Signature 子类扩展、pickle/copy.replace、完整 inspect 其它 API 单列后续。
本切基类协议和固定消费者使用的精确对象路径必须闭合。

## 4. 绑定与函数调用保持不同职责

PyBinder 的普通调用快路保持。inspect.bind 的输出先只记录实际绑定值，省略的默认参数
不出现于 arguments；bind_partial 允许缺少必需参数。apply_defaults 才补默认值、空 tuple
和空 dict。args/kwargs 每次从当前 arguments 按签名重建，保留对象引用。

新增独立冷绑定规划器，复用受检规则或明确共用纯规则表；不直接调用会填充默认值的
PyBinder.Bind 再猜哪些参数被省略。两种绑定的报错文本/优先级分别跟固定 CPython 差分。
用 `target(*bound.args, **bound.kwargs)` 正向验证结果，错误绑定必须发生在 target 体之前。

## 5. 单一元数据、读取时机与冷状态

canonical PyFunction 继续拥有唯一 annotations、defaults/kwdefaults 和普通属性字典。
inspect 从不可变 PySignature 读取参数名/kind，默认值通过当前函数实际冷元数据入口读取。
共享签名原始 HasDefault 不能替代随后新增/删除的默认值。inspection 属于默认值观察，
首次观察应进入现有 PyFunctionDefaults 状态，使 ADR-0212 未观察快路守卫正确失效。

每次 signature 生成新的不可变结构快照，持有当前 default/annotation 的原引用。
之后替换函数元数据只影响新签名；原默认对象自身的变动在旧签名中仍可见。
已有 Signature 实例直接交给 signature 时仍按 callable 合同处理；显式 `__signature__`
返回对象须保其身份，不能无条件复制。

函数上没有用户 `__signature__` 时仍保持属性缺失；禁止为了 inspect 反向给所有函数
安装静态缓存。显式值为 Signature 时按其对象返回；None 的停止解包及继续本体分析
行为按 CP3.12 的实际顺序锁定；其它类型在 inspection 时 TypeError。

先处理真实方法/描述符、wrapped/显式覆盖，再按基线读取实际函数元数据。descriptor、
stop 回调、annotation 求值和 repr 可以执行用户代码，不能预先复制所有成员后重放。
__wrapped__ 循环按身份检测；只捕获协议约定的 AttributeError，不能吞掉用户 RuntimeError。

冷状态存于独立 CWT 或现有冷载体。不得增加普通 PyFunction 的字段、委托捕获、逐调用
类型检查或环境开关；不按可变 Name、签名相同或方法名推断函数体身份。

## 6. callable 与描述符边界

- 普通/嵌套/生成器/协程函数：签名只消费 Python 参数元数据，排除 env、context、owner。
  读取签名不运行函数、生成器、协程或默认工厂，不伪造 co_code/co_flags。
- bound method：从真实 target 分析，接收者恰好移除一次；staticmethod 保留参数，
  classmethod 移除真实 cls；既有 method 上的元数据更改下次 inspection 可见。
- partial：消费实际 func、args 和实时 keywords，按绑定规则变换剩余参数及 kind；
  关键字预绑定可能把后续位置参数变成 keyword-only。非法 partial 的失败优先级差分。
- 简单 callable 实例：沿真实类型 MRO 的 `__call__` 描述符处理，不能按实例任意同名
  属性替代特殊方法查找。用户显式签名优先，接收者只处理一次。
- 类：显式类 `__signature__`、已支持的元类 `__call__`、类 `__new__`/`__init__` 按 CP
  优先级处理。首切准入真实 PyFunction 构造方法及现有 C1 已完成的绑定路径。
- 原生函数/描述符：PyNativeFunctions 已持参数元数据者显式登记；len、float、object
  等固定消费者需要的内建使用静态描述表，并逐 API oracle。未知或无签名的 native
  对象给 CP 对应 ValueError/TypeError，不能伪造 `(*args, **kwargs)` 成功。

复杂 foreign metaclass、用户 Signature 子类、任意 __text_signature__ 解析和完整 callable
分类另立门。固定消费者触及其中任一项就保 pending 并报具体触发链，不能按名字绕开。

## 7. globals、版本与注解求值

| 范围 | 首切处理 |
| --- | --- |
| 当前纯 Native 语言基线 | PySystemVersion 固定 3.12.13；本切不修改该事实 |
| CP3.9–3.13 的既有非延迟值注解 | def 位置保持原求值时序；签名读取当前 annotations 对象值 |
| eval_str=False | 字符串保持字符串，绝不偷偷解析或提前执行 |
| eval_str=True 且没有字符串 | 直接使用已有值，能覆盖本切的三个真实框架闭环 |
| 运行期字符串求值 | 现有 Runtime 无通用 eval/ForwardRef 求值器；合法字符串求值独立 pending，明确支持边界，禁止伪造 NameError 来触发框架 fallback |
| Instance globals | 使用定义时捕获的真实 globals dict；之后改 __module__ 或同名注册不改其来源 |
| Legacy globals | 仍明确 NI；禁止返回空 dict、扫描源码或借调用者环境补齐 |
| CP3.14/3.15 延迟注解 | 当前 HIR/MIR 无 __annotate__ thunk；VALUE/FORWARDREF/STRING 求值、缓存/失效和副作用时机仍独立 pending |

CP3.9 的 signature 没有 globals/locals/eval_str 参数，3.10 增加；3.14 增加
annotation_format。本切先验当前 Native 3.12 基线；跨 3.9–3.15 oracle 区分共同域和
各版本专属协议，宿主 ABI 范围继续保留。不得依据宿主 Python 版本或测试解释器路径
悄悄切 Native 语言语义，也不得宣称已完成 3.14 延迟注解。

FastAPI 无条件读取 unwrapped.__globals__，因此真实框架首切选显式 Instance 的 CIL
回调，并使用已求值注解对象。后续字符串/lazy 输入单列，不能通过删注解绕过原项目。
完整项目已有 future/字符串注解时，原始输入和阻断继续保留。

## 8. CPython 桥的真实对象身份与静态 ABI

Runtime 只提供中性签名值对象/受信只读数据合同，不依赖 RyuPy.CPython。桥负责将其
映射为宿主真实 inspect.Signature/Parameter、真实 kind 成员、真实 inspect._empty。
通用 managed proxy 不满足 CPython 的 isinstance 和 empty 身份要求，必须有专用转换。

同一个显式 Native Signature 反复导出保持同一 CP 对象；同一个 CP Signature 往返保持
原身份。桥复用现有弱身份表，参数 default/annotation 保留对应的真实外来对象引用。
派生签名快照可以跨域有只读视图，函数可写元数据只有 canonical 一份。

对受信精确类选择静态 C ABI 分配/填槽：宿主缓存真实 inspect 类型、kind/empty，
经版本化 schema 验证后用公开 C API 分配，硬编码已验证的 slot 名填入参数/只读映射。
Parameter 的 name/kind/default/annotation 与 Signature 的 parameters/return_annotation
均先经 Native 规则验证。BoundArguments 若跨域也使用真实宿主类型，signature 与
arguments 保同一对应对象；arguments 的修改复用既有字典同步合同，不能导出失联副本。
禁止裸写 CPython 内部结构偏移、任意 CLR Reflection.Emit、
用户类型的任意私有槽写入。schema/type/empty 不匹配必须在冷接口拒绝并给版本诊断。
此选择依赖私有槽名但不依赖对象内存偏移，是受信精确类型的显式取舍；每个宿主版本
须读实际源码、验证 slots、真实 isinstance、读写只读性及销毁/GC，才能签发该 profile。

导出的原生 C callable descriptor 提供即时的 `__signature__` 检查视图，内部向 Runtime
请求签名并转换，不安装 Python 转发函数。descriptor 的可写 defaults、annotations、
wrapped/显式 signature 等应回到 canonical；不能另存随后失联的 tuple/dict 副本。
已有 C1 descriptor 上的独立元数据字段须在候选协议内收敛为视图，旧兼容域单独保留。

CP 侧对象仍明确是原生 callable descriptor，不伪装成 types.FunctionType；其
`__signature__` 是派生检查协议。有效显式 Signature 保身份；None/非法覆盖及 wrapped
顺序由同次跨域正例锁定。自定义 __signature__ 被宿主 inspect 直接采用时，eval_str
及 annotation_format 不会替该自定义签名重新求值；首切消费者使用真实值注解。
完整 Python 函数类型判定、任意字符串/lazy 格式一致性按 §7 独立完成。

Instance globals 跨桥必须保持真实字典身份与已有同步协议；若消费者要求 exact dict，
提供受控的同一字典映射，不能临时复制 globals 冒充活字典。Legacy 的 NI 继续可见。
关闭/不同解释器、弱引用、GC 循环和错误路径的 borrowed/new 引用均有负门。

## 9. AOT 表示、必要文件与架构约束

运行数据为明确 C# 类型：Parameter 的四个只读值，Signature 的有序参数/返回注解，
BoundArguments 的签名引用与可变 dict，kind 的五个受信整数成员，empty 的唯一标记类。
所有方法使用静态委托/显式协议分派，NativeAOT 无运行期生成 CIL 或生成 Python 源码。

| 层 | 预计最窄文件边界，实施授权后再核对 |
| --- | --- |
| Runtime 新模块 | `PyInspectModule.cs`、`PyInspectParameter.cs`、`PyInspectSignature.cs`、`PyInspectBoundArguments.cs`、`PyInspectBinding.cs`、`PyInspectCallable.cs`、`PyInspectKind.cs`、`PyInspectBridgeContract.cs` |
| Runtime 窄接线 | PyNativeModules、PyFunctionMetadata/Defaults 的内部冷读取、PyPartial 的只读内部访问；kind 所需整数/比较/哈希显式协议，保持普通整数快路 |
| Bridge 新模块 | `CPythonInspect.cs`、`native/inspect_metadata.h`，静态 Native 声明；CPythonValues 的专用转换及 native method descriptor 冷 getter/setter |
| 注册/构建 | driver 的受信 host/profile 注册；仅新增冷 ABI 才同步 RuntimeFn、验证表与 codegen refs，codegen 仍只依赖 MIR |
| 测试 | Runtime API/状态/绑定、CPython 真类型与身份/GC、MIR/CIL 原热路保护、下列原生和真实消费者输入 |

Signature 实例布局及标准库 API 不用 Name/前缀授权。受信来源通过实际类型、canonical
函数/签名身份及显式 profile 搬运；假同名对象与同签名不同 body 不得获得内部权限。
普通 PyFunction 不加字段。新增源码单文件不超过 600 行，关键约束与介绍保留。

## 10. 本次新增 pending 输入与未来验收

- `tests/e2e/phase3/c2_inspect_signature_values.py`：kind/empty/只读映射、构造/replace/哈希及负例。
- `c2_inspect_signature_binding.py`：五类参数、bind/partial/apply_defaults、活 arguments、实际调用闭环。
- `c2_inspect_signature_metadata.py`：默认/注解替换、删除、旧快照与可变对象别名、无正文执行。
- `c2_inspect_signature_wrapped.py`：wrapped、显式签名、None/非法覆盖、循环及属性异常。
- `c2_inspect_signature_callables.py`：接收者、classmethod/staticmethod、partial、简单类及 callable。
- `tests/reflection/inspect_signature/consumers/`：固定真实 attrs/Pydantic/FastAPI 导入与应用行为，
  `compiled_callbacks.py` 必须编译为显式 Instance；框架源码保持原输入，不 monkeypatch inspect。
- `tests/reflection/inspect_signature/annotations_deferred.py`：3.14 专属原始正例，单列后续阻断，
  不能因首切已有值注解通过而去掉其 pending。
- 同目录 `annotations_strings.py` 保运行期字符串求值正例；`legacy_globals_boundary.py`
  保 globals 原始正例，未来分别以 Instance 等价臂与 Legacy 精确 NI 负门验收。

未来执行：每个输入当次固定 CPython 生成 stdout/stderr/状态 oracle，不保存生成 golden。
原生族以 3.12.13 首验，再分列 3.9.25/3.13.3 共同域，3.14/3.15 独立版本分支。真实
框架以冻结包和 CP3.13.3 首验。消费者组通过实际包的 Parameter 重建、模型签名、validator
info 判定、Depends/OpenAPI/422/回调结果验收，导入成功不满足完成门。

每臂独立仓库外目录、禁写输入 pycache；JIT 与 NativeAOT 分列。AOT 搬迁后删除原输入、
stage/build，再以干净 cwd 运行；记录实际 CIL 回调执行归属及 provider 选择。负例包含
未知 native descriptor、Legacy globals NI、错 profile/解释器、显式非法签名与 wrapped
循环。错误匹配协议阶段、类型及固定版本消息，不吞掉用户副作用或异常来制造成功。

静态/完整实参调用的 CIL、函数对象布局、字段数及现有 26 项功能/性能配对继续保护。
新冷检查的分配、桥对象和初始化体积单列；未经最终全门与产品/配对验收保持候选状态。

## 11. 仍须显式登记的阻断

1. 固定 attrs 原项目的活模块字典、slots/closure 替换和运行期生成方法归属；签名消费者
   通过后仍须原项目正例，不把 AnnotationExtractor 的局部成功泛化为 attrs 全部完成。
2. Pydantic validate_call 的真 FunctionType 类型准入及完整模型/ForwardRef，不能假装
   native descriptor 是 CPython function；首切使用真实 validator/模型签名消费者。
3. FastAPI 的完整 async 生命周期/数据库服务、callable 分类和 framework CIL 闭包继续
   各自验收；本切真实同步依赖可以解除签名参数解析阻塞。
4. Legacy globals、运行期字符串求值、3.14 lazy annotation thunk/profile 和全 inspect
   其它 API 继续保留原始 pending 输入与明确诊断。

后续实施先把这些正例在当次 oracle 上跑通，再实现并逐项转正。本次所有内容仅是计划。

## 12. 行为参考

原生实现按本 ADR 的显式数据与协议重写，行为核对使用以下上游版本资料及本节前述
固定消费者源码：

- [Python 3.12 inspect API](https://docs.python.org/3.12/library/inspect.html)。
- [Python 3.14 inspect API](https://docs.python.org/3.14/library/inspect.html) 与
  [annotationlib 求值格式](https://docs.python.org/3.14/library/annotationlib.html)。
- [attrs 23.2.0 注解消费者](https://github.com/python-attrs/attrs/blob/23.2.0/src/attr/_compat.py)。
- [FastAPI 0.158.1 依赖解析](https://github.com/fastapi/fastapi/blob/0.158.1/fastapi/dependencies/utils.py)。
- Pydantic 2.14.3 的 `_internal/_signature.py`、`_decorators.py`、`_typing_extra.py`、
  `_generate_schema.py`，由固定 requirements 环境的真实源码核对。
