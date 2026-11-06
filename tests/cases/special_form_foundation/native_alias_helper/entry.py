# 原生泛型别名的结构相等不消费typing._GenericAlias；热缓存必须保留。
import typing as t
a = list[int]
old = t.Optional[a]
# 驱逐运行时旧native Intern的驻留项，让b是真正不同对象。
for i in range(130):
    t.Literal[i]
b = list[int]
assert a is not b and a == b
saved = t._GenericAlias
t._GenericAlias = None
assert t.Optional[b] is old
print('native-warm-equal', True)
t._GenericAlias = saved
