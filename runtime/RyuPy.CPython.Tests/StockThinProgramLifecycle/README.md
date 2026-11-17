# stock 编译程序正常结束：产品程序集专项门

直接复制当前已构建的真实 Runtime/provider DLL，使用正式 `native/build.sh` 构建 shim。
驱动不修改产品源码、不注入构造器、不替换 callback 或终态实现；入口走真实
`Bootstrap.StartStockThin -> RunStockThinMain -> FinishStockThin`。
设计、首阶段失败与产品接线见 [ADR-0228](../../../docs/adr/0228-stock-compiled-program-lifetime.md)。

```sh
# 产品provider构建由主控统一完成，专项驱动不重建共享输出。
dotnet build runtime/RyuPy.CPython/RyuPy.CPython.csproj -c Release
python3 runtime/RyuPy.CPython.Tests/StockThinProgramLifecycle/run.py \
  --python ~/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3
python3 runtime/RyuPy.CPython.Tests/StockThinProgramLifecycle/run.py \
  --python ~/.local/share/uv/python/cpython-3.13.3-macos-aarch64-none/bin/python3
```

默认每版十三个进程，可用 `--case scoped` 等选定单门。C# 输入使用 `.cs.in`，避免进入共享 xUnit glob。
每个进程只初始化/终止一次解释器；业务体使用 Runtime public 中性 ABI 的静态 C#。
当前驱动针对 macOS arm64 JIT；日志在 `~/.cache/ryupy/validation/adr0228/` 逐次独立保存，
仓外临时 DLL/shim/观察库/配置/报告随 TemporaryDirectory 清理。

- `scoped`：Runtime 真 module/cache/context 和原 CP D；普通源码保留九个 F，内部临时 H 由产品入口作用域交还。
  原 D/builtins/identity、atexit、__del__ 原异常、unraisable 回调均通过，native/core/H/ledger/queue/late 全零。
- `scalar`：同一 CLR 字符串、同一非 small 整数重复导出，CP 每次释放返回值后重新调用；入口及 atexit 各1000轮。
  覆盖 scoped retirement 撤销 reverse/objects 身份，再真实 DECREF 的顺序。普通正例同样包含该回归。
- `external` / `external-d`：外部 H/独立 owned D 可恢复拒绝，原 H 可调用；明确释放外根后重试。
- `external-p` / `external-self` / `external-cell`：H=0，外逃 opaque 仍被拒绝；释放外包装后 CP 键留到合法终态。
- `external-container`：真实 synchronized transfer 仍持 F，显式 Dispose H 后 H=0 仍拒绝；交还 transfer 后成功。
- `external-callback`：入口执行期间第三方 managed getter 取新 opaque 并保存；代理先在 CP 侧析构、Roots 已零，
  独立 owned 仍正确拒绝且可读；随后释放并重试。第三方业务回调没有内部登记能力。
- `zero`：入口正常结束但未创建 F，仍交还 template/type 并 finalize。
- `entry-fail`：创建 F 前抛错，终态正常归零并保留退出码7。
- `no-host` / `pre-entry-fail`：回滚后无宿主、入口前无程序权限的稳定 finish，保留退出码17；普通报告没有伪造权限块。

每次真实 Entry 还通过公开 API 构造 `__builtins__=None` 的 CP frame，调用产品四个晚期异常/builtin API，
验证 NULL/非 dict 防护及静态异常回退。没有重建 builtins 或业务源码委派。

独立 C 观察库只使用公开 Py_AtExit，在解释器拆除后读取产品纯 C nativeLive 计数；不持 CP 对象、不做清理。
驱动还逐字段验证产品 `RYUPY_CPYTHON_REPORT.stock_compiled_program`，包括 permission/terminal/finalize、
实际 owner 析构、native calls、core/H/ledger/queue/late_releases/post_finalize_decrefs。

首阶段故意泄漏的两个源码实验日志继续保留：
`20261117T053038.938569Z.log`、`20261117T053040.070517Z.log`。
它们属于首阶段冻结源实验；当前驱动已切换实际产品，不再提供绕过产品释放的失败开关。

最终产品专项完整日志：

- 3.12.13 十三进程：`20261117T061115.127269Z.log`
- 3.13.3 十三进程：`20261117T061116.255702Z.log`

两版原普通薄函数四进程与各七项托管协议门也通过；日志分别为
`~/.cache/ryupy/validation/adr0208-20261117/stock-thin-provider-jit-cpython-3.12.13-macos-aarch64-none-20261117T060735.709271Z-24396.log`、
`~/.cache/ryupy/validation/adr0208-20261117/stock-thin-provider-jit-cpython-3.13.3-macos-aarch64-none-20261117T060857.408873Z-25141.log`。
实际 CLI/JIT/NativeAOT/onefile 的统一产品证明由主控另行执行，本驱动只验上述程序集门。
