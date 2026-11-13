# 快速上手与部署边界

当前已实现动态核心、容器、普通类、异常、模块、闭包、生成器、装饰器及部分标准库的有限能力；各自仍有明确边界，不能按阶段名称推断完整 Python 兼容。实际支持见 [semantics.md](semantics.md)、[TODO](../TODO.md) 与[验证归档](validation/)，路线图不是验收结果。

## 开发环境

| 依赖 | 版本 | 用途 |
| --- | --- | --- |
| Rust | stable，2024 edition | 构建编译器 |
| .NET SDK | 10.x | 构建 C# 运行时、测试与有限 NativeAOT 发布 |
| Python | 3.12.13 | 固定 CPython 差分验收基准 |

```bash
make runtime
make check
```

这两条命令构建运行时并执行 fmt、clippy、Rust / C# 测试、用例自检与 e2e。项目级覆盖率见 [compatibility.md](compatibility.md)，基准见 `make bench`。

## 编译、运行与查看 IR

```bash
make run FILE=tests/e2e/phase1/m1_5_dataclass.py
cargo run -q -p ryupy-cli -- build tests/e2e/phase1/m1_0_print_int.py -o out/
dotnet out/m1_0_print_int.dll

make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=ast
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=hir
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=types
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=mir
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=cil

make e2e
make e2e-all   # 包括未来尚未实现的 pending 用例
make bench     # CPython vs RyuPy Release 的固定输入基准（不进入 make check）
```

`import` 支持编译时可达的模块与包：搜索顺序是入口文件所在目录 → `-I/--search-path` 指定的额外根（可重复）。项目集用 `-I third_party/projects/...` 加载第三方源码。`from dataclasses import dataclass` 仍是特殊识别的编译期能力；部分标准库已有原生实现；任意动态 import hook 与 libpython 桥接不在当前完整支持范围。不要据未来路线图推断现有 CLI 已具备这些能力。

```bash
# 额外搜索根：入口目录优先，然后是给定的第三方源码目录
cargo run -q -p ryupy-cli -- run app.py -I third_party/projects/toml-0.10.2/toml-0.10.2
```

## 当前产物

`build` 默认把产物写在源文件旁的 `__pycache__/<文件名>/`（与 CPython 存放 `foo.cpython-312.pyc`
的 `__pycache__` 同位），也可以用 `-o` 指定目录：

```text
out/
├── m1_0_print_int.dll
├── m1_0_print_int.runtimeconfig.json
└── RyuPy.Runtime.dll
```

构建当前仓库需要 SDK 来生成 `RyuPy.Runtime.dll`。生成的普通程序集通过 .NET Runtime 运行，运行目标机不需要编译器或 Python。编译器自身不调用 CPython 做编译；Python 目前用于差分测试与基准对照。

### 发行包

`make build` 构建 Release 版 CLI 与 C# 运行时，并在 `releases/` 下打包宿主平台产物：

```text
releases/
├── ryupy-0.0.1-linux-x86_64/
│   ├── bin/ryupy                # Rust 编译器 CLI
│   └── runtime/RyuPy.Runtime.dll
└── ryupy-0.0.1-linux-x86_64.tar.gz
```

以上仅示意宿主平台包布局；实际版本/平台后缀由打包脚本决定，不是跨平台验收声明。本轮已证平台仅 Ubuntu 24.04 x86_64。

CLI 查找运行时的顺序是：`RYUPY_RUNTIME_DIR` → 可执行文件旁边的 `runtime/`（`bin/../runtime`
或同目录）→ 开发期的 `runtime/…/bin/Release/net10.0/`。所以解压后无需设置环境变量即可使用；
目标机仍需要 .NET Runtime（当前目标框架 net10.0），但不需要 Rust 工具链、.NET SDK 或 Python。
包名只反映构建机器的平台，`make build` 不做交叉编译。

## JIT 与 NativeAOT 的当前边界

