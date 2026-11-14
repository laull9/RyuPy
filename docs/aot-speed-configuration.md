# NativeAOT 速度优先发布配置评估与准备方案

日期：2026-11-15。
状态：实施子代理评估方案；为主代理（GPT6.1 Sol）准备低风险、可衡量的下一竖切。
当前规则约束：遵守 AGENTS.md、architecture.md、workflow.md 与 ADR-0182；当前禁止直接修改源码、禁止执行 cargo/dotnet/benchmark、不悄悄更改默认配置。

---

## 1. 本机 SDK 10.0.403 与 ILCompiler Targets 静态核查事实

基于本机已安装环境静态查验（无需网络，不计算哈希）：
- **.NET SDK 版本**：`10.0.403`（路径：`/usr/local/share/dotnet/sdk/10.0.403/`）
- **ILCompiler 包版本**：`Microsoft.DotNet.ILCompiler 10.0.14` 与 `runtime.osx-arm64.microsoft.dotnet.ilcompiler 10.0.14`
- **NativeAOT Targets 关键文件**：
  - `~/.nuget/packages/microsoft.dotnet.ilcompiler/10.0.14/build/Microsoft.NETCore.Native.targets`
  - `~/.nuget/packages/microsoft.dotnet.ilcompiler/10.0.14/build/Microsoft.NETCore.Native.Unix.targets`
  - `~/.nuget/packages/microsoft.dotnet.ilcompiler/10.0.14/build/Microsoft.NETCore.Native.Windows.targets`

### 1.1 属性名 `OptimizationPreference` 与默认行为

在 `Microsoft.NETCore.Native.targets` 中，NativeAOT 显式读取 `$(OptimizationPreference)`：

1. **最终 ILC 代码生成开关**（行 273–274）：
   ```xml
   <IlcArg Condition="$(Optimize) == 'true' and $(OptimizationPreference) == 'Size'" Include="--Os" />
   <IlcArg Condition="$(Optimize) == 'true' and $(OptimizationPreference) == 'Speed'" Include="--Ot" />
   ```
   - RyuPy 现有的 `Host.csproj` 已声明 `<Optimize>true</Optimize>`。
   - **未指定 `OptimizationPreference` 时（现有默认）**：`$(OptimizationPreference)` 为空，MSBuild 既不生成 `--Os`，也不生成 `--Ot`，ILC 仅接收基础优化开关 `-O`（行 245 `<IlcArg Condition="$(Optimize) == 'true'" Include="-O" />`）。
   - **显式指定 `OptimizationPreference=Speed` 时**：ILC 命令行追加 `--Ot`（`--optimize-time`），指示 RyuJIT 后端采取更偏向执行速度的代码生成策略（更宽容的内联预算、激进循环展开与调度）。

2. **PGO 选项联动**（行 29）：
   ```xml
   <IlcPgoOptimize Condition="'$(IlcPgoOptimize)' == '' and '$(OptimizationPreference)' == 'Speed'">true</IlcPgoOptimize>
   ```
   - 当设为 `Speed` 时，若未显式配置 `IlcPgoOptimize`，Targets 会自动将其置为 `true`，触发查找预置 Mibc 配置。

3. **向量化排序（VxSort）支持库联动**（Unix.targets 行 75 / Windows.targets 行 26）：
   ```xml
   <VxSortSupportName Condition="'$(OptimizationPreference)' == 'Speed' or '$(IlcEnableVxsort)' == 'true'">libRuntime.VxsortEnabled</VxSortSupportName>
   ```
   - 当设为 `Speed` 时，Unix 链接器改链接向量化支持库 `libRuntime.VxsortEnabled`，启用 SIMD 加速排序。

