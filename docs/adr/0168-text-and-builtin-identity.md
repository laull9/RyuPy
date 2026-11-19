# ADR-0168：共享 Python 空白语义与精确内建载体类型属性

- 状态：独立竖切实施中，尚未验收
- 日期：2026-11-12

## 真实失败与设计

真实 toml 进入解析后需要 str.isspace，编码时读取普通 dict.__class__。
这两项是通用语言/内建协议，不以 toml 源码、类名或调用位置放行。

Python 空白判定集中在内部 helper，供 isspace、缺省 strip/lstrip/rstrip、
缺省 split 和 Decimal 构造共同消费。Python 接受 U+001C–001F，不能直接
以 .NET Trim/IsWhiteSpace 替代；空串的 isspace 必须为 False。显式 chars
剥离保持原字符集合语义，不被缺省空白规则改变。

split 实现完整 sep=None/maxsplit=-1 常用合同，包括关键字绑定、maxsplit
的 __index__ 求值顺序/次数、溢出、空 separator 与 maxsplit=0 的余串。
不能继续静默忽略第二参数。码点边界须保留，不能让孤代理项匹配补充码点
的 UTF16 半部分。普通调用、保存方法、显式 str 根描述符及子类路径共用
同一 binder；不借本切扩展 rsplit、编码器或其它无关字符串 API。

精确内建载体的 __class__ 返回现有 TypeOf 的真实驻留类型，列明载体种类，
不按任意 TypeName 文本猜测。既有 PyInstance/native/用户类的属性协议先
消费，用户 descriptor、属性覆写和真实子类身份不被新快捷分支截获。

## 验证边界

固定 CP 合同覆盖完整 29 种空白、Unicode/孤代理项反例、参数副作用、
root/bound/override 路径、类型身份及用户 __class__ property。
其后须跑双 PIC、完整 C# 与 make check、原真实 toml 四图以及旧项目保护。
本切不以通用单测代替真实入口，也不以更多局部通过宣称整个标准库完成。
无新增公开运行时 ABI，性能未测量，不称为零成本变化。
