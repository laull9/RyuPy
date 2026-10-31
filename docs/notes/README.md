# 学习笔记

记录学到的东西、踩过的坑、读过的资料。文件名用 `YYYY-MM-DD-主题.md`，例如 `2026-11-03-m1.0-tracer-bullet.md`。

## 模板

```markdown
# 主题

- 日期：
- 关联里程碑 / ADR：

## 做了什么

## 踩到的坑
（现象 → 原因 → 解决。能写成测试的，一定要写成测试。）

## 学到的概念

## 参考资料

## 还没想明白的
```

## 推荐阅读

| 主题 | 资料 |
| --- | --- |
| 编译器整体 | *Crafting Interpreters*（Robert Nystrom）；*Engineering a Compiler* |
| HIR / arena 设计 | rust-analyzer 架构文档 `docs/dev/architecture.md` |
| 类型推导 | *Types and Programming Languages* 第 22 章；Hindley–Milner 与并查集的关系 |
| MIR / CFG | rustc dev guide 的 MIR 章节 |
| SSA | Braun et al., *Simple and Efficient Construction of SSA Form* (2013) |
| CIL / 元数据 | ECMA-335 标准（Partition II 元数据、Partition III 指令集） |
| Python 语义 | Python 语言参考手册「执行模型」「数据模型」两章 |
| 内联缓存 | Hölzle et al., *Optimizing Dynamically-Typed Object-Oriented Languages With Polymorphic Inline Caches* (1991) |
