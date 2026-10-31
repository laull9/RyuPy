# RyuPy 快速上手与发布架构说明

> RyuPy 是一个用 Rust 编写的 Python → .NET CIL 编译器。
> 原则：与 CPython 语义保持一致；不支持的特性在编译期明确报错，不产生静默运行时语义偏差。

---

## 1. 环境准备

| 依赖项 | 推荐版本 | 用途 |
| :--- | :--- | :--- |
| **Rust** | stable (2024 edition) | 构建编译器 CLI 本身 |
| **.NET SDK** | 10.x | 构建运行时库 `RyuPy.Runtime`、运行生成的 `.dll`、Native AOT |
| **Python** | 3.12+ | 端到端（e2e）差分测试的「黄金标准」对比源 |

### 快速校验开发环境

克隆仓库后，执行以下命令构建 C# 运行时并执行全量测试：

```bash
make runtime   # 构建 C# 运行时库（Release）
make check     # 运行 fmt 检查 + clippy -D warnings + 单元测试 + e2e 差分测试
```
如果终端输出全绿，说明开发环境配置完毕！

---

## 2. 常用开发命令

RyuPy 提供了开箱即用的 Makefile 指令：

```bash
# 1. 编译并运行 Python 文件
make run FILE=tests/e2e/phase1/m1_0_print_int.py

# 2. 查看编译流程中任意阶段的中间表示（IR）
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=ast    # Ruff 语法树
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=hir    # 高级 IR（去糖、作用域解析）
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=types  # 旁表类型信息
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=mir    # 中级 IR（控制流图、三地址指令）
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=cil    # ilasm 风格 CIL 汇编

# 3. 运行端到端差分测试（以 CPython 为基准比对输出）
make e2e        # 运行当前所有已实现的特性用例
make e2e-all    # 包含尚未实现的 pending 用例，查看整体里程碑进展
```

---

## 3. RyuPy 打包发布形态与依赖分析

作为一款现代编译器，RyuPy 在发布部署时被设计为一个**独立、便携的 CLI 命令行工具**。

### 3.1 发布形态（Artifact Format）

当我们将 RyuPy 正式发布给终端用户时（例如通过 GitHub Releases、Homebrew、`cargo binstall` 或 `winget`）：

```text
ryupy-dist/
├── ryupy                     # 独立的 CLI 二进制程序（Linux/macOS 为 Mach-O/ELF，Windows 为 ryupy.exe）
└── RyuPy.Runtime.dll         # RyuPy 运行时辅助库（可直接嵌入 CLI 二进制内部，见下文）
```

- **单一原生二进制**：
  `ryupy` 是一个完全用 Rust 编译出的原生可执行文件，体积小、冷启动快、无依赖。
- **内嵌运行时（Single-Binary Distribution）**：
  在正式分发构建中，利用 Rust 的 `include_bytes!` 宏，可以在编译 `ryupy` 时直接将编译好的 `RyuPy.Runtime.dll` 二进制字节打包进 `ryupy` 可执行文件本身。当执行构建时，`ryupy` 自动将其释出到输出目录或全局缓存目录（如 `~/.ryupy/runtime/`），实现**真正单一二进制文件分发**。

---

### 3.2 依赖解耦与架构设计

RyuPy 的一个核心架构优势是：**编译过程与目标平台的解耦**。

#### ① 编译阶段（Compile Time）：纯 Rust，零环境依赖
当用户执行：
```bash
ryupy build hello.py -o out/
```
- **不需要安装 Python**：RyuPy 静态内嵌了由 Rust 编写的高性能 Ruff AST 解析器；
- **不需要安装 .NET SDK**：RyuPy 使用纯 Rust 编写的 `dotnetdll` 库直接生成符合 ECMA-335 标准的 PE 结构文件（`.dll`）和 `.runtimeconfig.json`；
- **跨平台交叉编译**：在任何平台上都可以生成通用的 AnyCPU .NET 程序集。

#### ② 运行阶段（Run Time）：两种执行形态

用户运行编译出来的产物时，有两种形态选择：

| 运行形态 | 命令 | 产物形态 | 用户环境依赖 | 适用场景 |
| :--- | :--- | :--- | :--- | :--- |
| **标准 JIT 模式**<br>*(默认)* | `ryupy run hello.py`<br>或 `dotnet out/hello.dll` | `hello.dll`<br>`hello.runtimeconfig.json`<br>`RyuPy.Runtime.dll` | **.NET Runtime** (无需 SDK，仅需要标准 dotnet runtime) | 日常开发、快速测试、跨平台跨架构分发 |
| **Native AOT 模式**<br>*(Phase 1 尾声实现)* | `ryupy build --aot hello.py` | `hello` (原生二进制) | **完全零依赖**（目标机无需安装 .NET、无需 Python） | 命令行工具、微服务容器镜像、高性能生产交付 |

> [!NOTE]
> **总结**：
> RyuPy 本身发布为一个**独立的 CLI 工具**。用户安装了 `ryupy` CLI 即可直接把 Python 代码编译为 .NET 程序集；生成的程序既可以通过通用的 .NET Runtime 即时编译运行，也可以通过 `--aot` 输出完全自包含的原生单文件可执行文件！
