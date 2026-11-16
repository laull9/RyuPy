# 实例额外属性可显式访问，但不能伪造类型的隐式特殊协议。
import typing as t
import math
T = t.TypeVar('T')
U = t.TypeVar('T')
events = []
def poison(*args):
    events.append('poison')
    raise RuntimeError('instance field must not implement protocol')
for name in ('__hash__', '__eq__', '__getitem__', '__bool__', '__iter__', '__int__', '__trunc__', '__index__', '__length_hint__', '__call__', '__len__', '__float__', '__repr__', '__str__'):
    setattr(T, name, poison)
    assert getattr(T, name) is poison
    try:
        getattr(T, name)()
    except RuntimeError as error:
        assert str(error) == 'instance field must not implement protocol'
    else:
        assert False
assert len(events) == 14
events.clear()
assert hash(T) == hash(T) and T != U and T == T
assert bool(T) and len({T, U}) == 2
assert repr(T) == '~T' and str(T) == '~T'
print('identity-protocols', True)
def reject(label, call):
    try:
        call()
    except TypeError as error:
        print(label, type(error).__name__, str(error))
    else:
        assert False
reject('call', lambda: T())
reject('getitem', lambda: T[int])
reject('iter', lambda: iter(T))
reject('int', lambda: int(T))
reject('trunc', lambda: math.trunc(T))
reject('len', lambda: len(T))
reject('float', lambda: float(T))
reject('isinstance', lambda: isinstance(1, T))
assert events == []
print('no-implicit-events', events)
