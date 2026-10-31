# ADR-0001：使用 ruff 解析器，并隔离在 `ryupy-syntax`

- 状态：已接受
- 日期：2026-10-31

## 背景

需要一个完整支持 Python 3.12+ 语法（缩进、f-string、`match`、类型参数等）、带错误恢复的解析器。
自己写一个是数万行的工作量，且不是本项目的学习重点。

## 方案

### A. `ruff_python_parser`（Astral）
工业级、速度快、容错、持续跟进新语法。**缺点：未发布到 crates.io**，只能用 git 依赖；
API 被 Astral 视为内部接口，版本之间可能有破坏性变化。

### B. `rustpython-parser`
已发布到 crates.io，但维护活跃度和新语法跟进不如 ruff。

### C. 自己写
学习价值高，但会把 Phase 1 拖慢数月。

## 决定

选 A，并采取两项措施控制风险：

1. 在根 `Cargo.toml` 中用 **固定 tag**（当前 `0.17.3`）引用 `ruff_python_parser`、`ruff_python_ast`、`ruff_text_size`，三者必须同一 tag；
2. **只有 `ryupy-syntax` 直接依赖解析器**，下游通过 `ryupy_syntax::ast` 使用 AST。升级 ruff 时受影响的只有 `ryupy-syntax` 与 `ryupy-hir/src/lower.rs`。

## 后果

- 首次构建要克隆 ruff 仓库（较慢），之后有缓存。
- `ruff_text_size` 被 `ryupy-base` 用作 span 类型，这是有意的：避免在 ruff 的 `TextRange` 与自定义类型之间来回转换。
- 若将来 ruff 变动过于频繁，可以考虑切换到 B，改动范围同样被限制在上述两处。
