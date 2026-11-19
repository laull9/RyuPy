# count 有限数值语义应用。
# count 公开应用一：真实类型、完整构造绑定、参数求值顺序与保存根。
import itertools
from itertools import count


def take(iterator, size):
    result = []
    for _ in range(size):
        result.append(next(iterator))
    return result


root = count
assert type(root) is type
assert root.__name__ == 'count'
assert root.__qualname__ == 'count'
assert root.__module__ == 'itertools'
assert repr(root) == "<class 'itertools.count'>"
assert take(root(), 4) == [0, 1, 2, 3]
assert take(root(10), 3) == [10, 11, 12]
assert take(root(step=2), 3) == [0, 2, 4]
assert take(root(step=-2, start=5), 4) == [5, 3, 1, -1]
assert take(root(5, step=0), 3) == [5, 5, 5]
assert take(root(*[5], **{'step': 2}), 3) == [5, 7, 9]
value = root(20, 2)
assert type(value) is root and value.__class__ is root
assert isinstance(value, root) and isinstance(value, object)
assert issubclass(root, object)
try:
    itertools.count = lambda: 'replacement'
    assert itertools.count() == 'replacement'
    assert type(value) is root and value.__class__ is root
    assert isinstance(value, root)
    assert next(root(30)) == 30
finally:
    itertools.count = root
print('C1.public.root')

events = []


def arg(label, value):
    events.append(label)
    return value


def fail(call, message):
    try:
        call()
        assert False
    except TypeError as error:
        assert str(error) == message


fail(lambda: root(arg('a', 1), arg('b', 2), bad=arg('c', 3)),
     'count() takes at most 2 arguments (3 given)')
assert events == ['a', 'b', 'c']
fail(lambda: root(1, start=2), "argument for count() given by name ('start') and position (1)")
fail(lambda: root(1, 2, 3), 'count() takes at most 2 arguments (3 given)')
fail(lambda: root(1, 2, 3, bad=4), 'count() takes at most 2 arguments (4 given)')
fail(lambda: root(1, start=2, bad=3), 'count() takes at most 2 arguments (3 given)')
fail(lambda: root(None, bad=3), "'bad' is an invalid keyword argument for count()")
fail(lambda: root(step=None, bad=3), "'bad' is an invalid keyword argument for count()")
fail(lambda: root(bad=3, step=None), "'bad' is an invalid keyword argument for count()")
fail(lambda: root(bad=3, worse=4), "'bad' is an invalid keyword argument for count()")
fail(lambda: root(worse=4, bad=3), "'worse' is an invalid keyword argument for count()")
for invalid in [None, '4', b'4', [], (), {}, set(), object()]:
    fail(lambda: root(invalid), 'a number is required')
    fail(lambda: root(0, invalid), 'a number is required')
print('C1.binding.order')
