# ADR-0228：stock 编译程序的一次性正常结束合同

- 日期：2026-11-17。
- 状态：专用程序终态已产品接线，两版实际程序集 JIT 专项及真实 CLI 三形态24格通过；入口登记存储继续收敛。
- 范围：单 `__main__`、同步静态 CIL 函数体、真实模块字典；普通嵌入 Host 的 Dispose 合同保持严格。
- 关联：[ADR-0223](0223-stock-libpython-bounded-glue.md)、[ADR-0227](0227-stock-thin-function-metadata-probe.md)。
- 手写探针：[StockThinProgramLifecycle](../../runtime/RyuPy.CPython.Tests/StockThinProgramLifecycle/README.md)。

## 1. 决定

采用**专用编译程序结束 API + 明确的内部 owned 作用域 + stock Py_FinalizeEx**。
`__main__.__dict__['f']` 是程序正常内部根，源码可以一直保留它。
Runtime cache、module instance state、execution context 都可以保持 CLR 强可达到 finalize 返回后；
程序登记的跨域 owned 引用在提交终态时确定交还，后续 Python atexit 与析构依赖原生根继续执行。
普通 Dispose 继续拒绝 nativeLive/coreRoots/ownedH 非零。

终态 API 接受编译器组合根授予的一次性程序权限，不能由模块名字、函数名字或启用薄函数的开关推导。
第一切片只授权编译入口内部资源及初始化线程上的同步回调。
事务之外的 H、opaque PythonObject、synchronized 容器 owned 引用一律可恢复拒绝。
内部作用域结束与外部所有权获取必须有明确边界；禁止把任意现存 ledger 收编为内部资源。

## 2. 三种生命周期分别成立

### 2.1 运行期双 GC

ADR-0227 已证明 A closure/direct GC owner 的正常回收及 Python finalizer 复活。
图为 `F -> D`、`F -> closure -> P -> owner -> F`、`owner -> core K`、`K -> weak H`，
CLR H 自己另有 owned F。运行期只有外部根消失以后，跨堆回收才具备条件。
该合同继续有效，本轮无需重做完整双 GC 矩阵。

### 2.2 普通 Host.Dispose 的可恢复拒绝

`CPythonHost.Dispose` 先拒绝租约内关闭及错误线程，进入 state=2，等 active 归零，
`CheckStockThinShutdown` 在 GIL 内检查 nativeLive、coreRoots、ownedH。
检查抛错时恢复 state=1；原 H 继续可调用、可显式释放，调用方可重试。

`Release` 从 ledger 移除 token，并把指针交给队列；`Drain` 只在合法 GIL 边界真实 DECREF。
现有 state=4/3 的 Release 会登记 late 并放弃 DECREF；普通 Dispose 最后还有 handles.Clear。
这条退出路径不能直接承担编译程序终态：回调会新建 opaque/H，native owner 存活期间也不能先
调用 `rp_thin_clear` 撤销 template/type。单纯删除 live 检查将造成泄漏。

### 2.3 编译程序的一次性终态

编译入口结束后不再有外部 CLR 使用权；原 CP D/F、atexit 参数及析构对象仍可存活。
先交还已登记的 CLR owned 边，再让 stock 执行自己的终止协议，能保留真实回调时序并完成 native 析构。
提交前失败恢复可用宿主；提交后已经交还引用并开始解释器终止，采用不可回滚的终态错误报告。
普通 Dispose 与专用程序结束 API 必须分开。

## 3. 两版 stock 的公开终止入口与实际次序

