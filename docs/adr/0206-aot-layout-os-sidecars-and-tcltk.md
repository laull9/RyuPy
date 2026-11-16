# ADR-0206：AOT 布局精确 OS 旁件过滤、无摘要元数据与 Tcl/Tk 可搬迁隔离

- 日期：2026-11-16
- 状态：已实现（完整 `make check`、macOS arm64 真实 Tkinter 编译产品三模式搬迁验收通过）

## 背景

在推进通用 Python 项目兼容性路线 C（现有原生 CIL / CPython provider 分发基础设施加固）时，存在三处明确缺陷：

1. macOS Finder 与 Windows Explorer 在输出目录、发布树或单文件目录生成的 `.DS_Store` / `Thumbs.db` 旁件会导致 `aot_layout_files`、`manifest_files`、`onefile_archive`、`onefile_publication`、`cpython_deployment` 与 `resources_paths` 的严格一致性核验中断。
2. `aot_layout_files.rs` 的 `Node` 仍保留历史遗留的 `sha256` 字段，并在每次目录快照时调用 `bounded_read` 逐字节读取全部构建历史与产物内容再返回空串，违反项目关于普通文件不做 SHA256/MD5 校验的约定且带来无意义 I/O 开销。
3. Tkinter 可搬迁资源与宿主隔离存在两处缺口：
   - Windows `PlatformKind::WindowsX64::copy_directories()` 遗漏 `"tcl"` 子目录，导致依赖 `python/tcl/tcl8.6` / `tk8.6` 的 Windows CPython 发行在部署时丢失脚本库；
   - `CPythonHost` 使用 `PyConfig_InitIsolatedConfig` 仅隔离 `PYTHON*` 环境变量，未隔离宿主机残留或错版本的 `TCL_LIBRARY` / `TK_LIBRARY`，且 .NET 的 `Environment.SetEnvironmentVariable` 在 Unix 上不修改原生 `environ`、在 Windows 上不更新 UCRT `_wenviron`，导致随包 `libtcl` / `_tkinter` 仍读取外部路径而失败。

## 决定

### 1. 精确 OS 旁件过滤与链接/锁/临时文件拒绝保留

- 在 `manifest_files::is_os_sidecar(path, metadata)` 中**仅**匹配普通文件（`metadata.is_file()`）且精确文件名为 `.DS_Store` 或 `Thumbs.db` 的条目。
- 所有消费目录（`aot_layout_files`、`manifest_files`、`onefile_archive`、`onefile_publication`、`resources_paths`、`cpython_deployment`）必须先执行 `plain_metadata` / `symlink_metadata` 校验再判断 `is_os_sidecar`。
- 同名符号链接、Windows reparse point、同名目录、`.DS_Store.bak` / `Thumbs.db.tmp`、`.lock` 锁文件及任何 `.tmp` 临时文件一律**不忽略**，继续按原有严格规则拒绝或登记。

### 2. 移除 `aot_layout_files` 的 `sha256` 与 `bounded_read` 摘要，保留元数据与旧 owner 兼容

- 移除 `aot_layout_files::Node` 的 `sha256` 字段与整树 `bounded_read` 逐字节读取过程；`Node` 保留 `path`、`directory`、`size`、`modified`（文件 mtime 纳秒时间戳）和 `mode`。
- `copy_tree` 继续使用有界拷贝、源句柄身份（`same_file::Handle`）与 mtime 校验，并在复制完成后同步保留文件 `modified` 与权限，再对源/目标做元数据快照对比；`read_json` 改为单次有界句柄读取。
- `aot_layout::load_owner` 保持严格校验（拒绝重复字段与未知字段），同时通过 `LegacyOwner` / `LegacyNode` 兼容旧版含 `sha256`、无 `modified` 的 `ryupy-build-owner.json`：当且仅当现存 `.build`/`.dist` 与旧版记录的 `path`、`directory`、`size`、`mode` 完全一致时迁移并写出新版 owner；若存在篡改或格式不兼容则给出明确诊断并拒绝覆盖。
- 本次不修改 `manifest.rs`、`distribution_metadata_files.rs`、`resources.rs` 等任务范围外的历史结构字段。

### 3. Tkinter Tcl/Tk 分发覆盖与宿主环境隔离恢复

- `crates/ryupy-driver/src/cpython_platform.rs` 将 `"tcl"` 加入 `PlatformKind::WindowsX64::copy_directories()`，部署阶段按存在性安全复制。
- 在 `runtime/RyuPy.CPython/native/ryupy_cpython.c` 新增导出 `rp_set_env(name, value)`（POSIX 使用 `setenv`/`unsetenv`，Windows 使用 UTF-8 转 UTF-16 后调用 `_wputenv_s`），并在 `runtime/RyuPy.CPython/Native.cs` 添加静态 `DllImport` 声明（不引入 `GetExport + delegate*` 动态签名或 `ucrtbase` 回退树）。
- 在独立的 `runtime/RyuPy.CPython/CPythonTclTk.cs` 实现 `TclTkEnvironment`：启动前扫描 `home` 下 `lib` / `tcl` / `Lib` 中的 `tcl*`（含 `init.tcl`）和 `tk*`（含 `tk.tcl`）目录并校验无符号链接越界；若存在则写入 `TCL_LIBRARY` / `TK_LIBRARY`，若缺失则显式清除，防止外部环境污染。记录启动前原始值，在初始化失败或 `CPythonHost.Dispose()` 关闭时尽力恢复。
- 说明：产品构建始终同步发布新版 shim；若外部手动指定不含 `rp_set_env` 的旧版 shim，宿主将显式抛出带诊断的 `InvalidOperationException`。

## 验收与限制

- Rust 单元与布局/归档/部署测试覆盖：`.DS_Store` / `Thumbs.db` 忽略、不复制进 `.dist`、同名 symlink 与目录拒绝、`.lock`/`.tmp` 拒绝、同长度改写 mtime 拒绝、无 `sha256` 字段与旧版 owner 迁移/篡改拒绝、Windows `copy_directories` 含 `"tcl"`。
- 宿主 Tcl/Tk 隔离测试覆盖：受污染 `TCL_LIBRARY` / `TK_LIBRARY` 下 CPython 3.12/3.13（Tcl 9.0 / 8.6）搬迁至中文/空格/`&` 路径后的 `tkinter.Tcl()` / `tkinter.Tk()` 初始化与无 Tcl/Tk 时环境变量清除。
- 主代理合流后已完成完整 `make check`，并用原 SanBudget 编译类验证 JIT、standalone NativeAOT、onefile 的真实 macOS 窗口、按钮、SQLite 与删除输入搬迁。Windows/Linux 实机 Tcl/Tk GUI 留待对应平台验收。
