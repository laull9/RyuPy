# NativeAOT 速度优先发布配置评估与实测报告

日期：2026-11-15  
状态：子代理探索性A/B已完成；主代理核实参数证据，决定保持现有默认配置。  
规则约束：遵守 AGENTS.md、architecture.md、workflow.md 与 ADR-0182；所有构建与计时在仓库外系统临时目录完成并已清理，未修改任何产品源码（`crates/ryupy-driver/src/aot.rs` 保持只读未改动）。

---

## 1. 本机环境与实测方法学

### 1.1 实验环境配置
- **操作系统**：macOS (Darwin arm64, Apple Silicon)
- **.NET SDK**：`10.0.403`（路径：`/usr/local/share/dotnet/sdk/10.0.403/`）
- **目标运行时**：`osx-arm64`
- **基线编译器**：已冻结 CLI `/private/tmp/ryupy-string-before-pic-6bsaqis5/ryupy`（内嵌固定 Runtime 闭包与字符串编译器，未读写正在编辑的新 PIC 运行时）
- **对照 CPython**：`/Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12`（无 harness/stats，直接运行脚本验证退出码与 stdout/stderr 双流）

### 1.2 受控同输入（Control Invariants）双臂构建机制
1. **统一托管输入**：每个基准通过冻结 CLI 编译一次，在仓库外独立临时目录下获取完全一致的托管装配件（`<bench>.dll`）、展开固定的内嵌运行时（`RyuPy.Runtime.dll`）、`RuntimeDependencies.props` 及 `Program.cs`。
2. **两臂隔离发布**：
   - **Arm A（基线 Baseline）**：使用原生 `Host.csproj`，包含 `<PublishAot>true</PublishAot><Optimize>true</Optimize>`。
   - **Arm B（速度候选 Speed Candidate）**：追加 `<OptimizationPreference>Speed</OptimizationPreference>`；托管输入/引用相同，程序集名和独立发布路径分别处理。
3. **两臂独立构建输出**：分别在独立的 `armA/host`、`armA/publish` 与 `armB/host`、`armB/publish` 目录下执行 `dotnet publish -c Release -r osx-arm64 --self-contained true -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false`。

### 1.3 原始 ILC 参数与 `--Ot` 生效确证（ilc.rsp Diff 证据）

在两臂 obj 目录（`obj/Release/net10.0/osx-arm64/native/*.ilc.rsp`）提取真实编译器命令行，统一差异对比结果确证如下：

```diff
--- ArmA.ilc.rsp
+++ ArmB.ilc.rsp
@@ -247,6 +247,7 @@
 --stacktracedata
 --scanreflection
 --methodbodyfolding:generic
+--Ot
 --nowarn:"1701;1702;IL2121;1701;1702;8002"
 --warnaserror
 --warnaserr:";NU1605;SYSLIB0011"
```

**确证事实**：
- 基线 Arm A 仅含 `-O`，无 `--Ot`。
- 候选 Arm B 同时含 `-O` 与 `--Ot`。
- `--stacktracedata`、`--scanreflection`、`--methodbodyfolding:generic`、警告与异常选项完全保持不变，未改动任何折叠、元数据裁剪或全球化规则。

---

## 2. 正确性验证与交替测量协议

对 `tests/benchmarks/` 下挑选的 6 大典型基准（`fib`、`matmul`、`sieve`、`records`、`generators`、`str_concat`）及预检用例 `hello.py` 执行：
1. **正确性双流全同断言**：运行 CPython 3.12.13 获取权威基线，断言 Arm A 与 Arm B 在同次执行中的退出码均为 0，且 stdout 与 stderr 逐字节 100% 全同。
2. **1 warm-up + 5 交替测量**：
   - 预热轮：先执行 1 轮 Arm A 与 Arm B（丢弃计时）。
   - 稳态轮：执行 5 轮严格的交替进程运行（A1 -> B1 -> A2 -> B2 -> A3 -> B3 -> A4 -> B4 -> A5 -> B5），规避系统瞬态抖动与热缓存偏差。
3. **指标采集原语**：
   - **墙钟耗时（Wall-clock Duration）**：`time.perf_counter()` 高精度计时。
   - **峰值内存（Peak RSS）**：通过子进程 `os.fork()` 与 `os.wait4(pid, 0)` 获取 `rusage.ru_maxrss`（macOS 原生字节数 macbytes）。
   - **构建耗时（Build Duration）**：`dotnet publish` 端到端耗时。
   - **原生体积（Binary Size）**：可执行文件物理大小（字节）。

