# 兼容性清单与偏差

> **2026-11-02恢复与排期更新**：当前代码恢复自已推送M3.10（`a788d86`），其已归档项目能力为5/20。此前未推送M3.11源码随临时工作区丢失，历史10/20及725项C#测试不能当作当前checkout能力。用户已暂停16/20前置，优先原生AOT/JIT，并批准小步提交推送与独立WIP远端备份。详见[ADR0042](adr/0042-aot-jit-recovery-priority.md)；原兼容目标未达成、不勾完成。

本文从 Phase 2 开始持续更新。项目集已于 M2.0 冻结（20 个纯 Python 项目，6 类用途），
CPython 基线已逐项目记录；RyuPy 侧当前 **5/20 固定入口原生通过（25%）**。M2.9 已接入额外模块搜索根，
项目源码可以解析；剩余阻塞逐项目列在下表，集中在内建类型继承、导入/命名空间、typing元数据及剩余协议。
语言层面 M2.1–M2.8 的动态值、容器、普通类、异常、模块与 `with` 均有差分用例通过；M3.0 增加闭包、lambda 与 nonlocal，M3.1 增加 list / dict / set 推导式与惰性生成器表达式，M3.2 已增加yield状态机、返回值、send、现代单参throw/close、yield-from与跨暂停异常清理；旧式throw参数和通用兼容欠账仍明确列出。M3.3增加定义位置函数/类装饰器、可调用包装、函数元数据与混合dataclass动态表示。
2026-11-01 M3.7 进行中在 Linux x86_64 重跑固定项目集，wcwidth 入口编译、运行和固定基线输出一致，随后 termcolor、colorama 固定入口也通过，达到 3/20；没有新增桥接路径。termcolor 的 16 个强制 ANSI 上游场景现已通过，colorama 上游范围仍继续移植；不将指定范围通过称为完整库兼容。wcwidth 的独立上游核心九场景移植也通过，范围与许可见 tests/upstream/README.md，不另增加项目分子。
随后 texttable 与 progress 固定入口也通过。M3.7最终完整矩阵确认 **5 native / 14 unsupported / 1 failed**：chardet已编译并越过Flag，仍因bytes正则未支持运行失败。422 C#、214固定Python基线、189编译差分（29 ignored）、三个上游指定集合9/16/9及完整命令链均通过；这不代表原生16/20兼容门完成。
历史早期记录：ord/chr补齐后，wcwidth当时从ord前移至map；progress当时从生成器with清理前移到未知Progress基类。当时两者均未计通过，当前状态以上述最新证据与下表为准。
M3.3重跑attrs首阻断从类装饰器前移至Protocol基类，仍未计为项目通过。
M3.4原生系统/流/路径子集见stdlib-system.md；binaryornot首阻断从open前移至locals，当时固定项目仍0/20，Windows实际运行未验收。
M3.5新增math/time/json/re限定API与稳定模块异常导入，逐API范围见stdlib-numeric.md；固定项目复测仍0/20，tomli动态except表达式等阻断未解除。
M3.6 工具/元数据原始验收与扩展 API 差分、297 C# / 122 编译 e2e / 134 基线均通过；构造守卫、死装箱消除与受守卫累加槽已有独立实测与回退证据。当时 2026-11-01 重跑固定项目仍 0/20，首阻断保持（map/locals/Protocol/NamedTuple/继承等），不把 API 数计成项目通过。
急切推导式解除若干前端阻塞，但项目仍需后续语法和 API，不将语法差分通过计入项目分子。
语法现状见 [semantics.md](semantics.md)，标准库计划见 [stdlib.md](stdlib.md)。

用户已批准将标准库M3.7有界交付与原16/20兼容门拆分，后者保留为M3.11未完成项；
分母、入口、成功标准与当前5/20如实登记，详见 [ADR-0032](adr/0032-bounded-stdlib-and-compatibility-gate.md)。

## 兼容性目标怎么计算

M2.0 已按下面规则冻结选集，规则本身不再变动：

- 至少 20 个版本固定的纯 Python 项目，覆盖命令行工具、文本与数据处理、终端与文件自动化、
  通用库与数据结构等类别；
- 每个项目预先指定入口样例、上游测试范围与成功标准；
- 分母中的一个项目只有在「编译 + 运行 + 对照 CPython 基线」全部通过、依赖可用、没有未接受
  偏差时，才计为通过；只完成编译或 `import` 不计入；
- 测试范围有删减时逐项写原因，结果标注为「指定范围通过」，不声称整个项目兼容。