4. **脱水压缩（Dehydration）联动**（Native.targets 行 244）：
   ```xml
   <IlcArg Condition="$(IlcMultiModule) != 'true' and '$(IlcDehydrate)' != 'false' and ($(_targetOS) != 'win' or '$(OptimizationPreference)' == 'Size' or '$(IlcDehydrate)' == 'true')" Include="--dehydrate" />
   ```
   - 在 macOS/Linux 下，`$(_targetOS) != 'win'` 恒真，默认保留 `--dehydrate`；Windows 的此条件在空偏好与Speed下都可能不启用；这条规则无法单独证明从默认切Speed会减少开销。

### 1.2 `IlcFoldIdenticalMethodBodies` 的核实与真实限制

在 `Microsoft.NETCore.Native.targets`（行 226–230）：
```xml
<PropertyGroup>
  <_IlcMethodBodyFoldingValue Condition="$(IlcFoldIdenticalMethodBodies) == 'true' or $(StackTraceSupport) == 'false'">all</_IlcMethodBodyFoldingValue>
  <_IlcMethodBodyFoldingValue Condition="$(_IlcMethodBodyFoldingValue) == '' and $(IlcFoldIdenticalMethodBodies) != 'false' and $(IlcMultiModule) != 'true'">generic</_IlcMethodBodyFoldingValue>
  <_IlcMethodBodyFoldingValue Condition="$(_IlcMethodBodyFoldingValue) == '' or $(Optimize) != 'true'">none</_IlcMethodBodyFoldingValue>
</PropertyGroup>
...
<IlcArg Include="--methodbodyfolding:$(_IlcMethodBodyFoldingValue)" />
```

**关键事实与澄清**：
- **默认值并非禁用**：当未显式指定 `IlcFoldIdenticalMethodBodies` 时，其值为 `''`，条件 `$(IlcFoldIdenticalMethodBodies) != 'false'` 为真，NativeAOT 默认计算出的折叠模式为 **`generic`**！ILC 实际接收的参数是 `--methodbodyfolding:generic`。
- **不可称为“未启用”**：通用泛型方法实例化折叠默认是开启的。
- **强制 `all` 的真实限制与诊断风险**：
  - 只有显式设置 `<IlcFoldIdenticalMethodBodies>true</IlcFoldIdenticalMethodBodies>` 或 `<StackTraceSupport>false</StackTraceSupport>` 时，折叠才会升级为 `all`。
  - `all` 扩大可折叠目标；实际是否能合并仍取决于机器码、引用/重定位及工具链规则，不能仅凭thunk形态相似推断。
  - 这会导致不同方法共享相同的 PC 地址，可能影响运行时调用栈精度（`StackTrace` 无法正确区分重叠方法名）。对于依赖精确异常 traceback 的 Python 运行时诊断而言，破坏栈跟踪会导致诊断降级。
  - 此外，方法折叠本质是代码体积（Size）去重，对热循环执行吞吐量并无理论增益，实际吞吐影响需要测量。
- **结论**：**不修改 `IlcFoldIdenticalMethodBodies`，维持默认的 `generic` 折叠与高保真调用栈支持**。

---

## 2. 语义安全边界与最小仅 Speed 候选

为了保证 RyuPy 既有功能契约与 Python 语义纯净性，严格划定边界：
- **禁止添加 `InvariantGlobalization`**：Python 运行时依赖正确的区域与 Unicode 字符串大小写/格式化转换，启用不变区域会导致语义偏差。
- **禁止添加反射元数据激进裁剪（如 `IlcTrimMetadata=true`）**：RyuPy 依赖必要的基础元数据进行 CIL 互操作与动态分发。
- **禁止添加 `unsafe` 编译器开关**：禁止关闭边界检查或类型安全保障。

### 最小候选定义

修改范围仅限 `Host.csproj` 中的 `<PropertyGroup>`：
```xml
<!-- 现有基线 -->
<AssemblyName>{host}</AssemblyName><PublishAot>true</PublishAot><Optimize>true</Optimize>

<!-- 最小候选（仅追加 OptimizationPreference） -->
<AssemblyName>{host}</AssemblyName><PublishAot>true</PublishAot><Optimize>true</Optimize><OptimizationPreference>Speed</OptimizationPreference>
```

