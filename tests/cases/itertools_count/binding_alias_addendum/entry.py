# 独立边界复验：确知非数值、绑定失败先序、方法错误不推进与保存原生根。
import itertools
from itertools import count


def fail(call, message):
    try:
        call()
        assert False
    except TypeError as error:
        assert str(error) == message


for invalid in [bytearray(b'4'), frozenset(), slice(0, 3), range(3), {}.keys(), int]:
    fail(lambda: count(invalid), 'a number is required')
    fail(lambda: count(0, invalid), 'a number is required')
fail(lambda: count(None, 1, step=2), 'count() takes at most 2 arguments (3 given)')
fail(lambda: count(bad=1, start=None, step=None), 'count() takes at most 2 keyword arguments (3 given)')
fail(lambda: count(None, start=2), "argument for count() given by name ('start') and position (1)")
fail(lambda: count(step=None, other=2), "'other' is an invalid keyword argument for count()")
print('R2.binding.precedence')

iterator = count(1000, -2)
for method in [iterator.__iter__, iterator.__next__]:
    for operation in [lambda: method(1), lambda: method(bad=1)]:
        try:
            operation()
            assert False
        except TypeError:
            pass
        assert repr(iterator) == 'count(1000, -2)'
for name in ['start', 'step', '__length_hint__']:
    try:
        getattr(iterator, name)
        assert False
    except AttributeError as error:
        assert str(error) == "'itertools.count' object has no attribute '" + name + "'"
assert next(iterator) == 1000
print('R2.method.noadvance')

root = count
try:
    itertools.count = object()
    assert type(iterator) is root
    assert iterator.__class__ is root
    assert isinstance(iterator, root)
    another = root(5, -2)
    assert type(another) is root
    assert next(another) == 5
    assert next(iterator) == 998
finally:
    itertools.count = root
assert itertools.count is root
print('R2.saved.root')
