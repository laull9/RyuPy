"""R3：模板自身 str/bytes 域、sub/subn、延迟 join 类型错和回调原异常；精确期待来自52条参考。"""
import re


def outcome(call):
    try:
        value = call()
    except Exception as error:
        return type(error).__name__, str(error)
    return type(value).__name__, value


p = re.compile(b'(a)?b')
replacements = [r'\1', '', 'X', br'\1', b'', b'X']
unicode_error = ('TypeError', 'sequence item 1: expected str instance, bytes found')
bytes_error = ('TypeError', 'sequence item 0: expected a bytes-like object, str found')
expected_sub = [[('bytes', b'x')] * 6,
                [unicode_error, bytes_error, bytes_error, ('bytes', b'a'), ('bytes', b''), ('bytes', b'X')],
                [bytes_error, bytes_error, bytes_error, ('bytes', b''), ('bytes', b''), ('bytes', b'X')]]
for index, value in enumerate([b'x', b'ab', b'b']):
    actual = [outcome(lambda: p.sub(replacement, value)) for replacement in replacements]
    assert actual == expected_sub[index]
    print('R3.mixed-' + str(index), actual)
expected_expand = [[unicode_error, ('str', ''), ('str', 'X'), ('bytes', b'a'), ('bytes', b''), ('bytes', b'X')],
                   [('str', ''), ('str', ''), ('str', 'X'), ('bytes', b''), ('bytes', b''), ('bytes', b'X')]]
for index, value in enumerate([b'ab', b'b']):
    m = p.fullmatch(value)
    actual = [outcome(lambda: m.expand(replacement)) for replacement in replacements]
    assert actual == expected_expand[index]
    print('R3.expand-' + str(index), actual)
p = re.compile(b'(?P<x>a)(b)')
assert p.sub(br'\2-\g<x>-\g<0>', b'ab ab') == b'b-a-ab b-a-ab'
assert re.subn(b'(a)', br'<\1>', b'a a', count=1) == (b'<a> a', 1)
assert p.fullmatch(b'ab').expand(br'\2-\g<x>') == b'b-a'
assert p.sub(b'$1', b'ab') == b'$1'
print('R3.template-chain', p.sub(br'\2-\g<x>-\g<0>', b'ab ab'),
      re.subn(b'(a)', br'<\1>', b'a a', count=1), p.fullmatch(b'ab').expand(br'\2-\g<x>'),
      p.sub(b'$1', b'ab'))

bad = [(br'\9', 'error', 'invalid group reference 9 at position 1'),
       (br'\g<absent>', 'IndexError', "unknown group name 'absent'"),
       (br'\u0061', 'error', 'bad escape \\u at position 0'),
       (br'\q', 'error', 'bad escape \\q at position 0'),
       (3, 'TypeError', 'decoding to str: need a bytes-like object, int found')]
for index, row in enumerate(bad):
    replacement, kind, message = row
    actual = outcome(lambda: p.sub(replacement, b'none'))
    assert actual == (kind, message)
    print('R3.invalid-' + str(index), actual)
for index, call in enumerate([lambda: p.sub(br'\9', 3), lambda: p.sub(br'\9', b'none', -1)]):
    actual = outcome(call)
    assert actual == ('error', 'invalid group reference 9 at position 1')
    print('R3.template-order-' + str(index), actual)
assert outcome(lambda: p.sub(b'x', 3)) == ('TypeError', "expected string or bytes-like object, got 'int'")
print('R3.subject-order', outcome(lambda: p.sub(b'x', 3)))

# 收集所有回调结果再join：普通错误返回不能提前停第二个回调。
p = re.compile(b'a')
for label, returned in [('bytes', b'X'), ('none', None), ('str', 'X'), ('empty-str', ''), ('int', 3)]:
    seen = []
    subject = bytes([97, 97])
    def replace(match):
        assert match.string is subject and match.re is p
        seen.append((match.span(), match.group()))
        return returned
    actual = outcome(lambda: p.subn(replace, subject))
    assert seen == [((0, 1), b'a'), ((1, 2), b'a')]
    if label == 'bytes':
        assert actual == ('tuple', (b'XX', 2))
    elif label == 'none':
        assert actual == ('tuple', (b'', 2))
    else:
        kind = 'int' if label == 'int' else 'str'
        assert actual == ('TypeError', 'sequence item 0: expected a bytes-like object, ' + kind + ' found')
    print('R3.callback-' + label, actual, seen)
    seen = []
    subject = bytes([122, 122])
    unchanged = p.subn(replace, subject)
    assert unchanged[0] is subject and unchanged[1] == 0 and seen == []
    print('R3.no-callback-' + label, unchanged, seen)

seen = []
marker = RuntimeError('later-callback-marker')
def later_error(match):
    seen.append(match.span())
    if len(seen) == 1:
        return 'wrong-domain'
    raise marker

try:
    p.sub(later_error, b'aa')
except RuntimeError as error:
    assert error is marker and seen == [(0, 1), (1, 2)]
    print('R3.later-error', type(error).__name__, str(error), error is marker, seen)
else:
    raise AssertionError('later callback must precede join validation')

seen = []
def erase(match):
    seen.append(match.span())
    return None
assert p.subn(erase, b'aa', count=1) == (b'a', 1)
assert p.sub(erase, b'aa', count=-1) == b'aa'
assert seen == [(0, 1)]
print('R3.count', p.subn(b'X', b'aaa', count=2), seen)
returned = bytes([88, 89, 90])
assert p.sub(lambda m: returned, b'a') is returned
assert p.sub(returned, b'a') is returned
subject = bytes([122, 122])
assert p.sub(b'X', subject) is subject and p.sub(b'X', b'aa', count=-1) == b'aa'
print('R3.identity', p.sub(lambda m: returned, b'a'), p.sub(b'X', subject))
for index, case in enumerate([(b'.*?', b'-----', 5), (b'|a', b'-----', 5),
                               (b'a*', b'--', 2), (b'(?=a)', b'-a-a', 2)]):
    pattern, expected, count = case
    assert re.subn(pattern, b'-', b'aa') == (expected, count)
    print('R3.empty-' + str(index), re.subn(pattern, b'-', b'aa'))

# 现 text 替换路径保持原输出；不把bytes新join时序冒作修复text旧债。
assert re.sub('(a)', r'<\1>', 'a a') == '<a> <a>'
assert re.subn('a', lambda m: None, 'aXa') == ('X', 2)
assert re.fullmatch('(a)', 'a').expand(r'<\1>') == '<a>'
assert re.sub('a', lambda m: m.group().upper(), 'a a') == 'A A'
print('R3.text-control', re.sub('(a)', r'<\1>', 'a a'), re.subn('a', lambda m: None, 'aXa'),
      re.fullmatch('(a)', 'a').expand(r'<\1>'), re.sub('a', lambda m: m.group().upper(), 'a a'))
