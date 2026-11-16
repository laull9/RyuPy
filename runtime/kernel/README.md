# RyuPy 严格零字节码内核原型（`runtime/kernel/`）

本目录承接 [ADR-0208](../../docs/adr/0208-cil-execution-libpython-kernel.md) 与 [C0 盘点 §8](../../docs/cil-execution-inventory.md) 选定的 **C4 前置最小可重建 `libpython` 内核原型**，完全独立于现有 `RyuPy.CPython` / `driver` / `RyuPy.Runtime` 路径之外。

---

## 1. 支持矩阵严格分列（严禁混淆）

本目录首切固定 3.12.13，作为显式独立的 C4 前置内核原型。默认 auto provider 继续支持 Python 3.9–3.15；完整 Python 闭包、CIL 导入及产品集成由后续 C4 切片验收。

| 维度 | 严格零字节码内核原型（本目录） | RyuPy 既有产品兼容路径（`auto` provider） |
| :--- | :--- | :--- |
| **Python 版本范围** | **首切固定 `CPython 3.12.13`（常规 GIL）** | **保持 `Python 3.9–3.15`（常规 GIL）不变** |
| **构建来源** | 官方主源 `python.org` 源码实时获取 + 锚点变换构建（不以 PBS 代替） | `python-build-standalone`（`auto` 探测及已冻结分发链） |
| **多版本策略** | 后续扩展多版本须做**显式参数化配置与独立锚点集**；锚点或版本失配**直接硬失败** | 既有兼容性与回归合同继续保留，本目录不改动其任何代码 |
| **宿主平台状态** | **macOS arm64**：源码构建与四项 C 探针通过。Linux、macOS x64 构建路径待实测；Windows MSVC/DLL 待实现 | 保持既有三平台验收矩阵 |

---

## 2. 首切交付目标与已实现能力

1. **可复现官方源码获取与精确结构变换**
   - [build_kernel.py](build_kernel.py) 从官方主源 `https://www.python.org/ftp/python/3.12.13/Python-3.12.13.tar.xz` 获取源码，随后以版本与精确结构锚点确认支持范围。
   - [transform.py](transform.py)（联合 [anchors_eval_import.py](anchors_eval_import.py) 与 [anchors_frozen_build.py](anchors_frozen_build.py)）不使用 `.patch` / `.diff` 文件，而是对 `Include/patchlevel.h`、`Python/ceval.c`、`Python/import.c`、`Python/frozen.c`、`Modules/getpath.c`、`Makefile.pre.in` 执行**精确原段唯一匹配（`count == 1`）**的锚点变换；任何版本或上下文失配在写入磁盘前直接硬失败。
   - 所有上游源码、构建、日志与探针产物均位于仓库外临时目录，默认结束后自动清理；仅显式传入 `--keep` 时保留。绝不读取或修改用户缓存（`~/.cache/ryupy`、`uv` 缓存等）或系统 Python。

2. **三阶段默认求值器入口计数与严格拒绝执行开关**
   - 在 `Python/ceval.c` 的 `_PyEval_EvalFrameDefault` 入口处注入原子三阶段计数器（`0=startup`、`1=runtime`、`2=shutdown`）与 `strict_reject` 开关，并导出头文件 `Include/ryupy_kernel.h`：
     - `rp_kernel_version()` → `"ryupy-cpython-3.12.13-kernel-proto-v1"`
     - `rp_kernel_set_phase(int phase)` / `rp_kernel_get_phase()`
     - `rp_kernel_set_strict_reject(int enabled)` / `rp_kernel_get_strict_reject()`
     - `rp_kernel_reset_eval_stats()`
     - `rp_kernel_eval_entry_count(int phase)` / `rp_kernel_eval_executed_count(int phase)` / `rp_kernel_eval_rejected_count(int phase)`
     - `rp_kernel_eval_total_entries()` / `rp_kernel_eval_total_rejected()`
   - 开启 `strict_reject=1` 后，任何进入 `_PyEval_EvalFrameDefault` 的调用会在执行任何字节码之前记录拒绝计数、设置 `RuntimeError("CPython default bytecode evaluator is disabled in strict RyuPy kernel mode")` 并安全退栈返回 `NULL`。

