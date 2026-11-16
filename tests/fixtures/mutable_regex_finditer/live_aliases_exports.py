# ryupy: pending
"""mutable finditer的实时扫描、marks活读、alias/readonly独立lease与原E嵌套；候选未执行。"""
import re


RELEASED = "expected string or bytes-like object, got 'memoryview'"
LOCKED = 'Existing exports of data: object cannot be re-sized'


def error(kind, message, call):
    try:
        call()
    except Exception as caught:
        assert type(caught) is kind and str(caught) == message
        return str(caught)
    raise AssertionError('missing expected error')


def drain(iterator):
    result = []
    for unused in range(18):
        try:
            result.append(next(iterator))
        except StopIteration:
            return result
    raise AssertionError('bounded iterator failed to exhaust')


# 原C的间隔写可见；后半的缩短/再增长是B+C组合，首次应用CP核定。
data = bytearray(b'aXa')
p = re.compile(br'(a)(z)?')
iterator = p.finditer(data)
first = next(iterator)
saved = first.group()
data[1] = ord('a')
second = next(iterator)
third = next(iterator)
assert [match.span() for match in [first, second, third]] == [(0, 1), (1, 2), (2, 3)]
assert [match.group() for match in [first, second, third]] == [b'a', b'a', b'a']
data[0] = ord('Z')
assert first.group() == b'Z' and saved == b'a'
assert error(BufferError, LOCKED, lambda: data.append(33)) == LOCKED
assert next(iterator, None) is None
data.append(33)
del data[1:]
assert first.group() == b'Z' and second.group() == b'' and third.group() == b''
assert second.group(2) is None and third.group(2) is None
data.extend(b'123')
assert [match.group() for match in [first, second, third]] == [b'Z', b'1', b'2']
for match in [first, second, third]:
    assert match.string is data and match.re is p and match.pos == 0 and match.endpos == 3
assert [match.span() for match in [first, second, third]] == [(0, 1), (1, 2), (2, 3)]
assert next(iterator, None) is None
print('I2.live-kept', saved, [match.group() for match in [first, second, third]],
      [match.span() for match in [first, second, third]], data)

# 原C空匹配同位置非空retry；成功尾空yield仍未结束。
data = bytearray(b'b')
iterator = re.finditer(b'|a', data)
first = next(iterator)
assert first.group() == b'' and first.span() == (0, 0)
data[0] = ord('a')
second = next(iterator)
third = next(iterator)
assert second.group() == b'a' and second.span() == (0, 1)
assert third.group() == b'' and third.span() == (1, 1)
assert error(BufferError, LOCKED, lambda: data.append(33)) == LOCKED
assert next(iterator, None) is None
data.append(33)
print('I2.empty-retry', [match.span() for match in [first, second, third]], data)

# 原C已扫prefix影响word boundary，不能只刷新当前位置之后的suffix。
data = bytearray(b'ab')
iterator = re.finditer(br'a|\bb', data)
first = next(iterator)
assert first.group() == b'a' and first.span() == (0, 1)
data[0] = ord(' ')
second = next(iterator)
assert second.group() == b'b' and second.span() == (1, 2)
assert first.group() == b' '
assert next(iterator, None) is None
data.append(33)
print('I2.prefix-refresh', first.group(), second.group(), second.span(), data)

# 原C的c alias写；root view自身持scanner export，alias不是第二个scanner。
data = bytearray(b'ab')
root = memoryview(data)
chars = root.cast('c')
iterator = re.finditer(b'a', root)
first = next(iterator)
assert first.string is root and first.span() == (0, 1)
chars[1] = b'a'
second = next(iterator)
assert second.string is root and second.group() == b'a' and second.span() == (1, 2)
assert error(BufferError, 'memoryview has 1 exported buffer', root.release)
assert next(iterator, None) is None
root.release()
chars.release()
data.append(33)
print('I2.alias-write', first.span(), second.span(), data)

