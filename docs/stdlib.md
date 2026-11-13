# 标准库移植计划

标准库在 Phase 3 单独交付，M3.4 系统/文件/路径与 M3.5 数值/JSON/正则已按 [系统 API 表](stdlib-system.md) / [数值 API 表](stdlib-numeric.md) 实现限定范围，M3.6 容器/函数工具/元数据限定范围见 [容器表](stdlib-collections.md)、[函数表](stdlib-functional.md)、[元数据表](stdlib-metadata.md)，不代表完整 dataclasses 或 typing。M3.7 已完成用户批准的有界逐 API 交付，已实现的有限范围见 [项目工具表](stdlib-project-tools.md)，未列能力不自动视为支持。模块能导入、API 能调用、项目测试通过分别记录。语言进度见 [roadmap.md](roadmap.md)，任务见 TODO 的 M3.4–M3.8。

## 标准库与应用联合打包

catalog 标准库源码、声明式 C ABI 与显式包数据现在可以共同生成一个 NativeAOT 分发包。
`build` 的 `.source-plan.json` 记录本次绑定 provider、源码闭包与运行时注册来源；
运行时模块的注册事实在编译器构建期从全部 C# 源文件生成，逐 API 兼容性仍按各模块表验收。
联合分发使用 schema9，自动源码位置与显式资源分别记录完整性。
macOS arm64 已通过真实 CP/JIT/Native、缺资源失败保旧 dist、中文路径搬迁和空工具 PATH 验收。
合同与复现见 [ADR0175](adr/0175-stdlib-binding-resource-distribution.md) 和
[联合分发测试](../tests/aot/stdlib_distribution/README.md)。

## 项目驱动的基础内建

`ord` / `chr` 直接调用已原生实现，独立于下方待做模块表：ord接受单码点str和单字节bytes/bytearray，
chr接受整数/bool与一次`__index__`转换；代理项可往返。严格拒绝错误参数和关键字，区分C int溢出与Unicode范围错误。
M3.3已接入一等内建函数值，ord/chr可保存/传参与调用，保持同名对象身份。这只描述基础内建；后续map等竖切已使wcwidth固定入口及九项指定上游场景通过，不代表整个标准库完成。

## 后续项目驱动的有限适配

本地候选 `shutil` 仅公开真实 `copyfileobj` 和可重绑 `COPY_BUFSIZE`：复用普通read/write流协议，
不自动关闭/刷新，不提供copy/copyfile/copymode/rmtree等路径复制。10项C#与4项实际CP/JIT
差分及完整门禁通过，尚未发布；当前不增加项目通过数。范围及首失败见[ADR0058](adr/0058-shutil-copyfileobj.md)。

本地候选 `zipfile` 仅提供真实 `is_zipfile` 尾记录识别与 `BadZipFile` 异常；
不提供ZipFile/解压，不以True代表完整归档有效性。本地专项与完整门禁通过、未发布，见[ADR0059](adr/0059-zipfile-tail-identification.md)。

## 工作规模与实现路径

C# 重写 Python 对象、异常、参数绑定和系统 API 的适配层，连同测试按万行级工程规划。这是排期量级判断，实际规模要在 API 盘点后复核。每个模块都要独立列 API、依赖、边界和验证，不能用一个总复选框代替。

| 路径 | 使用条件 | 代价与验收 |
| --- | --- | --- |
| C# 重写 / .NET 适配 | 常用 API，能保持 Python 可观察行为 | .NET 仅提供底层实现；补参数、Unicode、异常、顺序和平台语义，逐 API 做 CPython 差分 |
| 编译纯 Python 标准库源码 | 所需语法与底层模块已经可用 | 跟踪具体 CPython 版本、许可证和传递依赖；编译执行不能依赖未实现内部模块 |
| libpython 桥接 | 项目必要依赖尚未原生移植，或需要 CPython 扩展 | 嵌入解释器、转换对象并管理引用与 GIL；列为桥接，保留 Python 与扩展部署依赖 |

