# 通用分隔应用：文本/二进制字段、捕获与零宽游标；不依赖项目特判。
import re


def check(label, pattern, subject, count, expected):
    module = re.split(pattern, subject, count)
    saved = re.compile(pattern).split
    bound = saved(string=subject, maxsplit=count)
    assert module == expected
    assert bound == expected
    print(label, module, bound)


for binary in (False, True):
    def convert(value):
        if value is None:
            return None
        return value.encode('ascii') if binary else value

    for label, pattern, subject, expected in [
        ('fields', ',', ',a,b,', ['', 'a', 'b', '']),
        ('optional', '(,)(x)?', 'a,b', ['a', ',', None, 'b']),
        ('branches', '(,)|(:)', 'a,b:c', ['a', ',', None, 'b', None, ':', 'c']),
        ('nested', '((,))', 'a,b', ['a', ',', ',', 'b']),
        ('repeat', '(a)+', 'baac', ['b', 'a', 'c']),
        ('empty', '', 'ab', ['', 'a', 'b', '']),
        ('empty-input', '', '', ['', '']),
        ('empty-unmatched', ',', '', ['']),
        ('boundary', r'\b', 'ab cd', ['', 'ab', ' ', 'cd', '']),
        ('zero-nonempty', '|a', 'a', ['', '', '', '']),
        ('lazy', '.*?', 'ab', ['', '', '', '', '', '']),
        ('empty-capture', '()', 'ab', ['', '', 'a', '', 'b', '', '']),
        ('lookahead', '(?=a)', 'aba', ['', 'ab', 'a']),
        ('anchor', '^|$', 'ab', ['', 'ab', '']),
        ('empty-after-match', 'x*', 'abxd', ['', 'a', 'b', '', 'd', '']),
    ]:
        check(('bytes-' if binary else 'text-') + label, convert(pattern),
              convert(subject), 0, [convert(x) for x in expected])
    for count in (1, 2, -1, True, False, 2**40, 2**63 - 1, -2**63):
        expected = ['a,b,c'] if count < 0 else (
            ['a', ',', 'b,c'] if count == 1 else ['a', ',', 'b', ',', 'c'])
        check(('bytes-limit-' if binary else 'text-limit-') + str(count),
              convert('(,)'), convert('a,b,c'), count, [convert(x) for x in expected])

# 无匹配以及负限制保留精确不可变输入身份。
for source, pattern in [('field ' + str(1234), ','), (bytes(bytearray(b'field 1234')), b',')]:
    for count in (0, -1):
        pieces = re.split(pattern, source, count)
        assert pieces == [source] and pieces[0] is source
        print('identity', type(source).__name__, count, pieces[0] is source)
check('unicode', '', 'a\U0001f600b', 0, ['', 'a', '\U0001f600', 'b', ''])
assert re.split('a', 'bAc', flags=re.I) == ['b', 'c']
assert re.split(pattern=re.compile(','), string='a,b', flags=0) == ['a', 'b']
print('flags', re.split('a', 'bAc', flags=re.I))
