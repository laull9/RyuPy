# 函数内属性与下标的普通注解赋值

本竖切用于解除 chardet 5.2.0 的 `self.x: Optional[...] = value` 等普通方法内注解目标阻断。
不按库名特判；不要求 `from __future__ import annotations`。

## 已验证范围

函数内 `obj.attr: annotation = value` 与 `obj[index]: annotation = value`：
先计算右值，再计算目标组成部分并写入；注解表达式不求值，不字符串化，不存储类型元数据。

没有右值时仍计算目标组成部分，但不读取属性/下标值、不写值，也不调用注解。与 future
注解共用目标构建逻辑，仅普通函数非简单目标进入此新路径；模块和类体的非 future 注解
不能因此被静默忽略，仍保留原明确诊断与独立 pending。

Python 的符号判定和运行时求值并不完全相同：局部注解内直接 `yield` 或 lambda 默认值中的
`yield` 仍将当前函数标为生成器，但注解没有真正产出点。lambda **函数体**的 yield 不影响
外层函数。已按固定 CPython 3.12.13 验证工厂不执行、首次 next 完成主体并返回终值。
注解内的 `super()` 不执行，因此不会触发“生成器内零参 super 未支持”的错误门禁。

## 仍未支持

- 局部目标注解内海象表达式：虽然不执行，却会影响局部名称绑定；明确 E0100，而不是忽略后
  悄悄读取同名全局值。首版保守包含注解内部 lambda 的海象，不扩完整符号表分析。
- 只为不执行注解保留的 CPython `co_freevars` 等完整代码对象元数据；不创建运行中用不到的
  lambda/捕获环境来模拟未承诺的元数据，也不声称完整 code object 兼容。
- 模块/类体普通属性、下标注解的实际注解求值：独立用例
  `m3_7_eager_annotated_targets.py` 保持 pending。类体控制流另属独立 pending。

## 实现与成本

HIR 复用已有目标赋值去糖，不新增 runtime ABI、反射或每次注解的字典/字符串分配。
多条目标组成表达式沿现有 `if True` 语句容器组织；不声称尚未测量的性能提升。
生成器判定仍扫描注解语法，但其调用能力诊断不会把未执行的调用当成真实恢复点。

六项 HIR 专测与两项固定 CPython 差分通过，`m3_7_annotated_targets.py` 和
`m3_7_future_ignored_annotations.py` 已转正。
日志 `/tmp/ryupy-m3_7-annotated-targets-{hir,e2e}.log` 与对应 `-results.jsonl`。
实际 chardet 指定入口复测前移至 `memoryview` 未实现，仍为 unsupported；不增加原生项目分子。
独立项目证据为 `/tmp/ryupy-m3_7-chardet-after-local-annotations.jsonl`，不能代替固定 20 项目全矩阵。

## future 非法注解的作用域校验

固定 CPython 3.12.13 禁止 future 注解当前作用域中的 yield、yield from、await 和海象表达式。
Ruff 只解析这些语法形状，没有替我们执行此 future 编译期限制；HIR 使用 E0100 明确拒绝，
不冒称实现完整 CPython SyntaxError 诊断系统。lambda 默认值、推导式最左 iterable 仍属于
当前注解作用域；lambda body 与推导式其它部分是独立作用域，不因此被误拒。
七个负例、四个合法独立作用域例由 HIR 单测锁定，合法函数目标注解另有真实差分。
最终 fmt/check/clippy 日志 `/tmp/ryupy-m3_7-annotation-{fmt,check,clippy}.log`，
最终差分结果分别是 `/tmp/ryupy-m3_7-annotated-targets-final-results.jsonl` 与
`/tmp/ryupy-m3_7-future-ignored-final-results.jsonl`。