原生通过率 = 原生通过项目数 / 冻结项目总数（当前 20）。若选集恰为 20 个，至少 16 个原生
通过才达到目标。桥接通过和带性能扩展通过分别统计，不能补入原生分子。每次发布保留原选集
结果；扩充或改版时公布新旧两组结果。

最近验证环境（2026-11-01；历史性能基线仍使用各笔记注明的平台）：

| 项 | 值 |
| --- | --- |
| CPython | 3.12.13（`tests/pinned-python.txt`，脚本经 `uv python find` 解析） |
| .NET SDK | 10.0.403（运行时目标 `net10.0`） |
| Rust | stable（`rust-toolchain.toml`） |
| 平台 | Linux x86_64 |
| 执行路径 | 原生 JIT（`dotnet x.dll`）；AOT 见 M5.2 |

选集、入口样例与上游测试范围见 [tests/projects.md](../tests/projects.md)；清单文件是
`tests/projects/manifest.tsv`，抓取锁定在 `tests/projects/lock.tsv`。

## 项目矩阵

结果由 `cargo run -p ryupy-e2e --bin projects` 实测，结论写进「结果与阻塞」列。
M2.9 起模块搜索支持额外根（`ryupy build/run -I <dir>`），项目 runner 会自动把
`third_party/projects/<包>-<版本>/`（含 `src/` 布局与依赖）加入搜索路径，因此「找不到第三方包」
不再是统一阻塞；下表是逐项目的第一项编译 / 运行阻塞，也是下一步工作的排序依据。

| 项目 | 版本与来源 | 入口与上游测试范围 | 传递依赖 | 路径 / 平台 | 结果与阻塞 | 最近验证 |
| --- | --- | --- | --- | --- | --- | --- |
| six | 1.16.0 / PyPI | `six_sample.py`；上游 test_six.py 公共 API 子集 | 无 | 原生 / Linux x86_64 | unsupported：内建名称 `__import__` 尚未实现 | 2026-11-01 |
| sortedcontainers | 2.4.0 / PyPI | 三种有序容器基本操作 | 无 | 原生 / Linux x86_64 | unsupported：未知基类 `dict` | 2026-11-01 |
| toml | 0.10.2 / PyPI | loads/dumps 往返 | 无 | 原生 / Linux x86_64 | unsupported：函数内动态类声明（toml/decoder.py:644，同时涉及 dict 与多基类） | 2026-11-01 |
| tomli | 2.0.1 / PyPI | 数组表与错误位置 | 无 | 原生 / Linux x86_64 | unsupported：except 只支持静态异常类或稳定的原生模块异常导入；动态类型表达式暂不支持 | 2026-11-01 |
| pyparsing | 3.1.2 / PyPI | 表达式与结果命名 | 无 | 原生 / Linux x86_64 | unsupported：暂不支持 `from ... import *` | 2026-11-01 |
| attrs | 23.2.0 / PyPI | define/field/asdict（元类路径） | 无 | 原生 / Linux x86_64 | unsupported：Enum 成员仅支持 int/str/bool/None 字面量及此前成员别名 | 2026-11-01 |
| networkx | 3.3 / PyPI | 建图、最短路、度序列 | 无 | 原生 / Linux x86_64 | unsupported：暂不支持 `from ... import *` | 2026-11-01 |
| python-dateutil | 2.9.0.post0 / PyPI | parse 与 relativedelta | six==1.16.0 | 原生 / Linux x86_64 | unsupported：内建名称 `globals` 尚未实现 | 2026-11-01 |
| click | 8.1.7 / PyPI | command/option/参数解析 | 无 | 原生 / Linux x86_64 | unsupported：`typing.Generic[V]` 参数化基类，TypeVar/Generic 尚未实现 | 2026-11-01 |
| docopt | 0.6.2 / PyPI | usage 解析与默认值 | 无 | 原生 / Linux x86_64 | unsupported：未知基类 `list` | 2026-11-01 |
| pyfiglet | 1.0.2 / PyPI | 字体渲染 | 无 | 原生 / Linux x86_64 | unsupported：内建名称 `exit` 尚未实现 | 2026-11-01 |
| tabulate | 0.9.0 / PyPI | 表格格式与空表 | 无 | 原生 / Linux x86_64 | unsupported：未知基类 `str` | 2026-11-01 |
| texttable | 1.7.0 / PyPI | 列对齐 | 无 | 原生 / Linux x86_64 | native：固定入口与九个原上游布局场景通过（指定范围） | 2026-11-01 |
| markdown | 3.5.2 / PyPI | 转换与扩展 | 无 | 原生 / Linux x86_64 | unsupported：未知基类 `list` | 2026-11-01 |
| chardet | 5.2.0 / PyPI | detect 结果字段 | 无 | 原生 / Linux x86_64 | failed（M3.7最终矩阵）：编译及Flag初始化已通过，运行 NotImplementedError：bytes 正则未支持 | 2026-11-01 |
| wcwidth | 0.2.13 / PyPI | 宽字符与组合字符 | 无 | 原生 / Linux x86_64 | native：固定入口及九个上游核心场景通过（指定范围） | 2026-11-01 |
| colorama | 0.4.6 / PyPI | ANSI 常量与 init | 无 | 原生 / Linux x86_64 | native：固定入口输出一致；上游测试移植范围继续验收 | 2026-11-01 |
| termcolor | 2.4.0 / PyPI | colored 属性组合 | 无 | 原生 / Linux x86_64 | native：固定入口及 16 个强制 ANSI 上游场景通过（指定范围） | 2026-11-01 |
| binaryornot | 0.4.4 / PyPI | 文本/二进制判定 | chardet==5.2.0 | 原生 / Linux x86_64 | unsupported：内建名称 `locals` 尚未实现 | 2026-11-01 |
| progress | 1.6 / PyPI | Bar 构造与计数 | 无 | 原生 / Linux x86_64 | native：类体 if 与 mapping % 完成后输出一致（最终全矩阵确认） | 2026-11-01 |

