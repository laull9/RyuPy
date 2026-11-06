# 全新顺序合同：一次base求值、类体写入、alias原身份与装饰完成；不用locals反射。
from typing import Generic, TypeVar
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
        assert cls.__orig_bases__[0] is A
        assert cls.__parameters__ == (T,)
        events.append('apply-' + label)
        return cls
    return apply
@decorate('outer')
@decorate('inner')
class Defined(receiver()[key()]):
    events.append('body')
    __orig_bases__ = None
    __parameters__ = ('body',)
assert events == ['eval-outer', 'eval-inner', 'receiver', 'key', 'body', 'apply-inner', 'apply-outer']
class Overwrite(A):
    __orig_bases__ = (Generic,)
assert Overwrite.__orig_bases__[0] is A
class Pair(Generic[T, U]):
    pass
assert Pair.__parameters__[0] is T and Pair.__parameters__[1] is U
print('definition-order', events)
print('body-overwrite-pair', True)