解析 Python 语法不能替代标准库实现；.NET 中存在同名或相似 API，也不能直接证明兼容。桥接宿主与语言运行时保持边界，Phase 3 先验证必要 API，Phase 4 再解决以 NumPy 为首的非纯 Python 模块导入与基本调用。数组内存协议属于后续优化。

## 分批范围

下表列出首批目标 API；M3.4 / M3.5 / M3.6 已实现范围和未支持参数以独立 API 表为准，其余仍待验证，不承诺整个模块。M2.0 项目盘点后可扩大范围，删减或换路径必须保留项目阻塞记录。

| 批次 / 里程碑 | 模块 | 首批 API 范围 | 需要单列的行为 |
| --- | --- | --- | --- |
| 系统与文件，M3.4 | `sys`、`os`、`os.path` | argv、标准流、exit、环境变量、目录与常用路径操作、受控模块缓存视图 | 编码、退出异常、模块状态、Windows / POSIX 路径差异；不宣称完整 sys |
| 系统与文件，M3.4 | `io`、`pathlib`、内建 `open` | 文本 / 二进制读写、上下文管理、常用 Path 读写与遍历 | 编码错误、换行、关闭、定位、权限与文件不存在异常 |
| 数值与数据，M3.5 | `math`、`time` | 常用标量数学、时间读取与计时 | 定义域、溢出、NaN、单调时钟；逐函数确定精度要求 |
| 数值与数据，M3.5 | `json` | loads / dumps / load / dump，常用参数 | 大整数、转义、键顺序、非法输入和异常位置；自定义编码器单独登记 |
| 数值与数据，M3.5 | `re` | compile / match / search / findall / sub，常用 flags | Python 模式语法、分组、Unicode、零宽匹配；.NET Regex 不等价，缺少的构造明确拒绝 |
| 组织与迭代，M3.6 | `collections`、`collections.abc` | deque、defaultdict、Counter、常用容器协议 | 默认工厂副作用、迭代失效、类型与协议检查；ABC 高级注册另列 |
| 组织与迭代，M3.6 | `itertools`、`functools`、`contextlib` | chain / islice / repeat、partial / reduce / wraps / lru_cache、contextmanager | 惰性消费、闭包与包装元数据、异常清理；缓存有容量上限且保持强引用规则 |
| 组织与迭代，M3.6 | `dataclasses`、`typing` | 字段默认值 / factory、repr / eq / frozen、常用类型别名与运行时元数据 | 扩展现有降级；装饰器返回行为、对象身份；完整泛型反射与 get_type_hints 另列 |
| 项目入口，M3.7 | `argparse`、`logging`、`unittest` | 常用参数解析、日志等级 / 格式 / handler、基础测试发现与断言 | 解析退出、stderr、格式、日志生命周期；高级插件与 runner 另列 |
| 项目补齐，M3.7 | `enum`、`copy`、`datetime`、`random`、`string` | 按冻结项目所需 API 建清单 | 特殊类构造、深拷贝循环、时区、随机序列均需验证；未通过项桥接或保持阻塞 |

每一行继续按单个 API 的竖切实施。若 `enum` 等依赖暂缓的类机制，应评估限定降级或桥接，不把受影响项目标成原生通过。

## 原源复用与构建期探测

版本化目录 `cpython-3.12.13-catalog-2` 已准入 `_markupbase`、`html`、
`html.entities`、`html.parser` 和 `colorsys`；每个模块仍按自己的 API 用例验收。
`colorsys` 复用 CPython 原源，六公开 API 已完成 55 行 CPython 差分和 macOS arm64
NativeAOT 搬迁，覆盖往返、色相分支、无彩色、截断与参数错误。转换值比较至小数九位，
自定义数值对象和完整上游测试继续待验。

