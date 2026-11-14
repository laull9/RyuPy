# 两轮索引、序列元素转换与失败副作用，输出标签与原始流由CP-first驱动冻结。
events = []
class Index:
    def __init__(self, name, value):
        self.name = name
        self.value = value
    def __index__(self):
        events.append(self.name)
        return self.value
for kind in ('array', 'bytes', 'self', 'view', 'list', 'tuple', 'range'):
    a = bytearray(b'XY')
    if kind == 'array':
        source = bytearray(b'XY')
    elif kind == 'bytes':
        source = b'XY'
    elif kind == 'self':
        source = a
    elif kind == 'view':
        source = memoryview(b'XY')
    elif kind == 'list':
        source = [88, 89]
    elif kind == 'tuple':
        source = (88, 89)
    else:
        source = range(88, 90)
    events = []
    a[Index('start', 0):Index('stop', 2):Index('step', 1)] = source
    expected = ['step', 'start', 'stop']
    if kind != 'array':
        expected = expected + expected
    assert events == expected and a == b'XY'
    if kind == 'view':
        source.release()
    print('slice.round-' + kind, a, events)
class GrowItem:
    def __index__(self):
        events.append('item')
        a.append(33)
        return 88
a = bytearray(b'abc')
events = []
a[Index('start', -1):Index('stop', 99):Index('step', 1)] = [GrowItem()]
assert a == b'abcX'
assert events == ['step', 'start', 'stop', 'item', 'step', 'start', 'stop']
print('slice.item-between', a, events)
class GrowStop:
    def __index__(self):
        events.append('stop')
        a.append(33)
        return 999
for distinct in (False, True):
    a = bytearray(b'abc')
    events = []
    source = bytearray(b'X') if distinct else b'X'
    a[-1:GrowStop()] = source
    assert a == (b'abcX' if distinct else b'abc!X')
    assert events == (['stop'] if distinct else ['stop', 'stop'])
    print('slice.final-length', a, events)
a = bytearray(b'abc')
events = []
try:
    a[:] = [GrowItem(), 256]
except ValueError as error:
    assert a == b'abc!' and events == ['item']
    print('slice.source-failure', type(error).__name__, str(error), a, events)
else:
    raise AssertionError('invalid byte accepted')
marker = RuntimeError('source marker')
class FailItem:
    def __index__(self):
        raise marker
try:
    a[:] = (FailItem(),)
except RuntimeError as error:
    assert error is marker and a == b'abc!'
    print('slice.user-error', type(error).__name__, str(error), error is marker, a)
else:
    raise AssertionError('user exception lost')
a[:] = [False, True, 255]
assert a == b'\x00\x01\xff'
a[:] = range(65, 68)
assert a == b'ABC'
a[::-1] = (67, 66, 65)
assert a == b'ABC'
print('slice.sequences', a)
# list迭代必须保留当前长度与后续元素读取，不能先复制元素引用。
class MutateList:
    def __index__(self):
        if mode == 'append':
            source.append(90)
        elif mode == 'remove':
            source.pop()
        else:
            source[1] = 90
        return 88
for mode in ('append', 'remove', 'replace'):
    a = bytearray(b'abc')
    source = [MutateList(), 89]
    a[:] = source
    expected = b'XYZ' if mode == 'append' else (b'X' if mode == 'remove' else b'XZ')
    assert a == expected
    print('slice.live-list-' + mode, a)
# 第二轮失败不能提交赋值，但该轮回调自己的修改保留。
class FailSecond:
    def __init__(self):
        self.calls = 0
    def __index__(self):
        self.calls += 1
        if self.calls == 2:
            a.append(33)
            raise marker
        return 0
a = bytearray(b'abc')
index = FailSecond()
try:
    a[index:] = b'X'
except RuntimeError as error:
    assert error is marker and index.calls == 2 and a == b'abc!'
    print('slice.second-error', error is marker, index.calls, a)
else:
    raise AssertionError('second conversion exception lost')
# self在两轮之间取得快照，第二轮的用户写入不能污染源快照。
class MutateSecond:
    def __init__(self):
        self.calls = 0
    def __index__(self):
        self.calls += 1
        if self.calls == 2:
            a[0] = 90
        return 0
a = bytearray(b'abc')
index = MutateSecond()
a[index:] = a
assert a == b'abc' and index.calls == 2
print('slice.self-between', a, index.calls)
# 保留完整cause/context、用户__index__与调用层帧；失败后finally仍能观察并扩容目标。
class ChainItem:
    def __index__(self):
        raise LookupError('slice-item')
def slice_middle():
    trace_target[:] = [ChainItem()]
def slice_outer():
    slice_middle()
try:
    raise ValueError('slice-cause')
except ValueError as error:
    saved_cause = error
try:
    raise RuntimeError('slice-ambient') from saved_cause
except RuntimeError:
    print('slice.chain')
    trace_target = bytearray(b'abc')
    try:
        slice_outer()
    finally:
        assert trace_target == b'abc'
        trace_target.append(33)
        print('slice.finally', trace_target)
