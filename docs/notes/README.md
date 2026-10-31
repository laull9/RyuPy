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

## 笔记索引

- [2026-10-31 M1.0 曳光弹：从 ruff 到 CIL 的垂直贯通](2026-10-31-m1.0-tracer-bullet.md)
- [2026-10-31 M1.1 函数定义与局部变量：ABI、签名与局部槽位管理](2026-10-31-m1.1-functions-and-locals.md)
- [2026-10-31 M1.2 算术与基础控制流：整型溢出、语义陷阱与短路逻辑](2026-10-31-m1.2-arith-and-control-flow.md)
- [2026-10-31 M1.3 for range 与斐波那契：原生计数循环、常量折叠与 6.2x 性能超越](2026-10-31-m1.3-for-range-and-fib.md)
- [2026-10-31 M1.4 浮点、类型转换与字符串：CPython 对齐、空字符串陷阱与 6.1x Mandelbrot 基准](2026-10-31-m1.4-floats-and-strings.md)
- [2026-10-31 M1.5 @dataclass：引用与值类型双轨、模块分层与 600 行拆分](2026-10-31-m1.5-dataclass.md)
- [2026-10-31 M2.0 项目基线、用例框架与动态值 ABI](2026-10-31-m2.0-project-baseline.md)
- [2026-10-31 M2.1 动态值管线：降级策略、装箱 ABI 与 BigInteger](2026-10-31-m2.1-dynamic-pipeline.md)
- [2026-10-31 M2.2 异常体系：类型化 catch、finally 状态机与 traceback 插桩](2026-10-31-m2.2-exceptions.md)
- [2026-10-31 M2.3 容器与可变别名：统一降级、精确数值比较、NaN 键与求值顺序陷阱](2026-10-31-m2.3-containers.md)

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
