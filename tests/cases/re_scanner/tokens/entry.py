# ryupy: pending
# Scanner 的完整多 token 流、左优先、零长停止与回调失败身份。
import re
import sys


class NoTruth:
    # 两个对象共用本次场景的原事件列表，身份与求真值副作用仍可观察。
    def __init__(self, effects):
        self.effects = effects

    def __bool__(self):
        self.effects.append('bool')
        raise AssertionError('action/result 不求真值')

    def __len__(self):
        self.effects.append('len')
        raise AssertionError('action/result 不求长度')


class Action:
    def __init__(self, result):
        self.result = result

    def __call__(self, owner, token):
        assert owner.match.group() == token
        return self.result


def flow():
    calls = []

    def word(scanner, token):
        assert scanner.match.group() == token
        calls.append((token, scanner.match.span()))
        return token.upper()

    scanner = re.Scanner([(r'[a-z]+', word), (r'[0-9]+', lambda s, t: int(t)), (' +', None)])
    assert scanner.scan('ab 12 cd!tail') == (['AB', 12, 'CD'], '!tail')
    assert calls == [('ab', (0, 2)), ('cd', (6, 8))]
    assert re.Scanner([('a', 'short'), ('ab', 'long')]).scan('ab!') == (['short'], 'b!')
    assert re.Scanner([('ab', 'long'), ('a', 'short')]).scan('ab!') == (['long'], '!')
    assert re.Scanner([('^a', 'first'), ('a', 'rest')]).scan('aa!') == (['first', 'rest'], '!')
    assert re.Scanner([(r'a(?=b)', 'A'), (r'b(?!x)', 'B')]).scan('ab!') == (['A', 'B'], '!')
    assert re.Scanner([('a$', 'A')]).scan('ab') == ([], 'ab')
    empty = re.Scanner([('', lambda s, t: calls.append('zero')), ('b', 'B')])
    assert empty.scan('b!') == ([], 'b!') and not hasattr(empty, 'match')
    assert len(calls) == 2
    assert re.Scanner([('a*', 'A'), ('b', 'B')]).scan('ab!') == (['A'], 'b!')
    # 固定 CP 实观修订：空 lexicon 在构造时失败，不能当作成功空扫描。
    for flag in (0, False):
        try:
            re.Scanner([], flag)
        except RuntimeError as error:
            assert type(error) is RuntimeError and error.args == ('invalid SRE code',)
        else:
            raise AssertionError('空 lexicon 根0/False 应保真实构造错误')
    partial = re.Scanner([('a', lambda s, token: token)])
    assert partial.scan('aa!') == (['a', 'a'], '!')
    previous_plan, previous_match = partial.scanner, partial.match
    replacement = []
    try:
        partial.__init__(replacement)
    except RuntimeError as error:
        assert error.args == ('invalid SRE code',)
    else:
        raise AssertionError('空 lexicon reinit 必须失败')
    assert partial.lexicon is replacement and partial.scanner is previous_plan
    assert partial.match is previous_match
    assert partial.scan('!') == ([], '!')
    try:
        partial.scan('a')
    except IndexError:
        pass
    else:
        raise AssertionError('失败 reinit 后旧 plan 仍读取新空 lexicon')
    assert partial.match is previous_match
    print('tokens-flow OK')


def constants():
    effects = []
    constant, result, empty = NoTruth(effects), NoTruth(effects), []
    scanner = re.Scanner([('a', constant), ('b', lambda s, t: result), ('c', False),
                          ('d', 0), ('e', ''), ('f', empty), ('g', None)])
    values, rest = scanner.scan('abcdefg!')
    assert len(values) == 6 and rest == '!'
    assert values[0] is constant and values[1] is result
    assert values[2] is False and values[3] == 0 and values[4] == '' and values[5] is empty
    assert effects == [] and scanner.match.group() == 'b'
    scanner.lexicon = [('a', 'A')] * 7
    old = scanner.match
    assert scanner.scan('aaa!') == (['A', 'A', 'A'], '!') and scanner.match is old
    assert scanner.scan('!') == ([], '!') and scanner.match is old

    action = Action(result)
    action.__call__ = lambda owner, token: 'WRONG'
    assert re.Scanner([('a', action)]).scan('aa!')[0] == [result, result]
    print('tokens-constants OK')


def exception():
    original, cause = RuntimeError('callback'), ValueError('cause')
    context = KeyError('outer handler')
    calls = []

    def action(scanner, token):
        calls.append(token)
        if token == 'b':
            raise original from cause
        return token

    scanner = re.Scanner([('[ab]', action)])
    try:
        raise context
    except KeyError:
        try:
            scanner.scan('ab!')
        except RuntimeError as error:
            assert error is original and error.__cause__ is cause
            assert error.__context__ is context and error.__suppress_context__ is True
            assert error.args == ('callback',) and error.__traceback__ is not None
            saved_traceback = error.__traceback__
            failed_match = scanner.match
            assert failed_match.group() == 'b' and failed_match.span() == (1, 2)
        else:
            raise AssertionError('不能吞回调异常')
    assert calls == ['a', 'b']
    assert scanner.scan('aa!') == (['a', 'a'], '!')
    assert calls == ['a', 'b', 'a', 'a']
    assert original.__traceback__ is saved_traceback and original.__context__ is context
    assert original.__cause__ is cause and failed_match.string == 'ab!'
    print('tokens-exception OK')


cases = [('flow', flow), ('constants', constants), ('exception', exception)]
selected = sys.argv[1] if len(sys.argv) > 1 else 'all'
assert selected == 'all' or any(name == selected for name, function in cases)
for name, function in cases:
    if selected == 'all' or selected == name:
        function()
