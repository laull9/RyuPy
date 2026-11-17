# stock 薄函数 provider 独立 Runner

本驱动验证显式内部 Host API：固定 A closure 胶水、GC owner、静态 C# 业务叶子、公开 H、
动态对象与异常、失败回滚和解释器关闭。产品默认关闭，尚无 Rust 编译资格或编译器接线。

## JIT 与 NativeAOT

要求 macOS arm64、clang、.NET SDK/NativeAOT 工具链，以及带头文件和共享库的常规 GIL
PBS CPython 3.12.13 或 3.13.3。必须已有经过验证的
`runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll`；驱动将它复制到仓外输入目录，
只按 DLL 引用，完全不重建 Runtime。外部依赖沿用 `RuntimeDependencies.props` 的固定版本。

默认仍为 JIT：

```sh
python3 runtime/RyuPy.CPython.Tests/StockThinProvider/run.py \
  --python "$HOME/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3"
```

显式 AOT，两版分别运行：

```sh
python3 runtime/RyuPy.CPython.Tests/StockThinProvider/run.py --aot \
  --python "$HOME/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3"
python3 runtime/RyuPy.CPython.Tests/StockThinProvider/run.py --aot \
  --python "$HOME/.local/share/uv/python/cpython-3.13.3-macos-aarch64-none/bin/python3"
```

`--aot` 使用 `dotnet publish -r osx-arm64 -p:PublishAot=true`，provider/Runner 开启 AOT
分析器及 warnings-as-errors，链接/原生编译告警同样按错误处理。执行时直接启动发布的原生
`RyuPy.CPython.Tests`，不经 `dotnet` 或 JIT apphost。`Program.Main` 在 Host.Start 前核对
`RuntimeFeature.IsDynamicCodeSupported`；驱动设置 `RYUPY_STOCK_THIN_MODE=aot` 后要求其为
false，默认 JIT 模式要求其为 true。

每版默认运行四个独立进程，每进程独占一次解释器初始化/终止：

1. 普通矩阵：binder、captured/live/rebind builtins 与 globals、F/H 身份、cell 改写/恢复、
   动态标量/可变对象身份、异常、构造失败、重导及失败事务、P/self/cell 外逃、回收和关闭重试。
2. `blocked`：已有 audit hook 拒绝新增 hook 时，候选创建资格明确拒绝。
3. `watcher-full`：外部占满 watcher 槽位，GC owner 候选仍可运行且不清除外部 ID。
4. `resurrect`：正常 GC 的 `__del__` 中即时调用 CIL，以及复活后的调用、audit 和 H 回流。

`--resurrection` 只诊断第四门；`--ordinary-only` 只运行前三门，二者均是缩小范围的诊断。
`--unit` 额外运行托管协议 xUnit 测试；这部分始终使用 JIT，在日志中单列 `UNIT mode=jit`。
高级原生 DESTROY watcher 复活后的已清字段对象由独立 stock oracle 定界，本 Runner 不调用它。

## 产物与证据

成功、失败、超时都会清理仓外临时项目、obj/bin、NativeAOT Runner、shim、测试动态库及配置。
命令超时只终止本命令的独立进程组；不会关闭主代理的共享构建服务。构建显式禁用 build server，
避免并行验证的服务状态干扰。AOT 发布超时为 600 秒，单个 Host 进程为 55 秒。

每次运行独立保留完整命令、退出码、stdout/stderr 与模式信息到：

```text
~/.cache/ryupy/validation/adr0208-20261117/stock-thin-provider-{jit|aot}-*.log
```

任何命令退出非零或 stderr 非空都会失败；旧日志不会覆盖。最终 `PASS suite` 明确列出模式、
固定 CPython 版本、进程数和实际执行的门；只有四门完整运行才是该模式的完整 Runner 结果。
仓库中不写构建产物、普通文件摘要、golden 或生成报告。

本门的业务体是 Runner 手写静态 C# 方法，AOT 模式验证这些方法真实编译执行。
shim/tools 被复制到本次独立输出目录，CPython 标准库和 libpython 仍来自固定发行原路径。
完整产品 CIL 闭包、Rust 编译资格、standalone/onefile 交付、删除输入后的搬迁、多平台及性能
继续由各自门验收；本门通过后产品默认开关保持关闭。
