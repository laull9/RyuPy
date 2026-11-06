# 显式模块实例基础用例

只有case.txt的instance_module目标选择真实实例globals；其余模块仍走默认静态路径。
Python源码可直接由固定CPython执行，不含RyuPy专有API或针对测试名字的运行时逻辑。

- ordinary_retry：失败import保留旧对象，重试创建新模块/dict/容器，不覆盖旧状态。
- dynamic_names：真实字典注入、外部类型变化、builtin与range遮蔽，不能保旧静态捷径。

两个正例已由完整 CP/JIT 差分通过后转正。pending_annotated_names 保留原完整注解
版本及后续断言，当前在注解处明确 E0100，不能缩短源程序冒称通过。内部双实例/直接
重复执行另由真实生成 PE 宿主验收；这些病例不宣称公开 loader 或完整函数归属已支持。
