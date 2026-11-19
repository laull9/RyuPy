# NativeAOT 编译器性能候选与验证计划

2026-11-15：编译器代理完成静态调查，主代理复核修订。本文记录候选和验证要求；尚未取得本轮全量 NativeAOT 基线。用户提供的 26 项运行均在产物布局验证阶段失败，当前先恢复测量入口。

## 1. 范围与决策原则

固定 CPython 3.12.13、相同源码与输入，逐项对照速度、内存；速度优先，按实测提出取舍。纯 RyuPy、随包 CPython 与 onefile 分别计量。保留整数溢出升级、身份、别名、用户协议、副作用顺序、异常和回调可观察状态。

下面的 MIR 观察属于初始生成或特定 Pass 的规则，最终 CIL、ILC 优化结果和运行热点需要另外验证。不能从源码中存在一个 Box 推断每次循环最终产生一个堆对象，也不能从某个 Pass 的拒绝条件推断整个函数没有优化。

## 2. 核对后的管线观察

### 模块顶层优化候选

[build.rs](../crates/ryupy-mir/src/build.rs) 对 ModuleMain 不分配 signature_global。[numeric_accumulator.rs](../crates/ryupy-mir/src/passes/numeric_accumulator.rs)、[tuple_scalar.rs](../crates/ryupy-mir/src/passes/tuple_scalar.rs) 等特定优化以 signature_global.is_none() 拒绝候选。CFG 简化和失用值清理仍在流水线中，顶层也会调用已经优化的用户函数。

模块绑定是可观察状态，需区分真正临时局部与模块槽位；globals()、导入、回调、异常及用户协议都可能观察中间值。放开签名门禁只是扩大候选面，匹配器的无逃逸、唯一写入、循环结构和观察点条件仍须逐一成立。顶层优化需独立合同、测试和必要 ADR，不能仅删除门禁就作为低风险修复。

[PyNumericAccumulator.cs](../runtime/RyuPy.Runtime/PyNumericAccumulator.cs) 已包含精确字符串的 StringBuilder 延迟物化。str_concat 顶层循环值得调查，但实际不命中的原因需用 MIR 确认；不能承诺放开一个条件即可消除全部二次方复制。

### range 条件与装箱

[build_loops.rs](../crates/ryupy-mir/src/build_loops.rs) 的动态 range 降级先冻结 start/stop/step，用 i64 计数，条件组合正负步长比较，并在循环体写入装箱后的 Python 目标。常量步长条件简化和未使用目标的 Box 消除是候选。

实施前检查最终 MIR/CIL 与 ILC 输出，确认冗余分支/分配是否仍在。优化必须保留循环参数一次求值、目标变量最后一次值、零次迭代时原值、break/continue/else、动态步长方向及整数边界。延迟装箱涉及身份观察和异常点，比常量方向折叠更复杂，应拆成两个竖切。

### 直接调用已经存在

[call_builder.rs](../crates/ryupy-mir/src/call_builder.rs) 的 build_user_call 支持静态函数和符合 fn_sources 条件的动态函数直接生成 Callee::Fn；编译期绑定支持默认值和关键字，并非仅支持纯位置参数。参数副作用前捕获实际函数引用，之后从该对象取 defaults/Module，属于必须保留的语义。

星号参数、动态来源限制或绑定失败回到 published function。其余动态对象调用可经过 PIC、参数 scratch、委托和 thunk。只有在实际 MIR 确认仍走该链路后，才研究 guarded direct call；同模块同名不足以证明函数对象未替换。

### 异常、闭包与生成器

EH、环境 cell、暂停和函数 locals 观察会限制部分特化。不能为获得特化删除这些拒绝条件；应按候选证明跨区域安全或保留物化边界。

[generators.rs](../crates/ryupy-mir/src/generators.rs) 中 frame/saved_locals 值得做暂停点活跃性调查，需确认实际保存集及恢复路径。任何引用提前清除必须覆盖 send/throw/close、yield from、finally、回调及帧观察，不能只依赖正常 next 输出。

## 3. 实验与构建配置复核要求

| 候选或历史实验 | 本轮处理 |
| --- | --- |
| direct accumulator（ADR-0033/0034） | 默认继续 holder；历史配对结果曾阻止晋升，当前 AOT 须重新 A/B，历史 JIT 数字不能当成本轮原因或收益 |
| foreach accumulator（ADR-0135） | 核对自动模块/InstanceGlobals 配置及严格匹配条件；未匹配与匹配后无收益分别记录 |
| leaf inline | 源码有界叶摘要仅证明候选有限，不能凭 matcher 窄断言收益为负；实际路径、守卫与调用次数需确认 |
| scalar/packed SLP（ADR-0040） | 保留同守卫标量控制，测 ILC 代码与装箱开销；不因使用 SIMD 就晋升 |
| numeric list（ADR-0039） | 核对实际匹配和默认开关；先扩大证据，再决定编译器岛或运行时存储方案 |
| AOT Speed | 在 SDK10.0.403 中确认有效属性及默认配置，单独 A/B 测吞吐、RSS、体积及构建时间 |
| 方法折叠/反射元数据 | 先检查 SDK 默认值与 runtime/provider 反射依赖；不同 token/重定位的 thunk 也可能无法合并，不预先承诺体积收益 |

不混入 InvariantGlobalization 等功能变化。NativeAOT 优化质量以本次实际原生产物和适用工具的机器码证据确认；JIT 反汇编另外登记。

## 4. 固定 26 项输入的调查映射

