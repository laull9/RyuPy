"""R2：module/Pattern/Match 全匹配链、原 bytes 身份、组与空匹配；text 码点旧域并列。"""
import re


def rows(iterator):
    result = []
    for unused in range(16):
        try:
            match = next(iterator)
        except StopIteration:
            return result
        result.append((match.group(), match.groups(), match.span()))
    raise AssertionError('bounded iterator failed to stop')


subject = bytes([0, 65, 128, 255])
p = re.compile(b'(?P<body>.+)', re.S)
m = p.fullmatch(subject)
assert type(m) is re.Match and isinstance(m, re.Match)
assert m.re is p and m.string is subject
assert m.group() is subject and m.group(1) is subject and m.group('body') is subject
assert m.groups() == (subject,) and m.groupdict() == {'body': subject}
assert m.span() == (0, 4) and m.start() == 0 and m.end() == 4
assert m.pos == 0 and m.endpos == 4
assert repr(m) == "<re.Match object; span=(0, 4), match=b'\\x00A\\x80\\xff'>"
print('R2.binary-identity', m.group(), m.group(0, 1), m.groups(), m.groupdict(), m.span(), repr(m))

p = re.compile(b'.', re.S)
for method in ('match', 'search', 'fullmatch'):
    m = getattr(p, method)(subject, pos=2, endpos=3)
    assert m.group() == b'\x80' and m.span() == (2, 3) and m.pos == 2 and m.endpos == 3
    assert m.string is subject and m.re is p
    print('R2.range-' + method, m.group(), m.span(), m.pos, m.endpos)
assert re.match(b'.', subject, re.S).group() == b'\x00'
assert re.search(b'\xff', subject).span() == (3, 4)
assert re.fullmatch(b'.+', subject, re.S).group() is subject
assert re.search(b'z', subject) is None and re.match(b'A', subject) is None
assert re.fullmatch(b'.', subject) is None
print('R2.module-match', re.match(b'.', subject, re.S).group(), re.search(b'\xff', subject).span(),
      re.fullmatch(b'.+', subject, re.S).group(), re.search(b'z', subject))

p = re.compile(b'(?P<x>a)?(b)')
m = p.search(b'zbz')
missing = []
assert m.group(0, 1, 2) == (b'b', None, b'b')
assert m.groups(missing)[0] is missing and m.groupdict(missing)['x'] is missing
assert m.span(1) == (-1, -1) and m.start(1) == -1 and m.end(1) == -1

# bytes 组号按 ssize 饱和后判断范围，不沿旧 text int32 或匹配 pos 的 Overflow 规则。
for method in ('group', 'start', 'end', 'span'):
    for number in (1 << 40, 1 << 100):
        try:
            getattr(m, method)(number)
        except IndexError as error:
            assert str(error) == 'no such group'
        else:
            raise AssertionError('wide group number accepted')

group_events = []


class GroupIndex:
    def __index__(self):
        group_events.append('index')
        return 2


for method, expected in [('group', b'b'), ('start', 1), ('end', 2), ('span', (1, 2))]:
    group_events.clear()
    assert getattr(m, method)(GroupIndex()) == expected
    assert group_events == ['index']

group_marker = RuntimeError('group index marker')


class BrokenGroupIndex:
    def __index__(self):
        group_events.append('broken')
        raise group_marker


for method in ('group', 'start', 'end', 'span'):
    group_events.clear()
    try:
        getattr(m, method)(BrokenGroupIndex())
    except RuntimeError as error:
        assert error is group_marker and group_events == ['broken']
    else:
        raise AssertionError('group index exception swallowed')

# public re.error 位置沿 bytes slice 边界；column 仍从原 pos 做减法，不先夹紧。
error_pattern = bytes([97, 10, 98, 10, 99])
for position, line, column in [(-1, 3, -4), (1 << 100, 3, (1 << 100) - 3)]:
    error = re.error('located', error_pattern, position)
    assert error.pattern is error_pattern and error.pos is position and error.msg == 'located'
    assert error.lineno == line and error.colno == column
    assert str(error) == ('located at position ' + str(position) +
                          ' (line ' + str(line) + ', column ' + str(column) + ')')

position_events = []


