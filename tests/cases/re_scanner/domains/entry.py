# ryupy: pending
# Scanner 的根 flags、双域、真实 Match 身份与普通子类/property 合同。
import re
import sys


class Action:
    def __call__(self, owner, token):
        return 'OLD'


# 两个 Scanner 子类保留继承的 __init__，环境必须先于构造就绪。
_property_environment = None
_original_action_call = Action.__call__


class Scanner(re.Scanner):
    @property
    def match(self):
        return self.saved_match

    @match.setter
    def match(self, value):
        _property_environment['events'].append(value.group())
        self.saved_match = value
        Action.__call__ = _property_environment['replacement']


class Setter(re.Scanner):
    @property
    def scanner(self):
        return self.saved_plan

    @scanner.setter
    def scanner(self, value):
        if hasattr(self, 'saved_plan'):
            raise _property_environment['failure']
        self.saved_plan = value


def _restore_properties_classes():
    # 删除本类覆盖以恢复真实继承，不写入一个同值但新建的 scan 槽。
    Action.__call__ = _original_action_call
    try:
        del Scanner.scan
    except AttributeError:
        pass


def flags():
    assert re.Scanner([(r'\w+', 'W')]).scan('é_a') == ([], 'é_a')
    assert re.Scanner([(r'\w+', 'W')], 32).scan('é_a') == (['W'], '')
    assert re.Scanner([(r'(?u)\w+', 'W')]).scan('é_a') == ([], 'é_a')
    assert re.Scanner([('(?i)a', 'A')]).scan('Aab') == ([], 'Aab')
    assert re.Scanner([('a', 'A')], 2).scan('Aa!') == (['A', 'A'], '!')
    assert re.Scanner([('k+', 'K')], 2).scan('Kk') == ([], 'Kk')
    assert re.Scanner([('k+', 'K')], 34).scan('Kk') == (['K'], '')
    assert re.Scanner([('(?x)a b', 'AB')]).scan('abab!') == (['AB', 'AB'], '!')
    assert re.Scanner([('(?x)  (?i)a', 'A')]).scan('Aa!') == ([], 'Aa!')
    assert re.Scanner([('(?x)  (?i)a', 'A')]).scan('aa!') == (['A', 'A'], '!')
    assert re.Scanner([('  (?i)a', 'A')], 64).scan('Aa!') == ([], 'Aa!')
    assert re.Scanner([('  (?i)a', 'A')], 64).scan('aa!') == (['A', 'A'], '!')
    assert re.Scanner([(r'(?x)a\ b', 'AB')]).scan('a b!') == (['AB'], '!')
    assert re.Scanner([('(?x)[ ]+', 'S')]).scan('  !') == (['S'], '!')
    assert re.Scanner([('(?s).', 'DOT')]).scan('\n') == ([], '\n')
    assert re.Scanner([('.', 'DOT')], 16).scan('\n!') == (['DOT', 'DOT'], '')
    assert re.Scanner([('^a', 'A'), ('\n', None)], 8).scan('a\na!') == (['A', 'A'], '!')
    assert re.Scanner([('a', 'A')], False).scan('aa!') == (['A', 'A'], '!')
    for phrase, flag in [('a', 4), ('a', 288), (b'a', 32), (b'a', 260)]:
        try:
            re.Scanner([(phrase, 'A')], flag)
        except ValueError:
            pass
        else:
            raise AssertionError('源域矛盾 flags 应报 ValueError')
    try:
        re.Scanner([('[', 'A')], 4)
    except re.error:
        pass
    else:
        raise AssertionError('当前 phrase 语法错先于源域矛盾')
    for phrase in ('(?L)a', '(?au)a', '(?L)['):
        try:
            re.Scanner([(phrase, 'A')])
        except re.error as error:
            assert error.pattern == phrase
            assert 'flag' in error.msg and 'unterminated character set' not in error.msg
        else:
            raise AssertionError('inline flags 错误必须在当前语法消费点发生')
    print('domains-flags OK')