以下是源码形态对应的调查点，最终路径待 dump/实际产物确认。保留原 benchmark 文件、规模与 pending 状态。

| 基准 | 调查点 | 必须验证的边界 |
| --- | --- | --- |
| hello | 宿主、provider 初始化、onefile 解包 | 直接脚本启动、standalone 与 onefile 分列 |
| fib | 静态递归/迭代与顶层调用开销 | 最终直调、溢出行为、启动占比 |
| mandelbrot | 静态浮点核、外层动态控制和字符串 | typed CIL、输出完全一致 |
| bigint | 溢出升级、大数运算与规范化 | 大整数正确性、实际分配热点 |
| matmul | 嵌套列表索引、动态浮点算术 | 原始 100×100 重复 3 次、浮点结果 |
| nbody | 列表字段读写与动态浮点 | 写回顺序、别名与误差合同 |
| sieve | 布尔列表引用存储与索引 | 原始输入规模、列表身份/异构写入 |
| spectral_norm | typed eval_a 与动态调用者 | 先确认已有直接调用、浮点装箱 |
| calls | 位置/默认/关键字/星号/双星号绑定 | 逐类实际路径、TypeError、默认值身份 |
| dynamic | 函数与方法调用、累加器 | 已有直调/PIC 命中、重绑定和回调 |
| attrs | 属性读写、property 与版本检查 | 数据描述符优先级、实例遮蔽、失效 |
| oop | 四类轮换及对象创建 | 当前 PIC 容量4，四类本身不会必然抖动；测实际命中/版本变化 |
| containers | 列表、字典、字符串组合 | 逐段成本、模块可观察绑定 |
| dict_ops | 索引/哈希/有序遍历和删除 | 不将墓碑遍历误认为普通键查找 |
| list_ops | append/get/set/pop/slice/extend/insert | 负下标、切片复制、别名 |
| set_tuple | 集合及临时/逃逸 tuple | 作为键的 tuple 必须物化，拆包临时另验 |
| str_concat | 顶层字符串累加与 join 对照 | 模块观察、每次快照身份及异常点 |
| strings | 方法分派与格式化 | Unicode、协议转换顺序 |
| nqueens | 递归、布尔列表索引 | 完整搜索工作量、调用路径 |
| sorting | 比较/key 与稳定排序 | 实际专用比较快路、相等键稳定性 |
| exceptions | 真实抛捕、finally、无异常保护区 | cause/context/traceback、处理状态 |
| records | 解析、元组键、聚合、排序与格式化 | 完整流水线和返回结果 |
| closures | cell 读写、函数对象及间接调用 | 捕获共享、默认值及闭包身份 |
| comprehensions | 隐式函数、迭代、容器构建 | 推导式作用域、求值顺序 |
| decorators | 包装器、变长参数、闭包 | 调用次数、副作用、异常顺序 |
| generators | yield/resume、frame 和迭代协议 | send/throw/close、finally 与引用释放 |

26 项语言基准之外，随包 provider 的联合应用、真实 NumPy 及固定项目代表输入独立测量。编译器本地快路改善不必然降低跨桥/解包成本，应发布各自的证据。

## 5. 竖切候选及实施门槛

### A：常量 range 方向简化

确认最终代码确实保留多余判定后，在 MIR 层按已冻结的常量步长生成有限简化，codegen 保持机械翻译。编译时为正、负、动态以及非法步长均有测试；运行时端到端保留参数副作用与循环终值。先做这一局部变化，不捆绑变量表示改变。

### B：非逃逸循环临时值的延迟装箱

先选择真正临时或可证明不可观察的函数局部。证明所有读取、重赋值、异常出口、cell 与身份观察；仍需发布到模块槽位的值不能省略。保持单版本物化的同一性，守卫失败回通用路径。测实际 Box 消除、分配与逐项速度，不能仅凭中间 IR 指令减少判成功。

### C：顶层受观察约束的累加/tuple 特化

为编译器临时局部和 Python 模块绑定分别制定准入规则。只允许匹配器证明未逃逸且观察点完备的候选；globals/locals、导入、模块回调及异常中途读取采用正确物化或拒绝。无需为模块伪造函数签名。接口和观察合同变化登记 ADR，再做异构评审。

### D：实际动态调用点的守卫直调

先用 calls/dynamic/spectral_norm 输出确认已有直调范围。对剩余调用保留 callee 求值、实参副作用、真实函数身份、defaults、module context 和重绑定。匹配目标时 direct core，不匹配时原 binder/PIC；变长参数与错误路径不擅自扩大。与 B/C 可独立推进，无强制依赖链。

### E：密集数值容器

只有基线确认容器存储或装箱占主导后，比较已有编译器 numeric list 岛和运行时自适应背板。bool[]/double[] 会影响元素身份、混合写入、切片、迭代、子类和跨桥同步；列表切片保持 Python 的独立列表语义。存储物化要保留容器身份和所有别名，不能重新装箱改变已有元素身份。作为重大架构单独 ADR/异构评审，收益以真实 RSS 与速度测量。

## 6. 验收与当前状态

每个竖切保留原源码、输入和模式，先写有意义的语义差分/结构断言，使用同次固定 CPython；覆盖 guard fallback 与异常，不生成 golden/snap/patch。优化产物做同输入 A/B，观察默认模式和 provider 模式是否出现退化，必要时实机搬迁。提交前完成 make check，人工记录已跑命令和平台限制。

当前仅完成源码调查与主代理修订，尚未实施上述产品优化。优先级随真实基线热点排序；任何历史性能数字、静态内存估算和源码路径猜测都不作为本轮达标结论。
