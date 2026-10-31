# CLAUDE.md

RyuPy：Rust 编写的 Python → .NET CIL 编译器。所有对话、文档、注释使用中文。

## 必读

- 分层与依赖规则：`docs/architecture.md` §2（违反即架构错误）
- 路线与兼容性：`docs/roadmap.md`、`docs/compatibility.md`（按固定项目集推进约 80% 实用兼容，长尾暂缓）
- 语义规范：`docs/semantics.md`（常用行为对齐 CPython；已登记偏差明确说明，不支持项明确报错）
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
- 单代码文件不超过600行，超出时拆分模块、设计分层。
- git提交：不提交未完成代码，也不提交极小的改动为一个版本
- 注释：关键逻辑、算法、结构都需要注释。文件开头写让人能直接了解整个文件做了什么的介绍。
- **不得为了追求行数达标而删减介绍和注释**
- 各阶段遇到不支持的语法：报 `Diagnostic` 并继续，**不要 panic**。错误码分段见 architecture.md §4。
- 只有 `ryupy-syntax` 依赖 ruff，只有 `ryupy-codegen-cil` 依赖 dotnetdll，codegen 不得依赖 HIR / ty。
- 新增 `RuntimeFn` 时同步修改 `runtime/RyuPy.Runtime/` 与 codegen 引用表。
- 新特性先写 e2e 用例（`# ryupy: pending`），完成后去掉标记。新发现的语义差异追加到 semantics.md §4。
- 计划与实现分开登记；桥接通过不计入原生项目通过率。标准库逐 API 验收，不能以导入成功代表模块兼容。
- 性能实现保留常用结果、身份、别名与副作用；低影响差异经 ADR 接受并登记后可作为高性能默认实现，影响常用结果的扩展由用户显式选择。refcount / 析构时机边界见 compatibility.md。
- 有取舍的决定写 ADR（`docs/adr/`）。