# 原D的child相对offset和parent提前释放；last-yield仍锁是新增组合。
data = bytearray(b'!ab?')
parent = memoryview(data)
child = parent[1:3]
iterator = re.finditer(b'.', child)
parent.release()
assert error(BufferError, 'memoryview has 1 exported buffer', child.release)
first = next(iterator)
assert first.group() == b'a' and first.span() == (0, 1)
data[2] = ord('Z')
second = next(iterator)
assert second.group() == b'Z' and second.span() == (1, 2)
assert first.string is child and second.string is child
assert first.pos == 0 and first.endpos == 2 and second.endpos == 2
assert error(BufferError, 'memoryview has 1 exported buffer', child.release)
assert error(BufferError, LOCKED, lambda: data.append(33))
assert next(iterator, None) is None
child.release()
data.append(33)
print('I2.child-offset', first.span(), second.span(), second.endpos, data)

# 原D两个iterator分别拥有lease；readonly也保持完全相同的view二级计数。
p = re.compile(br'(a)(z)?')
for name, owner in [('mutable', bytearray(b'aa')), ('readonly', b'aa')]:
    view = memoryview(owner)
    one = p.finditer(view)
    two = p.finditer(view)
    assert error(BufferError, 'memoryview has 2 exported buffers', view.release)
    kept_one = drain(one)
    assert [match.span() for match in kept_one] == [(0, 1), (1, 2)]
    assert error(BufferError, 'memoryview has 1 exported buffer', view.release)
    kept_two = drain(two)
    assert [match.span() for match in kept_two] == [(0, 1), (1, 2)]
    view.release()
    assert next(one, None) is None and next(two, None) is None
    match = kept_two[0]
    assert match.string is view and match.re is p and match.pos == 0 and match.endpos == 2
    assert match.span() == (0, 1) and match.span(2) == (-1, -1)
    assert match.group(2) is None and match.expand(b'literal') == b'literal'
    assert match.expand(br'<\2>') == b'<>'
    assert error(TypeError, RELEASED, match.group) == RELEASED
    assert error(TypeError, RELEASED, lambda: repr(match)) == RELEASED
    assert error(TypeError, RELEASED, lambda: match.expand(br'<\1>')) == RELEASED
    if name == 'mutable': owner.append(33)
    print('I2.dual-' + name, [item.span() for item in kept_one],
          [item.span() for item in kept_two], match.group(2), match.expand(br'<\2>'))

# 原E外sub/search/内finditer事件完整恢复；内耗尽只能撤自己的lease。
data = bytearray(b'aT')
view = memoryview(data)
events = []
retained = []


def replacement(match):
    retained.append(match)
    events.append([match.string is view,
                   error(BufferError, 'memoryview has 1 exported buffer', view.release)])
    nested = re.search(b'a', view)
    events.append(['nested_search', nested.string is view,
                   error(BufferError, 'memoryview has 1 exported buffer', view.release)])
    inner = re.finditer(b'a', view)
    events.append(['nested_iterator',
                   error(BufferError, 'memoryview has 2 exported buffers', view.release)])
    found = drain(inner)
    events.append(['nested_done', [item.span() for item in found],
                   error(BufferError, 'memoryview has 1 exported buffer', view.release)])
    retained.extend(found)
    data[1] = ord('Z')
    return b'!'


assert re.sub(b'a', replacement, view, count=1) == b'!Z'
assert events == [[True, 'memoryview has 1 exported buffer'],
                  ['nested_search', True, 'memoryview has 1 exported buffer'],
                  ['nested_iterator', 'memoryview has 2 exported buffers'],
                  ['nested_done', [(0, 1)], 'memoryview has 1 exported buffer']]
assert len(retained) == 2 and retained[0].string is view and retained[1].string is view
view.release()
data.append(33)
print('I2.sub-nested-iterator', events, data)
print('I2.complete')