---

## 3. 双臂 Host 项目生成与独立 Publish 方案

为使主代理在下一阶段进行严格、可复现、无污染的 A/B 验证，方案设计如下：

### 3.1 固定的受控输入（Control Invariants）

两臂评测必须固定完全相同的前置产物：
1. **相同的托管程序集**：同一 Python 源文件由同一编译器 Session 生成的 `<input>.dll`（文件哈希完全一致）。
2. **相同的托管运行时**：同一构建的 `RyuPy.Runtime.dll`。
3. **相同的入口桩代码**：
   ```csharp
   internal static class Program {
       private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args);
   }
   ```
4. **相同的 SDK 与 RID 环境**：macOS arm64（`osx-arm64`），.NET SDK 10.0.403。

### 3.2 独立工作根与双臂生成流程

依据 ADR-0182，所有构建与运行目录均建立在仓库外的系统临时目录（如 `/tmp/ryupy-aot-eval-*`），两臂完全隔离：

- **Arm A（基线 Baseline）**：
  - 工作根：`/tmp/ryupy-aot-eval-armA/`
  - 项目配置：`Host.csproj` 包含 `<PublishAot>true</PublishAot><Optimize>true</Optimize>`。
  - 发布命令：
    ```bash
    dotnet publish /tmp/ryupy-aot-eval-armA/host/Host.csproj -c Release -r osx-arm64 --self-contained true -o /tmp/ryupy-aot-eval-armA/publish -p:ImportDirectoryBuildProps=false -p:ImportDirectoryBuildTargets=false -p:IlcVerboseLogging=true
    ```
- **Arm B（速度优先 Speed Candidate）**：
  - 工作根：`/tmp/ryupy-aot-eval-armB/`
  - 项目配置：`Host.csproj` 包含 `<PublishAot>true</PublishAot><Optimize>true</Optimize><OptimizationPreference>Speed</OptimizationPreference>`。
  - 发布命令相同，但因项目属性触发 `--Ot`。

### 3.3 原始 ILC 命令与 `--Ot` 生效核验机制

为了确凿验证 `--Ot` 生效而非仅由 MSBuild 表面传递，在 publish 过程中：
1. 传入 `-p:IlcVerboseLogging=true`，或在发布输出 `publish.stdout.log` 中检索 `ilc` 调用。
2. 检查中间目录 `/tmp/ryupy-aot-eval-arm*/obj/Release/net10.0/osx-arm64/native/*.ilc.rsp` 响应文件内容：
   - **Arm A 响应文件**：仅含有 `-O`，无 `--Ot`。
   - **Arm B 响应文件**：同时含有 `-O` 与 `--Ot`。
3. 确认无任何回退警告与错误。

---

## 4. 六大代表样例 A/B 评测方案

选取 `tests/benchmarks/` 下的 6 个典型工作负载：

| 样例脚本 | 核心测试路径与特征 |
| :--- | :--- |
| `tests/benchmarks/str_concat.py` | 字符串拼接、内存分配与缓冲扩展 |
| `tests/benchmarks/generators.py` | 生成器状态机、`yield` 帧恢复与局部槽保留 |
| `tests/benchmarks/sieve.py` | 紧密循环、数组索引、埃氏筛素数算法与算术指令 |
| `tests/benchmarks/records.py` | 类实例化、属性读取与对象分配 |
| `tests/benchmarks/matmul.py` | 嵌套双重/三重循环、数值计算与 2D 列表寻址 |
| `tests/benchmarks/fib.py` | 深层递归调用、函数栈帧开销与调用契约 |

### 4.1 严格区分的五维衡量指标

