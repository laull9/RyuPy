# RyuPy

把常见 Python 项目编译成 .NET CIL，交给 RyuJIT 运行。编译器用 Rust 编写，运行时 `RyuPy.Runtime` 用 C# 编写；当前 WIP 已提供有界 Native AOT 发布，适用范围与验证记录见 docs/validation/。

## 当前状态

当前能力、有限兼容范围与未完成项以 [TODO](TODO.md)、[语义说明](docs/semantics.md) 和 [验证归档](docs/validation/) 为准。WIP 的专项通过不等于全部真实项目、任意扩展或所有部署环境通过。

近期目标为固定纯 Python 项目集至少 80% 原生通过。项目集已冻结；2026-11-01 历史 Linux JIT 指定入口记录为 5/20，后续特性没有自动增加项目通过数，逐项目范围和旧阻塞见 [compatibility.md](docs/compatibility.md)。已知偏差公开登记，refcount 与即时析构时机不作兼容承诺。阶段安排见路线图。

```bash
make run FILE=tests/e2e/phase1/m1_5_dataclass.py
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=ast
```

## 快速开始

依赖：Rust stable、.NET SDK 10、Python 3.12+。

```bash
make runtime   # 构建 C# 运行时
make check     # lint + 测试 + e2e
make build     # 打宿主平台发行包（releases/）
make bench     # CPython vs RyuPy JIT 固定输入基准（不进入 make check）
make bench-aot # AOT 后端；也可 make bench ARGS='--aot'
make help      # 全部命令
```

## AOT 构建与交付（WIP）

`ryupy build --aot path/name.py` 把中间产物放入 `path/name.build/`，交付目录为 `path/name.dist/`，原生入口为 `name`（Windows 为 `name.exe`）。`--out-dir ROOT` 将这两个目录放到 ROOT；JIT 缓存布局不变。已有未知或修改过的输出目录不会被覆盖。

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
