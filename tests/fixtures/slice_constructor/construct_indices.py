# slice公开构造、身份、只读字段、绑定及任意精度indices的CP-first合同。
factory = slice
s = factory(7)
assert type(s) is slice and isinstance(s, slice)
assert callable(slice) and not callable(s) and issubclass(slice, object)
assert s.start is None and s.stop == 7 and s.step is None
assert slice(1, 7).start == 1 and slice(1, 7).step is None
assert repr(slice(1, 7, 2)) == 'slice(1, 7, 2)'
assert slice.__repr__(slice(1, 7, 2)) == 'slice(1, 7, 2)'
print('public.identity', type(s).__name__, repr(s))
class Keep:
    def __index__(self):
        raise AssertionError('constructor called index')
a = Keep()
s = slice(a, a, a)
assert s.start is a and s.stop is a and s.step is a
for name in ('start', 'stop', 'step'):
    try:
        setattr(s, name, 1)
    except AttributeError:
        pass
    else:
        raise AssertionError('writable field')
for field in (0, 1, 2):
    try:
        if field == 0:
            del s.start
        elif field == 1:
            del s.stop
        else:
            del s.step
    except AttributeError:
        pass
    else:
        raise AssertionError('deletable field')
print('public.fields', s.start is a, s.stop is a, s.step is a)
huge = 10 ** 100
for key, length, expected in (
    (slice(None), huge, (0, huge, 1)),
    (slice(None, None, -1), huge, (huge - 1, -1, -1)),
    (slice(-huge, huge, huge), 7, (0, 7, huge)),
    (slice(huge, -huge, -huge), 7, (6, -1, -huge)),
    (slice(None, -1, -1), 7, (6, 6, -1)),
    (slice(False, True, True), True, (0, 1, 1)),
    (slice(None, None, -1), 0, (-1, -1, -1)),
):
    assert key.indices(length) == expected
    assert slice.indices(key, length) == expected
print('public.wide', slice(None).indices(huge))
def error(label, operation):
    try:
        operation()
    except (TypeError, ValueError) as caught:
        print(label, type(caught).__name__, str(caught))
    else:
        raise AssertionError('invalid call accepted')
error('bind.constructor-empty', lambda: slice())
error('bind.constructor-many', lambda: slice(1, 2, 3, 4))
error('bind.constructor-keyword', lambda: slice(stop=1))
error('bind.constructor-mixed', lambda: slice(1, 2, 3, 4, stop=1))
error('bind.indices-empty', lambda: slice(1).indices())
error('bind.indices-many', lambda: slice(1).indices(1, 2))
error('bind.indices-keyword', lambda: slice(1).indices(length=1))
error('bind.root-empty', lambda: slice.indices())
error('bind.root-self', lambda: slice.indices(1, 2))
error('bind.root-length', lambda: slice.indices(slice(1)))
error('bind.root-many', lambda: slice.indices(slice(1), 1, 2))
error('bind.root-keyword', lambda: slice.indices(slice(1), length=1))
error('bind.length-none', lambda: slice(1).indices(None))
error('bind.length-negative', lambda: slice(None, None, 0).indices(-1))
error('bind.step-zero', lambda: slice('bad', 'bad', 0).indices(1))
for method in ('__eq__', '__ne__', '__lt__', '__le__', '__gt__', '__ge__'):
    assert getattr(slice(1), method)(1) is NotImplemented
    assert getattr(slice, method)(slice(1), 1) is NotImplemented
error('bind.eq-empty', lambda: slice(1).__eq__())
error('bind.eq-many', lambda: slice(1).__eq__(1, 2))
error('bind.eq-keyword', lambda: slice(1).__eq__(other=1))
error('bind.hash-many', lambda: slice(1).__hash__(1))
error('bind.repr-many', lambda: slice(1).__repr__(1))
print('public.bindings-complete')
# 保存方法保持原接收者；yield暂停参数求值前必须先保存真正的绑定方法。
original = slice(1, 9, 2)
saved_indices = original.indices
original = slice(2)
assert saved_indices(10) == (1, 9, 2)
root_indices = slice.indices
assert root_indices(slice(1, 9, 2), 10) == (1, 9, 2)
def paused_indices():
    return slice(1, 9, 2).indices((yield 'length'))
paused = paused_indices()
assert next(paused) == 'length'
try:
    paused.send(10)
except StopIteration as done:
    assert done.value == (1, 9, 2)
else:
    raise AssertionError('paused indices did not return')
print('public.saved-paused')
Base = slice
try:
    class Child(Base):
        pass
except TypeError as caught:
    assert str(caught) == "type 'slice' is not an acceptable base type"
    print('public.non-subclassable', type(caught).__name__, str(caught))
else:
    raise AssertionError('slice subclass accepted')
try:
    class DirectChild(slice):
        pass
except TypeError as caught:
    assert str(caught) == "type 'slice' is not an acceptable base type"
    print('public.direct-non-subclassable', type(caught).__name__, str(caught))
else:
    raise AssertionError('direct slice subclass accepted')
# 方法相等性由同一接收者身份和方法名决定，不能按slice值相等合并。
method_receiver = slice(3)
assert method_receiver.indices == method_receiver.indices
assert hash(method_receiver.indices) == hash(method_receiver.indices)
assert method_receiver.indices != slice(3).indices
assert method_receiver.__hash__ == method_receiver.__hash__
assert method_receiver.__hash__ != slice(3).__hash__
print('public.method-identity')
