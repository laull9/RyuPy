# ADR-0224：真实 CP 薄函数、纯 CIL core 与独立 owned handle 的回收首证

- 日期：2026-11-17。
- 状态：独立生命周期探针已验证；产品接入、元数据扩围与性能晋升待实施。
- 接续：[ADR-0208](0208-cil-execution-libpython-kernel.md)、[ADR-0223](0223-stock-libpython-bounded-glue.md)。
- 实现与重跑：[core_handles](../../tests/libpython_embedding/core_handles/README.md)。

## 1. 决定与所有权

无默认值、无业务捕获的普通函数采用如下首证结构：

```text
CP 模块字典 D -> 真实 FunctionType F -> 固定胶水 closure -> PyCFunction P
P -> GC owner -> GCHandle 强根 -> 纯 CIL 执行 core K
GC owner -> 强 F                    K -> WeakReference(H)
CLR 公开 handle H -> 独立 owned F
```

F 原生持有 D、closure 和函数元数据；D 保存 F，形成由 CP 循环 GC 可见的强环。
GC owner 强持 K，K 没有 owned CP 引用或 CLR owner 回边。只有 H 独立拥有 F，
H 的终结器只把 owned 引用转移至释放队列，主线程持 GIL drain 后才 DECREF。
CP 端保留 F 时 K 必须存活；CLR 端保留 H 时 F 和旧 D 必须存活；两端释放后，
CP 循环 GC 释放 P/owner，真实 owner 析构回调解除 GCHandle，随后 CLR 收集 K。

现有捕获完整 PyFunction 的包装方式会形成 Roots → owner → owned D → F → Roots 强环。
后续接入必须拆开执行 core 与公开 handle，不可将旧 owner 换名后直接捕获。

## 2. 实现与实证

独立 C shim 使用公开 CPython C API；自有 CoreLink 结构的字段由 C 编译器访问，
没有 CPython 内存偏移、私有结构读写或动态签名反射。C# 只有固定 DllImport 与两个
UnmanagedCallersOnly 回调，checked Int64 加法由 C# 静态方法完成；C 仅提取/转换参数。

固定胶水来源为 `bridge.c/ch_init` 内嵌 `_factory` 与其嵌套 `f`，精确代码为
`f(x): return p(x)`。初始化仅取得模板的嵌套 code；每个函数由公开 PyFunction_New、
PyFunction_SetClosure 组装，绑定 GC owner 的强环境后才重新进入 GC 跟踪；每次 CP 调用执行一次 f。
F 的真实 `__code__`、`co_filename=<string>`、closure 与 frame 属于胶水，业务计算属于
`Core.Leaf`。测试驱动、模块 loader 和 oracle Python 均是测试控制面，和候选胶水分开。
产品逐函数三阶段清单及全帧审计继续按 ADR-0223 接入。

2026-11-17 在 macOS arm64，stock PBS CPython 3.12.13 与 3.13.3，.NET SDK 11.0.100、
net10.0 目标，JIT 与真实 osx-arm64 NativeAOT 各实跑一套最终功能用例。宿主检查
RuntimeFeature.IsDynamicCodeSupported，确认两种运行模式。
每套原 12 轮、48 个函数，追加三处失败和一个复活函数；172 次静态 CIL 叶子调用；每轮三处回收门同时验证：

- CP 单独保留 F，CLR handle 已终结、队列 drain 后仍能调用；回流重建 H 且同代身份相同。
- CLR 单独保留 H，删除 sys.modules 并做 CP GC 后旧 D 仍可读；真实 importlib 重导建立新 D，
  新旧函数分别读取各自的 live globals，修改新 D 不影响旧 D。
- 真实 importlib exec_module 抛错，由上游事务删除缓存；外逃 H 仍能读取旧环境。
- 两端释放、CLR GC 与 drain 后先观察 CP 强环仍活着，再显式 CP GC；F 与 globals 哨兵的
  真实 weakref 失效，owner 原生析构累计 52 次（49 个成功函数、三处失败），managed core 弱引用全部失效，
  GCHandle 根、owned H、队列、native live 均回零。全部发生在 Py_FinalizeEx 之前。
- 回流拒绝未知函数、未知业务捕获、动态新增默认值和替换 glue code；恢复元数据后可重建 H。

测试关闭自动 CP GC 以固定观察窗口，随后反复显式 gc.collect；不依赖退出清账。
所有构建在仓库外独立项目中执行并清理，产品 Runtime 与输出目录均未使用。
运行和全量矩阵共享机器负载，本轮仅登记功能证据，性能预算继续待验。
公开 API 选自 CPython 3.9+，实际编译链接使用各版本自身头文件和共享库；3.9–3.11、
3.14+、Windows/Linux、free-threaded、子解释器尚未验证。此处也未验证单份 abi3 二进制。

## 3. 后续中性接口

建议 Runtime 接入以下职责接口，CP 指针仅存在 provider/shim：

