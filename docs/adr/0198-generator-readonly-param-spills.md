# ADR-0198：生成器只读形参挂起写回消除

- 状态：已接受；有限默认，结构/JIT/NativeAOT/搬迁及完整门禁通过
- 日期：2026-11-15
- 关联：ADR-0182

## 决定

factory已初始化的Dynamic非cell形参，若全函数无Assign或Suspend.input定义且非隐藏槽，
在普通无EH、非trace product生成器挂起时省去重复ArraySet。恢复ArrayGet、非参数局部、
控制/异常槽和清帧不变。绑定不变对象的内容变化仍通过原引用可见。
旧活跃性实验已远端归档，从当前树撤下，不作为本实现依赖。

## 验收与取舍

9项专项、全MIR371/4、CP/JIT/NativeAOT及隐藏输入搬迁通过，完整make check退出0。
原始26项1warm5整体B/A几何耗时1.00102，generators本轮+0.35%；
后续1warm10重点为335.96→331.32ms（-1.38%），分配一致，样本重叠、收益较小。
保留closures重点墙钟+1.96%及全量其他不利值，接受此局部冗余消除；不宣称整体性能全面改善。
throw注入line0与生成器locals/隐式销毁边界独立登记，不扩展本次语义范围。
正确口径、完整门禁和报告纠正见[专项文档](../aot-generator-readonly-param-optimization.md)。
