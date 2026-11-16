# 全 CIL 第三轮子合同验收（2026-11-16）

本轮接续 `0a130009`：C1 原生 finder/loader、C2 原生签名反射、C3 原生 async for，
以及 C4-1 Loader 关键字参数回归。独立严格内核与兼容产品继续分开验收。
完整阶段状态见[实施路线](cil-execution-roadmap.md)；第二轮的性能和矩阵记录保留在
[第二轮验收](cil-execution-second-slices-validation.md)。

## 冻结与公共接口

主代理在本轮源码冻结后构建 Release CLI，C1、C2、多版本与三形态审计使用同一份
外置编译器。随后补齐 Parameter repr 的外双引号冷修正，重建 v2；新增值对象差分与
本轮性能使用 v2。两份编译器的差异仅此冷显示格式及对应手写测试。
当前 Runtime 与真实 `5d36db1e` 基线进行公共签名集合比较：1843 项历史签名全部保留，
当前共 1967 项，新增 124 项，删除 0 项。新增反射元数据保持冷登记；普通函数布局和
完整供参 typed ABI 保持原合同。新增及修改代码文件均在 600 行内。

## C1：原生导入钩子

[ADR-0220](adr/0220-native-compiled-import-hooks.md) 使用静态 C heap type 替换两个
Python finder/loader 类及三个成员函数，三个内建描述符别名改为真实 C 类型缓存。
默认路径保留方法 wrapper 的 factory 与闭包；显式 native-methods 候选不注入源码。
旧源码安装 ABI 保留，`rp_op(29)` 固定拒绝源码执行。

3.9.25、3.12.13、3.13.3 各两种模式、33 项真实类桥，共 198 项通过；每个版本还运行
独立 C ABI 宿主。最终冻结 CLI 的两种模式均通过同次 CPython、JIT、NativeAOT、
删除输入与构建目录后的中文空格路径搬迁、私有分发隐藏及关闭首次导入。

| 模式 | startup | runtime | shutdown | wrapper factory / 闭包 | 已删除内部帧 |
| --- | ---: | ---: | ---: | --- | ---: |
| 默认 | 826 | 2251 | 37 | 1 / 1 | 0 |
| native-methods | 825 | 2249 | 37 | 0 / 0 | 0 |

每种模式的三份关闭报告计数一致，丢帧与覆盖失败均为零。
兼容产品仍执行 CPython 标准库，`complete/startup_complete=false`。
完整 PEP451 两阶段模块事务属于 C4-2。

## C2：签名反射

[ADR-0219](adr/0219-native-inspect-signature.md) 实现 Parameter、Signature、
BoundArguments、五类参数绑定、实时默认值/注解读取、wrapped 与显式签名。
CPython 桥通过显式 inspect profile 与受信 slot schema 导出真实宿主值对象；
用户函数的 canonical 元数据仍只有一份。

签名缓存的拥有引用由解释器私有模块状态管理，atexit 期间保持有效，Finalize 后仅
清空借用指针。冻结 CLI 的五组核心、桥、atexit、owner、attrs、Pydantic 和 FastAPI，
共 11 组同次 CPython/JIT/AOT 删除输入与构建目录后搬迁通过。FastAPI 验证真实
200/422 请求、OpenAPI 与 CIL 依赖回调；该输入是反射消费者窄门，原复杂应用的全 CIL
闭包继续待办。无 profile 的旧 globals 诊断及默认值未观察状态独立回归通过。
Parameter repr 先以 Runtime 断言复现旧显示差异，最窄冷修正后十组 Runtime 测试通过；
v2 的 values、bridge、atexit 三组产品差分与删除输入搬迁全部通过。
字符串注解运行期求值、3.14 延迟注解、精确 CPython CodeType 继续另切。

## C3：原生异步迭代

[ADR-0218](adr/0218-native-async-for.md) 在显式 native-coroutines feature 内实现
async for。Start 求值一次，每轮重新绑定 anext，耗尽保护区只覆盖 next 与 await；
目标赋值、正文和 else 的异常按实际来源传播。循环不隐式调用 aclose。

候选 21 项通过，其中七组真实 NativeAOT 删除输入/构建目录后搬迁；六组输入在
CPython 3.9.25、3.12.13、3.13.3 输出一致。修复 while 的零次执行出口、删除局部的
回边事实以及合成控制流覆写源码行号，普通 typed 路径保留原有快路。
事件循环、Task/Future、真实 IO、contextvars、异步生成器及跨域 awaitable 仍待开发。

## C4-1：Loader 关键字兼容

[ADR-0217](adr/0217-strict-kernel-frozen-extensions-and-cil-import-abi.md) 后续回归将
内建、扩展和 CIL Loader 方法统一按真实签名解析位置/关键字参数，拆分共享解析、
扩展 Loader 和 CIL Loader 职责，保留缺参、重复及未知关键字的 TypeError 负例。

官方 CPython 3.12.13 macOS arm64 完整源码构建通过，22 项严格内核探针、30 个真实
测试扩展编译、10 项 stock CPython 对照与七项变换测试全部通过。严格 clean 探针的
三阶段求值入口/执行/拒绝计数均为零。Linux、Windows 和真实 Host 产品接入待验。

## 整体集成

