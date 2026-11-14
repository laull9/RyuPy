# ADR-0196：列表推导式私有收集器直接追加

- 状态：已接受；有限默认实现，结构/JIT/NativeAOT/搬迁/配对性能及完整门禁通过
- 日期：2026-11-15
- 关联：ADR-0016、ADR-0182

## 问题

HIR将列表推导式的收集操作表示为编译器私有`.result.append(element)`，此前每次走完整通用方法分派。
原始comprehensions基准的三个列表推导式共执行79.8万次append；伴随集合推导式执行40万次add。

## 决定

仅在MIR构建层识别已证明的私有列表：合成Nested/listcomp来源、无真实定义信息、非生成器；
receiver为非参数/非cell/free的`.result`本地；root首条为空列表初始化，所有语句中没有第二处绑定写入。
导入和handler绑定参与穷尽检查，删除、类定义及未知FunctionDefaults来源拒绝。
调用须为append和唯一位置实参。全部检查完成后，接收者及元素各求值一次，再发射已有ListAppend ABI。

普通append、自定义类、set/dict和生成器表达式走原路径。保留独立函数、闭包、最左迭代器时机与过滤/元素顺序；
不在本轮去掉函数包装、合并作用域、预估容量或改CIL局部保活策略。

## 异常与兼容性边界

元素/过滤失败的旧版与候选NativeAOT完整异常流一致。本轮CPython3.12.13少一条listcomp帧；
RyuPy沿ADR-0016保留独立函数，该既有差异登记在semantics §4.17，后续结合作用域方案修复。
模块未绑定list值向内建回退的问题另以pending反例保留（semantics §4.16）。

## 验证

8项真实源/MIR负例测试与严格Clippy通过，原始基准三个列表收集器直接追加；
即时CPython/JIT、源码/构建目录隐藏、空PATH中文搬迁通过。性能及完整门禁结果见推导式专项文档。

26项配对与搬迁通过，comprehensions为130.08→117.66ms，RSS及分配持平；完整make check退出0。
随包NumPy数组迭代源的provider NativeAOT及隐藏输入搬迁通过。详细数据与边界见
[推导式专项](../aot-comprehension-optimization.md)。
