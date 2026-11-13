# RyuPy

把 Python 项目编译为可独立交付的原生应用是当前主线：以 Nuitka 的实用兼容性为对标，逐步改进 AOT 性能与打包体验。编译器用 Rust 编写，经 .NET CIL / NativeAOT 发布，运行时用 C# 编写；同时保留 JIT 路径。这个目标尚未达成，不代表目前兼容或性能已全面超过 Nuitka。

## 当前状态

AOT 项目入口、资源声明、交付目录与平台范围见[快速上手](docs/quickstart.md#统一项目部署配置有限范围验收)。目前已验 pyfiglet 两入口的 Linux x64、macOS arm64、Windows x64 原位/搬迁运行；只有 Linux 有禁网无 Python/.NET 容器证据。其它项目按各自用例验收，不推导全库兼容。

下面是保留的 **2026-11-12 JIT 与早期专项基线**，不是最新 AOT 项目总数。产品证据对应代码 [85b11a94](https://github.com/laull9/RyuPy/commit/85b11a94368f0ba0a0bda80d611b9e0c39a5cf7b)，本次仅更新文档，不表示在后续文档提交上重新构建或运行产品。

固定 20 项目的最新完整入口结果：启用构建时 Cargo feature `ryupy-driver/ordinary-dynamic-module-pilot` 的普通动态模块 pilot 为 **14 成功 / 6 unsupported / 0 失败**；默认源计划为 **13 成功 / 6 unsupported / 1 失败**，差异仍是既有 Markdown 路径。这里的成功指托管程序集的 **JIT 执行**，不等于 NativeAOT，也不代表项目全部 API 兼容。默认 JIT 的 Markdown 失败是 `NotImplementedError: find_spec(): legacy or native module registrations without source metadata are not implemented`；pilot JIT 的该入口通过。独立 `distribution-metadata` AOT 的旧失败则是 `[E0100] 显式实例 AOT 需要资源清单且不支持原生绑定`，不是同一个失败。

独立默认 CLI 的有限 Decimal、tzinfo 用户单基图已在 Ubuntu 24.04 x86_64、.NET SDK 10.0.403 上完成真实 Linux x64 ELF 原位与搬迁运行。证据见 [CI 43210802587](https://github.com/laull9/RyuPy/actions/runs/43210802587) 及各独立 artifact；整次 CI 仍有旧门失败，不能据专项通过称全绿。完整 toml Native、全量 MI Native 不在通过范围。

pilot 是构建时 feature，不是 CLI 运行参数；普通默认构建未启用它，因此默认命令对应 13 / 6 / 1，不应期待 pilot 的 14 / 6 / 0。

当前能力、有限兼容范围与未完成项以 [TODO](TODO.md)、[语义说明](docs/semantics.md) 和 [验证归档](docs/validation/) 为准。WIP 的专项通过不等于全部真实项目、任意扩展或所有部署环境通过。

兼容性参考仍保留固定纯 Python 项目集至少 80% 原生语义兼容目标（当前以 JIT 指定入口计数，不是 80% NativeAOT）。pilot 的 14/20 为 70%，仍需两项完整入口通过。项目范围和历史记录见 [compatibility.md](docs/compatibility.md)；后续特性不会自动增加通过数。已知偏差公开登记，refcount 与即时析构时机不作兼容承诺。

```bash
make run FILE=tests/e2e/phase1/m1_5_dataclass.py
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=ast
```

## 快速开始

依赖：Rust stable（2024 edition）、.NET SDK 10；差分验收固定 CPython 3.12.13。NativeAOT 还需目标平台的原生工具链；已有 Linux x64、macOS arm64、Windows x64 的有限应用验证，不提供交叉编译承诺。

```bash
make runtime   # 构建 C# 运行时
make check     # lint + 测试 + e2e
make build     # 打宿主平台发行包（releases/）
make bench     # CPython vs RyuPy JIT 固定输入基准（不进入 make check）
make bench-aot # AOT 后端；也可 make bench ARGS='--aot'
make help      # 全部命令
```

## AOT 构建与交付（有限范围已验证）

`ryupy build --aot path/name.py` 把中间产物放入 `path/name.build/`，交付目录为 `path/name.dist/`，原生入口为 `name`（Windows 为 `name.exe`）。`--out-dir ROOT` 将这两个目录放到 ROOT；JIT 缓存布局不变。已有未知或修改过的输出目录不会被覆盖。

可复用的独立验收入口与边界见 [Decimal Native 门](tests/aot/decimal_finite_native/README.md) 和 [tzinfo Native 门](tests/aot/tzinfo_native/README.md)。通过的是默认 CLI 的这些有限图，不是完整 toml 或任意多继承程序。

新 AOT 基准直接执行交付目录中的原生程序，编译/ILC 不计入运行采样；保存结果标明后端，默认不混比 JIT/AOT。当前基准 runner 仅支持 POSIX，`--aot --profile` 不支持。完整限制与本轮验证状态见 [ADR-0052](docs/adr/0052-aot-build-dist-and-bench.md)。

## 显式包含文件包（本地候选）

`ryupy build app.py -I vendor --include-module package.assets` 将字符串形式引用的真实源码包纳入编译图；`run`也支持重复该选项。初始化仍在实际导入时发生，包含不自动复制数据。有限 `importlib.resources.files(anchor)` 可读取已登记文件包：JIT使用真实源根，AOT仍须通过现有 `--resources` 显式部署数据。非包/default caller/其它loader等边界见[ADR-0057](docs/adr/0057-explicit-package-resources.md)，不代表完整importlib或第三方项目通过。

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
| [docs/roadmap.md](docs/roadmap.md) | 愿景与五阶段路线图 |
| [docs/compatibility.md](docs/compatibility.md) | 80% 目标的计算方式、项目清单与偏差 |
| [docs/stdlib.md](docs/stdlib.md) | 标准库逐批移植、C# 实现与 libpython 桥接 |
| [docs/optimization.md](docs/optimization.md) | 各阶段的前瞻优化、守卫与性能验收 |
| [docs/architecture.md](docs/architecture.md) | 分层、依赖规则、各层 IR 设计、测试策略 |
| [docs/semantics.md](docs/semantics.md) | 支持的 Python 子集、类型映射、语义陷阱清单 |
| [docs/workflow.md](docs/workflow.md) | 开发循环、测试选择、调试手段、提交规范 |
| [docs/adr/](docs/adr/) | 架构决策记录 |
| [docs/notes/](docs/notes/) | 学习笔记 |
| [tests/README.md](tests/README.md) | 后续阶段 Python pending 用例与 CPython 基线 |
| [TODO.md](TODO.md) | 按里程碑拆分的任务清单 |
