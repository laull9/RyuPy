# 已知 str 的运行时属性读取

chardet 5.2.0 `version.py:9` 的 `VERSION = __version__.split('.')` 暴露了类型检查误拒：
接收者由前一行字面量推导为 Str，但属性检查仅接受 Class/Dynamic，尽管运行时已有 split。

本切只将 `Ty::Str` 的**属性读取结果**标为 Dynamic，交既有绑定方法/属性协议判断；
不复制 C# 方法白名单、不新添 split API、不对其它 primitive 作相同扩展。
不存在的属性在实际读取时抛 AttributeError，实际接收者与标注不同时仍调用用户协议。

dataclass 的字段静态检查、frozen/赋值路径及其它类型错误保持原状。现有函数级推导模式
会把使用该动态属性的所属字符串函数降级；不宣称保留该函数全部强类型局部槽。
独立、不访问属性的数值函数仍保持 Static、Int 参数/返回值和无 Dynamic 的表达式类型，
由 `primitive_attribute_fallback_keeps_unrelated_numeric_function_static` 结构测试锁定。

没有新增运行时分配机制或 ABI；方法调用继续使用已有 DynCallMethod/PIC 分派，
一等方法读取使用已有绑定对象。此修复为正确性与已有能力可达性，不承诺加速。

验证：专门结构测试与全部 16 项类型测试、workspace clippy 已通过；
真实差分 `m3_7_primitive_attributes.py` 已 unexpected-pass 并转正，独立只读审查通过。
chardet 定向已完成编译，但运行失败：`ImportError: cannot import name 'Flag' from 'enum'`。
结果 `/tmp/ryupy-str-attributes-chardet.jsonl`，native 0/1，不增加项目通过率。
日志前缀 `/tmp/ryupy-str-attributes-`。本切不意味着 chardet 已通过或整 M3.7 完成。
