# CP合法helper替换完整保留；F0冷消费明确NI，热缓存不得读取helper。
import typing as t
O = t.Optional
T = t.TypeVar('Warm')
a = O[T]
saved = t._type_check
calls = []
def changed(arg, message, *args, **kwargs):
    calls.append(arg)
    return float
t._type_check = changed
assert O[T] is a and calls == []
b = O[bytes]
assert b is float
assert len(calls) == 3
print('helper', len(calls), repr(b))
t._type_check = saved
