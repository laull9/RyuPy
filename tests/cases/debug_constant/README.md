# __debug__ 固定非优化模式合同

三组 CPython 3.12.13 正合同已通过真实 JIT 双 PIC 精确 stdout/stderr/退出码验证及独立审查：各词法作用域/默认参数、assert 条件与消息副作用、导入模块反射注入不遮蔽直接读取。.NET Release 不是 Python -O。

固定非 -O；Native 尚待 hosted 验证，完整回归另行执行，不宣称所有优化模式支持。
非法绑定作为外部 CP compile 参考提供给 HIR 诊断测试，不在正 baseline 中夹入非法 Python。CP 禁止 obj.__debug__ 点赋值，但允许属性删除与增强赋值，不能一概拒绝；普通名字的赋值/删除仍非法。
动态注入仅采用已验证模块 setattr/getattr 路径，不引入 globals() 新能力。
见 [ADR0170](../../../docs/adr/0170-debug-language-constant.md)。
