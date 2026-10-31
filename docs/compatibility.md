# 兼容性清单与偏差

本文从 Phase 2 开始持续更新。项目集已于 M2.0 冻结（20 个纯 Python 项目，6 类用途），
CPython 基线已逐项目记录；RyuPy 侧当前 **0/20 原生通过**，全部阻塞在导入系统（M2.7）。
语言层面 M2.1 的动态值管线已接通（未标注函数、异构赋值、大整数均有差分用例通过），
但项目入口都要 `import`，所以分母上的数字还不会变化。
语法现状见 [semantics.md](semantics.md)，标准库计划见 [stdlib.md](stdlib.md)。

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

固定环境（2026-10-31 验证）：

| 项 | 值 |
| --- | --- |
| CPython | 3.12.13（`tests/pinned-python.txt`，脚本经 `uv python find` 解析） |
| .NET SDK | 10.0.403（运行时目标 `net10.0`） |
| Rust | stable（`rust-toolchain.toml`） |
| 平台 | macOS arm64 |
| 执行路径 | 原生 JIT（`dotnet x.dll`）；AOT 见 M5.2 |

选集、入口样例与上游测试范围见 [tests/projects.md](../tests/projects.md)；清单文件是
`tests/projects/manifest.tsv`，抓取锁定在 `tests/projects/lock.tsv`。

## 项目矩阵

结果由 `cargo run -p ryupy-e2e --bin projects` 实测，结论写进「结果与阻塞」列。当前全部为
`unsupported`：编译器仍拒绝普通 `import`，阻塞统一记在 M2.7。

| 项目 | 版本与来源 | 入口与上游测试范围 | 传递依赖 | 路径 / 平台 | 结果与阻塞 | 最近验证 |
| --- | --- | --- | --- | --- | --- | --- |
| six | 1.16.0 / PyPI | `six_sample.py`；上游 test_six.py 公共 API 子集 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| sortedcontainers | 2.4.0 / PyPI | `sortedcontainers_sample.py`；三种有序容器基本操作 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| toml | 0.10.2 / PyPI | `toml_sample.py`；loads/dumps 往返 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| tomli | 2.0.1 / PyPI | `tomli_sample.py`；数组表与错误位置 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| pyparsing | 3.1.2 / PyPI | `pyparsing_sample.py`；表达式与结果命名 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| attrs | 23.2.0 / PyPI | `attrs_sample.py`；define/field/asdict（元类路径） | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7）；元类预期另需评估 | 2026-10-31 |
| networkx | 3.3 / PyPI | `networkx_sample.py`；建图、最短路、度序列 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| python-dateutil | 2.9.0.post0 / PyPI | `dateutil_sample.py`；parse 与 relativedelta | six==1.16.0 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| click | 8.1.7 / PyPI | `click_sample.py`；command/option/参数解析 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| docopt | 0.6.2 / PyPI | `docopt_sample.py`；usage 解析与默认值 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| pyfiglet | 1.0.2 / PyPI | `pyfiglet_sample.py`；字体渲染 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| tabulate | 0.9.0 / PyPI | `tabulate_sample.py`；表格格式与空表 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| texttable | 1.7.0 / PyPI | `texttable_sample.py`；列对齐 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| markdown | 3.5.2 / PyPI | `markdown_sample.py`；转换与扩展 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| chardet | 5.2.0 / PyPI | `chardet_sample.py`；detect 结果字段 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| wcwidth | 0.2.13 / PyPI | `wcwidth_sample.py`；宽字符与组合字符 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| colorama | 0.4.6 / PyPI | `colorama_sample.py`；ANSI 常量与 init | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| termcolor | 2.4.0 / PyPI | `termcolor_sample.py`；colored 属性组合 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| binaryornot | 0.4.4 / PyPI | `binaryornot_sample.py`；文本/二进制判定 | chardet==5.2.0 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |
| progress | 1.6 / PyPI | `progress_sample.py`；Bar 构造与计数 | 无 | 原生 / macOS arm64 | 不支持：`import`（M2.7） | 2026-10-31 |

