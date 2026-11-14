# ryupy: pending
# 真实CP正例：只做CP真值，不以NI期望替换合法数值协议/回调/状态。
from itertools import count

events = []


class Number:
    def __init__(self, value):
        self.value = value

    def __index__(self):
        events.append('index')
        raise AssertionError('count不应转换输入')

    def __add__(self, step):
        events.append(('add', self.value, step))
        return Number(self.value + step)

    def __repr__(self):
        return 'Number(' + str(self.value) + ')'


start = Number(10)
iterator = count(start, 3)
assert events == []
assert repr(iterator) == 'count(Number(10), 3)'
assert next(iterator) is start
assert events == [('add', 10, 3)]
assert next(iterator).value == 13
assert events == [('add', 10, 3), ('add', 13, 3)]
print('P1.custom.no.coercion')


class Step:
    def __float__(self):
        raise AssertionError('不能调用转换')

    def __radd__(self, left):
        events.append(('radd', left))
        return left + 4


events.clear()
iterator = count(1, Step())
assert next(iterator) == 1
assert events == [('radd', 1)]
assert next(iterator) == 5
print('P1.reflected.add')


class ChangesType:
    def __int__(self):
        raise AssertionError('不能调用转换')

    def __add__(self, step):
        return 'changed'


start = ChangesType()
iterator = count(start, 2)
assert next(iterator) is start
assert repr(iterator) == "count('changed', 2)"
try:
    next(iterator)
    assert False
except TypeError as error:
    assert str(error) == 'can only concatenate str (not "int") to str'
assert repr(iterator) == "count('changed', 2)"
print('P1.add.result.not.revalidated')

failure = ValueError('same error')


class Fails:
    def __index__(self):
        return 1

    def __add__(self, step):
        events.append('failure')
        raise failure


events.clear()
iterator = count(Fails(), 1)
for _ in range(2):
    try:
        next(iterator)
        assert False
    except ValueError as error:
        assert error is failure
assert events == ['failure', 'failure']
print('P1.error.retry.same.state')


class Pauses:
    def __init__(self):
        self.calls = 0

    def __index__(self):
        return 1

    def __add__(self, other):
        self.calls += 1
        if self.calls == 1:
            raise StopIteration('one call only')
        return 7


start = Pauses()
iterator = count(start)
sentinel = object()
assert next(iterator, sentinel) is sentinel
assert next(iterator) is start
assert next(iterator) == 7
print('P1.stop.does.not.close')

# 重入原尾例已隔离：初次完整源CP进程SIGSEGV，未取得可靠退出定位，不再执行。
