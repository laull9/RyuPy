# ADR-0219：原生签名值对象、实时元数据与真实框架消费者

- 日期：2026-11-16
- 状态：本切已实现；定向与产品验证通过，统一提交门及最终性能由主代理收口
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0212](0212-live-function-defaults.md)、[ADR-0214](0214-generic-default-function-companions.md)、[ADR-0211](0211-native-method-descriptor-candidate.md)

## 1. 决定与执行归属

实现原生 `inspect.signature` 的签名值对象、独立绑定协议和实时函数元数据读取，
并让固定 attrs、Pydantic、FastAPI 实际消费编译为 CIL 的应用回调。
普通完整供参 typed ABI、PyFunction 字段布局和静态热路保持原状。

本切交付三个闭环：值对象的构造/替换/比较/绑定；当前默认值与注解、包装和描述符；
真实框架的转换器、验证器、依赖解析及实际请求。框架仍运行在既有 CPython provider
域，应用回调使用显式 Instance CIL 模块。框架源码自身的 CIL 闭包属于 C4 后续合同。
原生 API 按数据和协议重写，没有复制 inspect.py、Python 转发 wrapper 或全局 monkeypatch。

## 2. 固定真实消费者

| 固定消费者 | 实际消费及验收 |
| --- | --- |
| attrs 23.2.0 | `_AnnotationExtractor`、Converter、真实生成 Record；读取 CIL converter 注解/签名并实际转换 |
| Pydantic 2.14.3 / core 2.50.4 | `generate_pydantic_signature`、`inspect_validator`、BeforeValidator/TypeAdapter；实际 CIL 验证器及拒绝分支 |
| FastAPI 0.158.1 | `get_typed_signature`、`get_dependant`、Depends；TestClient 真请求 200/422、OpenAPI、实际 CIL dependency 调用记录 |

框架环境为 CPython 3.13.3，Starlette 1.10.0、httpx2/httpcore2 2.16.2、anyio 4.16.0。
显式动态收集 httpx2 和 `anyio._backends._asyncio`，保持其真实客户端路径。
固定 attrs 采用 23.2.0，未用本机缓存的另一版本替代。
每个产品输入的 oracle 当次生成；产品额外确认回调没有 CPython CodeType，source-plan
登记 `user_source`，实际回调有行为证据。包导入与 API 格式字符串均不是单独验收项。

## 3. 原生公开 API

| API | 已实现合同 |
| --- | --- |
| signature | 当前函数及下述 callable、follow_wrapped、globals/locals/eval_str 入口；字符串求值见 §6 |
| unwrap | 真 wrapped 链、stop、身份循环检测、用户异常传播 |
| Parameter | name/kind/default/annotation、五个 kind、empty、replace、str/repr、值比较/哈希、不可变 |
| Signature | 验证、只读有序 parameters、return_annotation、empty、replace/from_callable、str/repr、比较/哈希 |
| bind/bind_partial | 位置、仅位置、仅关键字、变参、重复/未知/缺参错误；不运行被检查函数 |
| BoundArguments | signature、可变且可替换 dict arguments、实时 args/kwargs、apply_defaults |

Parameter.empty、Signature.empty、inspect._empty 是同一标记类对象，独立于 None 和
replace 的内部未提供哨兵。五个 `_ParameterKind` 成员稳定，保留整数身份、转换、
比较、哈希、算术、name/value/description；有限专用载体避免扩张通用 Enum 合同。
名字检查包含 Unicode 标识符和规范化闭包；原字符串保留。

parameters 是私有有序映射的 mappingproxy；构造验证重复、kind 顺序、必填/默认顺序及
变参默认值。keyword-only 顺序不改变 Signature 值相等/哈希。不可哈希默认值或注解
按需抛 TypeError；读取签名不会为缓存预先执行用户 hash/repr/equality。
Parameter repr 使用固定外层双引号，并原样嵌入参数文本，内部双引号不额外转义。

