# RyuPy

把常见 Python 项目编译成 .NET CIL，交给 RyuJIT 运行。编译器用 Rust 编写，运行时 `RyuPy.Runtime` 用 C# 编写；Native AOT 独立部署尚待实现。

## 当前状态

Phase 0、Phase 1 与 Phase 2 的 M2.1–M2.10 已完成：静态数值路径、动态值与容器、普通 class / 单继承、异常、模块与包、`with`、有界 PIC。下一步是 Phase 3 M3.0 的闭包、lambda 与 nonlocal。

近期目标为固定纯 Python 项目集至少 80% 原生通过。项目集已冻结，当前 0/20 原生通过，逐项目阻塞见 [compatibility.md](docs/compatibility.md)。随后推进闭包、生成器、推导式、装饰器和标准库移植。已知偏差公开登记，refcount 与即时析构时机不作兼容承诺。阶段安排见路线图。

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
make bench     # CPython vs RyuPy 固定输入基准（不进入 make check）
make help      # 全部命令
```

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
