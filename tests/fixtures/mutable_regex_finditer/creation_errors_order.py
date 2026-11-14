# ryupy: pending
"""finditer创建协议先序、消费者异常不关闭、完整新错误链；无backend/timeout/GC断言。"""
import re


RELEASED = "expected string or bytes-like object, got 'memoryview'"
events = []


def expect_error(label, kind, message, call):
    try:
        call()
    except Exception as error:
        assert type(error) is kind and str(error) == message
        print(label, type(error).__name__, str(error))
        return error
    raise AssertionError('missing expected error: ' + label)


def drain(iterator):
    result = []
    for unused in range(18):
        try:
            result.append(next(iterator))
        except StopIteration:
            return result
    raise AssertionError('bounded iterator failed to exhaust')


class Index:
    def __init__(self, name, action, value):
        self.name = name
        self.action = action
        self.value = value

    def __index__(self):
        events.append(self.name)
        self.action()
        return self.value


def idle():
    pass


p = re.compile(b'a')
saved = p.finditer
# 原G是search；finditer的release/反向范围新组合由本应用首次CP核定。
for route, pos, end in [('direct', 0, 2), ('saved', 2, 1)]:
    data = bytearray(b'aa')
    view = memoryview(data)
    events = []
    method = p.finditer if route == 'direct' else saved
    expect_error('I3.release-order-' + route, TypeError, RELEASED,
                 lambda: method(view, Index('pos', view.release, pos), Index('end', idle, end)))
    assert events == ['pos', 'end']
    data.append(33)
    print('I3.release-clean-' + route, events, data)

# 在Acquire之前允许resize；pos/end只转换一次，Match窗口按修改后长度冻结。
data = bytearray(b'a')
events = []


def grow():
    data.extend(b'aa')


iterator = saved(data, Index('pos', grow, 0), Index('end', idle, 99))
assert events == ['pos', 'end']
found = drain(iterator)
assert [match.span() for match in found] == [(0, 1), (1, 2), (2, 3)]
assert [(match.pos, match.endpos) for match in found] == [(0, 3), (0, 3), (0, 3)]
assert found[0].string is data
data.append(33)
print('I3.index-grow', events, [match.span() for match in found], data)

# 非int/ssize溢出在获取subject前失败；失败pos不能继续转换end。
for where, value, kind, message in [
        ('pos', 'bad', TypeError, '__index__ returned non-int (type str)'),
        ('end', 'bad', TypeError, '__index__ returned non-int (type str)'),
        ('pos-wide', 1 << 100, OverflowError, 'Python int too large to convert to C ssize_t'),
        ('end-wide', 1 << 100, OverflowError, 'Python int too large to convert to C ssize_t')]:
    data = bytearray(b'aa')
    view = memoryview(data)
    events = []
    is_pos = where in ('pos', 'pos-wide')
    start = Index('pos', idle, value if is_pos else 0)
    end = Index('end', idle, 2 if is_pos else value)
    expect_error('I3.bad-index-' + where, kind, message, lambda: saved(view, start, end))
    assert events == (['pos'] if is_pos else ['pos', 'end'])
    view.release()
    data.append(33)
    print('I3.bad-index-clean-' + where, events, data)

# 同时已released source与坏index仍先报index，不能提早检查view。
data = bytearray(b'aa')
view = memoryview(data)
view.release()
events = []
expect_error('I3.index-before-source', TypeError, '__index__ returned non-int (type str)',
             lambda: saved(view, Index('pos', idle, 'bad'), Index('end', idle, 2)))
assert events == ['pos']
data.append(33)
print('I3.index-before-clean', events, data)

# 每个独立用例使用新marker且只抛一次，避免旧context债污染identity测试。
for where in ('pos', 'end'):
    data = bytearray(b'aa')
    view = memoryview(data)
    events = []
    marker = LookupError('index-' + where)

    def fail():
        raise marker

    start = Index('pos', fail if where == 'pos' else idle, 0)
    end = Index('end', fail if where == 'end' else idle, 2)
    try:
        saved(view, start, end)
    except LookupError as caught:
        assert caught is marker
    else:
        raise AssertionError('index exception lost')
    assert events == (['pos'] if where == 'pos' else ['pos', 'end'])
    view.release()
    data.append(33)
    print('I3.index-identity-' + where, events, data)

# creation时拒绝released view；module错误只捕获比消息，不删包装File帧。
module_saved = re.finditer
for route in ('direct', 'saved', 'module', 'module_saved'):
    data = bytearray(b'aa')
    view = memoryview(data)
    view.release()
    if route == 'direct':
        action = lambda: p.finditer(view)
    elif route == 'saved':
        action = lambda: saved(view)
    elif route == 'module':
        action = lambda: re.finditer(b'a', view)
    else:
        action = lambda: module_saved(b'a', view)
    expect_error('I3.released-' + route, TypeError, RELEASED, action)
    data.append(33)
    assert bytes(data) == b'aa!'
expect_error('I3.bytes-text-domain', TypeError, 'cannot use a bytes pattern on a string-like object',
             lambda: p.finditer('aa'))
expect_error('I3.text-bytes-domain', TypeError, 'cannot use a string pattern on a bytes-like object',
             lambda: re.compile('a').finditer(bytearray(b'aa')))

# 外部消费者抛错不会自动关闭iterator；保留引用后必须显式继续到真正耗尽。
data = bytearray(b'aa')
view = memoryview(data)
iterator = p.finditer(view)
first = next(iterator)
marker = RuntimeError('consumer-body')
try:
    raise marker
except RuntimeError as caught:
    assert caught is marker
expect_error('I3.consumer-still-active', BufferError, 'memoryview has 1 exported buffer', view.release)
rest = drain(iterator)
assert first.span() == (0, 1) and [match.span() for match in rest] == [(1, 2)]
view.release()
data.append(33)
assert next(iterator, None) is None
print('I3.consumer-clean', first.span(), [match.span() for match in rest], data)
print('I3.protocol-complete')

# 最后新链的ambient在模块级建立，用户index失败发生在creation取得lease之前。
trace_data = bytearray(b'aa')
trace_view = memoryview(trace_data)
trace_pattern = re.compile(b'a')


def index_leaf():
    raise ValueError('finditer-index-body')


class TraceIndex:
    def __index__(self):
        return index_leaf()


def finditer_middle():
    return trace_pattern.finditer(trace_view, TraceIndex())


def finditer_outer():
    return finditer_middle()


try:
    raise LookupError('finditer-cause')
except LookupError as caught:
    saved_cause = caught

try:
    raise RuntimeError('finditer-ambient') from saved_cause
except RuntimeError:
    print('I3.chain')
    try:
        finditer_outer()
    finally:
        trace_view.release()
        trace_data.append(33)
        print('I3.finally-clean', trace_data)