`ryupy probe <入口> --aot -I <源码根> -o <报告目录>` 仅读取和解析源码，输出
`<入口>.source-plan.json`；普通源码构建也在编译图发现后输出同形报告。报告列出唯一 provider、
真实来源、版本、依赖闭包、传递缺口和动态调用候选，保留用户源码搜索优先级。
`queue` 只登记来源及依赖缺口，尚未准入；动态调用仍有静态盲区。

catalog 沿用 `ordinary-dynamic-module-pilot` 的显式策略门；发行默认构建关闭该策略时，
报告会给出 `catalog_unavailable`。复现 CLI 和逐 API 范围见
[ADR0174](adr/0174-versioned-source-catalog-probe.md) 与
[验收记录](validation/source-catalog-v2.md)。现有 C# provider 继续承担底层适配；SQLite、
OpenSSL 等后续接入复用成熟库，通过声明式 ABI 或显式兼容宿主处理部署和生命周期。

## 标准库底层能力与通用 C ABI

C# provider 提供对象语义和系统适配，纯 Python 上层实现复用 catalog 原源。
外部成熟 C 库通过声明式 ABI 接入：v3 已支持 13 类精确标量、void 返回，以及
UTF-8/bytes 输入展开的地址与 `size_t` 长度。C# 负责严格编码、BigInteger 范围检查、
缓冲快照和 pin 生命周期；编译器生成精确静态 PInvoke，NativeAOT 使用静态 archive 链接。
同一程序通过 `as_file()` 读取包资源后调用 C 函数，已完成 macOS arm64 搬迁与清除
Python 配置、工具 PATH 的执行验收。声明示例和错误合同见
[ADR0173](adr/0173-declarative-cabi-signatures.md)、[专项验收](../tests/aot/binding_abi/README.md)。

SQLite、OpenSSL 等沿用成熟库的实现；接入工作集中在公开 API、所有权、错误传播和部署。
opaque handle 将采用完整 SafeHandle 创建/销毁合同；可写缓冲、原生分配返回、回调、
多库依赖收集仍待实现。catalog 原源与 native binding 的联合准入保持独立集成项；
当前绑定构建沿用既有策略门，静态来源报告覆盖 probe 与普通源码构建。

## libpython 最小桥接，M3.8

先记录 Python 的准确版本、动态库定位、标准库路径、解释器初始化和退出边界，再做常用标量、字符串、容器、可调用对象与异常转换。对象包装保留共享身份；跨堆引用和回调要有可释放的所有权，不能制造无法回收的强引用环。

线程进入解释器时按该 Python 版本管理 GIL；borrowed / owned 引用与 .NET 生命周期分开处理。测量单次调用、批量调用、转换和复制成本。缺失 libpython、版本不匹配、调用失败均有测试。

桥接模块不能假装直接读写 RyuPy 的模块对象。编译模块与 CPython 的模块缓存、全局变量、回调和异常传播需要明确适配；不支持的混用拒绝。执行整段项目交给 CPython 只能算解释器回退，不能证明 RyuPy 编译兼容。

