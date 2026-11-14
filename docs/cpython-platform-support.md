# CPython Provider 多平台支持与独立交付规范

本文档定义 RyuPy 随包 CPython provider 在 macOS arm64、Linux x64 与 Windows x64 平台上的布局、ABI 契约、编译构建规则、依赖封闭性以及 CI 验收要求。

## 1. 平台目标与发行锁定

CPython provider 沿固定 CPython 3.12.13、python-build-standalone (PBS) `20260623` 发行构建。不随意扩大 minor 版本支持，不引入运行时联网或搜索开发机系统 Python。

| 平台 | RID | Target Triple | 核心库相对路径 | 上游归档 URL | Build Options | 证书策略默认路径 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| macOS arm64 | `osx-arm64` | `aarch64-apple-darwin` | `python/lib/libpython3.12.dylib` | [macOS 资产](https://github.com/astral-sh/python-build-standalone/releases/download/20260623/cpython-3.12.13%2B20260623-aarch64-apple-darwin-install_only.tar.gz) | `pgo+lto` | `/private/etc/ssl/cert.pem` |
| Linux x64 | `linux-x64` | `x86_64-unknown-linux-gnu` | `python/lib/libpython3.12.so.1.0` | [Linux 资产](https://github.com/astral-sh/python-build-standalone/releases/download/20260623/cpython-3.12.13%2B20260623-x86_64-unknown-linux-gnu-install_only.tar.gz) | `pgo+lto` | `/etc/ssl/certs/ca-certificates.crt` |
| Windows x64 | `win-x64` | `x86_64-pc-windows-msvc` | `python/python312.dll` | [Windows 资产](https://github.com/astral-sh/python-build-standalone/releases/download/20260623/cpython-3.12.13%2B20260623-x86_64-pc-windows-msvc-install_only.tar.gz) | `pgo` | Windows 系统证书存储 (CAFile 为 null) |

> [!NOTE]
> Windows MSVC 发行上游元数据中 `build_options` 为 `pgo`（无 LTO）；POSIX 发行上游元数据中为 `pgo+lto`。driver 平台抽象已参数化验证该项。经上游资产实际核对，Windows 官方归档文件名不包含 `-shared` 字样，已在 driver 与文档中统一修复。

## 2. 目录布局与标准库搜索路径

各平台在分发包内的目录组织遵循上游发行原生布局：

### 2.1 POSIX (macOS & Linux)
- 核心库：`python/lib/libpython3.12.{dylib,so.1.0}`
- 标准库源码与字节码：`python/lib/python3.12`
- 动态加载扩展：`python/lib/python3.12/lib-dynload`
- 随包第三方包：`python/lib/python3.12/site-packages`
- 运行时搜索路径列表：`["python/lib/python3.12", "python/lib/python3.12/lib-dynload", "python/lib/python3.12/site-packages"]`
- 扩展后缀：macOS 为 `.cpython-312-darwin.so`、`.abi3.so`、`.dylib`；Linux 为 `.cpython-312-x86_64-linux-gnu.so`、`.abi3.so`、`.so` 及版本化 `.so.N`

### 2.2 Windows
- 核心库：`python/python312.dll`（位于 python 根目录）
- 导入库：`python/libs/python312.lib`
- 标准库源码：`python/Lib`
- 动态扩展（.pyd）：`python/DLLs`
- 随包第三方包：`python/Lib/site-packages`
- 运行时搜索路径列表：`["python/Lib", "python/DLLs", "python/Lib/site-packages"]`
- 扩展后缀：`.cp312-win_amd64.pyd`、`.pyd`、`.dll`

## 3. 宿主加载与 UTF-8 隔离初始化合同

### 3.1 强制 UTF-8 Mode（PyPreConfig）
嵌入解释器在 POSIX / Windows 环境下默认 locale 可能退化为 ASCII 或 ANSI，导致无显式 `encoding` 参数的 `open()` / `Path.read_text()` / `Path.write_text()` 抛出 `UnicodeEncodeError`。
- C shim `ryupy_cpython.c` 在 `Py_InitializeFromConfig` 前必须调用 `PyPreConfig_InitIsolatedConfig`，显式设置 `preconfig.utf8_mode = 1` 并调用 `Py_PreInitialize`。
- 保证系统默认文件读写编码强制为 UTF-8，所有路径解析使用 UTF-8，不靠测试传递显式 `encoding` 掩盖问题。

### 3.2 动态库加载与 DLL 搜索路径
- **macOS**：通过 `dlopen(path, RTLD_NOW | RTLD_GLOBAL)` 载入 libpython，全局导出 `Py*` 符号供扩展解析。
- **Linux**：优先尝试 `libdl.so.2` 的 `dlopen`，并在 glibc 2.34+ 环境 fallback 到 `libc.so.6`。
- **Windows**：宿主彻底移除易受全局状态污染的 `SetDllDirectoryW`，改用现代安全的 DLL 搜索目录 API 组合：
  - 调用 `SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS)` 启用安全受控搜索；
  - 调用 `AddDllDirectory` 将随包分发的 `python` 根目录、`python/DLLs` 及配置冻结的第三方附属 DLL 目录加入进程 DLL 搜索白名单；
  - 加载库时显式传入 `LoadLibraryExW(..., LOAD_LIBRARY_SEARCH_DEFAULT_DIRS | LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR)`（标志位 0x1100），确保随包 `python312.dll` 及依赖扩展被无歧义解析，杜绝 DLL 劫持及外部系统开发环境泄漏。

## 4. 原生依赖闭包与重定位

### 4.1 机器架构核验（preflight）
严密核查二进制 64 位机器字长与指令集魔数，绝不允许仅通过删除 RID 分支假装支持：
- macOS: Mach-O 64 位（ARM64 / x86_64），并通过 `lipo` 二次验证。
- Linux: ELF64, Little Endian, Machine `0x3E` (EM_X86_64)。
- Windows: PE32+, Machine `0x8664` (IMAGE_FILE_MACHINE_AMD64)。

### 4.2 依赖审计与系统白名单
- **Linux**：
  - 严禁使用泛路径（如 `/lib/` 或 `/usr/lib/`）作为白名单，避免非系统第三方库逃逸审计。
  - 仅放行标准基础系统库：`ld-linux-x86-64.so.2`、`libc.so.6`、`libm.so.6`、`libpthread.so.0`、`libdl.so.2`、`librt.so.1`、`libresolv.so.2`、`libutil.so.1`、`libgcc_s.so.1`、`libstdc++.so.6`。
  - 所有非系统依赖必须在分发目录内唯一匹配，并通过 `patchelf` 将 rpath 设为 `$ORIGIN` 及指向所有被依赖目录的相对路径（如 `$ORIGIN/../..`）。
  - PBS 稳定 ABI 转发库的 `$ORIGIN` 依赖先验证实际目标位于产品根内，再重写为随包库名。缺少的 `libcrypt.so.1` 仅从唯一 Debian/Ubuntu amd64 系统包冻结，随包保存包版本、来源与许可证；其他缺库须显式提供。见 [ADR-0187](adr/0187-linux-libcrypt-closure.md)。
- **Windows**：
  - 严密解析 PE 的普通导入表（DataDirectory[1]）以及延迟导入表（Delay Import Table, DataDirectory[13]）。
  - 仅放行 Windows OS 核心库（`KERNEL32.dll`、`USER32.dll`、`WS2_32.dll`、`ADVAPI32.dll`、`api-ms-win-*` 等）或随包分发的 DLL。
  - VC运行时（`VCRUNTIME140.dll`、`MSVCP140.dll`）须随包，缺失时构建期拒绝；`ucrtbase.dll`沿Windows操作系统库处理。

## 5. 本机验证与 CI 门禁划分

### 5.1 验证项目与测试矩阵
CI 工作流 `.github/workflows/cpython-standalone.yml` 包含三个真实平台矩阵（`ubuntu-24.04`、`windows-2022`、`macos-15`），涵盖以下真实步骤：
1. **Rust 平台与 ABI 单元测试**：`cargo test -p ryupy-driver --lib -- cpython`（20 项通过）。
2. **Platform 契约测试**：`tests/cpython/test_cpython_platform.py`（3 项通过，已移除仅测试 Python 宿主本身的假 UTF-8 测试）。
3. **真实 NativeAOT 构建与搬迁验证**：`tests/cpython/standalone_run.py`：
   - 依赖固定 CPython 3.12.13 / PBS 20260623 官方归档，自动补齐官方 `PYTHON.json` 与完整 `licenses/`。
   - 建立隔离虚拟环境并安装构建工具 `packaging==25.0`，打包并安装专用手写 fixture wheel `standalone_pkg`。
   - 运行手写应用捕获 CPython 3.12.13 oracle 基准，覆盖标准库 sqlite3、ssl、默认 UTF-8 文件 IO、第三方包调用及 metadata/资源读取。
   - 执行 `ryupy --cpython-provider <decl> build <app> --out-dir <out> --aot` 真正完成 NativeAOT 构建与自动依赖打包。
   - 产物迁移至中文与空格路径（`独立发布 目录 & 空格路径`）。
   - 物理删除所有原始构建输入（基础发行、venv、项目源码、wheel），隔离开发机环境。
   - 在清洁 PATH 与受污染 `PYTHONHOME`/`PYTHONPATH` 下直接运行 NativeAOT 可执行文件，严格比较 stdout、stderr、退出码及文件副作用。
   - 逐项验证缺失 libpython、缺失 shim 共享库、缺失第三方包资源的运行时拒绝负例。
   - 临时工作区均位于仓库外并在完成后自动清理。

### 5.2 平台实测状态

2026-11-14，候选 `77605121` 的 [三平台运行](https://github.com/laull9/RyuPy/actions/runs/43585093316)：

| 平台 | NativeAOT 发布、删除输入后空 PATH 搬迁 | 缺 libpython / shim / 资源负例 | 独立目标 |
| --- | --- | --- | --- |
| macOS 15 arm64 | 通过 | 通过 | CI runner 中文与空格目录 |
| Ubuntu 24.04 x64 | 通过 | 通过 | CI runner ＋ 无 Python/.NET/SDK 容器均通过 |
| Windows 2022 x64 | 通过 | 通过 | CI runner 中文与空格目录 |

Linux 干净目标选择 Ubuntu 24.04 系的 `mcr.microsoft.com/dotnet/runtime-deps:10.0-noble`，
保留 ICU、tzdata 和 stdc++ 等操作系统原生依赖，沿既有 globalization 语义执行。镜像用途与
组成见[微软原生依赖镜像说明](https://github.com/dotnet/dotnet-docker/blob/main/README.runtime-deps.md)。
驱动先实际断言镜像中 python/python3/dotnet 命令及 .NET 安装目录均不存在，再以只读产品挂载、
独立工作目录、断网和空 PATH 执行 NativeAOT，比较三流及文件副作用并重跑三个缺依赖负例。

固定 20 项目和完整对象桥生命周期按各自矩阵验收：当前 macOS 三模式各 19/20，attrs 缺入口
模块活字典；其他平台完整项目集、NumPy、HTTPS 证书链，以及 macOS/Windows 无开发环境目标
仍待专项验证。onefile 与默认兼容配置晋升沿路线图独立推进。

### 5.3 CI 实测修复记录

- 从确切 PBS 20260623 官方资产准备基础发行；验证解释器版本后写入 BUILD 来源凭据
  （官方 install_only 归档没有该文件），shim、Oracle 与发布使用同一发行。
- Windows Cargo 使用 `D:\c` 并启用 Git longpaths；构建工具安装到独立 venv；MSVC 环境下
  托管桥显式 `Platform=AnyCPU`，立即检查输出并将本次桥和 shim 冻结到私有输入。
- Rust/Python 源路径两侧 canonicalize，收集器显式 `-X utf8`；RECORD 分隔符统一，包内
  相对导入递归发现。Linux shim 直接链接版本化 libpython，PE 表扫描兼容省略终结项的 Size。
- Linux 真实系统闭包专测覆盖 libcrypt 来源、包版本、许可证、递归边、符号版本、相对 rpath
  及错架构拒绝；公共许可证裸目录引用跳过，具名文件缺失与越界仍拒绝。
- 产品 PE 解析器已对固定 Windows 发行的 44 个 DLL/PYD 静态解析，补齐 14 个确切 OS DLL
  名；VC 运行时继续要求随包。macOS runner 使用仍受支持的 macos-15。