分母 20，固定入口原生通过 4，**20%**；完整指定上游测试范围另行验收。M2.9 不要求此时达到最终 80%：当前阻塞集中在
**内建类型继承、动态基类、装饰器 / 元类、内建函数与文件 API**，属于 Phase 3 的语言与标准库工作。
每次推进都重跑项目 runner 并更新本表；不因未通过而修改分母。

## 结果状态

| 状态 | 含义 |
| --- | --- |
| 原生通过 | 指定测试范围在 RyuPy 编译代码与原生运行时上通过，无 libpython 回退 |
| 桥接通过 | 指定范围通过，但部分模块或对象依赖 libpython；列出边界与部署依赖 |
| 带偏差通过 | 仅在明确偏差或显式性能扩展下通过；列出偏差编号，不计入默认原生通过率 |
| 部分通过 | 部分 API 或测试可用；列出未通过项 |
| 不支持 | 已知阻塞，注明诊断或运行时异常及排期 |
| 未评估 | 尚未运行；不能据语法表推定可用 |

原生 JIT、原生 AOT、桥接 JIT 和桥接 AOT 分开记录。一个平台或入口通过不代表其它组合通过。

## 偏差登记

“低影响”由受影响项目、用例和测量支持。性能收益不能凭实现形式推断。后续接受偏差必须补上
最小示例、测试位置、受影响版本和替代写法。