状态日期：2026-11-12。产品证据对应代码 [85b11a94](https://github.com/laull9/RyuPy/commit/85b11a94368f0ba0a0bda80d611b9e0c39a5cf7b)，本次仅更新文档，不表示在后续文档提交上重新构建或运行产品。

`--aot` 已有真实发布实现，不再是占位入口。普通 `build` 生成托管程序集，`build --aot` 调用 NativeAOT 生成宿主目标的可执行文件；构建需 .NET SDK 与原生工具链。Native 运行免装 .NET / Python，但仍依赖目标平台系统库。

下面直接使用已验收的有限 Decimal 图（在仓库根目录运行）：

```bash
cargo run -q -p ryupy-cli -- build tests/aot/decimal_finite_native/fixtures/entry.py --out-dir out/decimal-native --aot
./out/decimal-native/entry.dist/entry
```

AOT 输出根包含 `<入口名>.build/` 中间产物和 `<入口名>.dist/` 交付目录；部署应保留完整 `.dist`，不要只复制可执行文件。已有未知或修改过的输出目录不会被覆盖，重复构建请选新的输出根。

本轮 [CI 43210802587](https://github.com/laull9/RyuPy/actions/runs/43210802587) 的独立 Decimal 与 tzinfo 用户单基图，在 Ubuntu 24.04 x86_64、.NET SDK 10.0.403、CPython 3.12.13 基准下完成双 PIC JIT、真实 Linux x64 ELF 原位及搬迁验证。命令、fixture 与验收边界见 [Decimal](../tests/aot/decimal_finite_native/README.md)、[tzinfo](../tests/aot/tzinfo_native/README.md)。这不是 macOS / Windows AOT 验收，也不是完整 toml Native、全 MI Native 或完整 Decimal / datetime 支持。

固定 20 项目在构建时启用 Cargo feature `ryupy-driver/ordinary-dynamic-module-pilot` 的 pilot 为 14 成功 / 6 unsupported / 0 失败，默认源计划为 13 / 6 / 1（旧 Markdown 运行期失败）。这些是 managed JIT 入口结果，不是 NativeAOT 数量；默认 JIT 的 Markdown 失败是 `NotImplementedError: find_spec(): legacy or native module registrations without source metadata are not implemented`，pilot JIT 的该入口通过。独立 `distribution-metadata` AOT 仍失败于 `[E0100] 显式实例 AOT 需要资源清单且不支持原生绑定`，与默认 JIT 失败不同，整次 CI 不应称全绿。libpython 桥接 JIT / AOT 仍属于后续方案。
pilot 是构建时 feature，不是 CLI 运行参数；普通默认构建未启用它，因此默认命令对应 13 / 6 / 1，不应期待 pilot 的 14 / 6 / 0。

CLI 内嵌预构建运行时、全局缓存与跨平台发行仍属于后续分发设计；当前 `make build` 只产出宿主平台的
CLI + 运行时压缩包，没有单文件 CLI 发布。产物可跨哪些系统 / 架构运行，以实测支持矩阵为准。

原生通过、桥接通过和已接受偏差的区别见 [compatibility.md](compatibility.md)。Native AOT 的目标平台、构建与加载限制参考 [Microsoft 文档](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)。

### 统一项目部署配置（有限范围验收）

新入口将部署声明集中到一个显式 JSON 文件；首切只支持 `schema_version: 1`、`mode: "aot"`。例如项目目录中的 `ryupy.json`：

```json
{
  "schema_version": 1,
  "mode": "aot",
  "search_paths": ["package"],
  "include_modules": ["pyfiglet", "pyfiglet.fonts"],
  "resources": "resources.json",
  "distribution_metadata": "metadata-request.json",
  "targets": {
    "cli": {"entry": "package/pyfiglet/__main__.py"},
    "metadata": {"entry": "console_entry.py"}
  }
}
```

```bash
ryupy build --project /work/app/ryupy.json --target cli
ryupy build --project /work/app/ryupy.json --target metadata -o out/metadata
```

`--project` 必须指向文件，不自动发现目录；`--target` 必须显式选择一个目标，不构建所有入口。配置的 `mode` 已决定 AOT，无需且不能再加 `--aot`。示例要求对应源码及声明文件实际存在；可省略搜索根、显式模块及两种声明字段，不会因此自动发现包数据。

原生优化偏好可在项目顶层写 `"aot_optimization": "size"`，可选 `default`、`size`、`speed`，省略保持原有 SDK 默认。直接编译单文件时使用：

```bash
ryupy build app.py --aot --aot-optimization size -o out/app
```

`--aot-optimization` 不能与 `--project` 混用，也不适用于 JIT。`size` 偏向交付体积，`speed` 偏向运行速度；均不保证对所有应用获益，须用自己的真实负载比较。它们不关闭兼容性检查或 AOT 警告，也不启用增量缓存。SDK 实际采用值保存在本次 `.build` 的 `aot-optimization.json`；这份诊断收据不随 `.dist` 交付，不是缓存命中凭证。

配置中的入口、搜索根和声明文件路径相对于配置文件父目录，须留在该目录内且不跟随内部符号链接；搜索根可用 `"."`。显式 `--project` 本身可使用绝对路径或相对调用目录的路径。嵌套请求不重写：metadata 的 `roots` 仍相对该请求文件，resources 的文件仍按原模块资源合同解释。

资源请求可以保留逐文件 `files`，也可在明确选定的模块内声明目录与字面后缀。例如 `resources.json`：

```json
{
  "schema_version": 1,
  "modules": [{
    "module": "pyfiglet.fonts",
    "trees": [{"directory": ".", "suffixes": [".flf", ".flc"]}]
  }]
}
```

目录相对于该模块实际源码父目录，递归选择匹配文件；这不是 glob，也不会自动加入模块或第三方依赖。空匹配、链接、不安全路径及 Python 源码/字节码拒绝；仍受现有资源数量、大小和来源冻结限制。`files` 与 `trees` 可合用。实际生成的 `<入口>.build` / `<入口>.dist` 必须与资源树分离。相等或任一包含另一时，编译器会在资源树扫描、MIR发射与SDK启动前报告模块、资源路径及相交输出路径，建议调整 `-o` 或收窄 `directory`；不会静默排除资源。项目根作为 `-o`、`assets` 与 `.build` / `.dist` 为兄弟目录仍可用。早期失败可保留自有attempt及日志，但不改变旧dist。后缀区分大小写，不读取 `.gitignore`；路径合法性及链接/特殊文件检查适用于所有被扫描条目，包括未匹配文件。此新声明已独立通过 [Linux CI 43296651927](https://github.com/laull9/RyuPy/actions/runs/43296651927) 与[同树 macOS/Windows CI 43298130779](https://github.com/laull9/RyuPy/actions/runs/43298130779)：两入口、553资源与原位/搬迁运行。只有Linux另验禁网无Python/.NET容器；不产生新性能结论。

默认输出根为配置目录的 `build/<目标名>`，其下沿用 `<入口名>.build/`、`<入口名>.dist/`；`-o` 覆盖仍相对调用者当前目录。项目模式与位置输入、`-I` / `--search-path`、`--include-module`、`--resources`、`--distribution-metadata`、`--native-binding`、`--instance-module`、`--instance-direct-globals` 互斥，不隐式合并。

普通默认构建保持 `ExplicitOnly` 模块策略，不因项目配置或 AOT 源位置计划自动启用实例模块。项目文件是部署声明，不是完整依赖锁，也不证明动态依赖闭包。[CI 43255885614](https://github.com/laull9/RyuPy/actions/runs/43255885614) 在候选 `2556fea1` 的无 feature Release CLI 上，已验证 pyfiglet 1.0.2 原 CLI 与 metadata 加载入口：Linux x64 四次公开项目构建、四份独立产物验收、12 次隔离源码宿主运行及12次禁网无 Python/.NET 容器运行通过。

其中搬迁验证是保留旧 `build` 证据后，将自足源码项目移至新位置重新构建，不是复用搬迁的编译缓存。这一有限结果不证明跨平台、任意依赖闭包或其他项目兼容，也没有新的性能结论；合同及验证层次见 [deployment_project](../tests/aot/deployment_project/README.md)。

## 后续阶段的 Python 用例

Phase 2–4 用例已经写入 `tests/e2e/` 并标记 pending。`python3 tests/run_python_baseline.py` 自检 Phase 1–3 的 CPython 行为；安装 NumPy 后加 `--phase phase4` 检查扩展基线。辅助包与预期错误退出的运行方式见 [测试说明](../tests/README.md)。这些基线通过不表示 RyuPy 已支持相应功能。

## 可选：原生打包前验证无条件声明依赖

显式分发配置可以加入 `"dependency_check": {"mode": "unconditional_closure", "requirements": ["binaryornot==0.4.4"]}`。它在加载Python源码及调用SDK前检查该根的传递声明依赖，例如缺少chardet或版本不满足会直接报“依赖预检”错误。不会替你安装包，也不会读取宿主Python环境。

首切拒绝活跃依赖中的markers、extras和URL要求，不推测目标环境；不使用该字段则保持旧行为。成功的编译期 `dependency-check.json` 只证明所选声明闭包的存在/版本匹配，不能代替实际Native运行验证。配置和范围见[ADR-0176](adr/0176-explicit-dependency-preflight.md)。


### 部署什么、保留什么

- 交付整个 `<入口>.dist/`，不要只复制可执行文件；资源和分发 metadata 可能是程序必需的。
- `.build/` 是本轮构建与诊断资料，不是部署运行所需。Linux x64 会把**身份验证通过的本次 ELF 调试符号**留在这里，而不随 `.dist` 发货；未知旁件、用户资源和 macOS / Windows 的符号保持保守复制。
- CLI 报告 `AOT 调试符号清单：.../debug-symbols.json` 时，先看 `status`。只有 `separated` 表示已分离，`retained` 表示本轮未分离（也可能没有符号文件）；原因在 `reason`。保留符号仍占构建磁盘，交付变小不等于总磁盘变小。
- 要保留后续调试能力，归档本轮 `.build` 和匹配 `.dist`。清单中 `symbols_relative_path` 相对于该清单所在的 attempt 目录；不要混用另一次构建的同名符号。

搬迁后的 Linux 程序可以显式加载匹配符号，例如在 GDB 中执行（尖括号替换为实际值，先选择要检查的函数断点）：

```text
file /absolute/path/app.dist/app
symbol-file /absolute/path/attempt/ryupy-aot-0/publish/<本次host>.dbg
break <函数符号>
run <程序参数>
bt
```

这不是搬迁后自动发现符号的承诺；调试器、源码位置及实际崩溃栈也会影响可见信息。精确身份、事务行为及真实验收见[ADR-0177](adr/0177-linux-native-symbol-delivery.md)。不要手工删除 `.dist` 的未知文件来减包。