首份冻结源码的统一 make check 已通过：3398 项 C#、496 项默认 e2e、全部 Rust/fmt/clippy；
原有三项 C# skip 保留。六个稳定版本 3.9.25/3.10.21/3.11.15/3.12.13/3.13.3/3.14.7
均通过真实 NumPy/SQLite/SSL、自动探测、JIT 三流/副作用和 NativeAOT 删除项目/venv
后搬迁；3.15 稳定版待本轮产品实测。默认 JIT/Standalone/OneFile 三形态生命周期审计、
删除输入搬迁与严格门预期拦截通过。v2 统一检查已通过：3399 项 C#、496 项默认 e2e，
新增 repr 测试在内。五组已验反射输入移除 pending 后再次完整 make check，通过
3399 项 C#、501 项默认 e2e；三项 C# skip 与 295 项 e2e ignore 保留既有和未实现边界。
其中 C4-2 手写待办输入先经 CPython 基线修正了相对导入层级、失败 import 的局部绑定
及主脚本 builtins 类型假设；它继续保留 pending，仅作为后续实现验收合同。
三平台完整复杂项目全 CIL 与 C5 默认切换仍是开放完成门。

## NativeAOT 性能保护

macOS arm64，固定 CPython 3.12.13；旧臂为真实 `5d36db1e` 冻结 CLI，新臂为本轮 v2。
26 个原始基准各六次计时，另一次预热，CP/A/B 六种排列平衡；计时期间机器独占，
每项同次 CPython 三流比对、分配统计及中文空格搬迁通过，外部构建根自动清理。
原始样本仅保存在仓库外。本轮全量墙钟几何均值 **+0.82%**，正数表示变慢。

| 用例 | 旧臂中位 ms | 新臂中位 ms | 墙钟变化 | 分配变化 B |
| --- | ---: | ---: | ---: | ---: |
| hello | 5.198 | 5.363 | +3.16% | +3,904 |
| fib | 92.363 | 93.808 | +1.57% | +3,904 |
| mandelbrot | 172.187 | 172.056 | -0.08% | +3,904 |
| bigint | 83.367 | 82.685 | -0.82% | +3,904 |
| matmul | 26.997 | 27.004 | +0.03% | +3,904 |
| nbody | 300.557 | 290.343 | -3.40% | +3,904 |
| sieve | 27.630 | 28.512 | +3.19% | +3,904 |
| spectral_norm | 34.979 | 35.300 | +0.92% | +3,904 |
| calls | 96.576 | 100.819 | +4.39% | +3,904 |
| dynamic | 153.838 | 159.198 | +3.48% | +4,008 |
| attrs | 114.705 | 113.379 | -1.16% | +4,016 |
| oop | 90.425 | 90.140 | -0.32% | +4,008 |
| containers | 20.811 | 21.674 | +4.15% | +3,888 |
| dict_ops | 61.760 | 61.511 | -0.40% | +3,904 |
| list_ops | 78.942 | 78.567 | -0.47% | +3,880 |
| set_tuple | 74.300 | 73.718 | -0.78% | +3,904 |
| str_concat | 9.446 | 9.685 | +2.53% | +3,904 |
| strings | 106.912 | 96.936 | -9.33% | +3,912 |
| nqueens | 76.845 | 76.693 | -0.20% | +3,904 |
| sorting | 121.015 | 120.871 | -0.12% | +3,928 |
| exceptions | 50.533 | 52.530 | +3.95% | +10,671,912 |
| records | 118.826 | 119.469 | +0.54% | +4,008 |
| closures | 60.135 | 62.326 | +3.64% | +3,904 |
| comprehensions | 73.487 | 73.057 | -0.58% | +7,816 |
| decorators | 118.707 | 120.237 | +1.29% | +3,904 |
| generators | 218.330 | 234.712 | +7.50% | +5,008 |

异常额外分配主要继承第二轮真实 traceback 成本；普通项额外约 3.9 KB 包含新增冷登记。
本轮没有据此宣称吞吐优化。generators 回退超过第二轮记录，继续以第二轮冻结 CLI
和本轮 v2 做定向十二次配对，确认本轮增量；全量不利与有利数据均保留。

分配复核：第三轮相对第二轮，hello/calls/exceptions/generators 均增加 2712 B。
扣除各轮 hello 后，generators 两轮额外均为 1104 B，exceptions 均为 10,668,008 B。
这两项未增加逐次恢复/异常分配；源码复核也确认 PyGenerator、traceback、frame/code、
PyFunction/Binder 无本轮差异。墙钟变化继续依据直接配对，不能仅凭源码排除性能回退。

直接增量复验使用第二轮冻结 `second-cold-cli` 与本轮 v2，四项各十二次平衡排列：

| 用例 | 第二轮中位 ms | 第三轮中位 ms | 第三轮增量 |
| --- | ---: | ---: | ---: |
| calls | 95.368 | 98.128 | +2.89% |
| dynamic | 149.244 | 153.690 | +2.98% |
| exceptions | 50.681 | 51.113 | +0.85% |
| generators | 232.101 | 243.487 | +4.91% |

全部样本三流、统计与搬迁通过；每项相对第二轮均增加 2712 B。墙钟回退按真实成本保留，
当前尚未定位到新的逐次恢复/调用指令。功能子合同可独立交付，性能保护与 C5 默认切换
完成门继续开放，后续优化须保留本轮样本并重新配对。
