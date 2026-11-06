# 标准库移植计划

标准库在 Phase 3 单独交付，M3.4 系统/文件/路径与 M3.5 数值/JSON/正则已按 [系统 API 表](stdlib-system.md) / [数值 API 表](stdlib-numeric.md) 实现限定范围，M3.6 容器/函数工具/元数据限定范围见 [容器表](stdlib-collections.md)、[函数表](stdlib-functional.md)、[元数据表](stdlib-metadata.md)，不代表完整 dataclasses 或 typing。M3.7 已完成用户批准的有界逐 API 交付，已实现的有限范围见 [项目工具表](stdlib-project-tools.md)，未列能力不自动视为支持。模块能导入、API 能调用、项目测试通过分别记录。语言进度见 [roadmap.md](roadmap.md)，任务见 TODO 的 M3.4–M3.8。

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

当前只提供真实find_spec(name, package=None)，不以模块名登记代替API实现。
已编译源模块/包的发现与当前缓存读取见[发现合同](importlib-util.md)；固定版本CP事实、
完整源差分与门禁范围见[ADR0122](adr/0122-importlib-util-find-spec.md)。
module_from_spec、create_module、exec_module、可写sys.modules、自定义finder和
完整html.parser尚未实现。未发现域不能证明不存在，明确NI而不是假None。
