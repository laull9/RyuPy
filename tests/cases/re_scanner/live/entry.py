# ryupy: pending
# 每 token 重新读取 action，单次扫描固定 backend；嵌套扫描不恢复公共 match。
import re
import sys


class Item:
    def __init__(self, events):
        self.events = events

    def __getitem__(self, key):
        self.events.append(('item', key))
        assert key == 1
        return 'live'


class Live:
    # 每次取词条仍创建新 Item，两个层级只共享本次场景的同一事件列表。
    def __init__(self, events):
        self.events = events

    def __getitem__(self, key):
        self.events.append(('lexicon', key))
        assert key == 0
        return Item(self.events)


class Broken:
    def __init__(self, error):
        self.error = error

    @property
    def scanner(self):
        raise self.error


def actions():
    def first(scanner, token):
        scanner.lexicon[1] = ('not-the-compiled-pattern', 'new')
        return token

    lexicon = [('a', first), ('b', 'old')]
    scanner = re.Scanner(lexicon)
    assert scanner.lexicon is lexicon and scanner.scan('ab!') == (['a', 'new'], '!')
    assert scanner.scan('not-the-compiled-pattern') == ([], 'not-the-compiled-pattern')

    def rebind(scanner, token):
        scanner.lexicon = [('unused', None), ('unused', 'Y')]
        return token

    scanner = re.Scanner([('a', rebind), ('b', 'B')])
    assert scanner.scan('ab!') == (['a', 'Y'], '!')
    events = []

    scanner = re.Scanner([('a', 'old')])
    scanner.lexicon = Live(events)
    assert scanner.scan('aa!') == (['live', 'live'], '!')
    assert events == [('lexicon', 0), ('item', 1), ('lexicon', 0), ('item', 1)]
    print('live-actions OK')


def backend():
    other = re.Scanner([('b', 'B')])
    saved = []

    def swap(scanner, token):
        saved.append(scanner.match)
        scanner.scanner = other.scanner
        return token

    scanner = re.Scanner([('a', swap), ('b', 'B')])
    original = scanner.scanner
    assert scanner.scan('ab!') == (['a', 'B'], '!')
    assert saved[0].re is original and scanner.scanner is other.scanner
    scanner.lexicon = [('b', 'next')]
    assert scanner.scan('bb!') == (['next', 'next'], '!')
    scanner.scanner = None
    try:
        scanner.scan('b')
    except AttributeError:
        pass
    else:
        raise AssertionError('缺少 backend.scanner 应自然报错')
    del scanner.scanner
    try:
        scanner.scan('b')
    except AttributeError:
        pass
    else:
        raise AssertionError('删除 scanner 应自然报错')
    original_error = LookupError('backend property')

    scanner.scanner = Broken(original_error)
    try:
        scanner.scan('b')
    except LookupError as error:
        assert error is original_error
    else:
        raise AssertionError('backend property 异常必须保身份')
    print('live-backend OK')


def nested():
    events = []

    def action(scanner, token):
        if token == 'a':
            outer = scanner.match
            events.append(scanner.scan('bb!'))
            assert scanner.match is not outer and scanner.match.group() == 'b'
        return token

    scanner = re.Scanner([('[ab]', action)])
    assert scanner.scan('aa!') == (['a', 'a'], '!')
    assert events == [(['b', 'b'], '!'), (['b', 'b'], '!')]
    assert scanner.match.group() == 'b' and scanner.match.string == 'bb!'
    old = scanner.match
    scanner.lexicon = []
    try:
        scanner.scan('a!')
    except IndexError:
        pass
    else:
        raise AssertionError('action 查找错误应保先前 match')
    assert scanner.match is old
    print('live-nested OK')


cases = [('actions', actions), ('backend', backend), ('nested', nested)]
selected = sys.argv[1] if len(sys.argv) > 1 else 'all'
assert selected == 'all' or any(name == selected for name, function in cases)
for name, function in cases:
    if selected == 'all' or selected == name:
        function()
