# ADR-0218：原生 async for 的有限完整竖切

- 日期：2026-11-16
- 状态：显式原生协程候选；三版本oracle、JIT与macOS arm64 NativeAOT搬迁通过
- 接续：[ADR-0210](0210-native-coroutine-state-machine.md)、
  [ADR-0215](0215-native-async-with-traceback.md)

## 决定与准入

继续使用默认关闭的 `native-coroutines` Cargo 特性。开启后，原生协程中的
async for 复用 await 显式状态机、源码循环控制和真实异常传播；编译层保持纯函数。
关闭特性保留现有 E0100 与 SupportBoundary 延期分类。此切不晋升默认模式，
不引入 CPython 解释执行、事件循环、Task 调度器、异步生成器或异步推导式。

手写成功输入从pending转入 `tests/coroutines/async_for_*.py`，由同一个
feature专用集成门发现。异步生成器、自定义 `__getattribute__`、变量别名动态继承
内建异常仍保留为 `tests/e2e/phase3/c3_async_for_*_boundary_pending.py` 的独立负例；
该动态异常基类需要C1后续支持，不能因消费它的代码包含async-for而悄悄放行。

## 类型槽、求值和暂停

1. 源表达式仅求值一次，按对象实际类型查找并同步调用一次 `__aiter__`。
   实例同名字段、实例 `__getattribute__` 和 `__getattr__` 不参与特殊方法查找。
   返回对象立即检查类型是否具有 `__anext__` 槽；检查不能提前绑定或执行描述符。
   `__aiter__` 返回 coroutine/其它 awaitable 不触发 await，直接报告协议错误。
   `__aiter__` 的调用失败（包括 StopAsyncIteration）位于耗尽保护区之外。
2. 每一轮按迭代器当前类型重新检查 `__anext__` 槽、绑定并调用一次，再 await 返回值。
   首次槽存在检查不能缓存绑定方法。类型增删/重绑作用于下一轮；已经取得并暂停的
   awaitable 保持原身份，当前轮恢复不会重新调用 `__anext__`。
3. 返回值通过 async-for 专用 await 获取上下文进入现有委托状态机。
   `None`、`False` 等合法完成值照常赋给目标，不能借助结果真值判断耗尽。
   `send`、`throw`、`close` 精确转交当前 await 委托对象；不添加额外恢复或重复求值。
4. 外层 coroutine 的创建仍惰性：源表达式、槽绑定和循环正文均在首次恢复后执行。

