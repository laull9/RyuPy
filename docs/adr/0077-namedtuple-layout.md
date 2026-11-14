# ADR 0077：namedtuple 真实类型与 tuple 协议

状态：公共collections.namedtuple已注册，累计50项C#和5组真实CP/JIT应用首次通过。本地候选正常完整门禁首次通过，尚未公开发布；不称tabulate、完整namedtuple库、AOT或性能已通过。

## 范围与依据

目标是通用 `collections.namedtuple`，不能按项目类名、字段数或源码路径特化，也不能用空模块/可导入占位绕过真实阻断。固定 tabulate 0.9.0 单入口此前实际停在缺少该导出；该阶段尚未重跑项目。原 33 项 CPython 3.12.13 参考保留，阶段 A 没有重跑参考或应用。

来源前置已独立完成，见 [ADR0076](0076-function-module-origin.md)。现行工厂默认 module 只读真实最内层来源；Static 未知来源需要明确边界，不能按文件名、当前 globals 或下层模块猜测。显式任何非 None module 与默认查询分开，包括 falsey 值。公共来源组已覆盖这些有限工厂路径，Static调用结果仍不由结构门冒称通过。

## 载体选择和成本

选择 `PyNamedTupleInstance : PyTuple` 的专用 sealed 内部载体，增加只读真实 `PyClass` 引用。`PyTuple` 解除 CLR sealed，但其字段、构造、长度/索引等非虚方法及签名不变；普通 tuple 实例不增加类引用。`PyClass` 增加 nullable 布局引用，这也是实际类对象布局成本。

解除 sealed 可能改变 JIT 类型优化，并扩大 `is PyTuple` 的接受域，不能称零成本或性能无回退。后续必须逐项核消费者，尤其 type/repr、tuple 转换的身份、copy/deepcopy、字段对 count/index 的遮蔽、双向比较/hash及直接/保存/暂停调用的取属性顺序。

另一条独立载体路线可保持 sealed，但至少需另接约 15 组序列/比较/hash/格式/JSON 等分派，plain tuple 左侧比较也必须识别新值。当前 MIR 没有 typed tuple 类型，tuple 注解及值走 object，因此两方案均无需为身份重开编译器 ABI；本次选总接点较少的派生，不以不存在的 typed tuple ABI 否决另一方案。

以下 A–C2 记录各阶段当时状态，最终结果见末段。

## 阶段 A 实际实现

- 私有 schema 保存字段顺序、字段索引、构造签名和右对齐 defaults。复制字段名和 defaults 槽，默认元素本身仍共享原引用。
- 每次内部建类产生真实新 `PyClass`，不占用异常家族 `BuiltinBase`，不按名称缓存类。
- 构造以独立 `_cls` 前缀复用现有 binder，不占合法用户字段 self/cls。全位置、全关键字、混合及默认值使用同一字段顺序。
- binder 可能复用输入数组，因此无论绑定路径如何，结果都复制到新 payload；零字段也创建新实例。外部实参数组后续变化不影响已建对象。
- 阶段 A 当时仅有内部入口，没有接 `PyClass.Invoke`、标准库导出、编译器白名单或 Python 可访问测试 API。测试侧反射只进入内部构造，不增加生产反射路径。

阶段 A 当时不将内部 PyTuple 派生关系等同 Python `type`/`isinstance`/字段属性已完成。现 binder 错误文本也未被本阶段冒作完整 CP 构造消息；后续只可做本 API 冷校准，不能重包用户异常或改变全局 binder。

## 验证与后续门

独立实体审后首次 `dotnet test` 仅选择 `PyNamedTupleLayoutTests`：9 通过、0 失败、0 跳过。覆盖同名类不同身份、零/一/多字段新实例、位置/关键字/混合、自身接收者不占字段、错误绑定、输入数组隔离、默认值引用和未注册状态。

尚未运行本阶段 Python 差分、完整门禁、项目、AOT 或性能组。B/C 必须闭合公开类型、字段/helper、继承/变更守卫及受影响 tuple 消费者之后，才能注册真实工厂；copy 等暂缓域须在旧快捷路径前明确拒绝，不能错误返回 plain tuple 或复用实例冒成功。普通 iterable、字段 str 转换及 rename 真值优先复用已有协议，按原 CP 顺序测试，不为减少工作一律拒绝。

## 阶段 B1：身份、字段与发布边界

内部生成类现经 `PyClass.Invoke` 进入专用构造，仍没有公共工厂导出。实例的真实类身份、tuple/object 祖先、类元数据与字段读取分开处理，不占用异常家族 `BuiltinBase`。`_fields` 与 `__match_args__` 共用公开 tuple；`_field_defaults` 是独立字典，其内容变化不重写私有绑定默认值。

