# Enum auto 赋值期合同

四组有限域 CP3.12 合同已通过真实 JIT 双 PIC 的 stdout/stderr/退出码精确验证：整数/布尔/负数/BigInt及别名历史、经导入认证的零参调用及赋值期读取、非负 Flag、warning record/ignore/error与失败保持旧公开绑定。

参数调用与顶层 marker 初始化尚不属于本轮 HIR 承诺；外部 CP imports_calls.py 保留实参先求值及 auto(value) 既有支持。不能把 CP 允许的 auto(7) 改写成应报错的正合同。模块 auto 导出重绑、混数值与 str/None 的生成、Flag 负历史、自定义生成器、tuple 等域外由独立守卫/外部原流覆盖，不混入这里的精确正 golden。runtime 的 marker 复用及 auto(value) 由 C# 合同单独验证。

上述范围已有产品 JIT 证据，真实 click 原入口仍独立验收，不能以本目录通过代替完整项目或标准库兼容。

成员 initializer 保持原有字面量/成员别名及新增零参 auto 域；Flag 另保既有有限位运算。不扩展任意算术：BigInt 用同值十进制字面量，赋值期 A+4 见证放在已支持的方法默认参数。首轮算术 initializer E0100 原流保留，不能称旧图通过。

Flag 组合位继续遵守既有约束：所有单独位必须先有命名成员，因此组合值 3 之前明确声明 TWO=2。未扩大 Flag 布局能力，缺失命名位的 freeze1b 原失败保留。

本轮仅验收上述有限域 JIT；Native 仍待 hosted 验证，不宣称完整 Enum 或 Native 通过。
