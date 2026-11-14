# RyuPy

将Python项目编译为.NET CIL及NativeAOT独立应用。编译器用Rust编写，运行时用C#编写，
兼容模式复用随包CPython标准库与第三方扩展；本地应用模块进入CIL编译链。

## 使用

RyuPy CLI本身是一个文件，C#运行时、互操作桥、SQLite依赖、许可证和shim源码均内嵌。
将`ryupy`（Windows为`ryupy.exe`）放到PATH后，在已准备好业务依赖的Python项目中执行：

```sh
ryupy main.py                         # 默认自动环境＋单文件AOT
ryupy main.py --onefile               # 显式选择同一形态
ryupy main.py --mode standalone       # 独立运行目录
ryupy main.py --native                # 纯RyuPy执行域
ryupy main.py -o release              # 指定输出根，默认./dist
```

普通多文件项目的静态导入自动进入编译图。工具依次探测项目venv、激活环境和PATH中的Python，
核验完整版本和ABI，收齐已安装的第三方依赖、metadata与包资源。无需填写版本号或provider JSON，
构建工具在RyuPy私有缓存准备。`--help`查看常用调用，`--help-advanced`查看开发选项，
`--licenses`查看内嵌组件许可。

单文件应用位于`dist/main.onefile/main`（Windows为`main.exe`），搬走该文件即可运行。
启动时在独占临时目录解包，正常退出及已覆盖的中断后清理。目录发布位于`dist/main.dist`。
原生发布会自动检测.NET SDK 10及以上和Python环境，构建机还需平台C工具链、curl及支持
Zstandard的tar或zstd；运行生成的AOT应用无需安装.NET、Python或构建工具。
托管运行需要.NET Runtime 10。CLI的帮助、版本和许可查询无需.NET或Python。

## 当前验收

2026-11-14：macOS arm64已经用仓库外的唯一CLI文件、全新私有缓存构建普通五文件项目；
四路原生JIT冷缓存并发、深层第三方依赖与资源、删除CLI/缓存/项目全部输入后的唯一AOT搬迁，
三流和文件副作用均与同次CPython一致。三平台运行同一[多文件项目驱动](tests/cpython/multifile_README.md)。
三平台固定CPython standalone和onefile、Linux无Python/.NET/SDK断网容器已通过；
三平台Release单文件CLI已通过独立验收；Python3.9–3.15和新增src布局继续逐项复验，实际结果见[版本验收](tests/cpython/version_README.md)。

固定20项目CPython provider在macOS的JIT、NativeAOT及输入删除搬迁各通过19项，attrs缺入口
模块活字典。纯RyuPy保留门为13通过、6未支持、1失败，独立登记。范围见
[兼容性清单](docs/compatibility.md)、[平台状态](docs/cpython-platform-support.md)和[TODO](TODO.md)。

## 从源码构建

需要Rust stable（2024 edition，MSRV 1.85）和.NET SDK 10。Cargo在隔离输出目录编译C#组件
并嵌入CLI，源码仓库保留源码、手写测试输入、配置和人工文档。

```sh
cargo build -p ryupy-cli
make check     # fmt、clippy、Rust/C#测试与e2e
make build     # Release单文件CLI与仅含该文件的分发归档
```

旧`build/run/probe/dump`开发入口保留原有行为，`build/run`默认纯原生执行域；例如
`ryupy build main.py`生成托管产物，`ryupy run main.py`编译并运行。
实现与验收合同见[ADR-0190](docs/adr/0190-project-cli.md)、
[ADR-0191](docs/adr/0191-single-file-compiler-cli.md)及[CPython provider](docs/cpython-provider.md)。

## 目录

```text
crates/
  ryupy-base/          源码管理、Span、诊断
  ryupy-syntax/        ruff 解析器封装（唯一依赖 ruff 的 crate）
  ryupy-hir/           AST → HIR：名称解析、去糖
  ryupy-ty/            类型推导与检查
  ryupy-mir/           控制流图 IR、Python 语义改写、优化 Pass
  ryupy-codegen-cil/   MIR → CIL → .dll（dotnetdll）
  ryupy-driver/        流水线编排、产物、调用 dotnet
  ryupy-cli/           `ryupy` 命令行
  ryupy-e2e/           CPython 差分测试
runtime/RyuPy.Runtime/ C# 运行时
tests/e2e/             端到端用例（.py）
docs/                  文档
```

## 文档

| 文档 | 内容 |
| --- | --- |
| [docs/quickstart.md](docs/quickstart.md) | 快速上手与发布架构说明 |
| [docs/roadmap.md](docs/roadmap.md) | CPython provider 主线与历史五阶段 |
| [docs/compatibility.md](docs/compatibility.md) | 80% 目标的计算方式、项目清单与偏差 |
| [docs/stdlib.md](docs/stdlib.md) | CPython 优先覆盖与既有原生 API 清单 |
| [docs/cpython-provider.md](docs/cpython-provider.md) | 随包宿主、对象桥与短期验收门 |
| [docs/repository-layout-plan.md](docs/repository-layout-plan.md) | 代码/测试分层与产物治理计划 |
| [docs/optimization.md](docs/optimization.md) | 各阶段的前瞻优化、守卫与性能验收 |
| [docs/architecture.md](docs/architecture.md) | 分层、依赖规则、各层 IR 设计、测试策略 |
| [docs/semantics.md](docs/semantics.md) | 支持的 Python 子集、类型映射、语义陷阱清单 |
| [docs/workflow.md](docs/workflow.md) | 开发循环、测试选择、调试手段、提交规范 |
| [docs/adr/](docs/adr/) | 架构决策记录 |
| [docs/notes/](docs/notes/) | 学习笔记 |
| [tests/README.md](tests/README.md) | 后续阶段 Python pending 用例与 CPython 基线 |
| [TODO.md](TODO.md) | 按里程碑拆分的任务清单 |