独立冷绑定器记录实际提供值，bind_partial 容许缺参；apply_defaults 才补默认值及
空 tuple/dict。args/kwargs 从当前 arguments 派生，保留对象身份与别名。
错误文本和优先级与固定 3.12.13 差分；不借普通 PyBinder 已补齐的结果猜省略参数。

## 4. 唯一元数据与描述符时序

canonical PyFunction 的 defaults/kwdefaults、annotations 和属性字典仍只有一份。
签名结构来自 PySignature，当前值经既有冷元数据入口读取。inspection 会观察默认值，
使 ADR-0212 未观察快路守卫失效。每次新建结构快照持有当前对象引用；后续替换只影响
新快照，原 mutable 默认对象自身的变化仍在旧快照可见。PyFunction 没有新增字段。

原生函数本身没有自动安装的 __signature__ 属性。用户显式 Signature 保身份；None
停止继续 wrapped 解包但继续本体分析；非法值 TypeError。显式 Signature 优先于
字符串求值，eval_str=True 不重新执行其内部注解。

unwrap 先查询实际 __wrapped__ 是否存在，存在才调用 stop，再读取一次属性；因此终端
函数 stop 零次、描述符每层两读。类对象即使有 wrapped 也不解包。循环按身份检测，
保原对象 repr；本切深度上限 1000，不随宿主递归配置改变。只捕获 AttributeError，
用户 stop/属性异常保原顺序传播。

| callable | 处理 |
| --- | --- |
| 函数/嵌套/生成器/协程 | 排除 env/context/owner；读取签名不运行正文或默认工厂 |
| bound method | 分析真实 target；接收者只移除一次 |
| staticmethod/classmethod | 按实际描述符绑定；静态方法保参数，类方法移除真实 cls |
| partial | 消费真实 func/args 和当前 keywords，变换剩余参数及 keyword-only 顺序 |
| callable 实例 | 从真实类型 MRO 取原始 __call__ 槽后对实例绑定；忽略实例同名属性；自定义 descriptor 只绑定一次 |
| 类 | 显式签名优先；按 MRO 选真实 __new__/__init__ 定义，保继承优先级 |
| native | 已持元数据者及受控内建表；未知签名明确 ValueError/TypeError/NI |

普通、staticmethod、classmethod 三种实例 __call__ 均保正确参数，额外统一 drop 会破坏
后两者，故绑定结果直接继续解析。自定义描述符有真实 receiver/owner/次数差分。
复杂 foreign metaclass、Signature/Parameter 子类、任意 text_signature、pickle/copy.replace
和其余 inspect API 均在本切之外。

## 5. 显式 CP 桥 profile、身份和生命周期

只有显式 `--cpython-dynamic-module inspect` 启用 profile；单纯标准库清单含 inspect
不会启用。Host.Start 在 InstallImports 和项目/第三方回调之前捕获真实 CP 原始 API、
Signature/Parameter/BoundArguments 类型、kind/empty 和受信成员描述符。后续导入按
精确对象身份映射；保存的原 API 别名往返仍是原对象，用户 monkeypatch 得到的新函数
原样保留。没有按模块属性名字重抓、调用栈猜测或 TLS follow_wrapped 推断。

无 profile 时 getter/setter 在任何签名计算之前立即走旧 PyObject 路径，hasattr 不观察
默认值；旧 __code__ NI 与普通回调行为保留，普通产品没有启动 import inspect 成本。

桥通过静态 C ABI 与公开 C API 分配真实 CP 对象、填受信精确 slots，校验真实类型、
描述符、kind/empty schema。没有内部对象偏移或任意反射 emit。真实 isinstance、kind
和 empty 的 is、签名往返身份、default/annotation 原对象身份均有产品正例。
BoundArguments 使用真实 CP 类型与活 arguments 存储，替换与修改复用既有字典同步。
同名假类型、schema 槽变更和未知对象明确拒绝。