CPython 3.12.13 的类型槽和错误包装依据为
[GET_AITER / GET_ANEXT](https://github.com/python/cpython/blob/v3.12.13/Python/bytecodes.c#L659)。
错误类型与文案按同次 oracle 逐字比较；协议入口的四类专用文案为：

```text
'async for' requires an object with __aiter__ method, got T
'async for' received an object from __aiter__ that does not implement __anext__: T
'async for' requires an iterator with __anext__ method, got T
'async for' received an invalid object from __anext__: T
```

T 使用 Python 可观察类型名。`__aiter__`、`__anext__` 的调用异常直接传播，
保留对象身份。描述符绑定失败与调用失败分开：CPython的 `slot_am_aiter/anext/await`
将失败查找覆盖为 `AttributeError: object T does not have METHOD method`；此处不得
把描述符抛出的 StopAsyncIteration 当作自然耗尽，类型名按对应槽的50字符上限处理。
获取 await 迭代器失败时使用最后一类 TypeError 包装，并保留原 cause；
这也覆盖用户 `__await__` 在取得迭代器前直接抛错和返回非法迭代器。
已经取得合法迭代器后，其执行异常按正常 await 路径传播。
不得用统一 catch 把用户方法内所有 TypeError 改写为协议错误。
获取失败的cause只接收已经真实展开的callee帧；尚未接收原错误的调用者不能加入cause链。
包装TypeError自身随后沿调用者的真实catch传播，legacy Frames文本快照保持原合同。

GET_ANEXT对原生coroutine不执行普通GET_AWAITABLE的“已经被await”检查；
同时仍保留随后普通await的占用拒绝。共享anext返回对象按真实coroutine状态恢复，
其它awaiter在它已完成后继续恢复仍得到复用错误；不得为async-for额外加一层普通await检查。

## 耗尽和清理边界

每轮仅将 `__anext__` 槽绑定/调用、await 获取与执行置于耗尽保护区。
真正传播出的内建 StopAsyncIteration 及其子类结束循环；匹配使用受信内建身份，
不读取用户同名变量。await 获取失败已包装成 TypeError 时保持该错误。

目标绑定、解包、属性/subscript setter、正文、正文清理及 else 均在该保护区之外。
这些位置抛出的 StopAsyncIteration 正常向外传播，不能触发正常耗尽或执行 else。
目标失败的副作用和已经完成的部分赋值遵循普通赋值合同。

只有自然耗尽执行 else。break 跳过 else，continue 先完成当前正文 finally 再取下一项；
return 先保留返回值，再按现有清理规则离开。嵌套循环的 break/continue 只作用于本层。
无论自然耗尽、break、return、异常、throw 或 close，循环本身均不隐式调用迭代器
`aclose`。外层 with/async with/finally 仍必须真实运行，其暂停、抑制和异常链沿用 ADR-0215。

在下一项 await 期间注入异常，先交给委托对象；委托对象若处理后返回正常值，循环继续。
若抛出 StopAsyncIteration，按该轮耗尽处理；其它 BaseException 向外展开。
在正文 await 期间注入 StopAsyncIteration 则向外传播。
close 注入 GeneratorExit，清理仍产出时保留现有“忽略 GeneratorExit”及后续恢复合同。
取消 API 由后续事件循环实现；此切用真实 coroutine.throw(BaseException) 固定传播规则。

闭包捕获循环目标与普通 for 共用 cell，跨暂停保持身份和后期绑定；异步迭代器、
当前 awaitable、循环状态和 pending 控制流跨暂停保活。普通 generator/yield-from
仍使用既有路径，await 不能令 coroutine 被识别为普通 generator。

## 实现边界

- HIR 新建独立 async-for lowering 模块，以现有 while、窄 try、赋值和 await 表达协议。
  新增中性异步迭代操作和 `AwaitContext::AsyncNext`。`hir.rs` 已接近 600 行，
  先按定义职责拆出表达式类型；不得继续向该文件堆叠声明。
- ty 为异步迭代结果使用动态对象，沿用协程模式和闭包推断，不假定具体迭代器类型。
- MIR 复用现有循环/try/await builder；耗尽 catch 覆盖集合不得包含 target/body/else。
  不将同步 foreach 提前分配的基本块混入异常区域；必要时使用专属小模块降低控制流。
- Runtime 提供静态 `AsyncIterStart(object)` / `AsyncIterNext(object)` 和专用 await 错误入口。
  槽存在检查与实际绑定分离；复用受信特殊方法绑定、内建异常匹配和 traceback 协议。
- RuntimeFn、签名 validator 和 codegen 静态引用表同步；codegen 不依赖 HIR/ty。
  新源文件每个不超过 600 行，并保留中文职责介绍及关键算法注释。

源码结构门需证明 catch 的真实覆盖范围、每轮重查和直接状态机恢复；不得只断言指令数，
也不得构造与实现分支逐项同形的测试替代真实 Python 行为。

## 手写输入和验收

| 输入 | 高价值合同 |
| --- | --- |
| `async_for_protocol.py` | 惰性、源单次求值、描述符次数、实例遮蔽、暂停时重绑、删除槽、错误包装与cause真实帧 |
| `async_for_control.py` | 假值、continue/break/else/return、嵌套、跨暂停闭包和有副作用目标 |
| `async_for_exceptions.py` | 调用/await 耗尽、子类与局部遮蔽、aiter/目标/正文/else 异常不吞、绑定失败 |
| `async_for_lifecycle.py` | 真 send/throw/close、取消、委托恢复、外层 async with、暂停清理及无隐式 aclose |
| `async_for_global_shadow.py` | 全局同名变量不替换内建耗尽身份 |
| `async_for_shared_awaitable.py` | GET_ANEXT与普通await的共享coroutine占用差异及完成后复用错误 |
| 三个独立 `*_boundary_pending.py` | CPython成功；异步生成器/getattribute编译诊断、动态别名异常基类运行期NI |

实施后，在同一进程测试轮显式指定 CPython 3.12.13，先生成预期，再构建无 CPython provider
的候选 JIT 并比较退出状态、完整 stdout 和 stderr。手写成功输入还分别使用本机
3.9.25、3.13.3 运行 oracle，记录版本差异；产品特性不锁 Python 版本。
协议失败由源码捕获并打印实际错误/cause，不保存 golden，不归一化错误正文。
默认 feature 关闭、async for 在普通 def、异步推导式和异步生成器继续用诊断负例保护。

真实 NativeAOT 至少覆盖 control、exceptions、lifecycle 三组输入：源码复制到仓库外临时目录，
同次取 oracle，发布后删除输入目录，整体搬迁到新的中文空格目录，再删除整个原构建目录，
从搬迁后的原生可执行文件运行并比对输出。检查无 provider、无源码执行依赖、stderr 无泄漏警告。
异常组保留准确原异常身份；生命周期组保留跨暂停清理和后续恢复，不能只验程序启动。
结束删除仓库外产物；三平台、onefile 与真实 IO/事件循环验收分别报告。

回归保留现有六组 generator、yield-from、协程全部 JIT/AOT、async with/traceback、
默认 HIR 支持边界、MIR EH 与 TraceV1 反伪造门，以及普通调用和 generator 定向性能保护。
只有这些门在实施授权后真实通过，才移除相应 pending 标记并登记实际兼容范围。

本机本轮实际通过：六组成功源码在3.9.25/3.12.13/3.13.3输出一致，stderr为空；
固定3.12.13同次候选门21项通过，包含原13项和六组新成功输入、两个独立边界门。
AOT搬迁共七组，新增control/exceptions/lifecycle三组均执行删除输入、删除整个构建目录
及中文空格路径搬迁后的真实原生产品。Runtime相关109项通过，含五个协议宿主用例、
真实traceback身份/分配保护和七个PE门；MIR固定ABI实参篡改负例通过。
公开asyncgen、调度器、跨线程异步IO、其它平台与onefile仍按后续竖切分别验收。
