# 通用文本与内建类型元数据合同

五组合同来自 CPython 3.12.13 设计取证，已通过产品双 PIC 的 stdout、stderr 与退出码精确差分。用户 shadow property 读写删除和普通 shadow 字段也已纳入验证。

- unicode_whitespace：Python 29 个空白码点共用于 isspace、默认 strip/split、Decimal 外围；非空白、代理项、显式 strip 字符集合、用户 override 与真实根描述符、保存绑定方法及非法调用。
- split_protocol：位置及关键词参数、__index__ 一次及先于 separator 校验的副作用、用户 override 与显式根调用、保存绑定方法、空 separator、空输入、极大 maxsplit 溢出和代理项/补充平面码点边界。
- builtin_class：精确内建 __class__ 与 type 一致；用户子类身份；用户 __class__ property 与显式类属性必须保留，不能用全局快捷返回覆盖 descriptor。

原合同仅检查异常类别的路径继续只检查类别，不对产品输出做归一化。真实项目验证独立，不因这五组 CP 通过宣称产品支持。

- unicode_alnum：Unicode 字母、Nd/Nl/No 数字类别、补充平面字母与反例；孤代理项用 chr 构造；直接/保存/根调用、子类 override 及非法参数。

- class_shadow_mutation：dict 用户 property setter/deleter 与继承、普通类字段被实例覆盖及删除、真实 type 身份保持。无 shadow 真元数据写入/删除守卫另列产品边界，不纳入本目录精确 CP 合同；未承诺 CP 允许的同布局 heap 类重绑。