桥接需要把解释器、Python 标准库、原生扩展及其系统依赖纳入部署，不能宣传为免 Python 的单文件产物。嵌入接口参考 [Python 3.12 嵌入文档](https://docs.python.org/3.12/extending/embedding.html)。

## 每个 API 的完成标准

- 写明函数、类、参数与不支持部分，记录原生或桥接路径、平台和版本。
- 覆盖正常结果、错误类型、可变对象副作用和资源释放；移植上游测试时记录来源与许可证。
- 将模块登记到 Phase 2 的模块解析与运行时注册表，缺失 API 明确报错。
- 跑对应项目入口与固定测试，列出传递依赖；导入成功不能替代这些测试。
- 原生与桥接分开更新兼容性矩阵；新增差异进入 semantics.md 与 compatibility.md。

本地开发候选 `optparse` 提供有限真实OptionParser/Option/Values、六异常、常用单值解析及默认帮助；
不含OptionGroup/custom formatter/callback等完整长尾，当前差分6/6与完整门禁本地已验，尚未发布。
范围见[ADR0060](adr/0060-optparse-common-options.md)，不能据导入成功增加项目通过数。

本地候选 `getopt.getopt` 已提供有限真实短/长选项解析和GetoptError/error身份；当前21项C#、5项CP/JIT差分及正常完整门禁920C#/228CP/202e2e+30原ignored已验。仅确切list/tuple与str输入域，不含GNU或完整自定义协议，不据此增加项目通过数。见[ADR0075](adr/0075-getopt-local-exception-imports.md)。

### TypeVar 有限基础

真实 TypeVar 身份、核心属性、variance 及现有 typing 组合参数元数据见
[ADR0110](adr/0110-typevar-foundation.md)。NamedTuple/TypedDict 注解保变量原身份。
这不是 Generic 参数化继承、完整 typing 反射或 Markdown 入口通过；动态 bound helper、
变量替换和超域 ForwardRef 语法继续明确未实现。

### importlib.util 逐 API 有限支持

提供有限真实find_spec(name, package=None)与已编译源码能力的module_from_spec/create_module/exec_module，不以模块名登记代替API实现。
已编译源模块/包的发现与当前缓存读取见[发现合同](importlib-util.md)；固定版本CP事实、
完整源差分与门禁范围见[ADR0122](adr/0122-importlib-util-find-spec.md)。
公开编译loader首切见[ADR0129](adr/0129-public-module-from-spec.md)；sys.modules只准入[受限新模块别名](module-cache-alias-slice.md)。完整可写缓存、自定义finder和完整html.parser仍未完成。未发现域不能证明不存在，明确NI而不是假None。


### xml.etree.ElementTree有限对象

Element/SubElement、原值字段与attrib、append/insert/get/set/keys/items、整数/读slice、
活children和有限iter；ElementTree/getroot、Comment/ProcessingInstruction/PI、精确文本
QName及共享HTML_EMPTY已逐API验收。参考默认C加速对象，不是纯Python回退。
TreeBuilder、tostring/XMLParser/ElementPath、clear/extend/remove和写删children仍未做；
全反射与warning文件名也未对齐。范围/完整pending见[XML首切](xml-element-core-slice.md)。

### collections.OrderedDict有限精确类型

有限首切提供真实类型、标量键、双顺序、流式更新、move_to_end/popitem、
活视图及正反游标、有限相等与浅copy/fromkeys。dict只读根和公开序消费者分别接线，
根写入及未接通消费者明确NI；派生/用户hash键/union等不随导入开放。
31完整合同22严格通过、9pending；正常make check通过2577 C#/900 Rust/631 CP/
401 e2e、234 ignored，不等于远端发布。Markdown窄配置转缺NoReturn仍未过，默认
find_spec NI独立。范围见[容器API表](stdlib-collections.md)和[合同与验收](ordered-dict-core-slice.md)，
成本见[性能原报告](ordered-dict-performance.md)。

## importlib.resources 文件系统 as_file

`as_file(Path)` 已实现：返回原生生成器上下文，进入得到原 Path 对象，目录和不存在路径也可进入；退出保留文件并传播原异常。Path 协议及注册包资源两个 CP/JIT 差分用例通过，显式资源的 macOS arm64 NativeAOT 搬迁通过。

公开 `args/kwds/func/gen/__dict__` 与保存的绑定方法身份有差分覆盖。zip、自定义 Traversable 解包、singledispatch 注册/反射仍待实现；前两项保留 pending。无 SDK 目标环境及 Linux/Windows 新合同待验。见 [ADR0170](adr/0170-filesystem-as-file-context.md) 与 [复现记录](notes/resources-as-file.md)。

## importlib.resources 编译闭包候选补充

ADR0146只为JIT显式literal files锚点可选发现空普通直接子包；资源API运行时行为、AOT显式include/resources和旧负域不变。`files`重绑仍实际调用，未知/非空/编码声明候选不强制编译。候选尚待原pyfiglet/通用Native与完整同HEAD验收，不计新API或项目通过。

### 分发快照候选（ADR-0150，未验收）

新增显式闭包metadata请求与普通import_module候选；EntryPoint/EntryPoints只承诺name/value/group/load、iter/len/bool、select(name/group)、字符串索引。尚待CP差分/JIT/AOT及保护门，不计标准库或项目通过；高级构造、比较/hash、动态finder、zip与未配置环境明确NI。

修订002：未实现的version/files/requires/Distribution等高级API不公开，保持缺属性/from-import失败而非占位函数。无选项能import metadata但entry_points明确NI，不能将模块可导入当默认兼容。

### `itertools.starmap`（sortedcontainers 索引构建依赖）

原生逐行惰性解包调用；构造只取得外层迭代器，不提前验证 callable 或消费行。
覆盖 tuple/list 行、空输入、可变源、非可迭代行和 callable 异常后的继续、用户游标
非永久耗尽、`StopIteration.value` 及重入。复用已有参数调用与迭代协议，不声称
pickle 状态、任意自定义 descriptor 或迭代器子类完整兼容。
证据：`m3_6_itertools_starmap.py` 差分与 `PyStarMapTests`。
仅此 API 通过不代表 sortedcontainers 项目已运行通过。

### 版本位编码与当前线程身份

`sys.hexversion` 与固定语言目标 `3.12.13 final 0` 一致；不冒充 CPython 的实现身份。
`_thread.get_ident()` 返回当前实际 CLR 线程的非零身份，存活线程间不同，同线程稳定。
身份数值与 CPython 或其他进程不做比较，也不表示 OS native id。参数会正常拒绝。
仅提供此 API，不开放 Python 线程创建、锁或 GIL 控制。证据为
`m3_7_thread_identity.py` 差分与 `PyThreadIdentityTests` 的宿主双线程测试。

### collections.abc canonical类族（当前兼容竖切）

18个导出均为真实普通类，保留直接父图和C3身份：Iterable、Iterator、Reversible、Generator、Sized、Container、Callable、Collection、Sequence、MutableSequence、Set、MutableSet、Mapping、MutableMapping、MappingView、KeysView、ItemsView、ValuesView。模块导出重绑不替换内部canonical身份；用户子类采用nominal判断，不自动继承根的结构注册。继承定义完成时冻结抽象集合；普通类拷贝抽象函数不会自动变ABC。

提供Sequence/MutableSequence、Set/MutableSet、Mapping/MutableMapping常用mixins及live view。MappingView持有真实`_mapping`槽，空slots子类和实例字典遵循有限布局合同。原生生成器mixin仅提供iter/next与PEP479异常转换、耗尽终态；访问send/throw/close明确报NotImplementedError，不因显示为generator宣称完整控制协议。

边界：动态ABCMeta/register/update_abstractmethods、手写抽象集合和自定义抽象标记尚不支持；已认证抽象方法标记被改写时明确拒绝。`Set._hash`需与frozenset散列统一后再开放。Generator根默认throw的traceback参数尚不支持。Sized不可订阅；collections.abc.Callable非法订阅参数报TypeError，合法形状目前明确NotImplementedError，不冒充typing.Callable缓存别名。其余有限PEP585订阅保留真实canonical origin。

### 动态单基的延迟发布合同

编译器对已冻结的动态单基值使用独立冷准备入口，基值不重复求值，已知Generic别名只在旧单基构造核心解析一次。实际普通MI族进入同一C3验证/终结；其它类型继续str/list/dict、typing IO/Generic等既有单基专用布局，不把它们转换成MI。类体、终结或装饰器失败时的旧绑定和类cell由编译链保持。旧MakeClass仍拒绝MI族基类，动态异常继承仍需要编译异常factory，native MI等原有限域不放开。

运行时才识别出的MI继承要求`RuntimeFeature.IsDynamicCodeSupported`为真；NativeAOT及显式关闭动态码能力的JIT均明确报NotImplementedError。此限制只在真实MI分支消费，普通单基布局继续既有AOT能力；内部mode测试不能替代真实NativeAOT验收。