导出的 callable 提供实时合成 __signature__ 视图；用户显式值（含 None）原样返回。
C1 NativeMethod wrapper 继续遵守 ADR0211 的独立 metadata 合同，仅新增 signature/globals
冷视图，wrapper 自有 signature override 优先；其 defaults/annotations 未迁移到 canonical。
Instance globals 保真实字典身份和同步，Legacy globals 保 NI。

canonical PyFunction 的 CWT 登记真实函数 kind，由 HIR 经 MIR 定义发布，typed/generic
共同使用同一函数对象。只有 profile 下受信 OrdinaryModule 的 CP 视图 __code__ 缺失
（AttributeError），让固定同步分类器正确返回 False；generator/coroutine/unknown 明确 NI。
Runtime __code__ 和 C1 方法旧 NI 不变。未启 profile 的 MakeFunction 无新增登记 IL。

没有用户 signature 的真 wrapped callable：桥让合成 signature 缺失，CP 默认 unwrap
到真实 CIL target。CP follow_wrapped=False 无真实 CodeType 时明确 ValueError/NI；
原生 inspect 两种模式均已实现。没有伪造 CodeType 或返回已展开签名冒充 raw 签名。

拥有的 21 个缓存引用放入私有 `_ryupy_inspect_profile` 模块的 m_size 状态，由解释器管理。
C 全局仅借用；m_traverse/m_clear/m_free 幂等并核对当前 owner，旧 owner 销毁不能清除
新 owner 别名。保留旧 owner 后移除注册可从冻结状态克隆新 owner，绝不按名字重捕获。
owner 真正提前失效后借用清 NULL、后续明确 NI。atexit 执行期间保留桥和缓存；
Py_FinalizeEx 后只清别名/读数，绝不 Py_DECREF，也不提前 run_exitfuncs 改变关闭顺序。
多 owner 产品最终 created/cleared/freed 一致，active=0、aliases_null=1。

## 6. 版本、求值与 AOT 边界

| 范围 | 合同 |
| --- | --- |
| 原生语言基线 | 固定 CPython 3.12.13 行为，不按宿主路径切换语义 |
| eager 值注解 | 保 def 原求值时序，读取当前 annotations 值 |
| eval_str=False | 保字符串，不求值 |
| eval_str=True 无字符串 | 使用已有值；三个框架首切由此闭合 |
| 运行期字符串求值 | 明确 NI；保原始 pending，不伪造 NameError 诱导 fallback |
| Instance globals | 定义时真实 globals，不受后改 module 名影响 |
| Legacy globals | 明确 NI |
| 3.14/3.15 lazy annotations | 无 __annotate__ thunk；VALUE/FORWARDREF/STRING、缓存与副作用仍 pending |

CP3.9 没有 globals/locals/eval_str 参数，3.10 增加，3.14 增加 annotation_format。
产品版本范围仍为 3.9–3.15；本次新增 inspect profile 的实际产品证据为 3.13.3，
原生共同 API 的实际产品基线为 3.12.13。该证据不扩张为各宿主版本新增协议完成。
CP 真函数直接读取公开 code/function 元数据；检测到延迟 __annotate__ 时明确 NI。
跨域合成签名含原始字符串注解时明确 NI，原生 eval_str=False 可保字符串。

显式 inspect profile + Instance AOT 允许已有 eager 注解，但以更强的后置证明取代旧
AST typed 禁止门：所有对外函数实际 Dynamic，无 Unbox、精确拆箱或 typed 核心直调，
私有 source context 只在批准的定义位置使用。源注解原样保留。普通 Instance AOT 的
旧 typed 负例仍拒绝；没有全局关闭 typed 优化。

AOT source location kind3 只允许已冻结 provider bundle 的精确 compiled_sources 映射，
仍使用 ResolveBundle 路径验证；普通 NativeAOT 保资源要求。该 provider 源锚点遵守
现有兼容合同，移除解释执行的 C4 内核属于独立工作。

## 7. 文件边界与静态表示

