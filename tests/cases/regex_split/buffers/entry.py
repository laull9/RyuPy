# 二进制记录切分：连续B/c视图、捕获快照、先index再借用及返回后释放租约。
import re

for kind in ('bytearray', 'readonly', 'view', 'slice', 'char'):
    source = bytearray(b'a,b,c!')
    parent = None
    if kind == 'bytearray':
        subject = source
    elif kind == 'readonly':
        subject = memoryview(b'a,b,c!')
    elif kind == 'view':
        subject = memoryview(source)
    elif kind == 'slice':
        parent = memoryview(source)
        subject = parent[0:5]
    else:
        parent = memoryview(source)
        subject = parent.cast('c')
    expected = [b'a', b',', None, b'b', b',', None, b'c' if kind == 'slice' else b'c!']
    pieces = re.split(b'(,)(x)?', subject)
    assert pieces == expected
    source[0] = 90
    assert pieces == expected
    assert pieces[0] is not subject
    print('snapshot-' + kind, pieces, [type(x).__name__ for x in pieces])
    if kind != 'bytearray':
        subject.release()
    if parent is not None:
        parent.release()
    source.extend(b'?')
    print('resize-' + kind, bytes(source))

for source in (bytearray(b'abc'), memoryview(b'abc')):
    for count in (0, -1):
        pieces = re.compile(b',').split(source, count)
        assert pieces == [b'abc'] and type(pieces[0]) is bytes and pieces[0] is not source
        print('copy', type(source).__name__, count, pieces)

events = []
class Change:
    def __index__(self):
        events.append('index')
        source[1] = 58
        source.extend(b'!')
        return 1
source = bytearray(b'a,b')
pieces = re.compile(b',').split(source, Change())
assert pieces == [b'a:b!'] and events == ['index']
source.extend(b'?')
assert pieces == [b'a:b!']
print('before-scan', pieces, bytes(source), events)

class Release:
    def __index__(self):
        events.append('release')
        view.release()
        return 0
view = memoryview(b'a,b')
try:
    re.split(b',', view, Release())
except TypeError as error:
    assert str(error) == "expected string or bytes-like object, got 'memoryview'"
    print('released-before-scan', type(error).__name__, str(error), events)
else:
    assert False

source = bytearray(b'a,b,c')
view = memoryview(source)
try:
    re.compile(',').split(view)
except TypeError as error:
    assert str(error) == 'cannot use a string pattern on a bytes-like object'
    print('domain-error', type(error).__name__, str(error))
else:
    assert False
view.release()
source.extend(b'!')
print('error-release', bytes(source))
