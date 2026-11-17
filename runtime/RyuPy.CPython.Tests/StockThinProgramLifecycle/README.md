# stock 编译程序正常结束：独立真实 Host probe

只新增手写探针，产品代码与 fixture 保持原状。设计和精确接线见 [ADR-0228](../../../docs/adr/0228-stock-compiled-program-lifetime.md)。
固定 `1489dd2a` 的 Runtime/provider/shim 在仓外构建，C# 输入使用 `.cs.in`，避免进入既有 xUnit 的默认 glob。
每个进程初始化/终止一个原版解释器；业务体固定为静态 C#，不委派 Python 业务源码。

```sh
python3 runtime/RyuPy.CPython.Tests/StockThinProgramLifecycle/run.py \
  --python ~/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3
python3 runtime/RyuPy.CPython.Tests/StockThinProgramLifecycle/run.py \
  --python ~/.local/share/uv/python/cpython-3.13.3-macos-aarch64-none/bin/python3
```

默认九进程；可用 `--case scoped` 等选定单门。需要 .NET 10+ SDK、clang、对应固定 stock 头/库。
当前驱动仅针对 macOS arm64 JIT；所有产物在 `/tmp/ryupy-0228-*` 临时目录，并在结束或异常时清理。
逐命令三流、退出码、冻结版本和临时接线说明保存在 `~/.cache/ryupy/validation/adr0228/`。

- `scoped`：Runtime 真 module instance/cache、原 CP D 中保留七个 F；CLR module/D/context 保持强可达；
  atexit 读原 D/builtins、临时 H 往返 identity、__del__ 抛原异常及真实 unraisable hook，最后所有计数零。
- `external` / `external-d`：拒绝外部 H/额外 owned D，保留有效宿主；显式交还外根后正常结束。
- `external-p` / `external-self` / `external-cell`：H=0 时仍拒绝外逃 opaque，释放外包装后 CP 键继续保留到合法终态。
- `external-container`：真实 synchronized transfer 的 CLR list 持有 F；显式 Dispose H 后 H=0，仍因容器 owned 引用拒绝。
  交还 transfer 所有权后正常结束；初始化产生的内部 metadata/容器登记同样真实 DECREF。
- `unscoped`：现有 callback 边界导致 finalize=0 但 native/core 泄漏，故意退出 2。
- `keep-owned`：保留 CLR owned 边导致 ledger 非零，故意退出 2。

探针在冻结 provider 副本插入三个构造/进入观察钩子；仓库内原源码没有修改。
登记依赖创建事务的强 owner，未用 weak cache 推测归属。普通 Host.Dispose 仍执行原严格检查。
NativeProbe 的低层 Py_AtExit 只读计数；postFinalizeDecref 计数所有经 Host shim 释放边界的终止后尝试。
程序不清空 sys.modules，不删除 f，不用 handles.Clear；native owner 析构次数与 core 根由原产品代码记录。

最终完整运行，两版各九进程全部符合预期（七项成功终态、两项明确失败对照）：

- 3.12.13：`~/.cache/ryupy/validation/adr0228/20261117T053038.938569Z.log`
- 3.13.3：`~/.cache/ryupy/validation/adr0228/20261117T053040.070517Z.log`

主正例均为 native/core/H/ledger/queue/late 全零，destroyed=7，低层退出 native=0，
postFinalizeDecref=0，finalize=0，stderr 为空。所有失败原始日志同目录独立保留。
