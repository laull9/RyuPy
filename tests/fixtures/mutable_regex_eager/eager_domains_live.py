# ryupy: pending
"""阶段A候选：六种载体公开eager消费、Match身份/活读和固定marks；未执行。"""
import re


def match_row(match, pattern, source, span, endpos, captured):
    assert type(match) is re.Match and isinstance(match, re.Match)
    assert match.re is pattern and match.string is source
    assert type(match.group()) is bytes and match.group() == captured
    assert match.span() == span and match.start() == span[0] and match.end() == span[1]
    assert match.pos == 0 and match.endpos == endpos
    return (match.group(), match.groups(), match.groupdict(), match.span(), match.pos, match.endpos)


# 同一pattern在module、Pattern和保存的bound方法入口消费原对象，不做bytes(source)适配。
data = bytearray(b'!a\x80b?')
root = memoryview(data)
chars = root.cast('c')
child = root[1:4]
readonly = memoryview(b'!a\x80b?')
pattern = b'(?P<x>a)(\x80)(z)?'
p = re.compile(pattern)
whole = re.compile(b'.*')
carriers = [('bytes', b'!a\x80b?', b'!a\x80b?', (1, 3)),
            ('bytearray', data, b'!a\x80b?', (1, 3)),
            ('view_B', root, b'!a\x80b?', (1, 3)),
            ('view_c', chars, b'!a\x80b?', (1, 3)),
            ('view_slice', child, b'a\x80b', (0, 2)),
            ('view_readonly', readonly, b'!a\x80b?', (1, 3))]
for name, source, content, span in carriers:
    bound_search = p.search
    found = []
    for match in (p.search(source), re.search(pattern, source), bound_search(source)):
        found.append(match_row(match, p, source, span, len(content), b'a\x80'))
        assert match.group(0, 1, 'x', 2, 3) == (b'a\x80', b'a', b'a', b'\x80', None)
        assert match.groups() == (b'a', b'\x80', None)
        assert match.groupdict() == {'x': b'a'}
        assert match.span(3) == (-1, -1)
        assert match.expand(br'<\g<x>\2\3>') == b'<a\x80>'
    print('A1.search-' + name, found)
    results = []
    for match in (whole.match(source), re.match(b'.*', source),
                  whole.fullmatch(source), re.fullmatch(b'.*', source)):
        results.append(match_row(match, whole, source, (0, len(content)), len(content), content))
        assert (match.group() is source) is (name == 'bytes')
    print('A1.match-full-' + name, results)
    assert p.findall(source) == [(b'a', b'\x80', b'')]
    assert re.findall(pattern, source) == [(b'a', b'\x80', b'')]
    assert re.findall(b'a\x80', source) == [b'a\x80']
    assert re.findall(b'(a)\x80', source) == [b'a']
    assert re.search(b'z', source) is None
    assert re.match(b'z', source) is None
    assert re.fullmatch(b'a', source) is None
    print('A1.findall-' + name, p.findall(source), re.findall(b'(a)\x80', source))
for view in (root, chars, child, readonly):
    view.release()
data.append(33)
assert data == bytearray(b'!a\x80b?!')
print('A1.carriers-released', data)

# 保存Match不保导出锁；已取出的bytes不随原subject变化，新group则按旧marks活读。
data = bytearray(b'abcd')
p = re.compile(b'(?P<x>bc)(z)?')
match = p.search(data)
saved = match.group()
assert saved == b'bc'
print('A1.live-initial', match_row(match, p, data, (1, 3), 4, b'bc'))
data[1] = ord('X')
assert match.group() == b'Xc' and saved == b'bc'
assert match.expand(br'<\1>') == b'<Xc>'
assert repr(match) == "<re.Match object; span=(1, 3), match=b'Xc'>"
print('A1.live-write', match.group(), saved, match.expand(br'<\1>'), repr(match))
data.append(33)
del data[2:]
assert match.group() == b'X' and match.group(1) == b'X'
assert match.groups() == (b'X', None) and match.groupdict() == {'x': b'X'}
print('A1.live-shrunk', match_row(match, p, data, (1, 3), 4, b'X'))
del data[:]
assert match.group() == b'' and match.group(1) == b'' and match.group(2) is None
assert match.span() == (1, 3) and match.span(1) == (1, 3) and match.endpos == 4
print('A1.live-empty', match.group(0, 1, 2), match.span(), match.endpos)
data.extend(b'12345')
assert match.group() == b'23' and match.groups() == (b'23', None)
assert match.groupdict() == {'x': b'23'} and saved == b'bc'
print('A1.live-regrown', match_row(match, p, data, (1, 3), 4, b'23'))

# B/c别名及独立child：parent释放不等于child释放，span始终相对child。
data = bytearray(b'!ab?')
parent = memoryview(data)
chars = parent.cast('c')
child = parent[1:3]
match = re.search(b'(ab)', child)
old = match.group()
parent.release()
chars[1] = b'Z'
child[1] = ord('Y')
assert match.string is child and match.group() == b'ZY' and old == b'ab'
assert match.span() == (0, 2) and match.endpos == 2
assert match.expand(br'<\1>') == b'<ZY>'
print('A1.view-live', match.group(), old, match.span(), match.endpos)
child.release()
chars.release()
data.append(33)
assert data == bytearray(b'!ZY?!')
print('A1.view-released', data)

# 每次eager成功/无结果/pos>end后都应释放自己的短租约；不涉及mutable finditer/sub。
for mode in ('match', 'search', 'fullmatch', 'findall'):
    data = bytearray(b'a')
    p = re.compile(b'a')
    bound = getattr(p, mode)
    result = bound(data)
    data.append(33)
    assert bound(data, 2, 1) == ([] if mode == 'findall' else None)
    data.append(33)
    assert getattr(re.compile(b'z'), mode)(data) == ([] if mode == 'findall' else None)
    data.append(33)
    assert data == bytearray(b'a!!!')
    print('A1.short-lease-' + mode, data)

# 固定小范围和空输入推进复用已有bytes语义，新增域仍须首次CP守门确认。
for mode in ('match', 'search', 'fullmatch'):
    data = bytearray(b'za?')
    p = re.compile(b'a')
    match = getattr(p, mode)(data, 1, 2)
    assert match.string is data and match.span() == (1, 2)
    assert match.pos == 1 and match.endpos == 2 and match.group() == b'a'
    data.append(33)
    print('A1.bounds-' + mode, match.group(), match.span(), match.pos, match.endpos)
empty = bytearray()
match = re.fullmatch(b'()', empty)
assert match.group() == b'' and match.group(1) == b'' and match.string is empty
assert match.span() == (0, 0) and match.endpos == 0
empty.append(33)
assert match.group() == b'' and match.span() == (0, 0)
assert re.findall(b'|a', bytearray(b'aa')) == [b'', b'a', b'', b'a', b'']
assert re.findall(b'(a)?b', bytearray(b'b ab')) == [b'', b'a']
print('A1.empty-and-findall', match.group(), match.span(), re.findall(b'|a', bytearray(b'aa')))
print('A1.complete', True)
