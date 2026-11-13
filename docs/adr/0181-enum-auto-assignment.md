# ADR-0181：Enum auto 的赋值期生成与成员发布

- 状态：有限域 JIT/Native 合同、独审与完整回归通过；性能回退风险未排除
- 日期：2026-11-12

## 决策

真实 click 需要 enum.auto，采用真实导出对象及经认证导入的零参调用，不按包名/源码位置猜测。
生成发生于成员赋值期，后续成员别名和方法默认参数必须读到已解析值；冻结成员名单，最终成功后才发布公开类绑定。不能把生成延后到 Finalize，更不能让 warning-as-error 暴露半成品。

默认 Enum 的 int/bool/负数/BigInt 历史遵循 CP3.12 的最大值加一及真实 DeprecationWarning；全 str/None 历史生成 1 并警告。警告 record/ignore/error 共用现有协议。混数值与 str/None 生成仍明确拒绝，不静默近似。
Flag 保非负历史与下一位生成，组合位须已有独立命名成员。不扩展原成员 initializer：字面量/此前成员别名及新增零参 auto，Flag 另保既有有限位运算。

## 范围与证据

四组 tests/cases/enum_auto 在最终 CLI 下 CP3.12.13/JIT 双 PIC 逐字通过，包含类间独立序列、别名历史、方法默认参数读取和失败旧绑定。首轮算术 initializer 与缺失命名位的拒绝原流保留；修正 fixture 到已声明域不算旧图通过。

Python 非零参 auto、顶层 marker 初始化、自定义生成器、tuple、负 Flag、模块 auto 导出重绑均不在本轮 Python 准入承诺；runtime marker 复用和 auto(value) 的 C# 验证不扩大 HIR 范围。CP 对部分域已有支持，产品限制必须明示，不能改写 CP 语义。
Native 有限组合图已在 hosted 实际通过（见后续实证）；真实 click 原入口及项目保护独立验收，不以局部组合图代替整库兼容。未作性能提升声明。

## 后续实证（2026-11-12）

CI `43229286565` 在同候选上通过有限组合图的真正 Linux x64 ELF 原位与搬迁、CP/双 PIC 和双完整回归；整 CI 仍有旧 metadata 边界失败。该证据不扩大上述语义域或平台承诺。两批 JIT 配对性能保留全部样本，oop 的回退风险尚未排除，不能据功能通过宣称性能无回归。