def text_bytes():
    scanner = re.Scanner([('a', 'A'), (b'b', 'B')])
    assert scanner.scan('abba!') == (['A', 'B', 'B', 'A'], '!')
    assert scanner.scan(b'abba!') == (['A', 'B', 'B', 'A'], b'!')
    assert re.Scanner([('😀', lambda s, t: (t, s.match.span()))]).scan('😀😀!') == ([('😀', (0, 1)), ('😀', (1, 2))], '!')
    assert re.Scanner([('😀', 'wide')]).scan(b'x') == ([], b'x')
    assert re.Scanner([(b'a', lambda s, t: t)]).scan('aa!') == (['a', 'a'], '!')
    assert re.Scanner([('a', lambda s, t: t)], 32).scan(b'aa!') == ([b'a', b'a'], b'!')
    for subject in (None, 1, 1.5, [], {}):
        try:
            re.Scanner([('a', None)]).scan(subject)
        except TypeError:
            pass
        else:
            raise AssertionError('合法非空 plan 必须实际消费非法 subject')
    for phrase in (None, 1, 1.5, [], {}, re.compile('a')):
        try:
            re.Scanner([(phrase, None)])
        except TypeError:
            pass
        else:
            raise AssertionError('非模式源应报 TypeError')
    print('domains-text-bytes OK')


def metadata():
    seen = []

    def action(scanner, token):
        match = scanner.match
        assert type(match) is re.Match and isinstance(match, re.Match)
        assert match.re is scanner.scanner and match.string is subject
        assert match.pos == 0 and match.endpos == 3
        assert match.group(0) == token and match.lastgroup is None
        seen.append(match)
        return token

    scanner = re.Scanner([('a', action), ('b', action)])
    plan = scanner.scanner
    assert type(plan) is re.Pattern and isinstance(plan, re.Pattern)
    assert plan.pattern is None and plan.flags == 0 and plan.groups == 2
    assert re.compile(plan) is plan and re.compile(plan, 0) is plan
    try:
        re.compile(plan, 2)
    except ValueError:
        pass
    else:
        raise AssertionError('compiled 非零 flags 应拒绝')
    re.purge()
    assert scanner.scanner is plan
    subject = 'ab!'
    assert scanner.scan(subject) == (['a', 'b'], '!')
    assert seen[0].groups() == ('a', None) and seen[1].groups() == (None, 'b')
    assert seen[0].groupdict() == {} and seen[0].group(1, 2) == ('a', None)
    assert seen[0].lastindex == 1 and seen[1].lastindex == 2
    assert seen[0].regs == ((0, 1), (0, 1), (-1, -1))
    assert seen[1].regs == ((1, 2), (-1, -1), (1, 2))
    assert seen[1].start(1) == -1 and seen[1].end(2) == 2
    subject = b'ab!'
    assert scanner.scan(subject) == ([b'a', b'b'], b'!')
    assert seen[2].groups() == (b'a', None) and seen[3].span() == (1, 2)
    print('domains-metadata OK')


def properties():
    global _property_environment
    events = []

    def replacement(owner, scanner, token):
        return token.upper()

    _restore_properties_classes()
    _property_environment = {'events': events, 'replacement': replacement}
    try:
        scanner = Scanner([('a', Action())])
        assert isinstance(scanner, re.Scanner)
        assert scanner.scan('aa!') == (['A', 'A'], '!') and events == ['a', 'a']
        scanner.scan = lambda string: (['instance'], string)
        assert scanner.scan('x') == (['instance'], 'x')
        del scanner.scan
        canonical = re.Scanner.scan
        Scanner.scan = lambda self, string: (['class'], string)
        assert scanner.scan('x') == (['class'], 'x')
        assert canonical(scanner, 'aa!') == (['A', 'A'], '!')
        failure = RuntimeError('publish property')
        _property_environment['failure'] = failure
        target = Setter([('a', 'OLD')])
        plan = target.scanner
        new = [('b', 'NEW')]
        try:
            target.__init__(new)
        except RuntimeError as error:
            assert error is failure
        else:
            raise AssertionError('最终 scanner setter 异常必须传播')
        assert target.lexicon is new and target.scanner is plan
        assert target.scan('aa!') == (['NEW', 'NEW'], '!')
        print('domains-properties OK')
    finally:
        _restore_properties_classes()
        _property_environment = None


cases = [('flags', flags), ('text-bytes', text_bytes), ('metadata', metadata), ('properties', properties)]
selected = sys.argv[1] if len(sys.argv) > 1 else 'all'
assert selected == 'all' or any(name == selected for name, function in cases)
for name, function in cases:
    if selected == 'all' or selected == name:
        function()
