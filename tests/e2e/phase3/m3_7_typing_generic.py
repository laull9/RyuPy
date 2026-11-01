# ryupy: pending
# Generic 基类必须保留真实身份与参数，不能静默替换为 object。
from typing import Generic, TypeVar, get_args, get_origin

T = TypeVar('T')
other_t = TypeVar('T')
print(T.__name__, T is not other_t, T == other_t)
print(T.__constraints__, T.__bound__, T.__covariant__, T.__contravariant__)
B = TypeVar('B', bound=int)
C = TypeVar('C', int, str)
print(B.__bound__ is int, C.__constraints__ == (int, str))

base = Generic[T]
print(get_origin(base) is Generic, get_args(base) == (T,))
print(base.__parameters__ == (T,), base.__mro_entries__((base,)) == (Generic,))


class Box(Generic[T]):
    def __init__(self, value):
        self.value = value
        self.had_original_type = hasattr(self, '__orig_class__')


print(Box.__bases__ == (Generic,), issubclass(Box, Generic))
print(Box.__orig_bases__ == (Generic[T],), Box.__parameters__ == (T,))
alias = Box[int]
print(get_origin(alias) is Box, get_args(alias) == (int,), alias.__parameters__)
instance = alias(7)
print(type(instance) is Box, instance.value, instance.had_original_type)
print(instance.__orig_class__ == alias, isinstance(instance, Generic))


class Child(Box[T]):
    pass


print(Child.__bases__ == (Box,), issubclass(Child, Box))
print(Child.__parameters__ == (T,), Child.__orig_bases__ == (Box[T],))
print(get_origin(Child[str]) is Child, get_args(Child[str]) == (str,))

for value in [42, (T, T), ()]:
    try:
        Generic[value]
    except TypeError:
        print('Generic 参数错误')
for value in [(), (int, str)]:
    try:
        Box[value]
    except TypeError:
        print('实参数量错误')
try:
    isinstance(instance, Box[int])
except TypeError:
    print('参数化别名不能直接类型检查')