---

## 3. 实测数据与详细对比汇总

全部 6 项基准测试均 100% 通过 CPython 3.12.13 双流全等与退出码校验。以下汇总五维中位数、波动度及双臂差异：

| 基准名称 (Benchmark) | 类别特征 | 墙钟耗时 A (ms) / 波动 | 墙钟耗时 B (ms) / 波动 | 墙钟变化 (%) | 峰值 RSS A (B) | 峰值 RSS B (B) | RSS 变化 (%) | 原生尺寸 A (B) | 原生尺寸 B (B) | 尺寸变化 (%) | 构建耗时 A vs B (s) |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **`hello`** | 最小预检 | 12.35ms (1.1%) | 12.10ms (1.5%) | -2.02% | 13,877,248 | 13,910,016 | +0.24% | 11,137,840 | 11,499,648 | +3.25% | 10.45s vs 10.42s |
| **`fib`** | 递归与静态循环 | 92.39ms (1.4%) | 91.91ms (4.2%) | **-0.53%** | 14,336,000 | 14,385,152 | +0.34% | 11,137,840 | 11,499,648 | +3.25% | 10.47s vs 10.50s |
| **`matmul`** | 浮点矩阵嵌套循环 | 27.09ms (4.0%) | 30.04ms (31.8%) | **+10.92% (退化)** | 19,382,272 | 19,415,040 | +0.17% | 11,154,424 | 11,532,744 | +3.39% | 10.58s vs 10.46s |
| **`sieve`** | 列表切片与埃氏筛 | 173.02ms (5.7%) | 175.67ms (2.7%) | **+1.53%** | 55,263,232 | 55,328,768 | +0.12% | 11,137,848 | 11,516,184 | +3.40% | 10.53s vs 10.67s |
| **`records`** | 对象分发/排序/文本 | 137.06ms (1.8%) | 138.37ms (3.1%) | **+0.96%** | 57,475,072 | 57,393,152 | -0.14% | 11,187,496 | 11,549,320 | +3.23% | 10.62s vs 10.43s |
| **`generators`** | yield 状态机/生成器 | 410.42ms (2.1%) | 402.31ms (1.7%) | **-1.98% (提速)** | 91,619,328 | 91,684,864 | +0.07% | 11,154,408 | 11,516,232 | +3.24% | 10.45s vs 10.53s |
| **`str_concat`** | 密集字符串累加 | 11.35ms (19.9%) | 10.43ms (10.0%) | **-8.12% (提速)** | 23,871,488 | 23,920,640 | +0.21% | 11,154,376 | 11,516,200 | +3.24% | 10.50s vs 10.46s |

---

## 4. 主代理审查与决定

当前保持原默认优化配置，暂不新增产品CLI选项。探索性结果呈现正负变化，且 `matmul` 候选波动31.8%、`str_concat` 两臂波动19.9%/10.0%，单轮5次采样不足以确立这些变化的稳定性。

ILC响应文件中的 `--Ot` 增量已有原始命令证据。两臂除Speed外，还使用了不同程序集名和独立发布路径；其托管输入与运行时引用相同。计时表由子代理报告，计时脚本与原始数据已按本轮清理安排删除，主代理未独立复核该脚本；这些数值用于候选筛选，不与正式基线直接合并。

本轮没有反汇编、内联报告或硬件计数证据。原报告对循环展开、指令缓存、分支预测和寄存器分配的因果判断已撤回，原因待代码级测量。

主代理静态复核本机ILCompiler10.0.14 targets：

- `OptimizationPreference=Speed` 在 `Optimize=true` 时增加 `--Ot`，且在 `IlcPgoOptimize` 未指定时设为true。
- Unix targets会选择 `libRuntime.VxsortEnabled`；因此Speed配置效果还可能包含原生运行时库变化，不能仅归因于ILC指令优化。
- 默认方法体折叠为 `generic`。保持原值，不引入完整方法体折叠或元数据/全球化裁剪。

后续如重新评估，使用相同程序集名、同一托管输入、相同固定运行时与原始发布合同，复用已审查的 `benchlib.measure.run_once` 做交替采样，分别保留墙钟、RSS、体积和构建耗时。先证实稳定收益，再决定默认配置或显式发布选项。

## 5. 本轮交接

构建与计时独占窗口已释放。驱动产品源码未改，当前继续验证收益更直接的PIC按需分页；本报告记录探索结果与暂缓决定。
