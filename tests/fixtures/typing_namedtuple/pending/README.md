# 真实CP正例pending（未执行）

extended_contract.py.pending不注册任何active driver，也不以catch NI替代正确行为。此新组合未执行CP/JIT。

- 工厂重绑委托：原reference-tail第13组已实证；本文件换名组合未单跑。
- 用户annotation比较：原tail第14组实证8对eq/bool；本文件直接返回False是新组合，待CP先验。
- str子类：固定typing.py _type_convert/ForwardRef源码；本文件尚未参考执行。
- 显式mro_entries：固定typing.py尾部真实返回隐藏NamedTupleMeta对象；Ryu首切明确NI，不能返回PyType替身。原reference12只测隐式类路径，不声称跑过本例。
- __call__：原reference06实证。生成类继承是新的真实CP正例，待参考；不能写成Python禁止。
- Generic、多继承、get_type_hints、ForwardRef code/复杂表达式：固定typing.py合同与原reference08/10部分锚点，本组合未运行；不扩大active。

另外保持文档边界：特殊repr/hash/len、classcell/super、任意descriptor、Unicode/复杂SyntaxError、复杂tuple repr错误、函数内/嵌套class、late类修改。未新增这些代码，不把“未准备”记成完成。

class显式__module__=None也pending：CP会触发collections内部caller来源，不能保None或猜typing；active只用非None对象与真实globals来源。

function_class_metadata.py.pending锁真实函数的__class__。父报告首B2内部Get(root,'__class__')失败，而TypeOf(root)真function已支持；这是通用PyFunction.GetMetadata既有属性缺口，不是本切新功能回归。原失败完整C#源由classowner私有归档；本新增Python组合未执行CP/JIT。不得以该pending否认或删除type(NamedTuple)已有真实类型身份合同。
