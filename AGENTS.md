# CLAUDE.md

RyuPy：Rust 编写的 Python → .NET CIL 编译器。所有对话、文档、注释使用中文。

## 必读

- 分层与依赖规则：`docs/architecture.md` §2（违反即架构错误）
- 语义规范：`docs/semantics.md`（原则：与 CPython 一致，否则编译期报错）
- 开发循环：`docs/workflow.md` §3（竖切，一次一个特性）
- 当前任务：`TODO.md`

## 命令

```bash
make runtime   # 构建 C# 运行时（e2e 前必须）
make check     # 提交前必跑：fmt 检查 + clippy -D warnings + 测试 + e2e
make dump FILE=x.py EMIT=ast|hir|types|mir|cil
cargo test -p ryupy-e2e -- <名字过滤>
```

## 约定

- 学习项目：人不手写代码，但是得清楚知道作用、流程。
- 各阶段遇到不支持的语法：报 `Diagnostic` 并继续，**不要 panic**。错误码分段见 architecture.md §4。
- 只有 `ryupy-syntax` 依赖 ruff，只有 `ryupy-codegen-cil` 依赖 dotnetdll，codegen 不得依赖 HIR / ty。
- 新增 `RuntimeFn` 时同步修改 `runtime/RyuPy.Runtime/` 与 codegen 引用表。
- 新特性先写 e2e 用例（`# ryupy: pending`），完成后去掉标记。新发现的语义差异追加到 semantics.md §4。
- 有取舍的决定写 ADR（`docs/adr/`）。