字段使用固定下标的只读描述符，显式 `__get__` 接受整个 tuple 家族、忽略 owner，None 返回描述符自身；短 tuple 报 IndexError，非 tuple 报 TypeError。它不是只允许原生成类的假 property。实例无属性字典；生成类写/删、公开 `__dict__`、再派生、dataclass 和普通类 finalizer 旁路在修改前明确拒绝。本阶段暂缓这些 CP 合法扩展，不冒完整 namedtuple 类可变性。

新增的本 API 绑定错误校准仅包无用户协议的 `PyBinder.Bind`，准备 caller/context，不 catch 用户正文。实例字段名先于继承 count/index 查找；正常直接方法的求值前捕获、类侧 tuple 方法及 copy/getopt 等旧快捷路径仍由下一阶段闭合，当前不注册工厂来绕过缺口。

独立源码审通过后，父统一首次运行 `PyNamedTupleLayoutTests|PyNamedTupleIdentityTests`：18 通过、0 失败、0 跳过（A 原 9 项加 B1 新 9 项）。这仍是内部 C# 专项，不是 Python 公共 API 差分或完整门禁。没有运行新项目、AOT 或性能组。

## 阶段 B2：真实属性捕获与消费者边界

保留历史 `IsModule`/`CaptureModuleTarget` ABI 名称，但捕获资格扩到真实生成类/实例；方法属性在实参求值前冻结，缺属性先失败，已取到不可调用字段则仍先求参再报调用错误。PrepareMethod/CallMethod 优先新载体字段，不按字段名或 CLR 类型跨生成类缓存；普通 tuple/list 原分派保留。为遵守每文件 600 行，CallMethod 正文和注释完整迁入独立 partial 文件，不删注释凑行数。

生成类侧 count/index/__len__ 具有有限非绑定 tuple 描述符；实例绑定继承方法保原 receiver。index 的 start/stop 和这些原 tuple 方法的精确错误文本仍是旧边界，不借本段声称补齐。copy/deepcopy 遇 namedtuple 节点在遍历其成员前明确 NI，getopt 在原 longopts→args 顺序下拒绝该新增派生表示，避免旧 PyTuple 快捷路径误接纳；嵌套图已处理节点的副作用不回滚。

普通方法捕获入口新增类型检查，PyBuiltinMethod.Invoke/元数据读取也增加新载体识别；只有原静态列表证明能消除的形状继续依靠既有优化，不能称所有旧热路径零变化。未做性能测量。

独立源码审后首次累计专项 26/26 通过、0 失败/跳过（原 18 项加 B2 8 项，另加强原 B1 caller 帧断言）。公共工厂仍未注册；五组真实 Python 应用差分仅准备并源审，尚未执行。C 工厂/helpers、完整门禁和任何新项目验证仍待。

## 阶段 C1：内部工厂与名字存活取舍

内部工厂先完整绑定，再按原顺序逐项 fields→str、typename 类型检查、rename 真值及名称校验、defaults 完整物化，最后才读取默认 module。显式任意非 None module 原样保存，不触来源/真值。fields/defaults 的局部迭代边界保用户原异常，特殊槽 property 绑定与 hint 调用分开；吞 StopIteration 或允许忽略的 TypeError 后恢复帧深度和 ambient，避免随后来源查询读到残留 iterator 帧。没有改变全局 iter/list/tuple 或编译器 ABI。

精确 plain defaults tuple 保原对象；None 与空 tuple 区分，公开 defaults 载体稳定且元素共享。__bases__/__mro__ 改为每类创建时缓存，保持重复读取身份及各类隔离。当前 readonly defaults 的公共 helper 入口仍待 C2。

拒绝使用 CLR 永久字符串 intern 池保存用户可控名称。名称只随 schema 保存：同一类的 typename 与等值字段复用引用，跨独立创建类的等值名称不做全局规范化。内容比较、属性查找、类型/实例身份不变；这与 CPython 的跨类名字 `is` 结果存在明确差异，不能宣称完整字符串规范化兼容。

唯一固定 CPython 3.12.13 补充参考以 29+1 秒上限执行一次：0.107275 秒、exit0、3 行 JSON、空 stderr，证明动态不同输入的 typename/字段名被 CP 跨类合并，而类身份仍独立、普通值一致。原正例逐字保留为 `tests/fixtures/namedtuple_core/pending/name_intern_identity.py.pending`，不算 RyuPy 通过。没有为该细节引入全局弱表、同步或任意名字数量上限。

独立审后首次累计内部专项 37/37 通过、0 失败/跳过（原 26 项加 C1 11 项）。旧永久 intern 草稿从未集成/执行。仍未注册 `collections.namedtuple`，公共五应用、C2 helpers、完整门禁、项目及 AOT/性能均未执行。

## 阶段 C2：真实 helpers 与有限函数对象

保护的原生 callable 分开实现 static `__new__`、classmethod `_make` 和普通函数/绑定方法；不提供可假写却不影响执行的 PyFunction 元数据。name/qualname/module/doc/defaults 原对象稳定，metadata 写入/复杂函数反射明确 NI。字段描述符 doc 同样创建时保存；公开 `__get__(None)` 与双 None 按原绑定后报 TypeError，内部未绑定读取不混淆。