上游源码：[3.12.13 pylifecycle.c](https://github.com/python/cpython/blob/v3.12.13/Python/pylifecycle.c)、
[3.13.3 pylifecycle.c](https://github.com/python/cpython/blob/v3.13.3/Python/pylifecycle.c)。
公共入口是 Py_FinalizeEx；下面的内部函数名用于阅读原版顺序，探针没有调用它们。

两版均先等待 threading 管理的线程、处理 pending calls、执行 Python atexit。
之后设置全局 finalizing 状态，整理线程状态、刷新流、执行销毁模块前的 GC。
`finalize_modules` 自行移除和清理 sys.modules、恢复原 builtins，执行 GC；
再清仍存活模块的 globals、sys/builtins 与解释器导入缓存，并再次 GC。
随后整理解释器和类型状态，低层 Py_AtExit 回调在 Python 环境已拆除后执行。
3.13 在线程停顿/分离及解释器清理细节上有变化，以上与本切片相关的顺序相同。

因此：Python atexit 可以使用原 D/F/builtins；终止 GC 内的 __del__ 会按所在阶段看到 stock 环境。
不能承诺任意晚期 __del__ 仍有完整 sys/builtins。探针特意构造在模块销毁前 GC 中执行的循环析构对象，
实际事件为 `atexit-cil -> del-cil -> unraisable-cil`。
低层 Py_AtExit 只读取 C 计数，既不清账，也不调用 Python API。

## 4. 精确的 owned 时序

1. 编译入口开始内部创建事务。PythonObject constructor 登记强 owner；薄函数 holder constructor
   另登记 IDisposable holder。临时 H 与模块/D/元数据 owned 边分别记录。
   外部嵌入调用与任意第三方回调不具备这项事务权限。
2. `def` 创建真实 F，写入原 D。创建时临时 H 在定义冷 API 的 finally 或入口作用域结束时 Dispose。
   对 `return global_f`，CP→CIL 导入也可能重建临时 H；入口作用域同样负责它。
   交还 CLR H 的 owned F 后，D 内 F 保持原身份。全程没有源码 `del f`。
3. 内部执行作用域结束，冻结模块资源登记；从此通过 Host 外部 getter 获取的 H/opaque 属于外部所有权。
   终态预检要求初始化线程、leaseDepth=0、active=0、ownedH=0，ledger token 集合精确等于登记集合，
   普通 managed proxy Roots 为零。Drain 排空已经合法提交的释放。
   活 H、P/self/cell、独立 D 引用、容器 transfer owner 任一存在，都恢复 running 并拒绝。
4. 进入预检门时即禁止新外部租约，拒绝时同时恢复该门；通过后提交终态，只允许初始化线程同步回调。
   将 shim 自己的 template code/type 两份 owned 引用交还；正在调用的 F 保活 code，owner 保活 heap type。
   shape/dispatch 的两个比较别名暂留为 borrowed，禁止新 make，避免使用其已结束的生命周期。
   每个 existing dispatch 都由当前 owned F/owner 证明模板/type 仍有效，未保留 raw F。
5. 在 GIL 内对登记的 module/D/元数据逐个 Dispose，经 Release/Drain 真实 DECREF。
   CLR cache/state/context 此时仍有强根，但其跨域包装已撤销 owned 使用权。
   sys.modules 中的真实 module 继续持 D，D 继续持 F；不预清业务 D 或 sys.modules。
6. 调用原 `rp_stop -> Py_FinalizeEx`。callback 外层建立强 owned 作用域，包住现有参数导入、
   ManagedHandledScope、静态 CIL body、结果导出、异常恢复和清理。
   result/raised 必须先独立 owned；随后倒序 Dispose 本次新建的 opaque 与 H holder，GIL 内 Drain。
   析构重入使用嵌套作用域；最终恢复原 raised 异常，不让 CLR 异常穿 reverse P/Invoke。
7. stock 自己清模块并 GC。owner dealloc 先摘 native link，再清 F，最后释放 core token 一次。
   低层退出观察见 nativeLive=0；返回后再读纯托管 coreRoots、ownedH 与 ledger。
   计数同时为零才通过；失败原样保留。没有 handles.Clear 或终止后的补偿 DECREF。
8. finalize 返回后仅清 shim borrowed 别名、撤销组合根/托管缓存。库保持加载到进程结束。

嵌入端持有的别名只要共享同一已登记 wrapper，就无法仅靠引用计数识别其 CLR 使用权。
因此程序权限只向封闭编译入口授予；开放嵌入仍使用严格 Dispose。
未来任意 managed 逃逸必须具有显式 ownership transfer/外部租约，不能扩展内部登记范围来掩盖它。

## 5. 首阶段实验实现与后续接线清单

第一阶段仓库只新增本 ADR 和探针目录。驱动从 Git 对象读取 `1489dd2a`，Runtime/provider 都在仓外构建；
工作树中并行的中性 ABI、Bootstrap 与 Rust 编译器改动不会渗入本实验。
仓外 provider 副本有三处明确源码接线：PythonObject constructor 强 owner 登记、
StockThinFunctionOwner constructor holder 登记、Host.Enter 的终态外线程门。
`HostProbe.cs.in` 是手写 partial；`NativeProbe.c` 包含冻结的原 shim，新增公开 C API 实验导出及计数。
这些是源码实验，现有产品 Bootstrap.Finish 仍走旧 Dispose。

真实路径经过 `RegisterStockEntrySourceLocated -> PyModules.Import -> PyModuleInstances.Create/Execute`，
验证 Runtime Cache 与 Host imported cache 均指向同一 CLR module。
静态 Entry 保存真实 execution context；静态业务体分别执行 echo、读 marker、读 f、退出检查、析构检查及抛原异常。
Python 控制胶水只构造对象、注册 atexit/__del__/unraisable、执行身份断言；业务运算固定在 C#。

还发现两个具体接线问题，已保留失败并在独立实验中显式处理：

- `PyModuleInstances.Create` 无条件写 managed `PyModuleBuiltins.Default`；Host 尚无独立 stock builtins
  适配能力。试图在 Publish 改回 CP dict 后，Execute 会在 `RequireBuiltins` 抛
  `instance __builtins__ must be a dictionary namespace`。
  最终 probe 在固定 Entry 开始、创建业务 F 前用公开 module/dict API 恢复 canonical builtins，
  并注销先前 managed 容器适配。context 已捕获的 managed builtins 仍保留，其语义与退出证明分别登记。
  **产品应在 Execute 捕获前完成 canonical 初始化及中性适配**，从而同时保证顶层和 F 的 builtins。
- synchronized 模式 Unregister Dispose transfer.Owner 后，transferIndex 的弱项可能暂存到下次 CLR GC。
  下次同步会访问 disposed Owner。探针仅移除已 Dispose owner 的失效弱观察项，真实 CP 引用已经由队列交还。
  产品应在 Unregister 中同步摘除对应 transferIndex 项。没有强迫 GC 释放活 H。

### 产品最小接线文件

- `runtime/RyuPy.Runtime/PyStockThinFunctions.cs`：中性 Define/Publish 冷入口，创建临时 H、StoreName、
  finally 交还临时 owned；或与入口执行 owned 作用域组合。调用方无须主动 del。
- `runtime/RyuPy.Runtime/PyModuleInstances.cs`、`PyModuleNamespace.cs`：foreign entry 保留 canonical
  builtins；增加窄的 dictionary/builtins 中性能力，避免为了读 builtins 实现整套严格导入 provider。
- `runtime/RyuPy.CPython/CPythonStockNamespace.cs`：在 namespace 创建/发布事务中登记 module/D/元数据 owner，
  提供 canonical builtins namespace；Runtime cache/state 的终态 tombstone 与 native owned 交还分开。
- `runtime/RyuPy.CPython/PythonObject.cs`、`CPythonStockThinFunctions.cs`：constructor 级强 owner/holder
  登记；复用产品 InvokeStockThin，增加异常安全的 owned 作用域，覆盖正常入口临时 H 与终态回调。
- `runtime/RyuPy.CPython/CPythonHost.cs`：独立 FinishCompiledProgram，专用终态状态，owner-only 回调，
  Release/Drain 在该状态仍执行真实 DECREF；预检失败恢复 running，最终 ledger 原样验零。
  Unregister 同步移除失效 transferIndex 项。
- `runtime/RyuPy.CPython/native/stock_thin_function.h`：程序终态 prepare，交还 shim template/type owned，
  保留仅供当前 F/owner 见证的比较别名，拒绝新 make 并消费失败 token；finalize 后只撤销借用。
- `runtime/RyuPy.CPython/Bootstrap.cs`：编译入口授权模式选择专用 finish；保留普通 Stop/Dispose。
  保持 `Builtins.FinishExit` 的 Runtime atexit/flush 次序，随后由 CP finalize 执行 Python atexit。
  预检失败时保留 Bootstrap.Host 与 PyProvider，不执行失效撤销。
- `crates/ryupy-codegen-cil/src/entry.rs` 与 stock thin 冷调用引用表：入口作用域 try/finally，所有成功/异常
  退出均进入授权 finish；嵌套 Python 回调只使用其 owned scope。Rust 仅发射 Runtime 中性合同。

以上为第一阶段提出的产品清单；第二阶段实际接线与验证见§8。

## 6. 第一阶段两版实测与失败保留

固定 stock 3.12.13 / 3.13.3 常规 GIL，各用自身头文件与原版 libpython。
每版九个独立 JIT 进程：两个故意失败的释放对照、普通终态、外部 H、外部 D、P、self、cell、
真实 synchronized 容器 transfer。每个负例都先证明拒绝后宿主 running、外根仍有效，再显式交还外部使用权并重试。
所有进程还验证租约内、其它线程已有 active、以及终态外线程伪装 callback 的关闭/进入拒绝。

最终成功臂：nativeLive=0，destroyed=7，coreRoots=0，ownedH=0，ledger=0，queue=0，late=0，
lowlevelNative=0，postFinalizeDecref=0，finalize=0，stderr 为空。
主 scoped 臂 13 次 native dispatch；七个 core 对应七个固定静态业务/协议叶子。
CLR module/D/context 在最终断言后 KeepAlive；完整 Runtime Cache 仍保有模块对象。
终止后额外 CLR GC/WaitForPendingFinalizers 验证迟到释放和 DECREF 尝试仍为零。

失败对照的有意非零进程退出码为 2：

- `unscoped`：保留现有 callback，虽然 finalize=0、atexit/__del__/异常回调执行，
  仍有 nativeLive=7、destroyed=0、coreRoots=7、lowlevelNative=7；终止后的 CLR finalizer 产生 late=9。
  ledger 被 Release 移除到零仍然存在真实 native 泄漏，驱动明确判为释放失败证据。
- `keep-owned`：使用 scoped callback，但不交还登记的 CLR owned 边。
  stock 的模块清理仍让 nativeLive/coreRoots 归零、destroyed=7，却剩 ledger=21；synchronized
  准备路径的内部登记更多。该对照说明 native 析构计数和 CLR owned ledger 必须同时验证。

原始日志保存在仓外 `~/.cache/ryupy/validation/adr0228/`；成功、失败、编译错误与异常退出均追加独立文件。
早期普通回调泄漏、metadata 键遗漏、外逃 getter 元数据键遗漏、canonical builtins 适配失败、
synchronized 失效弱观察项的错误日志都保留，最终成绩以 README 列出的最终两次完整运行标识为准。
驱动清理仓外 Runtime/provider/shim/配置/可执行产物，不提交报告、摘要、golden 或生成 fixture。

## 7. 第一阶段验收边界

2026-11-17 产品接线审查补充：独立实验暴露三项具体缺口，接线必须覆盖。
scope 交还标量 owned 时要同步撤销 reverse/objects 关联，导出前也必须验证缓存 owner 仍有效，
否则重复返回同一 CLR 常量会使用失效地址。第三方 managed 回调暂停内部登记权限，
防止入口执行窗口收编外部新建 owner。零存活 F 仍然允许正常交还模板/type 并结束；
无副作用的 prepare 校验失败留在可恢复预检阶段。实验通过记录保留，产品结果按修复后重新采集。

真实 Python 编译产品门见 [驱动说明](../../tests/cpython/stock_thin_compiler_README.md)。

已经证明原版 finalize 足以清掉正常 `__main__` F/D 和 direct owner/core，条件是 CLR owned 边具有
确定的内部作用域及终态 callback scope。无需修改内核、写 CP 私有内存、卸载原生库或主动清业务模块。
本轮结论是该有限合同具备实际实现路径，最后阻塞是上述产品接线，而非 CPython 终止能力不足。

本轮只运行独立 macOS arm64 JIT 门；NativeAOT、产品实际 CLI exit=0、搬迁、三平台、全量门禁、
异常进程强杀、任意第三方扩展的故意 native 泄漏及无界线程/managed 逃逸分别保留后续验收。
用户报告的首个实际 CLI identity 运行已经业务成功、旧 Finish exit=1；该产品结果由主代理持久日志保留。


## 8. 专用程序终态已进入产品

新增 `CPythonStockProgram.cs` 承担程序权限、入口 owned 登记、精确 token 集合预检、专用状态5、
callback owned 作用域与终态快照。Bootstrap 增加与 codegen 精确对应的无参 void RunStockThinMain、
int FinishStockThin(int)；StartStockThin 只启用薄函数能力，Run 才授予程序权限。
入口前失败、启动回滚后无 Host 时，专用 finish 回到原普通 Finish，保留错误码；有活 owner 仍严格拒绝。

实际程序入口 finally 交还内部临时 H，真实 F 保留于原 D。callback 的参数转换、结果导出及异常处理
在明确的静态业务能力内创建 owned scope，正常运行与终态均确定交还临时引用。
所有 scoped PythonObject 在 GIL 内调用 RetireScoped，先由 CPythonValues.RetireOwnedIdentity 撤销所属身份，
再 Dispose/Release/Drain；同一 CLR 常量重新导出时不会从 reverse 命中已释放地址。
FromOwned/ToPython 对已 Dispose identity 的防护同时保留。

第三方 CPythonCallbacks.Dispatch 业务边界暂停入口及 callback 登记权限，涵盖参数/handled 异常导入、
业务调用、结果导出与异常映射；原生编译导入组合根保留专门权限。
已准入的静态薄函数另建独立 owned scope，不能借第三方回调的时间窗口收编新外根。
入口内第三方 getter 保存新 opaque 的实测，在 CP 代理已经析构、Roots=0、H=0 后仍拒绝终态，
原 opaque 可正常读取，显式交还后重试成功。共享已有 wrapper 的外部别名仍受§4封闭入口合同约束。

native 新增无副作用 rp_thin_program_check，与不可失败的引用移交 prepare 分开，允许零实例。
prepare 交还 shim template/type owned，拒绝新 make 并消费失败 core token；finalize 后只撤销借用别名。
post-finalize DECREF 尝试由产品 shim 纯 C 计数；不卸载 libpython、不清 handles ledger。
状态5从预检开始就禁止外线程新租约，拒绝恢复 state=1；普通 Dispose 的检查和等待合同保留。

原 native 四个 builtins 消费点先验证 NULL/非 dict，使用静态 RuntimeError/TypeError 或 BaseException
分类回退，避免晚期映射异常时 NULL 解引用；正常字典路径保持原查找。
两处安装当前 builtins 的冷路径只拒绝 NULL，保留已有 mapping 的安装行为；没有晚期重建或源码求值。
Unregister 同步移除实际已 Dispose transfer owner 的弱观察项，下一次同步不再访问旧 owner。

CaptureModulePaths 在解释器终止前执行；WriteReport/WriteExecutionAudit 在终止后按原次序输出。
只对授权程序新增 stock_compiled_program 块：permission、terminal、finalize_result、native_live、
destroyed、native_calls、core_roots、owned_h、ledger、queue、late_releases、post_finalize_decrefs。
closed 阶段读取在 rp_stop 返回后由纯 C 计数和托管 ledger 生成的快照，没有 PythonCall 或 CP 对象访问。
普通兼容报告保持原结构。

专项驱动已移除冻结源码与三处注入，直接使用实际产品 DLL、正式 native/build.sh、真实 Bootstrap 三 ABI。
业务体改用 public IPyStockThinEnvironment/PyStockThinBody；观察库只登记只读低层退出回调。
两版 11 项实际产品 JIT 门通过：原七个成功臂、zero、entry-fail、scalar、external-callback；
后补 no-host/pre-entry-fail 两门验证入口前/回滚后 finish。报告字段逐项验零，普通 Dispose 拒绝仍可恢复。
同一 CLR 字符串和非 small 整数在入口及 atexit 各反复导出1000轮；每轮 CP 释放结果再调用。
另在公开构造的 builtins=None CP frame 中调用四个实际产品 native API，验证受控失败。
典型正常臂 destroyed=9、native/core/H/ledger/queue/late/post-finalize 全零；空入口与入口前抛错 destroyed=0，
入口错误码7和无权限错误码17保留。实际 CLI 与多发布模式的统一门使用同一产品报告，另行登记结果。

原兼容薄函数两版 ordinary/blocked/watcher-full/resurrect 与各7项内部协议测试继续定向回归。
未运行全量 make check、固定二十项目矩阵或性能窗口；本次实现与其它窗口的产品成绩分开登记。

最终产品专项日志为 `adr0228/20261117T061115.127269Z.log` 与
`adr0228/20261117T061116.255702Z.log`，两版各十三进程均通过；全部构建/测试产物清理，原三流日志保留。

主控实际 CLI 三形态门随后通过两版共24格，详见产品驱动说明；完整 make check exit=0。
RSS 诊断发现长入口强持临时 owner 的额外保留，需将长入口与短 callback 的存储分离。
该问题和后续修复单独验收；原正常结束/搬迁证据保留其实际源码快照。
