# Scanner 首切 NI 与资源负门：不是 CP 正面用例，精确类型/现场完成才打印 marker。
import re
import sys


class Index:
    def __init__(self, effects):
        self.effects = effects

    def __index__(self):
        self.effects.append('index')
        raise AssertionError('拒绝应先于参数消费')


class Template:
    def __init__(self, effects):
        self.effects = effects

    def __str__(self):
        self.effects.append('template')
        raise AssertionError('拒绝应先于模板转换')


class Backend:
    def __init__(self, effects):
        self.effects = effects

    def scanner(self, subject):
        self.effects.append('private backend invoked')
        raise AssertionError('不得调用半次私有 backend')


class SequenceOnly:
    def __init__(self, events):
        self.events = events

    def __getitem__(self, index):
        self.events.append(index)
        raise AssertionError('本切拒绝 sequence fallback 前不能调用索引0')


class Flags:
    def __init__(self, events):
        self.events = events

    def __index__(self):
        self.events.append('index')
        return 0

    def __and__(self, value):
        self.events.append('and')
        return 0


class Lexicon:
    # 标志与词条协议共享原事件列表，仍可严格判断先消费了哪一方。
    def __init__(self, events):
        self.events = events

    def __iter__(self):
        self.events.append('iter')
        return iter([('a', None)])


def ni(operation):
    try:
        operation()
    except NotImplementedError:
        return
    raise AssertionError('必须明确 NotImplementedError，不能静默接受')


def consumers():
    scanner = re.Scanner([('a', lambda s, token: token)])
    assert scanner.scan('aa!') == (['a', 'a'], '!')
    plan, match = scanner.scanner, scanner.match
    effects = []

    def replacement(value):
        effects.append('callback')
        raise AssertionError('拒绝不能先调用替换回调')

    for name in ('match', 'search', 'fullmatch', 'findall', 'finditer'):
        saved = getattr(plan, name)
        assert callable(saved)
        ni(lambda: saved(None))
        ni(lambda: getattr(re, name)(plan, None))
    for name in ('sub', 'subn'):
        saved = getattr(plan, name)
        assert callable(saved)
        ni(lambda: saved(replacement, None, Index(effects)))
        ni(lambda: getattr(re, name)(plan, replacement, None, Index(effects)))
    saved = plan.split
    ni(lambda: saved(None, Index(effects)))
    ni(lambda: re.split(plan, None, Index(effects)))
    saved_expand = match.expand
    assert callable(saved_expand)
    ni(lambda: saved_expand(Template(effects)))
    for operation in (lambda: repr(plan), lambda: str(plan),
                      lambda: plan.groupindex):
        ni(operation)
    saved_scanner = plan.scanner
    assert callable(saved_scanner)
    ni(lambda: saved_scanner('a'))
    assert effects == []
    for operation in (lambda: plan.match(), lambda: plan.sub(),
                      lambda: match.expand(), lambda: plan.split(), lambda: saved_scanner()):
        try:
            operation()
        except TypeError:
            pass
        else:
            raise AssertionError('绑定错误必须先于正文 NI')

    scanner.scanner = Backend(effects)
    ni(lambda: scanner.scan('a'))
    scanner.scanner = re.compile('a')
    ni(lambda: scanner.scan('a'))
    assert effects == []
    print('ni-consumers NI_OK')


def syntax():
    for pattern in ('(a)', '(?P<word>a)', r'(a)\1', '(a)(?(1)b|c)',
                    '(?<=a)b', '(?<!a)b', '(?i:a)', '(?>a)', 'a++'):
        events = []

        def lexicon():
            events.append('first')
            yield (pattern, 'A')
            events.append('forbidden next')
            yield ('a', 'B')

        ni(lambda: re.Scanner(lexicon()))
        assert events == ['first']
    print('ni-syntax NI_OK')


def sources():
    events = []

    ni(lambda: re.Scanner(SequenceOnly(events)))
    assert events == []
    for pattern in (bytearray(b'a'), memoryview(b'a')):
        ni(lambda: re.Scanner([(pattern, None)]))
    for subject in (bytearray(b'a'), memoryview(b'a')):
        ni(lambda: re.Scanner([('a', None)]).scan(subject))
    print('ni-sources NI_OK')


def flags():
    events = []

    scanner = re.Scanner([('a', 'OLD')])
    previous = scanner.scanner
    lexicon = Lexicon(events)
    ni(lambda: scanner.__init__(lexicon, Flags(events)))
    assert scanner.lexicon is lexicon and scanner.scanner is previous
    assert events == ['iter']
    for value in (1, 128, 512, -1, 2147483648):
        ni(lambda: re.Scanner([('a', None)], value))
    ni(lambda: re.Scanner([(b'a', None)], 4))
    print('ni-flags NI_OK')


def resources():
    events = []

    def lexicon():
        for index in range(1026):
            events.append(index)
            yield ('a', None)

    ni(lambda: re.Scanner(lexicon()))
    assert events == list(range(1025))
    ni(lambda: re.Scanner([('a' * 16385, None)]))
    ni(lambda: re.Scanner([('a' * 10000, None), ('b' * 10000, None)]))
    try:
        re.Scanner([('(?:' * 257 + 'a' + ')' * 257, None)])
    except RecursionError as error:
        assert type(error) is RecursionError
        assert error.args == ('re: supported group nesting limit exceeded (256)',)
    else:
        raise AssertionError('必须保留既有嵌套资源错误类型和消息')
    translated = []

    def unicode_lexicon():
        for index in range(1024):
            translated.append(index)
            yield (r'\w', 'W')

    ni(lambda: re.Scanner(unicode_lexicon(), 32))
    # 只用短源，必须在达到词条数 cap 前由累计译文门拒绝，不完整预物化图。
    assert 1 < len(translated) < 1024
    assert translated == list(range(len(translated)))
    print('ni-resources NI_OK')


def timeout():
    # 后端合法优化可能快速完成，不能强迫超时；只接受精确完成或既有资源异常。
    scanner = re.Scanner([('(?:a+)+b', None)])
    subject = 'a' * 8192 + '!'
    try:
        actual = scanner.scan(subject)
    except RuntimeError as error:
        assert type(error) is RuntimeError
        assert error.args == ('re: supported matching time budget exceeded (2 seconds)',)
        branch = 'RESOURCE_TIMED_OUT'
    else:
        assert actual == ([], subject)
        branch = 'RESOURCE_COMPLETED'
    assert not hasattr(scanner, 'match')
    print('resource-timeout', branch)


cases = [('consumers', consumers), ('syntax', syntax), ('sources', sources),
         ('flags', flags), ('resources', resources), ('timeout', timeout)]
selected = sys.argv[1] if len(sys.argv) > 1 else 'all'
assert selected == 'all' or any(name == selected for name, function in cases)
for name, function in cases:
    if selected == 'all' or selected == name:
        function()