| 编号 | 触发条件与差异 | 当前状态 / 处理 | 影响与替代方式 |
| --- | --- | --- | --- |
| D001 | 依赖对象离开作用域后立即析构、`__del__` 或弱引用回调的具体时机 | 架构边界，长期不复现 CPython refcount；相关用户 API 尚未完整实现 | .NET GC 的回收时机不同；使用 `with` / `finally` / `close()`，依赖原时机的项目列为有偏差或不支持 |
| D002 | `sys.getrefcount`、`id()` 的地址值、对象大小和解释器内部布局 | 内部观测接口暂缓，不伪造 CPython 数据 | 监测 CPython 内存细节的项目不在默认兼容承诺内；普通对象身份语义仍须保留 |
| D003 | 静态特化路径（带完整标注）的 Python `int` 超过 i64 范围 | 动态路径已按 ADR-0010 升级 BigInteger；静态路径仍是 i64，`+ - * // % abs ** <<` 越界抛 Python `OverflowError`（消息注明 RyuPy static integer limit），不泄漏 CLR 异常与 .NET 栈。示例 / 测试：`m2_1_bigint`、`snapshots.rs::fib_iter_100_overflows` | 去掉标注或在调用点传动态值即可走任意精度；静态路径的大整数结果当前受限 |
| D004 | Phase 1 frozen dataclass 赋值与传参采用值拷贝 | 已修（M2.5）：frozen dataclass 改为 .NET 引用类型，跨动态调用只复制引用，身份稳定；ADR-0008 的值类型降级不再用于默认路径。测试：`m2_5_identity`、`m1_5_dataclass` | 缺少针对值表示的显式性能扩展；需要值语义的优化必须先证明身份不可观察或由用户显式选择 |
| D005 | 深递归 | 已修（M2.2）：`PyTraceback.PushFrame` 按帧数守卫，超过 1000 层抛可捕获的 `RecursionError`，可用 `RYUPY_RECURSION_LIMIT` 调整。测试：`m2_2_recursion` | 默认上限比 CPython 保守（一层 Python 调用占多层 CLR 栈帧）；极深递归仍可能先耗尽 CLR 栈 |
| D006 | Phase 1 签名标注与模块级变量限制 | M2.1 / M2.7 已解除；M3.7 已支持函数内静态导入及 local/global/nonlocal 绑定，见[函数内导入范围](function-imports.md)。测试：`m2_1_*`、`m2_7_modules`、`m3_7_function_imports`、`m3_7_function_import_bindings`；星号导入、类体导入及函数本地异常别名作为动态 except 类型仍明确诊断 | 受限代码仍在编译期报错，不能据路线图认定已可用 |
| D007 | Phase 1 除零、数值转换等错误暴露 .NET 异常，浮点除法缺少完整 Python 检查 | 已修：真除法 / `//` / `%` / `int(float)` 统一抛 Python 异常，未捕获时按 Python 形式退出（M2.2）；索引 / 属性错误已随 M2.3 / M2.5 接入 Python 异常。测试：`m2_2_exceptions`、`m2_3_*`、`errors/unhandled_zero_division` | 无已知剩余差异 |
| D008 | `with` 的 `__enter__` / `__exit__` 使用普通属性查找，CPython 按类型槽查找 | M2.8 已知边界；类方法与常见上下文管理器无差异，`m2_8_with` 覆盖这些路径 | 不要在实例字典里遮蔽 `__enter__` / `__exit__`；需要类型槽精确语义时改用普通 `try` / `finally` |
| D009 | 两个独立代理码点拼接后UTF-16表示将其当作一个补充码点 | 已知待修，非已接受性能偏差；`ord(chr(0xD800)+chr(0xDC00))`应TypeError却返回65536，独立pending `m3_builtins_surrogate_pair`，见ADR-0025 | 普通补充平面/单代理项支持；依赖独立相邻代理码点的程序不计兼容，后续统一处理字符串表示 |

D001 接受生命周期模型差异，不自动接受任何 `__del__` / weakref 行为；实际开放 API 时仍需定义
支持范围和测试。D003、D005、D006、D007 属于待修限制，不作为永久性能取舍。

## 性能实现的边界

| 可以默认采用 | 需要守卫或用户选择 | 当前暂缓 |
| --- | --- | --- |
| 保留结果和副作用的拆箱、shape 槽位、模块槽位、有上限的 PIC（`RYUPY_PIC=0` 可关闭）、非逃逸临时对象消除 | 固定宽度整数、可观察的值类型身份变化、浮点重排、改变匹配或随机序列的替代算法；新增前写 ADR，默认关闭 | 任意元类、多重继承、完整描述符协议、`eval/exec`、动态 import hook、frame / tracing 内部接口、完整 asyncio |

有界 PIC 与动态热路径优化是当前默认性能实现，可用环境变量 `RYUPY_PIC=0` 关闭快速入口
（见 ADR-0012 / ADR-0013）。其它性能扩展仍是边界说明。字典顺序、相等与哈希、可变别名、异常传播、求值顺序和普通对象身份
属于常见行为，不按“冷门差异”略过。

## 拒绝与报错

静态可确定的不支持能力报告源码 `Diagnostic`。运行时才能知道的缺失属性、模块或非法操作，
分别抛 Python 语义的 `AttributeError`、`ModuleNotFoundError`、`TypeError` 等，并附导入或调用
位置。构建时声明的桥接依赖缺失必须明确失败，不能偷偷切换执行引擎或静默返回占位值。

低影响差异经 ADR 接受并公开登记后，可以采用高性能默认实现。默认构建的限制与偏差进入可读报告；
影响常用计算结果的性能扩展由用户显式启用，在执行前可见并进入产物记录。M2.9 已提供 PIC 的
运行期开关，统一的限制报告与产物记录仍安排在 M5.1。

## 规范依据

Python 数据模型允许不同垃圾回收实现，并提醒程序显式关闭外部资源；refcount 和内存地址属于
CPython 实现细节。RyuPy 因此保留 GC 差异，同时继续实现普通对象身份与可变别名。
[Python 3.12 数据模型](https://docs.python.org/3.12/reference/datamodel.html)

Python 运行时不强制函数与变量的类型标注，Phase 2 特化不能自行增加这种限制。
[Python 3.12 typing](https://docs.python.org/3.12/library/typing.html)