| 层 | 实际文件/职责 |
| --- | --- |
| Runtime | PyInspectModule/Kind/Identifier/Parameter/Signature/BoundArguments/Binding/Callable；明确 C# 数据类型和静态协议分派 |
| 冷定义 | PyInspectFunctionKind CWT；HIR ModuleInfo.introspection_profile、MIR DefinitionInfo.introspection_kind；codegen MakeFunction 仅显式 profile 发射登记 |
| Bridge | CPythonSignatureBridge/Native/Values/Functions/Metadata/BoundStorage、native/signature_bridge.h |
| 窄接线 | PyNativeModules、PyModuleInstances；Host bootstrap/Values/Callbacks；RuntimeFn 固定 ABI 和 codegen refs |
| AOT 证明 | HIR instance_aot、driver Instance 冻结 plan、MIR inspect_instance_contract |

无普通 PyFunction 字段扩张，无运行时生成 CIL/源码；codegen 只依赖 MIR。
注册依据实际类型、canonical 函数和显式 profile，拒绝同签名不同 body 冒用权限。

## 8. 验证证据与转正

手写五个 `tests/e2e/phase3/c2_inspect_signature_*.py` 覆盖值对象、绑定、实时元数据、
wrapped 和 callable；`tests/reflection/inspect_signature/run.py` 执行同次 oracle 与真实产品。
Runtime PyInspect 10/10、无 profile 真 CP 桥 1/1 已通过；MIR/driver 的 Dynamic ABI、
source context、路径/普通 AOT 负门及 kind 的真实 PE 发布结构门已通过。

唯一冻结 CLI v1 的 11 组全部通过：五个原生组、bridge、atexit、owner、attrs、Pydantic、
FastAPI。每组分别构建 JIT/AOT，移动产品并删除输入/build，干净 cwd/env 下运行；
产物只用 bundle 内 Python/shim。host closed、lease/handle/root/release 全零且同步无失败。
atexit 首次/再次签名和 kind/empty 身份、owner 重建后旧析构不清新别名均通过。

v1 验证后发现 Parameter repr 外引号差异：先新增手写差分和 Runtime 断言，旧实现红，
仅修改冷 PythonRepr 一行后 Runtime 10/10 绿。v2 唯一 CLI 再验新增 values 与冷桥/atexit，
保留 v1 完整 11 组矩阵；v1/v2 差异仅此 repr 冷修。

复现命令（所有路径均为仓库外工具/产物）：

```sh
python3 -B tests/reflection/inspect_signature/run.py \
  --ryupy /tmp/ryupy-adr0208-validation/third-final-cli \
  --python /tmp/ryupy-adr0219/consumer-venv/bin/python \
  --core-python /Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12 \
  --case core --case bridge --case atexit --case owner \
  --case attrs --case pydantic --case fastapi \
  --logs /tmp/ryupy-adr0219/third-final-products
```

日志目录 `/tmp/ryupy-adr0219` 保存 third-final-products、v2-directed-products、
runtime-final-tests、repr-red/green、noprofile-tests、lifecycle-final 及结构门结果。
输出不进入仓库，没有生成 golden 或快照。统一 make check 和性能由主代理同一来源收口。

## 9. 保留阻断

1. attrs 全项目的活模块字典、slots/closure 替换、运行期生成方法；本切验真实签名与 converter。
2. Pydantic validate_call 的真 FunctionType 准入、完整模型 ForwardRef；不伪装 native descriptor。
3. FastAPI 完整 async/数据库服务和框架源码 CIL 闭包；本切验真实同步请求/依赖/OpenAPI。
4. Legacy globals、字符串求值、3.14 lazy、其余 inspect API、复杂 foreign descriptor。
5. CP wrapped 的 raw CodeType 检查边界；原生 follow_wrapped=False 已闭合，跨域保持明确拒绝。

`annotations_strings.py`、`annotations_deferred.py`、`legacy_globals_boundary.py` 保留
原始正例 pending。独立 wrapped false / owner 提前失效负门仍为 pending，产品验证待执行。