_make 完整消费后检查长度，忽略构造 defaults。_replace 按定义处字段与实际 receiver 内容惰性组合，再调用实际 receiver._make，之后才报剩余关键字；支持不同真实生成类 self，不把它们误拒成同类限定。_asdict/getnewargs 读实际 receiver；repr 使用定义处格式和实际类名，并保值转换与异长错误时序。函数与绑定方法的身份、eq/hash 走本地真实原生槽，不改全局 hash 或迭代机制。

显式 __new__ 与保存的 _make.__func__ 使用定义方签名，绑定完成后只允许原定义类；异 cls 的不安全异长载体分配明确 NI，不能误用另一类常规构造。非生成类 duck self 暂 NI。这里不宣称完整 Python 函数/描述符反射、用户派生或元数据可写能力。

独立实体审及 metadata 尾审后首次累计 47/47、0 失败/跳过（原 37 项加 C2 10 项）。旧未缓存 helper metadata 的草稿未执行。公共注册仍待独立最小接线；已审五应用还没有运行，不能以这些内部 C# 结果替代 CP/JIT 或项目验收。

## 公开接线与首轮真实应用

C3 仅新增保护的工厂singleton及collections导出，直转同一C1绑定器，不增加假Python帧/schema全局缓存。原阶段未注册断言明确迁为真实导出/callable/稳定同对象验证，新增3项后累计50/50首次通过。factory借普通类绑定为实例方法、doc/复杂反射明确NI，不用可假写函数元数据冒完整函数对象。

五组真实应用按序首次CP→compile→JIT全通过，stdout原字节相等且stderr空，固定标签25+34+14+29+21=123；有源内身份/顺序/结果断言，不仅输出计数。五次CP版本探针另计，5次真实CP、5次进程内编译、5次JIT，无重跑。外层600秒包含cargo准备和五组，不声称每应用独立超时。来源组额外3份Types/default MIR结构门锁实际Dynamic/生成器owner与Static原ABI；Static源码未注册第六应用，不冒其NI路径已运行。

测试helper只锁固定CP3.12.13及实际collections源码路径形状，不硬编码本机安装根。所有原流/源码/退出/编译诊断独占留证。其余pending原CP程序不计Ryu通过；只有名字intern补充参考实际单跑。

随后正常完整门禁首次通过：fmt/strict clippy/workspace、982项C#零失败/跳过、228项固定CP基线、202项e2e及30原ignored；Release构建0warning/0error。五应用在正常gate内亦通过；这次gate复验另计，不把首轮5应用称作本切总执行次数。原log SHA256为4fc54334f5e0488f5ce01b339bf67311e89ff20493e88ac4fa7a4e7eea81afbd，status0。该门禁阶段未重试固定tabulate、AOT或性能组。新增载体unseal及方法类型检查成本保留，没有性能收益结论。

## 后续固定 tabulate 单入口

在未发布候选 `4521e6e7ac07fdf01dc08aaa59f43998a5ad67a0`（tree `41f92607ebff46e3c8babd5e54b2bbc010f2e023`）上，Linux JIT 单入口按原合同只运行一次 CP→编译→JIT，另有一次候选 CLI 准备。官方 tabulate 0.9.0 sdist 为 81090 字节，SHA256 `0095b12bf5966de529c0feb1fa08671671b3368eec77d7ef7ab114be2c068b3c`；32 个源码文件与归档逐成员字节及原入口完全一致，没有修改项目源码。

CLI 准备 exit0、1.113965 秒；CP exit0 且输出与原 golden 精确相等，编译 exit0，JIT exit1、stdout 空，原 stderr 仅 `ModuleNotFoundError: No module named 'html'`，没有 traceback 帧。三个阶段分别 0.114610、1.871158、0.164838 秒，总 2.152068 秒；各阶段只一次、无超时，沿用 30/90/30 秒及总 180 秒限额，首失败停止。runner 自身 exit0 仅表示证据记录完成，不是项目通过。

实际锁定源码第 3 行导入 namedtuple，第 5 行导入 html，首次 namedtuple 工厂调用在第 52/55/89 行。因此本轮错误不再是缺 namedtuple 导出，但不能据此声称项目内这些类型已经构造或表格已经渲染；无帧原错误也不是唯一动态源码位置证据。

独立审重核官方归档/32 源、入口/golden、CLI/runtime/解释器、三个编译产物及六份原流全部哈希。result SHA256 为 `37121ed7cbddd4cda91b1446fb8cffe3e37170ae447d8591ade1841ef24ec775`。结论仅该候选的固定 Linux JIT 入口仍失败；不改历史 5/20，不冒 main、全库、AOT或性能通过。本段只归档结果，不重跑完整门禁。
