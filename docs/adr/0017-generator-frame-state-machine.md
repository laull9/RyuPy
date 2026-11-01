# ADR-0017：普通生成器先使用全局部堆帧状态机

后续更新：ADR-0026将控制槽扩为PC/send/注入异常/handler异常，locals从4起，并接入异常清理与委托；下文保留当时首切决策记录。

- 状态：已接受（普通 yield 首切）
- 日期：2026-11-01

## 背景与方案

暂停不能依赖 CLR 栈持续存在，且循环、共享 cell 和异常结束必须保持 Python 可观察行为。
按源码切分 continuation 难以维护循环回边；把 Python 语义移到 CIL 后端又违反层边界。
可以直接生成每函数状态机类型，也可以用共享运行时状态机类型配合每函数静态恢复委托。
后者无需为每个局部生成字段和类型，先复用现有 MIR CFG 与运行时数组操作。

## 决定

HIR 标记生成器并保留 Yield，类型层强制 Dynamic；MIR 独立 pass 将 Suspend 边转换为
工厂与恢复函数。每实例分配 object[]，槽 0 为恢复块编号，后续按 LocalId 保存所有局部。
工厂绑定参数而不执行用户函数体；恢复先装载帧，再分派到首次入口或暂停后块。
cell 保存引用，标量装箱；恢复委托为静态函数，不为每个元素创建新闭包或状态机。

共享 PyGenerator 管理 running / closed；每次恢复单独 push/pop traceback，暂停后不遗留
线程局部帧。PC -1 区分正常耗尽和 yield None。错误永久关闭；裸 StopIteration 包装为
RuntimeError 并保留 cause/context。完成和错误释放帧保存引用及委托。

首切明确拒绝生成器零参 super、try/with、return value、表达式型 yield 和 yield from，不伪造清理语义。
send/throw/close 显式 NotImplementedError，留后续独立验收。MIR 验证拒绝残留 Suspend。

## 代价与后续

每次暂停 / 恢复保存所有局部，带来装箱、数组访问和不必要引用保留；恢复分派目前为线性分支链。
这不是性能收益结论。后续先加入跨暂停存活性分析，再考虑仅 spill 活跃局部、switch 分派、
专用类型字段或委托缓存，须以差分、分配量和基准测量证明，而非牺牲共享 cell 或异常语义。
完整异常区域状态与 return/send/throw/close 协议仍为后续竖切；项目兼容率不因本语法通过自动增加。
