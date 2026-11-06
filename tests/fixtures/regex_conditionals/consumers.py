"""K2：条件组经过全部现有匹配/替换消费者；两域、空推进与真实对象身份并列。"""
import re


def snapshot(match):
    return (match.group(), match.groups(), match.groupdict(),
            [match.span(i) for i in range(match.re.groups + 1)])


def collect(iterator):
    result = []
    for unused in range(16):
        match = next(iterator, None)
        if match is None:
            assert next(iterator, None) is None
            return result
        result.append(snapshot(match))
    raise AssertionError('bounded conditional iterator did not stop')


def value(text):
    return text.encode('ascii') if binary else text


for binary in (False, True):
    prefix = 'K2.bytes' if binary else 'K2.text'
    re.purge()
    pattern = value(r'(?P<n>a)?(?(n)b|c)')
    compiled = re.compile(pattern)
    assert compiled.pattern is pattern and compiled.groups == 1
    assert re.compile(pattern) is compiled and re.compile(compiled) is compiled
    print(prefix + '.cache', compiled.pattern, compiled.groups)

    subject = value('zabcq')
    match = compiled.match(subject, 1, 3)
    assert match.group() == value('ab') and match.span() == (1, 3)
    assert match.re is compiled and match.string is subject
    assert match.pos == 1 and match.endpos == 3
    assert compiled.fullmatch(subject, 1, 3).span() == (1, 3)
    absent = compiled.search(subject, 3, 4)
    assert absent.group() == value('c') and absent.span() == (3, 4)
    assert compiled.match(subject) is None and compiled.fullmatch(subject) is None
    print(prefix + '.range', snapshot(match), snapshot(absent))

    assert re.match(pattern, value('abc')).group() == value('ab')
    assert re.search(pattern, subject).span() == (1, 3)
    assert re.fullmatch(pattern, value('c')).group() == value('c')
    assert re.fullmatch(pattern, value('ac')) is None
    print(prefix + '.module', snapshot(re.match(pattern, value('abc'))),
          snapshot(re.search(pattern, subject)), snapshot(re.fullmatch(pattern, value('c'))))

    missing = []
    assert match.group(0, 1, 'n') == (value('ab'), value('a'), value('a'))
    assert match.groups() == (value('a'),) and match.groupdict() == {'n': value('a')}
    assert absent.group(1) is None and absent.groups(missing)[0] is missing
    assert absent.groupdict(missing)['n'] is missing
    assert absent.start('n') == -1 and absent.end('n') == -1 and absent.span('n') == (-1, -1)
    print(prefix + '.groups', match.group(0, 1, 'n'), match.groups(), match.groupdict(),
          absent.groups(), absent.groupdict(), absent.span('n'))

    data = value('ab c')
    expected = [(value('ab'), (value('a'),), {'n': value('a')}, [(0, 2), (0, 1)]),
                (value('c'), (None,), {'n': None}, [(3, 4), (-1, -1)])]
    iterator = compiled.finditer(data)
    assert iter(iterator) is iterator
    actual = collect(iterator)
    assert actual == expected and collect(re.finditer(pattern, data)) == expected
    assert compiled.findall(data) == [value('a'), value('')]
    assert re.findall(pattern, data) == [value('a'), value('')]
    print(prefix + '.iter-findall', actual, compiled.findall(data))

    template = value(r'<\g<n>:\g<0>>')
    replaced = value('<a:ab> <:c>')
    assert match.expand(template) == value('<a:ab>')
    assert absent.expand(template) == value('<:c>')
    assert compiled.sub(template, data) == replaced
    assert compiled.subn(template, data) == (replaced, 2)
    assert re.sub(pattern, template, data) == replaced
    assert re.subn(pattern, template, data) == (replaced, 2)
    assert compiled.sub(value(r'<\1>'), data) == value('<a> <>')
    print(prefix + '.templates', replaced, match.expand(template), absent.expand(template))

    events = []

    def callback(current):
        assert current.re is compiled and current.string is data
        events.append(snapshot(current))
        return value('[') + current.group() + value(']')

    assert compiled.subn(callback, data) == (value('[ab] [c]'), 2)
    assert events == expected
    print(prefix + '.callback', events)
    events.clear()
    assert compiled.subn(callback, data, 1) == (value('[ab] c'), 1)
    assert events == expected[:1]
    events.clear()
    assert compiled.subn(callback, data, -1) == (data, 0) and events == []
    assert compiled.subn(value('X'), data, 0) == (value('X X'), 2)
    assert compiled.subn(lambda current: None, data) == (value(' '), 2)
    print(prefix + '.count', compiled.subn(value('X'), data, 1),
          compiled.subn(value('X'), data, 0), compiled.subn(value('X'), data, -1))

    marker = RuntimeError('conditional callback marker')

    def broken(current):
        assert current.group() == value('ab')
        raise marker

    try:
        compiled.sub(broken, data)
    except RuntimeError as error:
        assert error is marker
    else:
        raise AssertionError('conditional callback exception swallowed')
    print(prefix + '.callback-error', str(marker))

    empty_pattern = value(r'()?(?(1)|a)')
    empty = re.compile(empty_pattern)
    empty_data = value('a')
    empty_expected = [(value(''), (value(''),), {}, [(0, 0), (0, 0)]),
                      (value('a'), (None,), {}, [(0, 1), (-1, -1)]),
                      (value(''), (value(''),), {}, [(1, 1), (1, 1)])]
    assert collect(empty.finditer(empty_data)) == empty_expected
    assert collect(re.finditer(empty_pattern, empty_data)) == empty_expected
    assert empty.findall(empty_data) == [value(''), value(''), value('')]
    assert re.findall(empty_pattern, empty_data) == [value(''), value(''), value('')]
    assert empty.subn(value(r'<\g<1>>'), empty_data) == (value('<><><>'), 3)
    assert re.subn(empty_pattern, value(r'<\g<1>>'), empty_data) == (value('<><><>'), 3)
    print(prefix + '.empty', empty_expected, empty.findall(empty_data),
          empty.subn(value(r'<\g<1>>'), empty_data))

    whole = bytes([97, 98]) if binary else 'ab'
    full = compiled.fullmatch(whole)
    assert full.string is whole and full.re is compiled
    if binary:
        assert full.group() is whole
        assert compiled.sub(value('X'), whole, -1) is whole
    original = compiled
    re.purge()
    assert re.compile(pattern) is not original and original.fullmatch(whole).group() == whole
    print(prefix + '.identity', full.group(), full.span())