3. **无 Frozen 载荷与 `_install_importlib=0` 启动 / 关闭 / 内建 `math` 运算**
   - `Python/frozen.c`、`Modules/getpath.c` 与 `Makefile.pre.in` 清空全部 frozen 与 deepfreeze 字节码载荷（保留 ABI 符号空哨兵），且构建时通过 `Modules/Setup.local` 将 `math` 静态编入 `PyImport_Inittab`。
   - `Python/import.c` 在 `IMPORTLIB(interp) == NULL`（即 `_install_importlib=0`）时启用 C 内建模块路由 `rp_kernel_import_builtin_only`：覆盖 `PyImport_ImportModule` 与 `PyImport_ImportModuleLevelObject`，支持单阶段及多阶段初始化（如 `math`）。

4. **手写 C 探针（[probe/kernel_probe.c](probe/kernel_probe.c)）**
   - 直接链接源码构建出的 `libpython3.12` 验证四项场景：
     - `strict-clean`：`_install_importlib=0` + `strict_reject=1` 启动、运行 `math.sqrt` / `factorial` / `fabs` / `sin` / `gcd`、拒绝未支持消费者并关闭，三阶段计数全为 `(0, 0, 0)`；
     - `strict-reject`：尝试 `PyEval_EvalCode` 被硬拒绝并记录 `entry=1, rejected=1, executed=0`；
     - `phase-counters`：关闭 `strict_reject=0` 验证三阶段真实计数 `(1, 2, 1)`；
     - `reject-importlib`：强制尝试 `_install_importlib=1` 必须明确失败（不可回落 frozen 字节码）。

---

## 3. 保留为后续切片的边界与显式拒绝策略

以下能力明确超出本首切边界，当前**不做静默兼容，而是显式拒绝**：

1. **完整 C 导入路由（`.so`/`.pyd` 扩展加载、包 `fromlist`、相对导入、回调 CIL `PyModules.Import`）**：
   - 当前 `_install_importlib=0` 仅放行已在 `sys.modules` 或 `PyImport_Inittab` 中的内建 C 模块（如 `math`）。
   - 对非内建模块（`json`、`encodings`、磁盘 `.py`/`.pyc`、未接入的动态扩展）统一抛出 `ModuleNotFoundError`；对 `PyImport_ReloadModule` 或带 `__path__` 的 `fromlist` 抛出 `NotImplementedError`；彻底消除原生的 `SystemError: null argument to internal routine`，绝不静默执行字节码。
2. **进程 UTF-8 C Stdio 桥接（`sys.stdin` / `sys.stdout`）**：
   - 当前仅保留 C `stdprinter` 的初步 `sys.stderr`，`sys.stdin` / `sys.stdout` 留待后续切片接入；调用 `builtins.print` 等依赖 `sys.stdout` 的消费者将显式抛出 `RuntimeError: lost sys.stdout`。
3. **Windows 宿主构建与 NativeAOT/RyuPy.CPython 集成**：
   - Windows MSVC/DLL 构建路径当前明确为待验；与 `runtime/RyuPy.CPython` 及 `crates/ryupy-driver` 的集成由主代理统筹，本目录不改动任何现有驱动或桥代码。

---

## 4. 使用方法

```bash
# 运行锚点与路径安全单元测试（轻量，不编译 CPython）
python3 -B runtime/kernel/test_transform.py

# 仅验证官方 3.12.13 源码获取与锚点变换（不运行 configure/make）
python3 -B runtime/kernel/build_kernel.py --transform-only

# 在 Linux/macOS 上执行完整源码构建 + 符号核验 + 4 项 C 探针（默认自动清理外部临时目录）
python3 -B runtime/kernel/build_kernel.py

# 如需保留仓库外构建目录与产物供进一步检查，显式传入 --keep
python3 -B runtime/kernel/build_kernel.py --keep
```

## 4. 当前实测

2026-11-16，macOS arm64：由官方 3.12.13 源码完整构建动态库，四项探针通过。
`strict-clean` 启动/运行/关闭入口与执行次数均为 `(0,0,0)`，真实内建 math 运算通过；
`strict-reject` 的运行阶段记录一次拒绝、零执行；`phase-counters` 记录 `(1,2,1)`；
启用 importlib 的负例明确初始化失败。交付前缀保留 Python 许可，包含动态库与头文件，
无 `.py/.pyc`。所有构建日志与源码树位于仓库外。

首轮实编发现并修复空 deepfreeze stub 缺少上游 `_Py_next_func_version` 状态，
以及 C ModuleSpec 属性字典构造的格式串错误；修订后的完整源码重建与探针均已重跑。
