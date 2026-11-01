# Ellipsis源码字面量、内建名字和typing参数必须指向同一真实单例。
from typing import Tuple, get_args
value = ...
print(value is Ellipsis, repr(value), str(value), bool(value))
print(type(value).__name__, type(value).__module__)
print(type(value)() is Ellipsis, isinstance(value, type(Ellipsis)))
print(callable(value), callable(type(value)))
print(hash(value) == hash(Ellipsis), {...: 'ellipsis'}[Ellipsis])
print(value == Ellipsis, value is not None)
annotation = Tuple[int, ...]
print(get_args(annotation)[0] is int, get_args(annotation)[1] is Ellipsis)
# 源码字面量不是普通名字查找；同名用户绑定不能替换它。
Ellipsis = 'shadowed'
print(Ellipsis, value is ..., type(value)() is ...)
