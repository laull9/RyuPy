# ryupy: pending
# 首切之外仍保留 CP 正面合同；不得以产品 NI 替换这些正期待或计为已支持。
import re
import sys


def syntax():
    seen = []

    def capture(scanner, token):
        seen.append((token, scanner.match.groups(), scanner.match.lastindex))
        return token

    assert re.Scanner([('(a)', capture)]).scan('aa!') == (['a', 'a'], '!')
    assert seen == [('a', ('a',), 1), ('a', ('a',), 1)]
    assert re.Scanner([(r'(a)\1', 'AA')]).scan('aa!') == (['AA'], '!')
    assert re.Scanner([('a', 'A'), ('(?<=a)b', 'B')]).scan('ab!') == (['A', 'B'], '!')
    assert re.Scanner([('(?i:a)', 'A')]).scan('Aa!') == (['A', 'A'], '!')
    assert re.Scanner([('(?>a)', 'A')]).scan('aa!') == (['A', 'A'], '!')
    assert re.Scanner([('a++', 'A')]).scan('aa!') == (['A'], '!')
    print('pending-syntax CP_OK')


def buffer():
    subject = bytearray(b'ab!')
    events = []

    def action(scanner, token):
        if token == b'a':
            subject[1] = ord('c')
            try:
                subject.append(120)
            except BufferError:
                events.append('leased')
            else:
                raise AssertionError('扫描期间应持可变 buffer 租约')
        return token

    scanner = re.Scanner([(b'[abc]', action)])
    result, rest = scanner.scan(subject)
    assert result == [b'a', b'c'] and rest == bytearray(b'!')
    assert type(rest) is bytearray and events == ['leased']
    subject.append(120)
    assert subject == bytearray(b'ac!x')
    print('pending-buffer CP_OK')


def escaped():
    scanner = re.Scanner([('a', lambda s, t: t)])
    plan = scanner.scanner
    assert plan.groupindex == {}
    assert type(repr(plan)) is str and str(plan) == repr(plan)
    assert scanner.scan('aa!') == (['a', 'a'], '!')
    assert scanner.match.expand(r'<\g<0>>') == '<a>'
    assert plan.match('a').group() == 'a'
    assert plan.findall('aa!') == ['a', 'a']
    events = []

    class Backend:
        def scanner(self, string):
            events.append(string)
            return re.compile('(a)').scanner(string)

    scanner.scanner = Backend()
    assert scanner.scan('aa!') == (['a', 'a'], '!') and events == ['aa!']
    print('pending-escaped CP_OK')


def protocol():
    events = []

    class Flags:
        def __and__(self, value):
            events.append(('and', value))
            return 0

    class Lexicon:
        def __iter__(self):
            events.append('iter')
            return iter([('a', None)])

    try:
        re.Scanner(Lexicon(), Flags())
    except TypeError:
        pass
    else:
        raise AssertionError('CP 自定义 flags 算术应到达真实 TypeError')
    assert events == ['iter', ('and', 64), ('and', 4), ('and', 256)]

    class SequenceOnly:
        def __getitem__(self, index):
            return [('a', 'A')][index]

    assert re.Scanner(SequenceOnly()).scan('aa!') == (['A', 'A'], '!')

    class Text(str):
        pass

    class Octets(bytes):
        pass

    assert re.Scanner([(Text('a'), 'A')]).scan(Text('aa!')) == (['A', 'A'], '!')
    assert re.Scanner([(Octets(b'a'), 'A')]).scan(Octets(b'aa!')) == (['A', 'A'], b'!')
    print('pending-protocol CP_OK')


cases = [('syntax', syntax), ('buffer', buffer), ('escaped', escaped), ('protocol', protocol)]
selected = sys.argv[1] if len(sys.argv) > 1 else 'all'
assert selected == 'all' or any(name == selected for name, function in cases)
for name, function in cases:
    if selected == 'all' or selected == name:
        function()
