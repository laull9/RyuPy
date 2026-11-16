# ryupy: pending
"""mutable subject替换的六载体/入口/模板/count；原exact/text对照，新组合仍须CP-first。"""
import re


def outcome(call):
    try:
        value = call()
    except Exception as error:
        return type(error).__name__, str(error)
    return type(value).__name__, value


data = bytearray(b'!a\x80b?')
whole = memoryview(data)
chars = whole.cast('c')
child = whole[1:4]
readonly = memoryview(b'!a\x80b?')
pattern = b'(?P<x>a)(\x80)(z)?'
p = re.compile(pattern)
carriers = [('bytes', b'!a\x80b?', b'!<a>b?', b'!*b?', (1, 3), 5),
            ('bytearray', data, b'!<a>b?', b'!*b?', (1, 3), 5),
            ('view_B', whole, b'!<a>b?', b'!*b?', (1, 3), 5),
            ('view_c', chars, b'!<a>b?', b'!*b?', (1, 3), 5),
            ('view_slice', child, b'<a>b', b'*b', (0, 2), 3),
            ('view_readonly', readonly, b'!<a>b?', b'!*b?', (1, 3), 5)]
for name, source, expected, callback_output, span, length in carriers:
    saved_sub = p.sub
    saved_subn = p.subn
    results = [re.sub(pattern, br'<\1>', source), p.sub(br'<\g<x>>', source), saved_sub(br'<\1>', source)]
    assert results == [expected, expected, expected]
    assert all(type(result) is bytes for result in results)
    pairs = [re.subn(pattern, br'<\1>', source), p.subn(br'<\g<x>>', source), saved_subn(br'<\1>', source)]
    assert pairs == [(expected, 1), (expected, 1), (expected, 1)]
    assert all(type(pair[1]) is int for pair in pairs)
    print('S1.routes-' + name, results, pairs)
    seen = []

    def replace(match):
        assert match.string is source and match.re is p
        assert match.pos == 0 and match.endpos == length and match.span() == span
        assert match.groups() == (b'a', b'\x80', None)
        seen.append((match.group(), match.groups(), match.span(), match.endpos))
        return b'*'

    assert saved_subn(replace, source) == (callback_output, 1)
    assert len(seen) == 1
    print('S1.callback-' + name, callback_output, seen)
    optional_output = b'<>b' if name == 'view_slice' else b'!<>b?'
    literal_output = b'$1b' if name == 'view_slice' else b'!$1b?'
    assert p.sub(br'<\3>', source) == optional_output
    assert p.sub(b'$1', source) == literal_output
    print('S1.optional-' + name, p.sub(br'<\3>', source), p.sub(b'$1', source))
for view in (whole, chars, child, readonly):
    view.release()
data.append(33)
print('S1.carriers-release', data)

p = re.compile(b'a')
for kind in (bytes, bytearray):
    for count, expected, changed in [(-1, b'aaa', 0), (0, b'bbb', 3), (1, b'baa', 1)]:
        source = kind(b'aaa')
        result = p.subn(b'b', source, count)
        assert result == (expected, changed) and type(result[0]) is bytes
        if kind is bytearray:
            source.append(33)
        print('S1.count-' + kind.__name__ + '-' + str(count), result)
    source = kind(b'aXa')
    seen = []

    def erase(match):
        seen.append(match.span())
        assert match.string is source
        return None

    assert p.subn(erase, source, count=1) == (b'Xa', 1)
    assert p.subn(erase, source, count=-1) == (b'aXa', 0)
    assert p.subn(erase, kind(b'zzz')) == (b'zzz', 0)
    assert seen == [(0, 1)]
    print('S1.none-count-' + kind.__name__, seen)

for name, source in [('bytes', b''), ('bytearray', bytearray()), ('view', memoryview(b''))]:
    assert re.subn(b'', b'X', source) == (b'X', 1)
    assert re.subn(b'a', b'X', source) == (b'', 0)
    if name == 'view': source.release()
    if name == 'bytearray': source.append(33)
    print('S1.empty-' + name, True)

# 保留既有exact str模板与bytes捕获的细分合同，不把str模板一概预拒绝。
p = re.compile(b'(a)?b')
replacements = [r'\1', '', 'X', br'\1', b'', b'X']
text_group_error = ('TypeError', 'sequence item 1: expected str instance, bytes found')
bytes_join_error = ('TypeError', 'sequence item 0: expected a bytes-like object, str found')
expected_rows = [[('bytes', b'x')] * 6,
                 [text_group_error, bytes_join_error, bytes_join_error, ('bytes', b'a'), ('bytes', b''), ('bytes', b'X')],
                 [bytes_join_error, bytes_join_error, bytes_join_error, ('bytes', b''), ('bytes', b''), ('bytes', b'X')]]
for kind in (bytes, bytearray):
    for index, raw in enumerate((b'x', b'ab', b'b')):
        source = kind(raw)
        actual = [outcome(lambda: p.sub(replacement, source)) for replacement in replacements]
        assert actual == expected_rows[index]
        if kind is bytearray: source.append(33)
        print('S1.typed-template-' + kind.__name__ + '-' + str(index), actual)

source = bytearray(b'zz')
output = re.sub(b'a', b'X', source)
assert type(output) is bytes and output == b'zz'
source[0] = ord('Q')
assert output == b'zz'
print('S1.mutable-snapshot', output, source)
data = bytearray(b'zz')
view = memoryview(data)
output = re.sub(b'a', b'X', view)
view.release()
data[0] = ord('Q')
data.append(33)
assert type(output) is bytes and output == b'zz'
print('S1.view-snapshot', output, data)

# exact bytes旧路保身份；mutable单片replacement也只能返回原exact bytes而非source。
source = bytes([122, 122])
returned = bytes([88, 89, 90])
p = re.compile(b'a')
assert p.sub(b'X', source) is source
assert p.sub(returned, b'a') is returned
assert p.sub(lambda match: returned, b'a') is returned
assert p.sub(returned, bytearray(b'a')) is returned
print('S1.exact-identity', source, returned)
for index, case in enumerate([(b'.*?', b'-----', 5), (b'|a', b'-----', 5),
                               (b'a*', b'--', 2), (b'(?=a)', b'-a-a', 2)]):
    expression, expected, count = case
    assert re.subn(expression, b'-', b'aa') == (expected, count)
    assert re.subn(expression, b'-', bytearray(b'aa')) == (expected, count)
    print('S1.empty-progress-' + str(index), expected, count)
assert re.sub('(a)', r'<\1>', 'a a') == '<a> <a>'
assert re.subn('a', lambda match: None, 'aXa') == ('X', 2)
assert re.fullmatch('(a)', 'a').expand(r'<\1>') == '<a>'
print('S1.text-control', re.sub('(a)', r'<\1>', 'a a'), re.subn('a', lambda match: None, 'aXa'))
print('S1.complete', True)
