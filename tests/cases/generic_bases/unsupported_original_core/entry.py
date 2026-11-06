# ryupy: pending
# G2 单参数化基类、真实普通后代与终结覆盖；原源应直接用于未来差分。
from typing import Generic, TypeVar, get_origin, get_args
T = TypeVar('T')
U = TypeVar('U')
A = Generic[T]
events = []

def receiver():
    events.append('receiver')
    return Generic

def key():
    events.append('key')
    return T

def decorate(label):
    events.append('eval-' + label)
    def apply(cls):
        events.append('apply-' + label)
        print('decorator', label, cls.__orig_bases__[0] is A, cls.__parameters__ == (T,))
        return cls
    return apply

@decorate('outer')
@decorate('inner')
class Box(receiver()[key()]):
    events.append('body')
    print('body-orig', '__orig_bases__' in locals(), '__parameters__' in locals())
    __orig_bases__ = None
    __parameters__ = ('body',)
    def __new__(cls, value):
        print('new', cls.__name__, value)
        return object.__new__(cls)
    def __init__(self, value):
        print('init', type(self).__name__, hasattr(self, '__orig_class__'))
        self.value = value
    def read(self):
        return self.value
    def __getitem__(self, key):
        return self.value + key
print('events', events)
print('identity', type(Box) is type, Box.__base__ is Generic, Box.__bases__ == (Generic,))
print('mro', Box.__mro__ == (Box, Generic, object), Box.mro() == [Box, Generic, object])
print('metadata', Box.__orig_bases__[0] is A, Box.__parameters__[0] is T, Box.__module__)
print('origin', get_origin(Box), get_args(Box))
x = Box(7)
print('instance', type(x) is Box, isinstance(x, Generic), isinstance(x, Box), x.read(), x[2])
x.extra = 9
print('dict', x.__dict__, hasattr(x, '__orig_class__'))
class Child(Box):
    print('child-body', '__orig_bases__' in locals(), '__parameters__' in locals())
    __parameters__ = ('discard',)
print('child', Child.__parameters__, '__orig_bases__' in Child.__dict__, Child.__orig_bases__ is Box.__orig_bases__)
print('child-mro', Child.__mro__ == (Child, Box, Generic, object))
y = Child(8)
print('child-instance', type(y) is Child, isinstance(y, Box), y.read())
class Grandchild(Child):
    pass
print('grandchild', Grandchild.__parameters__, Grandchild.__orig_bases__ is Box.__orig_bases__)
class Saved(A):
    pass
print('saved', Saved.__orig_bases__[0] is A, Saved.__parameters__ == (T,))
class Foreign(Generic[T]):
    def __new__(cls):
        return 17
    def __init__(self):
        print('WRONG foreign init')
print('foreign', Foreign())
class Own(Child):
    __orig_bases__ = (Generic[U],)
print('own', Own.__parameters__ == (U,), Own.__orig_bases__[0] is Generic[U])
