"""R1：真实 bytes 域、ASCII 语义、缓存与错误；并列原 text 对照，不包含 mutable 成功域。"""
import re


def expect_error(label, kind, message, call):
    try:
        call()
    except Exception as error:
        assert type(error) is kind and str(error) == message
        print(label, type(error).__name__, str(error))
        return error
    raise AssertionError('expected error: ' + label)


re.purge()
raw = bytes([97, 43])
equal = bytes([97, 43])
assert raw == equal and raw is not equal
p = re.compile(raw)
text = re.compile('a+')
assert type(p) is re.Pattern and isinstance(p, re.Pattern)
assert p is re.compile(equal) and p.pattern is raw and re.compile(p) is p
assert p is not text and p.flags == 0 and text.flags == re.UNICODE
assert p.groups == 0 and repr(p) == "re.compile(b'a+')"
print('R1.cache', p.pattern, p.flags, p.groups, repr(p), text.pattern, text.flags)
expect_error('R1.compiled-flags', ValueError,
             'cannot process flags argument with a compiled pattern', lambda: re.compile(p, re.I))
re.purge()
assert re.compile(raw) is not p and p.fullmatch(b'aa').group() == b'aa'
print('R1.purge', p.fullmatch(b'aa').group())
expect_error('R1.bytearray-pattern', TypeError, "unhashable type: 'bytearray'",
             lambda: re.compile(bytearray(b'a')))
expect_error('R1.bytes-on-text', TypeError, 'cannot use a bytes pattern on a string-like object',
             lambda: p.search('aa'))
expect_error('R1.text-on-bytes', TypeError, 'cannot use a string pattern on a bytes-like object',
             lambda: text.search(b'aa'))
expect_error('R1.bad-subject', TypeError, "expected string or bytes-like object, got 'int'",
             lambda: p.search(3))

alphabet = bytes(range(256))
word = [i for i in range(256) if 65 <= i <= 90 or 97 <= i <= 122 or 48 <= i <= 57 or i == 95]
digit = list(range(48, 58))
space = [9, 10, 11, 12, 13, 32]
for label, pattern, expected in [('word', br'\w', word), ('digit', br'\d', digit),
                                 ('space', br'\s', space)]:
    actual = []
    for flags in (0, re.A, re.I):
        found = re.findall(pattern, alphabet, flags)
        assert all(type(item) is bytes and len(item) == 1 for item in found)
        assert [item[0] for item in found] == expected
        actual.append([item[0] for item in found])
    print('R1.class-' + label, actual)
for label, pattern, excluded in [('word', br'\W', word), ('digit', br'\D', digit),
                                 ('space', br'\S', space)]:
    found = re.findall(pattern, alphabet)
    assert [item[0] for item in found] == [i for i in range(256) if i not in excluded]
    print('R1.complement-' + label, [item[0] for item in found])
assert re.findall(b'[a-z]', b'AZaz\xc0\xe0\xff', re.I) == [b'A', b'Z', b'a', b'z']
assert re.findall(b'\xc0', b'\xc0\xe0', re.I) == [b'\xc0']
assert re.findall(b'[^a-z]', b'a\xc0\xe0', re.I) == [b'\xc0', b'\xe0']
print('R1.ignorecase', re.findall(b'[a-z]', b'AZaz\xc0\xe0\xff', re.I),
      re.findall('[a-z]', 'aİıſK', re.I), re.findall('[a-z]', 'aİıſK', re.I | re.A))
assert [m.span() for m in re.finditer(br'\b', b'\xffa_\x80')] == [(1, 1), (3, 3)]
assert [m.span() for m in re.finditer(br'\B', b'\xffa_\x80')] == [(0, 0), (2, 2), (4, 4)]
print('R1.boundaries', [m.span() for m in re.finditer(br'\b', b'\xffa_\x80')],
      [m.span() for m in re.finditer(br'\B', b'\xffa_\x80')])
assert re.compile(b'a', re.M | re.S | re.X).flags == 88
assert re.compile(b'(?i)a').flags == re.I and re.compile(b'(?a)a').flags == re.A
assert re.findall(b' ^ a . # note\n ', b'a\n', re.M | re.S | re.X) == [b'a\n']
assert repr(re.compile(b'(?P<body>.+)', re.S)) == "re.compile(b'(?P<body>.+)', re.DOTALL)"
print('R1.flags', re.compile(b'a', re.A).flags, re.compile(b'(?i)a').flags,
      re.findall(b' ^ a . # note\n ', b'a\n', re.M | re.S | re.X),
      repr(re.compile(b'(?P<body>.+)', re.S)))
for label, flags, message in [('unicode', re.U, 'cannot use UNICODE flag with a bytes pattern'),
                             ('unicode-ascii', re.U | re.A, 'cannot use UNICODE flag with a bytes pattern'),
                             ('locale-ascii', re.L | re.A, 'ASCII and LOCALE flags are incompatible')]:
    expect_error('R1.flag-' + label, ValueError, message, lambda: re.compile(b'a', flags))
expect_error('R1.inline-unicode', re.error,
             "bad inline flags: cannot use 'u' flag with a bytes pattern at position 3",
             lambda: re.compile(b'(?u)a'))

invalid = [(br'\u0061', 'bad escape \\u', 0), (br'\U00000061', 'bad escape \\U', 0),
           (br'\N{LATIN SMALL LETTER A}', 'bad escape \\N', 0),
           (br'[\u0061]', 'bad escape \\u', 1), (br'[\U00000061]', 'bad escape \\U', 1),
           (br'[\N{LATIN SMALL LETTER A}]', 'bad escape \\N', 1),
           (br'\q', 'bad escape \\q', 0), (br'\xG0', 'incomplete escape \\x', 0),
           (b'(?P<\xe9>a)', "bad character in group name '\\xe9'", 4)]
errors = []
for pattern, message, position in invalid:
    try:
        re.compile(pattern)
    except re.error as error:
        assert error.pattern is pattern and error.msg == message and error.pos == position
        assert error.lineno == 1 and error.colno == position + 1
        assert str(error) == message + ' at position ' + str(position)
        errors.append((error.pattern, error.msg, error.pos, str(error), error.args))
    else:
        raise AssertionError('invalid bytes escape accepted')
print('R1.invalid-escapes', errors)
raw_error = bytes([255, 10, 65])
error = re.error('located', raw_error, 2)
assert error.pattern is raw_error and error.lineno == 2 and error.colno == 1
assert str(error) == 'located at position 2 (line 2, column 1)'
print('R1.error-object', error.msg, error.pattern, error.pos, error.lineno, error.colno, error.args)
raw_escape = bytes([0, 35, 32, 91, 255])
escaped = re.escape(raw_escape)
assert type(escaped) is bytes and escaped == b'\x00\\#\\ \\[\xff'
assert re.fullmatch(escaped, raw_escape).group() is raw_escape
print('R1.escape', escaped, re.escape('a b[c]'))
