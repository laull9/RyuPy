# ryupy: pending
# 未知 hook 的 CP 消费相位参考；不是要求 G2 实现任意 descriptor/回调。
from typing import Generic, TypeVar
T = TypeVar('T')
A = Generic[T]
class Descriptor:
    def __set_name__(self, owner, name):
        print('set-name', owner.__name__, name, '__orig_bases__' in owner.__dict__,
              '__parameters__' in owner.__dict__, owner.__orig_bases__[0] is A)
class Parent(A):
    print('parent-body')
    def __init_subclass__(cls):
        print('hook-before', cls.__name__, '__parameters__' in cls.__dict__, cls.__parameters__)
        super().__init_subclass__()
        print('hook-after', cls.__name__, cls.__parameters__)
print('parent-ready')
class Child(Parent):
    print('child-body')
    field = Descriptor()
print('child-ready')
class Direct(A):
    print('direct-body')
    field = Descriptor()
    __orig_bases__ = None
    __parameters__ = ('body',)
print('direct-ready', Direct.__parameters__)
class BadDescriptor:
    def __set_name__(self, owner, name):
        print('bad-set-name', owner.__name__, name)
        raise ValueError('descriptor wins')
Old = 'old'
try:
    class Old(Generic):
        print('bad-descriptor-body')
        field = BadDescriptor()
except Exception as error:
    print('descriptor-failure', type(error).__name__, str(error),
          type(error.__cause__).__name__, str(error.__cause__), Old)
class Skipper(A):
    def __init_subclass__(cls):
        print('skip-super', cls.__name__, '__parameters__' in cls.__dict__)
class Skipped(Skipper):
    print('skipped-body')
print('skipped', '__parameters__' in Skipped.__dict__, Skipped.__parameters__)
class Failure(A):
    def __init_subclass__(cls):
        print('failure-hook')
        raise ValueError('hook wins')
try:
    class Old(Failure):
        print('hook-failure-body')
except Exception as error:
    print('hook-failure', type(error).__name__, str(error), Old)
saved = Generic.__dict__['__init_subclass__']
def replacement(cls):
    print('replacement-hook', cls.__name__, '__parameters__' in cls.__dict__)
Generic.__init_subclass__ = classmethod(replacement)
print('replacement-stored')
try:
    class Replaced(A):
        print('replacement-body')
    print('replacement-result', '__parameters__' in Replaced.__dict__)
finally:
    Generic.__init_subclass__ = saved
class Restored(A):
    pass
print('restored', Restored.__parameters__ == (T,))
