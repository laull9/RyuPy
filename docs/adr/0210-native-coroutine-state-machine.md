# ADR-0210：原生协程显式状态机首切

- 日期：2026-11-16
- 状态：显式候选；默认关闭，C3 完整调度门待办
- 关联：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0202](0202-resident-generator-state.md)

## 决定

以 Cargo feature `native-coroutines` 显式启用原生 `async def` / `await` 候选。
CLI、driver 与 e2e 将该特性传给 HIR；编译层保持纯函数，不读取进程环境。
普通构建对 async def 给出 E0100，既有 CPython 委派配置继续独立工作。
该特性没有 CPython 产品版本条件；既有 3.9–3.15 自动 provider 合同保持原范围。
固定 3.12.13 仅用于本切同次 oracle，属于验证配置。

HIR 保存 `is_coroutine` 与独立 `Await` 节点，协程统一强制动态类型，并通过
既有 `is_generator` 暂停标记进入显式状态机降级。MIR 将 await 展开为取得迭代器、
委托推进、Suspend、恢复及取得终值的 CFG。普通 yield-from 继续使用原入口。
生成的 CIL 恢复方法保留局部、闭包 cell、异常处理状态和 finally 路由。
后端只发射 `CoroutineCreate` / `CoroutineAwaitStart` 及已有暂停协议。

工厂完成字段/数组状态机降级后，用 `PyCoroutine` 包装其独立状态。
创建只绑定参数和保存帧，第一次 send/await 才执行函数体。
`PyCoroutine` 没有普通迭代协议；其 `__await__` 返回独立 wrapper。
这条路径没有 Python helper、CPython 字节码或解释执行 fallback。

## 协议范围

- native async 普通函数、嵌套闭包和普通方法，包含完整类型标注的定义。
- `send(None)` 启动、后续发送值、单参数 throw、close；首次非 None send 保留未启动状态。
- 完成、关闭或未处理异常后，再次恢复报 `cannot reuse already awaited coroutine`。
- 同一活动协程的第二次语言 await 报 `coroutine is being awaited already`；重入报 ValueError。
- await 按类型级 `__await__` 查找，忽略实例同名覆盖；返回值必须已经是迭代器，
  不再次调用其 `__iter__`。普通 iterable 和非法返回值报 TypeError。
- 委托完整转发 send/throw/close，保留 StopIteration.value、原异常身份及跨暂停 finally。
  显式 `next(c.__await__())` 同样保留本次完成值。
- PEP 479 区分协程自身泄漏 StopIteration 与子生成器的错误，保留真实错误来源。

async for、async with、异步生成器和异步推导式保持明确诊断。
零参 super 的跨暂停方法上下文尚未保存，同样在 HIR 拒绝。
显式 Instance 模块继续受原 async 守卫约束；候选首切走已有普通动态状态机。
外部 CPython awaitable 的跨域协议尚未接入，原生 await 仅接收本域协程和本域类型槽。

## 后续范围与成本

事件循环、Task/Future、真实 IO、调度与取消、contextvars、线程亲和性及混合 GIL 重入
仍由 C3 后续竖切实现。本切没有以同步普通函数替代 async，也没有实现 asyncio 调度器。
`inspect.iscoroutinefunction`、cr_frame/cr_await、精确 code flags 及未 await 析构警告
保留为后续可观察合同，当前不构造伪造的 CPython 帧或代码对象。

复杂暂停沿用数组帧与逻辑异常路由，动态对象包装、wrapper 和参数绑定存在分配成本。
性能门尚未运行，因此本切保持显式候选；默认晋升单独验收。
已有普通生成器增加生命周期观察、协程来源标记及 native 迭代器终值分支，
其真实产品和 Runtime 协议回归独立执行。

## 本次验证

macOS arm64、.NET 10：

- 三组原生协程 JIT 产品与当次固定 CPython 3.12.13 的 stdout、退出状态和空 stderr 一致。
  覆盖惰性、真正挂起、send/throw/close、重复/并发 await、类型槽、终值、异常、
  两层 finally、handler 再次暂停、闭包及方法。
- 同一三组手写源码在本机 CPython 3.9.25、3.12.13、3.13.3 共 9 次运行输出一致。
  3.10、3.11、3.14、3.15 的本切行为尚未逐版本运行。
- cleanup.py NativeAOT 真实发布后删除输入与整个构建目录，中文空格路径搬迁执行，
  与同次 CPython oracle 一致。
- 六个原有 generator basic/send/exceptions/cleanup/return/yield-from 真实 JIT 产品差分通过。
- 新协程及原生成器 Runtime 定向 33 项通过；四个中端/后端 crate unit/doc 测试通过；
  generator 定向 135 项通过、1 项原有独立实验 ignore 保留。
- 常规 CLI cargo check 与候选 CLI/e2e clippy `-D warnings` 通过。

Linux/Windows、本切 onefile、跨域 awaitable、性能和完整 make check 另行验收。
重跑入口见 [tests/coroutines](../../tests/coroutines/README.md)。
