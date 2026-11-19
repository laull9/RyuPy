"""R4：bytes方法index次序/64位边界与完整用户异常链；Pattern方法避免stdlib包装帧。"""
import re


def expect_error(label, kind, message, call):
    try:
        call()
    except Exception as error:
        assert type(error) is kind and str(error) == message
        print(label, type(error).__name__, str(error))
        return error
    raise AssertionError('expected error: ' + label)


p = re.compile(b'a')
events = []
class Index:
    def __init__(self, name, value):
        self.name = name
        self.value = value

    def __index__(self):
        events.append(self.name)
        return self.value


expect_error('R4.index-order', TypeError, "expected string or bytes-like object, got 'int'",
             lambda: p.search(3, Index('pos', 0), Index('end', 2)))
assert events == ['pos', 'end']
print('R4.index-events', events)
events = []
expect_error('R4.count-order', re.error, 'invalid group reference 9 at position 1',
             lambda: p.sub(br'\9', 3, Index('count', 1)))
assert events == ['count']
print('R4.count-events', events)
events = []
expect_error('R4.bad-index', TypeError, '__index__ returned non-int (type str)',
             lambda: p.sub(br'\9', 3, Index('invalid-count', 'not-int')))
assert events == ['invalid-count']
print('R4.bad-index-events', events)
marker = RuntimeError('index-marker')
class BadIndex:
    def __index__(self):
        events.append('bad-pos')
        raise marker


events = []
try:
    p.search(3, BadIndex(), Index('must-not-run-end', 2))
except RuntimeError as error:
    assert error is marker and events == ['bad-pos']
    print('R4.index-identity', str(error), error is marker, events)
else:
    raise AssertionError('index error lost')
assert list(p.finditer(b'a', 1 << 40, 1 << 41)) == []
assert [m.span() for m in p.finditer(b'a', 0, 1 << 40)] == [(0, 1)]
assert p.sub(b'X', b'aa', 1 << 40) == b'XX'
assert p.sub(b'X', b'aa', -(1 << 40)) == b'aa'
print('R4.wide', list(p.finditer(b'a', 1 << 40, 1 << 41)),
      [m.span() for m in p.finditer(b'a', 0, 1 << 40)], p.sub(b'X', b'aa', 1 << 40),
      p.sub(b'X', b'aa', -(1 << 40)))
expect_error('R4.overflow-pos', OverflowError, 'Python int too large to convert to C ssize_t',
             lambda: p.search(b'a', 1 << 100))
expect_error('R4.overflow-count', OverflowError, 'Python int too large to convert to C ssize_t',
             lambda: p.sub(b'X', b'a', 1 << 100))

# 普通成功index保转换副作用，并在操作前冻结已转换pos/end/count。
events = []
m = p.search(b'za', Index('good-pos', 1), Index('good-end', 2))
assert m.span() == (1, 2) and events == ['good-pos', 'good-end']
events = []
assert p.sub(b'X', b'aa', Index('good-count', 1)) == b'Xa' and events == ['good-count']
print('R4.good-index', m.span(), events)

# module级except维持真实ambient；下面两段完整链由同应用首次CP守门，不能删除用户帧。
class ContextIndex:
    def __index__(self):
        raise RuntimeError('index context')


def index_middle():
    return p.search(b'a', ContextIndex())


def index_outer():
    return index_middle()


def callback_leaf():
    raise LookupError('callback chain failed')


def callback_middle(match):
    return callback_leaf()


def replace_middle():
    return p.sub(callback_middle, b'a')


def replace_outer():
    return replace_middle()


print('R4.chain', 'next exception is intentional')
try:
    index_outer()
except RuntimeError:
    replace_outer()
