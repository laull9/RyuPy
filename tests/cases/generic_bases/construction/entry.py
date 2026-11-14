# 全新G2普通构造合同；不依赖实例字典反射，不替换原core证据。
from typing import Generic, TypeVar, get_origin, get_args
T = TypeVar('T')
A = Generic[T]
events = []
class Box(Generic[T]):
    def __new__(cls, value):
        events.append(('new', cls.__name__, value))
        return object.__new__(cls)
    def __init__(self, value):
        events.append(('init', type(self).__name__, value))
        self.value = value
    def read(self):
        return self.value
    def __getitem__(self, key):
        return self.value + key
class Child(Box):
    pass
class Saved(A):
    pass
x = Box(7)
y = Child(8)
assert type(x) is Box and type(y) is Child
assert isinstance(y, Generic) and isinstance(y, Box)
assert issubclass(Child, Generic)
assert x.read() == 7 and x[2] == 9 and y.read() == 8
x.extra = 19
assert x.extra == 19
assert Box.__base__ is Generic and Child.__base__ is Box
assert Box.__parameters__ == (T,) and Child.__parameters__ == ()
assert Child.__orig_bases__ is Box.__orig_bases__
assert Saved.__orig_bases__[0] is A
assert get_origin(Box) is None and get_args(Box) == ()
assert Box.__module__ == '__main__'
assert Child.__mro__ == (Child, Box, Generic, object)
class Foreign(Generic[T]):
    def __new__(cls):
        return 17
    def __init__(self):
        raise ValueError('foreign init must not run')
assert Foreign() == 17
print('construction', events)
print('identity-fields-methods', True)