class ErrorPosition:
    def __int__(self):
        position_events.append('int')
        return 5

    def __index__(self):
        position_events.append('index')
        return 2 if len(position_events) == 2 else 4

    def __sub__(self, other):
        position_events.append(('sub', other))
        return 10 - other


position = ErrorPosition()
error = re.error('located', error_pattern, position)
assert error.pattern is error_pattern and error.pos is position
assert position_events == ['int', 'index', 'index', ('sub', 3)]
assert error.lineno == 2 and error.colno == 7
assert str(error) == 'located at position 5 (line 2, column 7)'
print('R2.missing', m.group(0, 1, 2), m.groups(), m.groupdict(), [m.span(i) for i in range(3)])
for label, value in [('index', 3), ('name', 'absent')]:
    try:
        m.group(value)
    except IndexError as error:
        assert str(error) == 'no such group'
        print('R2.bad-group-' + label, type(error).__name__, str(error))
    else:
        raise AssertionError('missing group accepted')

assert re.findall(b'.', subject, re.S) == [b'\x00', b'A', b'\x80', b'\xff']
assert re.findall(b'(a)?b', b'b ab') == [b'', b'a']
assert p.findall(b'b ab') == [(b'', b'b'), (b'a', b'b')]
print('R2.findall', re.findall(b'.', subject, re.S), re.findall(b'(a)?b', b'b ab'), p.findall(b'b ab'))
it = re.finditer(b'.', subject, re.S)
assert iter(it) is it
found = rows(it)
assert found == [(b'\x00', (), (0, 1)), (b'A', (), (1, 2)),
                 (b'\x80', (), (2, 3)), (b'\xff', (), (3, 4))]
assert next(it, None) is None
print('R2.finditer', found)
expected_ranges = [[(b'', (), (0, 0))], [(b'a', (), (0, 1)), (b'', (), (1, 1))],
                   [(b'a', (), (1, 2)), (b'', (), (2, 2))], [(b'', (), (2, 2))], []]
for index, bounds in enumerate([(0, 0), (-2, 1), (1, 2), (9, 9), (2, 1)]):
    actual = rows(re.compile(b'a*').finditer(b'aa', bounds[0], bounds[1]))
    assert actual == expected_ranges[index]
print('R2.clamped-ranges', expected_ranges)
empty_cases = [(b'.*?', [b'', b'a', b'', b'a', b'']), (b'|a', [b'', b'a', b'', b'a', b'']),
               (b'a*', [b'aa', b'']), (b'(?=a)', [b'', b''])]
for index, pair in enumerate(empty_cases):
    pattern, expected = pair
    actual = re.findall(pattern, b'aa')
    assert actual == expected
    assert [item[0] for item in rows(re.compile(pattern).finditer(b'aa'))] == expected
    print('R2.empty-' + str(index), actual)
whole = bytes([65, 66, 67])
assert re.findall(b'.+', whole)[0] is whole
assert re.compile(b'(.+)').findall(whole)[0] is whole
print('R2.whole-slice', re.findall(b'.+', whole), re.compile(b'(.+)').findall(whole))
assert re.search(b'a$', b'a\n').span() == (0, 1)
assert re.fullmatch(b'a$', b'a\n') is None
assert re.findall(b'^a', b'a\na', re.M) == [b'a', b'a']
assert re.fullmatch(br'(?P<x>a)(?P=x)', b'aa').group() == b'aa'
print('R2.anchors-backref', re.search(b'a$', b'a\n').span(), re.fullmatch(b'a$', b'a\n'),
      re.findall(b'^a', b'a\na', re.M), re.fullmatch(br'(?P<x>a)(?P=x)', b'aa').group())

# 共享载体必须保持旧 text 码点/Unicode/原模板域，不只验证新 bytes 分支。
text = '😀猫'
m = re.search('猫', text)
assert m.string is text and m.span() == (1, 2)
assert re.findall('.', text) == ['😀', '猫']
assert re.findall(r'\w+', 'á‿²猫𝟙') == ['a', '²猫𝟙']
assert re.findall(r'\s', '\x1c\t') == ['\x1c', '\t']
assert re.findall(r'(a)?b', 'b ab') == ['', 'a']
print('R2.text-control', m.group(), m.span(), re.findall('.', text),
      re.findall(r'\w+', 'á‿²猫𝟙'), re.findall(r'\s', '\x1c\t'), re.findall(r'(a)?b', 'b ab'))
