# RyuPy

把 Python 编译成 .NET CIL 程序集，交给 RyuJIT 运行或用 Native AOT 编译为原生可执行文件。
编译器用 Rust 编写，运行时底座 `RyuPy.Runtime` 用 C# 编写。

> **这是一个学习项目。** 目标是亲手走一遍「源码 → AST → HIR → 类型 → MIR → CIL」的完整编译器流水线。

## 当前状态

**Phase 0（脚手架）已完成，Phase 1 M1.0 待开工。** 解析器与诊断已可用，其余各阶段为带设计说明的 `todo!()` 骨架。

```bash
make dump FILE=tests/e2e/phase1/m1_0_print_int.py EMIT=ast   # ✅ 可用
make e2e-all                                                  # 查看全部用例的进度（目前均为 pending）
```

## 快速开始

依赖：Rust stable、.NET SDK 10、Python 3.12+。

```bash
make runtime   # 构建 C# 运行时
make check     # lint + 测试 + e2e
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
| [docs/roadmap.md](docs/roadmap.md) | 愿景与五阶段路线图 |
| [docs/architecture.md](docs/architecture.md) | 分层、依赖规则、各层 IR 设计、测试策略 |
| [docs/semantics.md](docs/semantics.md) | 支持的 Python 子集、类型映射、语义陷阱清单 |
| [docs/workflow.md](docs/workflow.md) | 开发循环、测试选择、调试手段、提交规范 |
| [docs/adr/](docs/adr/) | 架构决策记录 |
| [docs/notes/](docs/notes/) | 学习笔记 |
| [TODO.md](TODO.md) | 按里程碑拆分的任务清单 |
