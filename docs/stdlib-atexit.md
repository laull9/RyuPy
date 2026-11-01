# atexit：显式退出回调（M3.7，已验证子集）

这里实现实际回调执行，不以空注册表或 D001 的析构时机偏差替代显式 atexit
语义。固定验收基线为 CPython 3.12.13；不承诺线程并发、信号终止、致命宿主
崩溃或其它 Python 版本的注册变化行为。

## 逐 API 范围

| API | 行为 |
|---|---|
| `register(func, *args, **kwargs)` | 第一个参数仅位置传入，立即验证 callable；保存参数对象引用及绑定快照，返回原 callable，支持装饰器形式 |
| `unregister(func)` | 按注册顺序比较已保存 callable 与查询对象，移除全部相等项；查询对象不要求可调用；返回 None |
| `_run_exitfuncs()` | 本轮初始倒序下标执行剩余槽位；全部结束清空注册表，手动调用后自动退出不重复执行 |
| `_clear()` | 清空注册表并释放保存引用，不执行回调 |
| `_ncallbacks()` | 返回注册槽位数，包含注销后空槽；清表后为零，与固定基线一致 |

注销过程若用户 `__eq__` 抛错，异常正常传播，已删除项不回滚。回调执行中允许
注销、清表、注册与有限重入：不提前删除当前回调，故重入可以再次访问当前项；
新注册项位于本轮初始末端之后时不会执行，并在最终清表中移除。清表后再注册
可能使用低下标而进入剩余遍历；这些结果有固定基线黑盒验证，不使用快照冒充。

## 退出接线与流所有权

生成入口在正常结束、主体 `SystemExit`、未处理 Python 异常及已捕获宿主异常
后统一调用 `FinishExit`，顺序为：退出回调 → 现有标准流刷新。回调不是 CLR
ProcessExit 事件，也不挂到任意 `flush` 调用，避免重复执行或用户刷新意外退出。

退出回调抛出普通异常或 `SystemExit` 时，均报告后继续剩余回调，**不改主体
原退出码**。最终流刷新若失败，仍按已有规则变为 120。不会额外关闭未注册的
流；用户显式注册的 close 与任何其它回调一样执行。

## 错误报告与异常上下文

默认报告前缀为 `Exception ignored in atexit callback:`，包含 callable 的 repr
及该异常自身的 traceback；不展开 cause/context 链，不修改异常对象的链数据。
repr 失败使用 `<object repr() failed>`，异常 str 失败使用安全占位。报告优先写
当前 `sys.stderr`；其自身失败时只尝试宿主 stderr 兜底，不递归调用坏流，不让
CLR 异常逃逸或改变原退出码。该兜底是明确的宿主安全边界，不承诺模拟失效
stderr 下 CPython 的每个诊断细节。

每次调用保存 Python traceback 深度与当前异常 token；失败时先保全异常快照，
再恢复调用者状态，报告过程与下一个回调不继承失败回调的残留帧。手动
`_run_exitfuncs` 在 except 内执行时仍保留调用者异常，不能把“读取 token”误作
清空上下文。

**未支持**自定义 `sys.unraisablehook`：当前读取该属性仍 AttributeError，重绑定
明确 NotImplementedError，不允许用户安装后被静默忽略。完整 unraisablehook
参数对象、审计 hook 与解释器多实例生命周期尚不在此范围。

## 成本与验证

注册分配一个记录与参数绑定快照，参数值不深复制；退出遍历 O(n)，注销 O(n)
次 Python 相等比较，不在每轮移除后搬移列表。最终清表释放全部回调引用；不
宣称即时析构顺序或未经测量的速度提升。

验证入口：`m3_7_atexit.py`、`m3_7_atexit_mutation.py`、`PyAtexitTests`，以及
`tests/fixtures/atexit_exit.py` / driver `atexit_exit.rs` 的真实进程退出码、错误报告、
继续清理与不关闭未注册流验收。四项 runtime 专测、两条固定基线差分与十种
真实进程退出模式均已通过；不计作 colorama 项目通过。
