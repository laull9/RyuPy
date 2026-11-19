# RyuPy 全 CIL 执行与 libpython 底层化：C0 回归合同与可复现基线冻结

- 阶段：C0（执行归属、启动链与回归基线盘点）
- 日期：2026-11-16
- 决策基准：[ADR-0208](adr/0208-cil-execution-libpython-kernel.md)
- 执行路线：[全 CIL 执行与 libpython 底层化实施路线](cil-execution-roadmap.md)
- 状态：**C0 兼容/性能/NativeAOT 回归合同基线已冻结；C1–C3 首轮切片已实施，完整阶段门继续待办**
- 规范说明：本文档记录当前冻结基准、现有驱动入口与历史已验收证据。严格区分「当前实测探测」与「历史已验收结论」，不将历史记录冒充当前实测。

2026-11-16 后续决策：[ADR-0223](adr/0223-stock-libpython-bounded-glue.md) 已接受原版
libpython 与少量必要胶水。本文的 C0 历史冻结数据保持原样；新主线按该 ADR 验收执行清单、
语义、性能和分发，原全阶段零执行门留给可选严格研究。

---

## 1. 当前 Git 基准与版本元数据

- **分支**：`feat/adr0208-cil-execution`
- **基准 Commit**：`5d36db1e952f1db07aee6563d7a800cc1df94548`（`5d36db1e`）
  - Commit 信息：`docs: 规划全CIL执行与libpython底层化路线`
  - 前序关键提交：
    - `a1419ac5c41376e984969fea1eda435acc672a5e`（`feat: 补齐通用项目委派、跨域继承与可搬迁交付`）
    - `2128d6bc` / `86c8b97b` / `3f8edd8c`（AOT 容器与列表切片性能优化系列）