1. `IExecutionCore`：纯执行状态与静态调用目标，无强 owner、公开 handle 或 owned CP 环境。
2. `IFunctionEnvironmentLease`：公开 handle 独立拥有 F；借用 D 仅限 lease 有效且 GIL 已持有期间。
3. `TryAcquirePublicHandle`：从真实 F 验证 core 身份与元数据，弱缓存命中返回同一 H，失效则重建。
4. `EnqueueOwnedRelease` / `DrainUnderGil`：终结器不调 C API；队列本身是尚未释放的 owned 根，
   关闭前先阻止新回调并等待活动调用退出，再收集/drain，最后结束解释器。
5. `LifetimeSnapshot`：同时暴露真实 native 析构、core root、owned handle、待释放队列和弱存活量。

本探针 H.Call 为真实 CP 回调路径。后续 CIL 直达快路须由 H 保持环境 lease，调用纯 core 时
借用同一个 D，保持异常、身份和副作用；直达快路及产品接口尚未实现。

## 4. 确切未解边界

首证 ABI 为单个 Int64 位置参数，读取 globals 中 Int64 offset，返回 checked Int64 加法。
普通 Python 大整数、通用绑定、参数关键字、默认值、kwdefaults、annotations、装饰器、
业务 closure/cell、递归、生成器、协程与类描述符继续按独立合同推进。

真实 CP F 可由外界改写 __code__/__defaults__/__kwdefaults__/closure 内容。
本探针只在回流 Acquire 时验证原 glue code、无默认值和唯一原生 dispatch 身份；
外部直接改写 __code__ 后可走 CP 的替换函数体，已持有 H 的元数据变更也未设统一失效协议。
产品接入须决定单一元数据权威、动态变更与 CIL 绑定同步、直达快路失效及明确能力诊断。
不得从本探针扩展放行任意业务捕获。业务 signature/source/traceback 的独立表示与胶水 frame
归属仍需设计，不能用业务 CodeType 冒充真实薄函数代码。

原生线程/GIL 重入、关闭时存活 H、atexit 新建函数、队列并发与关闭后迟到终结器尚未测试。
本探针在所有根回零后调用普通 Py_FinalizeEx；完整 stock 主动关停合同仍需单独验收。
产品默认晋升与旧路径退出继续遵循 ADR-0223 的三平台、多版本、搬迁和性能门。

## 主代理重入复核

参数转换 `PyLong_AsLongLong` 可以执行用户 `__index__`。shim 在跨该回调前独立持有
offset，并在转换完成后释放，避免 globals 被改写后使用已释放的借用值。新增 int 子类
析构哨兵和重入参数验证，四组合各十二次回归通过，CIL 调用累计为 168；运行 stderr
为空。此项复验仍限独立所有权首证，产品接入及关闭期边界继续按前节推进。

独立复核发现 factory 的 return trace 可提前观察半初始化 F；主代理取消创建期的 factory
执行，直接组装函数与 closure，并补创建期 sys.settrace 回归：factory return 不触发，
发布后的正常 f 调用仍被 trace 观察。两版 JIT/AOT 四组合最终复验通过，回收终态保持一致。

2026-11-17 失败路径追加复核：半成品 F 取消 GC 跟踪后，如果 closure/owner 初始化失败，
须在 DECREF 前恢复跟踪，满足 stock func_dealloc 的约定。主代理已修正并在相同 shim
新增三处确定性失败注入；两版 JIT/NativeAOT 四组合各新增三个 core 根，全部在 finalize
前释放，capsule 总析构由原成功构造的 48 次增至 51 次，CIL 运算仍为 168 次。


## GC owner 结构修订（2026-11-17）

GC 会先清 weakref 再执行 Python finalizer，旧弱身份方案使真实复活的 F 无法调用/回流。
raw-live watcher/callback 补救又依赖可能因分配失败而缺席的通知，已退出。最终使用
`function_owner.h` 的直接 GC owner，强持 F 且精确 traverse F/heap type，纯 native
context 只存 core token；无 capsule、weak-F 或内部 watcher。P 的实例引用仅存在 closure，
禁止放入没有 GC traversal 的 stock GIL code 常量。详见 [ADR-0227](0227-stock-thin-function-metadata-probe.md)。

两版各 JIT/NativeAOT 最终四组合均通过：三处创建失败及时释放 core；Python __del__
在 GC 内调用真实 CIL 并保存 F；旧 weakref 仍按 CP 规则死亡，使用复活 F 回流新 H，
随后释放两端并再次回收。每组合 52 次 owner 析构、172 次 CIL 运算，core/owned/队列/native
全零。native DESTROY 在 GC clear 内复活时的已清字段是高级消费者边界，按同次 stock
oracle 观察，禁止盲目调用；本合同的 Python finalizer 复活完整保留。

P、P.__self__ 或 cell 外逃会延长 F/D/K 生命期，最终根释放后由 CP GC 回收。普通 closure
写入仍是 CP 语义；本首证只准入仍绑定原 P 的静态 core，产品 H 调用前另做能力验证。
