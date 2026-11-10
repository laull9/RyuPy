# ryupy: pending
# 此源绝不赋值或global声明KeyError，必须命中直接builtin入口；两个argv只供真实错误退出验收。
import sys


class BadRepr:
    def __repr__(self):
        raise ValueError('repr marker')


if len(sys.argv) > 1:
    if sys.argv[1] == 'uncaught':
        raise KeyError('missing')
    if sys.argv[1] == 'bad-repr':
        raise KeyError(BadRepr())
    raise AssertionError('unknown mode')

empty = KeyError()
none = KeyError(None)
blank = KeyError('')
assert empty.args == () and str(empty) == '' and repr(empty) == 'KeyError()'
assert none.args == (None,) and str(none) == 'None'
assert blank.args == ('',) and str(blank) == "''"
assert str(KeyError('missing')) == "'missing'"
assert str(KeyError(17)) == '17' and KeyError(17).args == (17,)
print('direct zero None empty and scalar args')

items = [1]
pair = (1, 'two')
list_error = KeyError(items)
pair_error = KeyError(pair)
assert list_error.args[0] is items and pair_error.args[0] is pair
items.append(2)
assert str(list_error) == '[1, 2]' and str(pair_error) == "(1, 'two')"
two = KeyError(items, pair)
three = KeyError(items, pair, None)
assert two.args[0] is items and two.args[1] is pair
assert len(three.args) == 3 and three.args[2] is None
events = []


def mark(value):
    events.append(value)
    return value


four = KeyError(mark('a'), mark('b'), mark('c'), mark('d'))
assert four.args == ('a', 'b', 'c', 'd') and events == ['a', 'b', 'c', 'd']
assert str(four) == "('a', 'b', 'c', 'd')"
saved = KeyError
assert saved(items).args[0] is items
print('direct identity and complete positional arguments')


class Key:
    def __init__(self):
        self.value = 'first'

    def __str__(self):
        raise AssertionError('key str must not run')

    def __repr__(self):
        events.append('repr')
        return '<key:' + self.value + '>'


events.clear()
key = Key()
error = KeyError(key)
assert events == [] and error.args[0] is key
assert str(error) == '<key:first>' and events == ['repr']
key.value = 'second'
assert str(error) == '<key:second>' and events == ['repr', 'repr']
events.clear()
assert repr(error) == 'KeyError(<key:second>)' and events == ['repr']
events.clear()
print(error)
assert events == ['repr']
print('deferred repr and repeated display')

marker = ValueError('same repr failure')


class RaisingKey:
    def __repr__(self):
        events.append('raise')
        raise marker


events.clear()
failed = KeyError(RaisingKey())
assert events == []
try:
    str(failed)
except ValueError as caught:
    assert caught is marker and events == ['raise']
else:
    raise AssertionError('repr failure was swallowed')


class WrongKey:
    def __repr__(self):
        return 7


wrong = KeyError(WrongKey())
try:
    str(wrong)
except TypeError as caught:
    assert str(caught) == '__repr__ returned non-string (type int)'
else:
    raise AssertionError('wrong repr result was accepted')
print('explicit repr failures propagate')


class Display(KeyError):
    def __str__(self):
        return 'custom display'


assert str(Display('plain key')) == 'custom display'
try:
    {}['missing']
except KeyError as caught:
    assert caught.args == ('missing',) and str(caught) == "'missing'"
else:
    raise AssertionError('dict lookup did not raise')
print('user str override and internal dictionary error preserved')
