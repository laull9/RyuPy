# ryupy: pending
# Scanner 构造、绑定与失败现场：正面 CP 合同，未验证前不代表产品支持。
import re
import sys


class Lexicon:
    # 辅助类在模块定义，显式保存本次场景的原事件列表，不复制捕获对象。
    def __init__(self, calls):
        self.calls = calls

    def __iter__(self):
        self.calls.append('iter')
        return iter([('a', 'A')])


class Broken:
    def __init__(self, error):
        self.error = error

    def __iter__(self):
        raise self.error


def binding():
    calls = []
    lexicon = Lexicon(calls)
    for invoke in (
        lambda: re.Scanner(),
        lambda: re.Scanner(lexicon, lexicon=lexicon),
        lambda: re.Scanner(lexicon, unknown=1),
        lambda: re.Scanner(lexicon, 0, 1),
    ):
        try:
            invoke()
        except TypeError:
            pass
        else:
            raise AssertionError('构造绑定错误必须先于迭代')
    assert calls == []
    scanner = re.Scanner(lexicon=[('a', 'A')], flags=0)
    assert scanner.scan(string='aa!') == (['A', 'A'], '!')
    for invoke in (lambda: scanner.scan(), lambda: scanner.scan('a', 'b'),
                   lambda: scanner.scan('a', string='b', unknown=1)):
        try:
            invoke()
        except TypeError as error:
            pass
        else:
            raise AssertionError('scan 绑定必须拒绝')
    try:
        scanner.scan('a', string='b', unknown=1)
    except TypeError as error:
        assert str(error) == "Scanner.scan() got multiple values for argument 'string'"
    else:
        raise AssertionError('重复参数应先于未知参数')
    assert not hasattr(scanner, 'match')
    print('construction-binding OK')


def iteration():
    events = []

    def lexicon():
        events.append('first')
        yield ('a', 'A')
        events.append('bad')
        yield ('[', 'BAD')
        events.append('unreachable')
        yield ('b', 'B')

    try:
        re.Scanner(lexicon())
    except re.error as error:
        assert error.pattern == '[' and error.pos == 0
    else:
        raise AssertionError('当前 phrase 语法错必须停止迭代')
    assert events == ['first', 'bad']
    unpack_events = []

    def entry():
        for value in ('[', 'A', 'extra'):
            unpack_events.append(value)
            yield value

    try:
        re.Scanner([entry()])
    except ValueError as error:
        assert str(error) == 'too many values to unpack (expected 2)'
    else:
        raise AssertionError('解包应先于语法错误')
    assert unpack_events == ['[', 'A', 'extra']
    gen = ((phrase, action) for phrase, action in [('a', 'A')])
    scanner = re.Scanner(gen)
    assert scanner.lexicon is gen and scanner.scan('!') == ([], '!')
    try:
        scanner.scan('a')
    except TypeError:
        pass
    else:
        raise AssertionError('生成器不能在首 token 下标')
    assert not hasattr(scanner, 'match')
    original = RuntimeError('iterator identity')

    try:
        re.Scanner(Broken(original))
    except RuntimeError as error:
        assert error is original
    else:
        raise AssertionError('不能包装用户迭代异常')
    print('construction-iteration OK')


def partial():
    scanner = re.Scanner([('a', lambda s, token: token)])
    assert scanner.scan('aa!') == (['a', 'a'], '!')
    plan, match = scanner.scanner, scanner.match
    bad = [('[', 'NEW')]
    try:
        scanner.__init__(bad)
    except re.error:
        pass
    else:
        raise AssertionError('reinit 应失败')
    assert scanner.lexicon is bad and scanner.scanner is plan and scanner.match is match
    assert scanner.scan('aa!') == (['NEW', 'NEW'], '!')
    assert scanner.match is match
    good = [('b', None)]
    scanner.__init__(good)
    assert scanner.lexicon is good and scanner.scanner is not plan
    assert scanner.match is match and scanner.scan('bb!') == ([], '!')
    scanner.custom = 7
    assert scanner.custom == 7
    del scanner.custom
    assert not hasattr(scanner, 'custom')
    print('construction-partial OK')


cases = [('binding', binding), ('iteration', iteration), ('partial', partial)]
selected = sys.argv[1] if len(sys.argv) > 1 else 'all'
assert selected == 'all' or any(name == selected for name, function in cases)
for name, function in cases:
    if selected == 'all' or selected == name:
        function()
