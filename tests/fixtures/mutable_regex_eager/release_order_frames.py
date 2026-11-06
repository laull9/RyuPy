# ryupy: pending
"""阶段A候选：released-view错误、参数转换顺序与不删帧的完整用户异常链；未执行。"""
import re


MESSAGE = "expected string or bytes-like object, got 'memoryview'"


def expect_error(label, kind, message, action):
    try:
        action()
    except Exception as error:
        assert type(error) is kind and str(error) == message
        print(label, type(error).__name__, str(error))
        return error
    raise AssertionError('expected error: ' + label)


# Match不保view导出；已release后仅参与组重新acquire，未参与组和纯marks可读。
data = bytearray(b'ab')
view = memoryview(data)
p = re.compile(b'(?P<x>a)(?P<missing>z)?')
match = p.search(view)
assert match.string is view and match.re is p
view.release()
view.release()
assert match.string is view and match.re is p
assert match.span() == (0, 1) and match.start() == 0 and match.end() == 1
assert match.pos == 0 and match.endpos == 2
assert match.group(2) is None and match.group('missing') is None
assert match.span(2) == (-1, -1)
assert match.expand(b'literal') == b'literal' and match.expand(br'<\2>') == b'<>'
print('A2.released-metadata', match.span(), match.pos, match.endpos, match.group(2),
      match.expand(b'literal'), match.expand(br'<\2>'))
expect_error('A2.released-group', TypeError, MESSAGE, match.group)
expect_error('A2.released-named', TypeError, MESSAGE, lambda: match.group('x'))
expect_error('A2.released-groups', TypeError, MESSAGE, match.groups)
expect_error('A2.released-groupdict', TypeError, MESSAGE, match.groupdict)
expect_error('A2.released-expand', TypeError, MESSAGE, lambda: match.expand(br'<\1>'))
expect_error('A2.released-repr', TypeError, MESSAGE, lambda: repr(match))
expect_error('A2.released-search', TypeError, MESSAGE, lambda: p.search(view))
expect_error('A2.released-bad-group', IndexError, 'no such group', lambda: match.group(9))
data.append(33)
print('A2.released-unlocks', data)

# 成功的零长度组也是参与组；不能用返回空bytes的快路跳过已释放检查。
view = memoryview(b'a')
empty = re.search(b'()(z)?', view)
view.release()
assert empty.span() == (0, 0) and empty.span(1) == (0, 0) and empty.group(2) is None
expect_error('A2.released-empty-whole', TypeError, MESSAGE, empty.group)
expect_error('A2.released-empty-group', TypeError, MESSAGE, lambda: empty.group(1))
assert empty.expand(br'<\2>') == b'<>'
print('A2.released-empty-marks', empty.span(), empty.span(1), empty.group(2))

# 全部捕获都未参与时groups/groupdict的default须保身份，无需获取释放后的source。
view = memoryview(b'b')
missing = re.search(b'(?P<x>a)?', view)
view.release()
default = []
assert missing.groups(default)[0] is default
assert missing.groupdict(default)['x'] is default
print('A2.released-missing-default', missing.groups(), missing.groupdict())


events = []


class Index:
    def __init__(self, label, action, result):
        self.label = label
        self.action = action
        self.result = result

    def __index__(self):
        events.append(self.label)
        self.action()
        return self.result


def idle():
    pass


# pos先release也不能跳过endpos的转换；获取buffer必须晚于两次__index__。
for mode in ('match', 'search', 'fullmatch', 'findall'):
    data = bytearray(b'ab')
    view = memoryview(data)
    p = re.compile(b'a')
    bound = getattr(p, mode)
    events = []
    expect_error('A2.pos-release-' + mode, TypeError, MESSAGE,
                 lambda: bound(view, Index('pos', view.release, 0), Index('end', idle, 2)))
    assert events == ['pos', 'end']
    data.append(33)
    print('A2.pos-events-' + mode, events, data)


# 使用已实现的语法操作，不依赖bytearray尚无的显式__delitem__/__setitem__属性。
def shrink_data():
    del data[2:]


def change_first():
    data[0] = ord('X')


data = bytearray(b'abcd')
match = re.search(b'(bc)', data)
events = []
assert match.group(Index('group', shrink_data, 1)) == b'b'
assert events == ['group'] and data == bytearray(b'ab')
assert match.span() == (1, 3) and match.endpos == 4
print('A2.group-shrink', match.group(), match.span(), match.endpos, events)
view = memoryview(bytearray(b'a'))
match = re.search(b'(a)(z)?', view)
events = []
expect_error('A2.group-release', TypeError, MESSAGE,
             lambda: match.group(Index('release', view.release, 1)))
assert events == ['release'] and match.span() == (0, 1)
assert match.group(Index('missing', idle, 2)) is None
assert events == ['release', 'missing']
print('A2.group-release-marks', match.span(), events)
data = bytearray(b'ab')
match = re.search(b'(a)(b)', data)
events = []
actual = match.group(Index('first', idle, 1), Index('second', change_first, 2), 1)
assert actual == (b'a', b'b', b'X') and events == ['first', 'second']
print('A2.multigroup-interleaved', actual, events)

# 用户协议抛错保原异常身份，且pos失败不能执行end；普通绑定错误先于buffer消费。
marker = RuntimeError('same index marker')


class BadIndex:
    def __index__(self):
        events.append('bad')
        raise marker


view = memoryview(b'a')
p = re.compile(b'(a)')
match = p.search(view)
view.release()
events = []
try:
    p.search(view, BadIndex(), Index('must-not-run', idle, 1))
except RuntimeError as error:
    assert error is marker and events == ['bad']
else:
    raise AssertionError('pos exception lost')
print('A2.pos-exception-identity', events)
events = []
try:
    match.group(BadIndex())
except RuntimeError as error:
    assert error is marker and events == ['bad']
else:
    raise AssertionError('group exception lost')
print('A2.group-exception-identity', events)
events = []
expect_error('A2.bad-index-result', TypeError, '__index__ returned non-int (type str)',
             lambda: p.search(view, Index('invalid', idle, 'bad'), Index('must-not-run', idle, 1)))
assert events == ['invalid']
expect_error('A2.overflow-before-release', OverflowError, 'Python int too large to convert to C ssize_t',
             lambda: p.search(view, 1 << 100))
expect_error('A2.group-wide-before-release', IndexError, 'no such group', lambda: match.group(1 << 100))
print('A2.protocol-complete', True)

# 终尾故意exit1：第一段原用户__index__帧，第二段regex参与group的普通错误帧。
# 仅用Pattern/Match内建方法，避免stdlib re包装帧；两段完整链均须CP先确认。
frame_data = bytearray(b'a')
frame_view = memoryview(frame_data)
frame_pattern = re.compile(b'(a)')
frame_match = frame_pattern.search(frame_view)
frame_marker = RuntimeError('frame index marker')


class FrameIndex:
    def __index__(self):
        frame_view.release()
        raise frame_marker


def index_middle():
    return frame_pattern.search(frame_view, FrameIndex(), 1)


def index_outer():
    return index_middle()


def group_middle():
    return frame_match.group(1)


def group_outer():
    return group_middle()


print('A2.chain', 'next exception is intentional')
try:
    index_outer()
except RuntimeError as caught:
    assert caught is frame_marker
    frame_data.append(33)
    group_outer()