分母 20，原生通过 0，**0%**。这个数字在 Phase 2 期间会随 M2.1 / M2.2 / M2.3 / M2.5 / M2.7 推进
而变化，每次变化都重跑项目 runner 并更新本表；不因未通过而修改分母。

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
| D003 | Phase 1 Python `int` 超过 i64 范围 | 当前 checked 溢出；M2.1 改为 BigInteger 升级，完成前保留失败记录 | 大整数计算当前受限；升级后改为差分成功用例 |
| D004 | Phase 1 frozen dataclass 赋值与传参采用值拷贝 | 当前实现见 ADR-0008；M2.5 / M2.6 进入动态路径前补身份策略，受 ADR-0009 限定 | 当前仅支持静态字段子集；后续默认保留身份，值表示需证明身份不可观察或显式选择 |
| D005 | 深递归 | 当前没有 Python 递归深度守卫，可能发生 .NET 栈溢出；M2.2 研究可控守卫 | 不能依赖与 CPython 相同的递归上限；改写循环或保持项目阻塞，不能靠捕获栈溢出来补救 |
| D006 | Phase 1 签名标注、模块级变量与导入限制 | 当前限制；分别由 M2.1 / M2.7 解除 | 现阶段受限代码在编译期报错，不能据路线图认定已可用 |
| D007 | Phase 1 除零、数值转换等错误暴露 .NET 异常，浮点除法缺少完整 Python 检查 | 当前差异；M2.2 统一 Python 异常并补边界检查 | 目前不能依赖 Python 异常类型捕获；不把正常路径输出一致视为错误路径兼容 |

D001 接受生命周期模型差异，不自动接受任何 `__del__` / weakref 行为；实际开放 API 时仍需定义
支持范围和测试。D003、D005、D006、D007 属于待修限制，不作为永久性能取舍。

## 性能实现的边界

| 可以默认采用 | 需要守卫或用户选择 | 当前暂缓 |
| --- | --- | --- |
| 保留结果和副作用的拆箱、shape 槽位、模块槽位、有上限的 PIC、非逃逸临时对象消除 | 固定宽度整数、可观察的值类型身份变化、浮点重排、改变匹配或随机序列的替代算法；新增前写 ADR，默认关闭 | 任意元类、多重继承、完整描述符协议、`eval/exec`、动态 import hook、frame / tracing 内部接口、完整 asyncio |

这些性能扩展目前只是边界说明，尚无对应 CLI 开关。字典顺序、相等与哈希、可变别名、异常传播、
求值顺序和普通对象身份属于常见行为，不按“冷门差异”略过。

## 拒绝与报错

静态可确定的不支持能力报告源码 `Diagnostic`。运行时才能知道的缺失属性、模块或非法操作，
分别抛 Python 语义的 `AttributeError`、`ModuleNotFoundError`、`TypeError` 等，并附导入或调用
位置。构建时声明的桥接依赖缺失必须明确失败，不能偷偷切换执行引擎或静默返回占位值。

低影响差异经 ADR 接受并公开登记后，可以采用高性能默认实现。默认构建的限制与偏差进入可读报告；
影响常用计算结果的性能扩展由用户显式启用，在执行前可见并进入产物记录。这些报告机制安排在
M2.9 与 M5.1，当前尚未实现。

## 规范依据

Python 数据模型允许不同垃圾回收实现，并提醒程序显式关闭外部资源；refcount 和内存地址属于
CPython 实现细节。RyuPy 因此保留 GC 差异，同时继续实现普通对象身份与可变别名。
[Python 3.12 数据模型](https://docs.python.org/3.12/reference/datamodel.html)

Python 运行时不强制函数与变量的类型标注，Phase 2 特化不能自行增加这种限制。
[Python 3.12 typing](https://docs.python.org/3.12/library/typing.html)
