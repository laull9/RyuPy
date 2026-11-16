# C1新增alias必须保持公开hash复合关系；不比较随机化的hash数值。
import typing as t

T = t.TypeVar('T')
for label, value in [('int', int), ('none', None), ('variable', T), ('nested', t.List[T])]:
    alias = t.ClassVar[value]
    print(label, hash(alias) == hash((alias.__origin__, alias.__args__)))
print('none-equal-hash', t.ClassVar[None] == t.ClassVar[type(None)], hash(t.ClassVar[None]) == hash(t.ClassVar[type(None)]))
print('distinct-raw-identity', t.ClassVar[None] is t.ClassVar[type(None)])