评测时必须分离以下指标，绝不混淆概念，不承诺预设收益：
1. **冷启动耗时（Startup Latency）**：空载或仅执行 minimal warm-up 的端到端进程退出墙钟时间。
2. **稳态吞吐耗时（Throughput Duration）**：大规模循环迭代（如 N 次运算）的核心执行耗时。
3. **构建耗时（Build Duration）**：`dotnet publish` 自启动至生成 native 二进制的耗时。
4. **产物体积（Binary Size）**：剥离符号后的最终可执行文件物理大小（字节）。
5. **最大常驻内存（Peak RSS）**：通过 `/usr/bin/time -l`（macOS）或 `/usr/bin/time -v`（Linux）采集的峰值内存。

> **免责声明**：当前阶段不编造任何候选性能数据。由于 RyuPy 现阶段有部分开销处于 Python 对象分发与运行时方法，JIT 后端的 `--Ot` 优化对不同特征代码的实际增益需由主代理实测确认。

---

## 5. 对现有 Driver FakeSDK 单测与契约保留的具体规划

现有驱动与测试中，涉及 AOT csproj 属性校验的单测必须保持 100% 兼容。

### 5.1 涉及的既有测试清单

1. `crates/ryupy-driver/src/aot_tests.rs`:
   - `host_project_is_explicit_strict_and_escapes_literal_paths`:
     断言了 `<EnableDefaultCompileItems>false</EnableDefaultCompileItems>`、`<Compile ` 唯一性、`TreatWarningsAsErrors`、`IlcTreatWarningsAsErrors`、`ILLinkTreatWarningsAsErrors`，以及否定断言 `!text.contains("TrimmerRootAssembly")`、`!text.contains("NoWarn")`、`!text.contains("Reflection")`。
   - `host_imports_the_same_runtime_dependency_contract`
   - `sdk_version_target_uses_same_publish_and_escaped_owned_path`
2. `crates/ryupy-cli/tests/aot_failure.rs`:
   - `failed_sdk_keeps_old_native_and_reports_both_streams`
   - `successful_sdk_without_new_native_cannot_reuse_old_artifact`
   - `verify_failure`: 断言了 `-r linux-x64`、`--self-contained true`、`-p:ImportDirectoryBuildProps=false`、`-p:ImportDirectoryBuildTargets=false`，以及 `<PublishAot>true` 等。
3. `crates/ryupy-cli/tests/binding_aot_failure.rs`:
   - `binding_failed_sdk_keeps_old_native_and_reports_both_streams`:
     除普通 AOT 属性外，断言了 `<DirectPInvoke ...>` 与 `<NativeLibrary ...>`。

### 5.2 参数保留与扩展方案

在后续源码实施阶段：
- **既有参数完全不动**：保留现有的 `PublishAot`、`Optimize`、`EnableDefaultCompileItems`、`TreatWarningsAsErrors` 等全部标记，不破坏任何一个既有测试的字符串匹配契约。
- **在 `aot_tests.rs` 中新增独立断言/测试**：
  - 增加对 `<OptimizationPreference>Speed</OptimizationPreference>` 的属性结构断言。
  - 验证其与 `<Optimize>true</Optimize>` 共存且不引入非法的 XML 标签。
  - 既有单元测试 `host_project_is_explicit_strict_and_escapes_literal_paths` 保留，仅在其属性集合中追加或者在新增专用单测中检查 `OptimizationPreference`。

---

## 6. 下一步主代理（GPT6.1 Sol）竖切建议

1. **第 1 步（双臂脚手架验证）**：在系统临时目录生成 Arm A 与 Arm B 的 `Host.csproj`，运行单次 publish，打印并对比 `.ilc.rsp` 文件，确认 `--Ot` 正式出现在命令行中。
2. **第 2 步（六大基准微测）**：针对 `str_concat`、`generators`、`sieve`、`records`、`matmul`、`fib` 编译后分别以 Arm A 和 Arm B 运行 5 轮，记录启动、吞吐、体积与 RSS。
3. **第 3 步（驱动集成与验收）**：根据实测收益决定是否将 `OptimizationPreference=Speed` 固化至 `crates/ryupy-driver/src/aot.rs`，并在 `aot_tests.rs` 中添加对应断言，最后全量执行 `make check`。