- **版本定义**（[Cargo.toml](../Cargo.toml#L6-L8)）：
  - Workspace 版本：`0.0.1`
  - Rust Edition：`2024`（Resolver: `3`）
  - 最低 Rust 版本（MSRV）：`1.85`

---

## 2. 实际工具链版本（当前开发机轻量探测）

2026-11-16 在当前开发环境执行轻量探测获得的确切工具链版本如下：

| 工具 / 组件 | 当前实测版本 | 说明与路径 |
| :--- | :--- | :--- |
| **操作系统** | macOS 27.2.1 arm64 (Darwin 27.2.1) | Apple M5，BuildVersion `26B131` |
| **Rust 编译器** | `rustc 1.99.0 (b940084d7 2026-09-28)` | 宿主目标 `aarch64-apple-darwin` |
| **Cargo** | `cargo 1.99.0 (5f94df478 2026-08-27)` | 满足 Edition 2024 与 Resolver 3 |
| **.NET SDK** | `11.0.100` | `/usr/local/share/dotnet/sdk/11.0.100` |
| **.NET 运行时** | `Microsoft.NETCore.App 11.0.0`<br>`Microsoft.AspNetCore.App 11.0.0` | 编译目标维持 `net10.0`；通过 `rollForward: Major` 兼容运行 |
| **固定差分 CPython** | `CPython 3.12.13` | 登记于 [tests/pinned-python.txt](../tests/pinned-python.txt)；由 uv 解析至 `/Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12` |
| **FastAPI 验证 CPython** | `CPython 3.13.3` | `/Users/laull/.local/share/uv/python/cpython-3.13-macos-aarch64-none/bin/python3.13` |
| **包与解释器管理** | `uv 0.12.13 (0ebbd9274 2026-09-10)` | `aarch64-apple-darwin` |

---

## 3. 编译器与运行时默认开关与配置

依据 [crates/ryupy-cli/src/args.rs](../crates/ryupy-cli/src/args.rs) 与驱动当前实现，冻结以下默认配置：

### 3.1 CLI 默认意图模型（`ryupy entry.py`）
- **默认模式（Mode）**：`Mode::Onefile`（单一原生可执行文件交付，隐含 `--aot` 与运行时闭包打包）。
- **默认 CPython Provider**：`auto`（自动探测当前 Python 环境，支持正式版本 3.9–3.15 常规 GIL；应用模块编译为 CIL，第三方依赖由随包 CPython 承载）。
- **默认输出根目录**：`./dist`。
- **显式发布替代模式**：
  - `--mode standalone` / `--standalone`：目录发布原生 NativeAOT 应用。
  - `--mode jit`：编译为 .NET JIT DLL 程序集，配合 runtimeconfig.json 向上兼容执行。
- **显式原生执行域**：
  - `--native`：强制纯 RyuPy CIL 执行域，禁用 CPython provider（与 `--cpython-provider` 及委派参数互斥）。
- **显式本地委派开关（禁止静默回退）**：
  - `--cpython-entry`：显式将 `__main__` 入口脚本委派给随包 CPython 解释执行。
  - `--cpython-module <MOD>`（可重复）：显式将指定的本地顶层模块或整包委派给随包 CPython 解释执行。
  - `--cpython-dynamic-module <MOD>`（可重复）：显式声明动态导入的第三方 CPython 依赖（如 `aiosqlite`、`httpx`）。

### 3.2 优化与实验开关（默认全部关闭）
- `--experimental-direct-accumulator`：循环直接累加展开（默认使用 holder）。
- `--experimental-records-increment`：records 键物化守卫。
- `--experimental-leaf-inline`：纯叶调用内联。
- `--diagnostic-scalar-slp` / `--experimental-packed-slp`：SLP 标量对照与 packed SIMD 优化。
- `--instance-module` / `--instance-direct-globals`：模块实例与直接 globals 冷路。
- `--trace-product-v1`：强制有限 trace V1 产物核验。

### 3.3 布局与跨域模型契约
- **AOT 布局与旁件规则**（[ADR-0206](adr/0206-aot-layout-os-sidecars-and-tcltk.md)）：
  - 仅忽略普通文件 `.DS_Store` 与 `Thumbs.db`；
  - 严格拒绝同名符号链接、同名目录、锁文件（`.lock`）与临时文件（`.tmp`）；
  - 移除普通文件 sha256 摘要与全量 `bounded_read`，依文件元数据（大小、mtime、权限）校验。
- **跨域类继承中性桥**（[ADR-0205](adr/0205-foreign-class-inheritance-bridge.md)）：
  - 在加载 CPython Provider 时，通过 `IPyForeignClassBridge` 支持用户编译类继承真实 CPython 类型（如 `tk.Tk`、`UserDict`、`deque`），由 CPython 侧构造真实派生类型与实例句柄；
  - 在纯原生模式（`--native`）下，非普通用户基类继承直接抛出 `NotImplementedError`。

---

## 4. 现有回归合同：项目、API 与性能现有驱动/版本/入口

### 4.1 固定 20 纯 Python 项目（M2.0 基准选集）
- **驱动位置**：
  - 原生门禁驱动：[tests/run_projects_gate.py](../tests/run_projects_gate.py)
  - 编译与运行器：`cargo run -p ryupy-e2e --bin projects`
  - Provider 矩阵驱动：[tests/cpython/projects_runner.py](../tests/cpython/projects_runner.py)
- **事实清单与锁定**：
  - 清单文件：[tests/projects/manifest.tsv](../tests/projects/manifest.tsv)（固定 20 个项目及入口样例）
  - 锁文件：[tests/projects/lock.tsv](../tests/projects/lock.tsv)
  - 保护合同：[tests/projects/protected.json](../tests/projects/protected.json)（Schema 1，锁定保留门与晋升门）

#### 固定 20 项目清单与历史验收基准表
> 注：以下状态源自仓库历史记录（2026-11-13/14 记录），非当前执行生成。

| 序号 | 项目 ID | 分类 | PyPI 包名 | 固定版本 | 顶层导入 | 入口样例 | 传递依赖 | 原生默认 JIT 历史状态 | Provider 三模式历史状态 (macOS arm64) |
| :---: | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :---: | :---: |
| 01 | `six` | 通用库 | six | 1.16.0 | `six` | `entries/six_sample.py` | `-` | unsupported | **PASS** (JIT/AOT/Reloc) |
| 02 | `sortedcontainers` | 数据结构 | sortedcontainers | 2.4.0 | `sortedcontainers` | `entries/sortedcontainers_sample.py` | `-` | **native** | **PASS** (JIT/AOT/Reloc) |
| 03 | `toml` | 配置解析 | toml | 0.10.2 | `toml` | `entries/toml_sample.py` | `-` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 04 | `tomli` | 配置解析 | tomli | 2.0.1 | `tomli` | `entries/tomli_sample.py` | `-` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 05 | `pyparsing` | 解析 | pyparsing | 3.1.2 | `pyparsing` | `entries/pyparsing_sample.py` | `-` | unsupported | **PASS** (JIT/AOT/Reloc) |
| 06 | `attrs` | 数据类 | attrs | 23.2.0 | `attrs` | `entries/attrs_sample.py` | `-` | unsupported | **RUNTIME_FAIL** (缺少活字典 `__dict__`) |
| 07 | `networkx` | 图算法 | networkx | 3.3 | `networkx` | `entries/networkx_sample.py` | `-` | unsupported | **PASS** (JIT/AOT/Reloc) |
| 08 | `python-dateutil` | 日期 | python-dateutil | 2.9.0.post0 | `dateutil` | `entries/dateutil_sample.py` | `six==1.16.0` | unsupported | **PASS** (JIT/AOT/Reloc) |
| 09 | `click` | 命令行工具 | click | 8.1.7 | `click` | `entries/click_sample.py` | `-` | unsupported | **PASS** (JIT/AOT/Reloc) |
| 10 | `docopt` | 命令行工具 | docopt | 0.6.2 | `docopt` | `entries/docopt_sample.py` | `-` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 11 | `pyfiglet` | 命令行工具 | pyfiglet | 1.0.2 | `pyfiglet` | `entries/pyfiglet_sample.py` | `-` | **native** | **PASS** (JIT/AOT/Reloc) |
| 12 | `tabulate` | 文本处理 | tabulate | 0.9.0 | `tabulate` | `entries/tabulate_sample.py` | `-` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 13 | `texttable` | 文本处理 | texttable | 1.7.0 | `texttable` | `entries/texttable_sample.py` | `-` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 14 | `markdown` | 文本处理 | markdown | 3.5.2 | `markdown` | `entries/markdown_sample.py` | `-` | failed (`find_spec` 缺 source) | **PASS** (JIT/AOT/Reloc) |
| 15 | `chardet` | 编码检测 | chardet | 5.2.0 | `chardet` | `entries/chardet_sample.py` | `-` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 16 | `wcwidth` | 终端自动化 | wcwidth | 0.2.13 | `wcwidth` | `entries/wcwidth_sample.py` | `-` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 17 | `colorama` | 终端自动化 | colorama | 0.4.6 | `colorama` | `entries/colorama_sample.py` | `-` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 18 | `termcolor` | 终端自动化 | termcolor | 2.4.0 | `termcolor` | `entries/termcolor_sample.py` | `-` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 19 | `binaryornot` | 文件自动化 | binaryornot | 0.4.4 | `binaryornot` | `entries/binaryornot_sample.py` | `chardet==5.2.0` | **native** (受保护) | **PASS** (JIT/AOT/Reloc) |
| 20 | `progress` | 终端自动化 | progress | 1.6 | `progress` | `entries/progress_sample.py` | `-` | **native** (晋升要求项) | **PASS** (JIT/AOT/Reloc) |

- **历史门禁汇总**：
  - 原生默认 JIT：**13 native / 6 unsupported / 1 failed**（保 11 项全过；progress 已恢复；markdown 失败于 `find_spec` 缺失）。
  - Provider 模式（JIT / NativeAOT / 搬迁）：macOS arm64 记录 **19/20 通过（95%）**，唯一失败项为 `attrs`（失败于 `__main__` 编译入口模块缺少活字典 `__dict__`）。

---

### 4.2 通用真实项目（两套典型应用，2026-11-16 记录）
- **驱动位置**：[tests/general_projects/run.py](../tests/general_projects/run.py)、[tests/general_projects/oracle.py](../tests/general_projects/oracle.py)
- **项目 A：FastAPI 异步 RESTful 服务 (`python-samples-fastapi-restful`)**：
  - 运行依赖：CPython 3.13.3，固定依赖见 [tests/general_projects/requirements.txt](../tests/general_projects/requirements.txt)（包含 FastAPI 0.158.1、SQLAlchemy 2.0.52、aiosqlite 0.22.1、aiocache 0.12.3、greenlet 3.5.6 等）。
  - 当前交付形式：显式入口委派 `--cpython-entry`，本地模块委派 `--cpython-module databases --cpython-module routes` 等，动态依赖声明 `--cpython-dynamic-module aiosqlite --cpython-dynamic-module httpx --cpython-dynamic-module greenlet`。
  - 验收范围：ASGI TestClient 请求、OpenAPI、Pydantic 校验、Depends 注入、异步上下文、SQLite CRUD、缓存。
  - 历史状态：macOS arm64 下 JIT、standalone NativeAOT、onefile NativeAOT 搬迁执行记录通过。
- **项目 B：Tkinter 桌面记账应用 (`SanBudget`)**：
  - 运行依赖：CPython 3.12.13，纯标准库（Tkinter、ttk、sqlite3、csv、datetime）。
  - 当前交付形式：原业务编译路径（`--compiled-app`，原业务类 `ExpenseTracker` 继承 `tk.Tk` 保持走 CIL 编译并跨域回调），或显式源码委派。
  - 验收范围：真实 macOS 窗口、按钮命令回调、表单保存/编辑、Treeview、SQLite 同步、关闭销毁。
  - 历史状态：macOS arm64 下 JIT、standalone NativeAOT、onefile NativeAOT 搬迁执行记录通过。

---

### 4.3 CPython Provider 核心 API 与生命周期回归套件
- **驱动位置**：
  - 协议用例：[tests/cpython/run.py](../tests/cpython/run.py)
  - 宿主生命周期探针：[tests/cpython/lifecycle.py](../tests/cpython/lifecycle.py)
  - Standalone 搬迁与负例：[tests/cpython/standalone_run.py](../tests/cpython/standalone_run.py)
  - 委派与跨域类桥：[tests/cpython/delegation_run.py](../tests/cpython/delegation_run.py)
- **核心用例（21 项手写协议）**：
  - `host`、`protocols`、`identity`、`hash_keys`、`provider_state`、`import_cycles`、`exceptions`、`callbacks`、`container_transfer`、`shutdown`、`stream_sharing`、`exception_laziness`、`exit_none/int/text/tuple`、`exceptions_state`、`exception_handled_state`、`mixed_traceback`、`numpy_arrays`、`numpy_callbacks`、`stdlib_service`。
- **宿主生命周期探针（6 项）**：
  - `steady-state`（跨堆断环与句柄稳态）、`late-release`（关机后释放）、`disposal`（确定释放幂等）、`exception-release`（回调异常句柄稳态）、`numpy-lifetime`（ufunc 确定释放稳态）、`mixed-limit`（双域帧边界）。
- **NumPy 验收门禁**：
  - 固定版本：`NumPy 2.3.5`（CPython 3.12 `cp312-macosx_14_0_arm64` 官方 wheel），真实 ufunc 回调、ndarray 别名与异常往返。

---

### 4.4 性能状态与演进记录（关于当前提交的性能边界说明）
- **驱动位置**：[tests/run_benchmarks.py](../tests/run_benchmarks.py)（搭配 [tests/benchlib/](../tests/benchlib/)）
- **历史文档与数据口径纠正**：
  - [docs/aot-performance-baseline.md](aot-performance-baseline.md) 记录的是早期 Commit `e8c07b85`（2026-11-15 前）优化前数据（当时测得几何平均加速比 0.86x）；**当前提交的性能基线另行采集**。
  - 在 `e8c07b85` 之后，代码库陆续引入了多项性能优化切片：
    - [ADR-0201 / aot-hotspot-dispatch-optimization](aot-hotspot-dispatch-optimization.md)（Commit `767e665c`：tuple 直接比较、集合小参数 PIC、整型比较，使 records 缩短 15.86%、comprehensions 缩短 12.85%、list_ops 缩短 10.14%）；
    - [ADR-0202 / aot-generator-state-optimization](aot-generator-state-optimization.md)（生成器状态驻留优化）；
    - [ADR-0203 / aot-comprehension-region-optimization](aot-comprehension-region-optimization.md)（Commit `5b4eb87c`：推导式标量区、enumerate 解包与预留容量，comprehensions 单项耗时由 96.89ms 降至 71.26ms，减少 26.46%）；
    - 列表容器优化系列提交：`2128d6bc`（尾部浮点插入保留背板）、`86c8b97b`（单索引删除保留紧凑背板）、`3f8edd8c`（数值 pop 仅物化被移除槽）。
- **当前 Commit `5d36db1e` 性能边界确认**：
  - 已从 `5d36db1e` 经 `git archive` 提取源码、独立构建冻结 CLI，并完成全量 26 项 NativeAOT 测量；
  - 原始提交基线与首轮候选 26/26 功能核验均通过；首轮 calls 的 Binder 成本和 attrs 的 super 分配回退已触发修复，最新候选的 26 项六次平衡配对已完成，墙钟几何平均 +0.55%，全部单项与剩余性能核查见[首轮集成验收](cil-execution-first-slices-validation.md)；
  - 后续阶段若需衡量全 CIL 改造带来的性能损耗或收益，必须在清洁独占环境重新运行全量 26 项基准作为对齐输入，保留所有不利样本。

---

## 5. 三平台已有证据与未验证边界

### 5.1 三平台实测与验证证据
| 平台 | 架构与环境 | 历史已有验证证据 | 当前保留未验证边界 |
| :--- | :--- | :--- | :--- |
| **macOS** | `osx-arm64`<br>(macOS 15 / 27.2.1 arm64) | 1. 历史 CI（Run `43585093316`）及本地验收记录；<br>2. NativeAOT standalone/onefile 构建、删除构建输入后的空 PATH 搬迁、中文与空格目录（`独立发布 目录 & 空格路径`）搬迁通过；<br>3. 缺 libpython、缺 shim、缺第三方资源负例全部通过；<br>4. FastAPI 与 SanBudget 两真实项目三模式搬迁通过；<br>5. 固定 20 项目 19/20 通过（attrs 活字典阻断）。 | 1. 外部生产 Uvicorn TCP 实际网络监听与高并发压测（当前仅通过 ASGI TestClient）；<br>2. SanBudget 的系统原生文件选择对话框（filedialog）与 CSV 导出交互；<br>3. 无任何开发工具链（纯裸机环境）的物理机交付验证。 |
| **Linux** | `linux-x64`<br>(Ubuntu 24.04 x64) | 1. 历史 CI（Run `43585093316`、`43610181667` 等）验证通过；<br>2. NativeAOT standalone 发布、删除输入后搬迁通过；<br>3. **无开发环境容器验证**：在无 Python、无 .NET、无 SDK 的 `mcr.microsoft.com/dotnet/runtime-deps:10.0-noble` 容器中只读挂载产品、断网、空 PATH 执行通过；<br>4. 缺 libpython、缺 shim 负例正确拒绝；<br>5. 真实系统闭包 `libcrypt.so.1` 来源与相对 `$ORIGIN` rpath 核验通过；<br>6. Python 3.9–3.15 多版本门禁全部通过。 | 1. 两个通用真实项目（FastAPI 与 SanBudget）在 Linux 平台上的实测搬迁；<br>2. Linux X11 / Wayland 下真实 Tk 桌面窗口与平台事件循环；<br>3. Linux 平台上的固定 20 项目全量 NativeAOT 矩阵；<br>4. 真实生产环境中的高并发与外部网络请求。 |
| **Windows** | `win-x64`<br>(Windows Server 2022 x64) | 1. 历史 CI（Run `43585093316`、`43610181631` 等）验证通过；<br>2. NativeAOT standalone 发布、删除输入后搬迁（中文与空格目录）通过；<br>3. PE 导入表与安全 DLL 搜索机制（`SetDefaultDllDirectories` + `AddDllDirectory` + `LoadLibraryExW` 0x1100）通过；<br>4. 随包 `python312.dll` 与 `VCRUNTIME140.dll` / `MSVCP140.dll` 闭包核验通过；<br>5. 缺组件负例正确拒绝。 | 1. 两个通用真实项目（FastAPI 与 SanBudget）在 Windows 平台上的实测搬迁；<br>2. Windows 下 Tkinter 与 Tcl 8.6 资源目录搜索与真实桌面窗口渲染；<br>3. Windows 平台上的固定 20 项目全量 NativeAOT 矩阵；<br>4. Windows 平台无开发环境纯裸机验证。 |
| **通用跨平台** | 全平台共通 | 1. 平台原生架构核验（Mach-O arm64、ELF64、PE32+）；<br>2. UTF-8 隔离初始化（`PyPreConfig_InitIsolatedConfig` 设置 `utf8_mode = 1`）。 | 1. 真实多线程下 CPython GIL 重入与托管线程池交叉并发现象；<br>2. 真实 HTTPS 客户端系统根证书链校验。 |

### 5.2 主代理当前测试状态说明
- 主代理在通用项目验收中记录了当时完整 `make check` 通过（3349 项 C# 测试通过、3 项既有跳过；编译 e2e 482 项通过、291 项 ignored；cases-selftest 769 项通过）。
- 本轮冻结源码的最终 `make check` 已再次通过：3368 项 C# 测试通过、3 项既有跳过；487 项编译 e2e 通过、292 项既有 ignored；fmt、clippy、Python 合同、C# 与 Rust 测试通过。后续切片独立重跑。

---

## 6. 可复现执行命令清单

所有运行产物均须置于仓库外部临时目录（如 `/tmp/ryupy-*`），测试完成后自动清理；遵循 ADR-0182 免哈希约定。

### 6.1 工具链与环境轻量探测
```bash
# 检查工具链
rustc --version
cargo --version
dotnet --version
uv --version
python3 -B -c "import sys; print(sys.version)"

# 检查固定解释器
uv python find 3.12.13
```

### 6.2 基础构建与代码规范门禁
```bash
make runtime            # 构建 C# 运行时 RyuPy.Runtime
make fmt                # 格式化 Rust 源码
make lint               # fmt 检查 + clippy -D warnings
make python-baseline    # 自检 CPython 用例清单、项目门禁与 baseline
make cases-selftest     # 用例与测试框架自检（不编译 RyuPy）
make e2e                # 运行端到端差分测试（跳过 pending）
```

### 6.3 固定 20 项目门禁复现
```bash
# 原生 JIT 保留门（保 11 项，需预先准备的源码目录）
make projects-preserve PROJECTS_STORE=/absolute/path/to/prepared-store

# 原生 JIT 默认晋升门（要求 progress 恢复）
make projects-promote PROJECTS_STORE=/absolute/path/to/prepared-store

# 直接调用 run_projects_gate.py 驱动（补齐必填 --gate 与 --runner）
python3 -B tests/run_projects_gate.py \
  --gate preserve \
  --runner target/debug/projects \
  --source-store /absolute/path/to/prepared-store

# Provider 模式全量 20 项目（依 argparse 支持 jit / aot / all）
python3 -B tests/cpython/projects_runner.py --mode jit --output-json /tmp/ryupy_jit_results.json
python3 -B tests/cpython/projects_runner.py --mode aot --project six
python3 -B tests/cpython/projects_runner.py --mode all --output-json /tmp/ryupy_all_results.json
```

### 6.4 通用真实项目（FastAPI / SanBudget）交付复现
```bash
# 准备外部环境（仅首次）
uv venv --python 3.13.3 /tmp/ryupy-general-venv
uv pip install --python /tmp/ryupy-general-venv/bin/python -r tests/general_projects/requirements.txt

# 运行 CPython Oracle 差分基准
python3 -B tests/general_projects/oracle.py \
  --fastapi-python /tmp/ryupy-general-venv/bin/python \
  --tk-python /Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12

# 运行 RyuPy 构建、删除输入搬迁与回归
python3 -B tests/general_projects/run.py --ryupy /absolute/path/to/ryupy \
  --python /tmp/ryupy-general-venv/bin/python --project fastapi
python3 -B tests/general_projects/run.py --ryupy /absolute/path/to/ryupy \
  --python /Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12 --project sanbudget --compiled-app
```

### 6.5 性能基准采样（同提交实测准备）
```bash
# 运行 26 项 NativeAOT 基准（纯净直接脚本 1warm5 测量）
python3 -B tests/run_benchmarks.py --detail --aot --save baseline-current

# 快速基准粗测（1 次热身 + 3 次计时）
python3 -B tests/run_benchmarks.py --quick
```

### 6.6 CPython Provider 核心协议、生命周期与 Standalone 复现
```bash
# 21 项核心协议差分（指定 Python 解释器）
python3 -B tests/cpython/run.py --python /Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12

# 6 项宿主生命周期探针（必须指定包含 ryupy-cpython.json 的已构建 JIT 分发目录）
python3 -B tests/cpython/lifecycle.py --dist /absolute/path/to/ryupy-cpython-jit-dist

# Standalone 真实构建、删除输入搬迁与负例（必须指定 Python 与发行根）
python3 -B tests/cpython/standalone_run.py \
  --python /Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none/bin/python3.12 \
  --distribution /Users/laull/.local/share/uv/python/cpython-3.12.13-macos-aarch64-none \
  --metadata /absolute/path/to/metadata-with-PYTHON.json-and-licenses \
  --ryupy /absolute/path/to/ryupy
```

---

## 7. 后续阶段准入矩阵（C1–C5 准入与退出门）

依据 [ADR-0208](adr/0208-cil-execution-libpython-kernel.md) 与 [实施路线](cil-execution-roadmap.md) 第 3 节及第 5 节，严格定义 C0 完成门与后续各阶段准入退出条件：

| 阶段 | 核心目标与交付物 | 准入前置条件 | 核心验收指标（交付门） | 回归保护要求（防回退门） | 退出与晋升条件 |
| :---: | :--- | :--- | :--- | :--- | :--- |
| **C0** | **执行归属盘点、启动链方案与基线冻结** | `feat/adr0208-cil-execution` 分支建立，ADR-0208 方向确定 | 1. **执行归属清单（Inventory）**：登记所有源码/字节码执行入口、嵌入 Python 字符串、导入钩子与反向回调（覆盖 `rp_run_entry`、`rp_install_imports`、`_rp_make_class`、`_rp_wrap_method` 以及间接求值）；<br>2. **启动链盘点与方案**：核对 libpython 初始化/关闭的 frozen importlib 与编码依赖，完成最小内核方案选型；<br>3. **回归基线冻结**：交付工具链、默认开关、现有项目/API/性能基线与三平台边界文档（本文档）。 | 不修改产品代码；不运行长耗时或破坏性测试；不更改已有未跟踪测试 fixture。 | 交付可追踪调用链、执行归属清单、底层 ABI 消费者及阶段验收方案，选定严格产品启动方案。 |
| **C1** | **移除内部 Python 胶水** | C0 门禁全部闭环，启动方案选定 | 1. CIL 实现模块导入与初始化状态机、类构建及元类选择、函数绑定与异常转换；<br>2. 静态 C API / 原生类型槽替代 Python wrapper 函数；<br>3. 消除内部调用链中的 Python 字符串与动态求值。 | 1. SanBudget 跨域继承编译路径保持通过；<br>2. 固定 20 项目既有 19/20 JIT/AOT 不回退；<br>3. 21 项核心 API 与 6 项生命周期探针通过；<br>4. 完整 `make check` 绿。 | 内部导入、类构建和回调链无 Python 胶水执行；外部纯 Python 库阶段性保留在旧兼容域。 |
| **C2** | **函数元数据、动态对象与反射** | C1 门禁全部闭环 | 1. 编译器生成函数签名、注解、默认值、限定名与闭包元数据；<br>2. Runtime 实现 `inspect.signature`、装饰器解包与元类钩子；<br>3. 修复 `attrs` 的 Live Dict `__dict__` 缺口；<br>4. 解除随包 `.py` 源码锚点，默认产品查询原文抛出明确异常。 | 1. FastAPI Depends 注入与 Pydantic 模型通过纯元数据通过；<br>2. attrs 恢复为 PASS；<br>3. JIT 与 NativeAOT 元数据一致；<br>4. 不伪造 CPython 的 `co_code`。 | 反射/对象协议同次 CPython 差分通过；固定 20 项目达到 20/20 通过。 |
| **C3** | **原生协程状态机与异步运行时** | C2 门禁全部闭环 | 1. 中端 MIR 实现显式 coroutine/awaitable 状态机；<br>2. 依次支持 `await`、`async with`、`async for` 与异步生成器；<br>3. .NET Task 承接底层等待，RyuPy Runtime 维护 Python 调度、取消与上下文。 | 1. FastAPI 移除 `--cpython-entry` 与 `--cpython-module` 委派，直接编译为 CIL 执行并通过 ASGI TestClient；<br>2. 真实 HTTP 请求、并发任务取消与数据库连接释放通过；<br>3. JIT 与 NativeAOT 搬迁通过。 | 现代异步 Web 栈具备原生 CIL 执行能力，消除对 Python 异步解释器依赖。 |
| **C4** | **完整 Python 依赖闭包编译，libpython 底层化** | C3 门禁全部闭环 | 1. 应用、纯 Python 标准库及第三方包装逐族编译为 CIL；<br>2. SanBudget 完整标准库（含 tkinter/sqlite3 Python 层）编译为 CIL；<br>3. 依据 C0 启动链收齐 libpython 初始化，移除 frozen Python 执行；<br>4. 产品中零 `.py/.pyc` 源码与字节码载荷。 | 1. 固定 20 项目与两通用真实应用全部达到全 CIL 原生执行；<br>2. 进程启动/运行/关闭期间 CPython 字节码执行计数为零；<br>3. NumPy 原生核心与 Python 包装层分别验收通过。 | 达到 ADR-0208 终态定义：严格产品零 Python 源码胶水、零 CPython 字节码执行。 |
| **C5** | **默认晋升与旧执行路径退出** | C0–C4 门禁全部闭环并通过三平台验证 | 1. 提供显式候选路径切换；<br>2. 默认 CLI 切换为全 CIL 执行；<br>3. 删除源码委派、旧 Python 胶水及过渡载荷；<br>4. 发布说明与最终文档更新。 | 1. 默认项目零新增失败；<br>2. 三平台（macOS arm64 / Linux x64 / Windows x64）同提交验证通过；<br>3. 性能基准无非预期回退；<br>4. 默认切换与代码删除各用独立可回退提交。 | 完成全 CIL 执行路线终态交付，旧兼容过渡路径完全退出。 |
