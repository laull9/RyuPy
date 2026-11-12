# 通用文本与内建类型元数据合同（pending）

三组合同来自仓库外 CP 设计取证，全部 pending，尚未运行产品。

- unicode_whitespace：Python 29 个空白码点共用于 isspace、默认 strip/split、Decimal 外围；非空白、代理项、显式 strip 字符集合、用户 override 与真实根描述符、保存绑定方法及非法调用。
- split_protocol：位置及关键词参数、__index__ 一次及先于 separator 校验的副作用、用户 override 与显式根调用、保存绑定方法、空 separator、空输入、极大 maxsplit 溢出和代理项/补充平面码点边界。
- builtin_class：精确内建 __class__ 与 type 一致；用户子类身份；用户 __class__ property 与显式类属性必须保留，不能用全局快捷返回覆盖 descriptor。

原合同仅检查异常类别的路径继续只检查类别，不对产品输出做归一化。真实项目验证独立，不因这三组 CP 通过宣称产品支持。
