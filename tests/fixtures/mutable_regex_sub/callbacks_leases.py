# ryupy: pending
"""原E mutation/活读/租约；嵌套sub与空匹配刷新是独立CP-first新组合，不调用finditer。"""
import re


RESIZE = 'Existing exports of data: object cannot be re-sized'


def blocked(action, message):
    try:
        action()
    except BufferError as error:
        assert type(error) is BufferError and str(error) == message
        return str(error)
    raise AssertionError('expected BufferError: ' + message)


data = bytearray(b'paxaT')
events = []
kept = []
old_groups = []
p = re.compile(b'a')


def replacement(match):
    kept.append(match)
    old_groups.append(match.group())
    events.append(('before', match.span(), match.group(), match.string is data))
    assert match.re is p and match.pos == 0 and match.endpos == 5
    data[0] = ord('Q')
    data[1] = ord('Z')
    data[2] = ord('a')
    data[3] = ord('b')
    data[4] = ord('Y')
    events.append(('after', match.group(), blocked(lambda: data.append(33), RESIZE)))
    return b'#'


assert re.subn(b'a', replacement, data) == (b'p##bY', 2)
assert events == [('before', (1, 2), b'a', True), ('after', b'Z', RESIZE),
                  ('before', (2, 3), b'a', True), ('after', b'a', RESIZE)]
assert old_groups == [b'a', b'a']
assert [match.group() for match in kept] == [b'Z', b'a']
assert [match.span() for match in kept] == [(1, 2), (2, 3)]
assert [match.endpos for match in kept] == [5, 5]
print('S2.mutation', (b'p##bY', 2), events)
data.append(33)
assert all(match.string is data for match in kept)
assert [match.group() for match in kept] == [b'Z', b'a']
print('S2.retained', [match.group() for match in kept], [match.span() for match in kept], data)

# 外层sub的具体view租约独立于嵌套search/sub；readonly view同样阻止release。
for name, source in [('mutable', bytearray(b'aT')), ('readonly', b'aT')]:
    view = memoryview(source)
    events = []
    kept = []

    def inner(match):
        kept.append(match)
        assert match.string is view and match.span() == (0, 1)
        events.append(('inner', blocked(view.release, 'memoryview has 2 exported buffers')))
        return b'I'

    def outer(match):
        kept.append(match)
        assert match.string is view and match.span() == (0, 1)
        events.append(('outer', blocked(view.release, 'memoryview has 1 exported buffer')))
        nested = re.search(b'a', view)
        assert nested.string is view and nested.group() == b'a'
        events.append(('after-search', blocked(view.release, 'memoryview has 1 exported buffer')))
        assert re.sub(b'a', inner, view, count=1) == b'IT'
        events.append(('after-sub', blocked(view.release, 'memoryview has 1 exported buffer')))
        if name == 'mutable':
            source[1] = ord('Z')
            assert blocked(lambda: source.append(33), RESIZE) == RESIZE
        return b'!'

    expected = b'!Z' if name == 'mutable' else b'!T'
    assert re.subn(b'a', outer, view, count=1) == (expected, 1)
    assert [event[0] for event in events] == ['outer', 'after-search', 'inner', 'after-sub']
    assert [match.endpos for match in kept] == [2, 2]
    view.release()
    if name == 'mutable': source.append(33)
    print('S2.nested-' + name, expected, events, [match.span() for match in kept])

# child仅锁自己的二级导出；parent/别名释放不撤child租约，B/c同长写立即可见。
data = bytearray(b'!aTa?')
parent = memoryview(data)
chars = parent.cast('c')
child = parent[1:4]
events = []
kept = []


def child_replace(match):
    kept.append(match)
    assert match.string is child and match.pos == 0 and match.endpos == 3
    events.append((match.span(), match.group()))
    if len(kept) == 1:
        parent.release()
        chars[2] = b'a'
        child[2] = ord('b')
        chars.release()
    assert blocked(child.release, 'memoryview has 1 exported buffer') == 'memoryview has 1 exported buffer'
    assert blocked(lambda: data.append(33), RESIZE) == RESIZE
    return b'#'


assert re.subn(b'a', child_replace, child) == (b'##b', 2)
assert events == [((0, 1), b'a'), ((1, 2), b'a')]
assert [match.span() for match in kept] == [(0, 1), (1, 2)]
assert [match.group() for match in kept] == [b'a', b'a']
child.release()
data.append(33)
assert data == bytearray(b'!aab?!')
print('S2.child-alias', events, data, [match.endpos for match in kept])

# 原C是finditer实证；以下sub结果是待首次应用CP验证的新组合。
data = bytearray(b'b')
seen = []


def empty_retry(match):
    seen.append((match.span(), match.group()))
    if len(seen) == 1:
        data[0] = ord('a')
    return b'-'


assert re.subn(b'|a', empty_retry, data) == (b'---', 3)
assert seen == [((0, 0), b''), ((0, 1), b'a'), ((1, 1), b'')]
data.append(33)
print('S2.empty-retry', seen, data)
data = bytearray(b'ab')
seen = []
kept = []


def prefix_change(match):
    kept.append(match)
    seen.append((match.span(), match.group()))
    if len(seen) == 1:
        data[0] = ord(' ')
    return b'!'


assert re.subn(br'a|\bb', prefix_change, data) == (b'!!', 2)
assert seen == [((0, 1), b'a'), ((1, 2), b'b')]
assert kept[0].group() == b' ' and kept[0].span() == (0, 1)
data.append(33)
print('S2.prefix-refresh', seen, kept[0].group(), data)
print('S2.complete', True)
