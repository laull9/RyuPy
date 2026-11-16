"""T4：module真实工厂/根方法重绑、消费时点与模块context后的完整用户hook链。"""
import textwrap
from textwrap import TextWrapper as TW


def expect_error(label, operation, expected_type, message):
    try:
        operation()
    except Exception as error:
        assert type(error) is expected_type
        assert str(error) == message and error.args == (message,)
        print(label, type(error).__name__, str(error), error.args)
    else:
        raise AssertionError('expected TextWrapper error')


events = []


class Replacement:
    def __init__(self, **kwargs):
        events.append(['init', kwargs])

    def wrap(self, text):
        events.append(['wrap', text])
        return ['replacement']

    def fill(self, text):
        events.append(['fill', text])
        return 'replacement fill'


original_factory = textwrap.TextWrapper
try:
    textwrap.TextWrapper = Replacement
    wrapped = textwrap.wrap('a', 5, extra=True)
    filled = textwrap.fill('b', 6)
    assert wrapped == ['replacement'] and filled == 'replacement fill'
    assert events == [['init', {'width': 5, 'extra': True}], ['wrap', 'a'],
                      ['init', {'width': 6}], ['fill', 'b']]
    print('T4.module-factory', wrapped, filled, events)
finally:
    textwrap.TextWrapper = original_factory
assert textwrap.TextWrapper is TW

original_wrap = TW.wrap
try:
    TW.wrap = lambda self, text: ['changed method']
    assert textwrap.wrap('a') == ['changed method'] and textwrap.fill('a') == 'changed method'
    print('T4.module-method', textwrap.wrap('a'), textwrap.fill('a'))
finally:
    TW.wrap = original_wrap

split_events = []


def replacement_split(self, text):
    split_events.append([text, self.width])
    return ['made', ' ', 'later']


original_split = TW._split
try:
    TW._split = replacement_split
    assert textwrap.wrap('ignored', 30) == ['made later']
    assert textwrap.fill('other', 30) == 'made later'
    assert split_events == [['ignored', 30], ['other', 30]]
    print('T4.module-inner-hook', split_events)
finally:
    TW._split = original_split

deleted = TW(3)
del deleted.width
expect_error('T4.deleted-width', lambda: deleted.wrap('abc'), AttributeError,
             "'TextWrapper' object has no attribute 'width'")
expect_error('T4.zero-empty', lambda: TW(0).wrap(''), ValueError,
             'invalid width 0 (must be > 0)')
expect_error('T4.text-before-width', lambda: TW(0).wrap(None), AttributeError,
             "'NoneType' object has no attribute 'expandtabs'")
expect_error('T4.disabled-text',
             lambda: TW(0, expand_tabs=False, replace_whitespace=False).wrap(None), TypeError,
             "expected string or bytes-like object, got 'NoneType'")

token = object()
unused_tab = TW(4, expand_tabs=False, tabsize=token)
assert unused_tab.tabsize is token and unused_tab.wrap('abc') == ['abc']
print('T4.unused-tabsize', unused_tab.wrap('abc'))
unused_placeholder = TW(4, placeholder=token)
assert unused_placeholder.placeholder is token and unused_placeholder.wrap('ab cd') == ['ab', 'cd']
print('T4.unused-placeholder', unused_placeholder.wrap('ab cd'))

# 构造仍走module当前工厂；仅两个确切CP库帧由driver投影，所有用户帧必须保留。
def hook_leaf(chunks):
    assert chunks == ['ab', ' ', 'cd']
    raise LookupError('TextWrapper hook chain failed')


class Failure(TW):
    def _wrap_chunks(self, chunks):
        return hook_leaf(chunks)


def middle():
    textwrap.wrap('ab cd', 4)


def outer():
    middle()


textwrap.TextWrapper = Failure
print('T4.chain', 'ready')
try:
    raise ValueError('TextWrapper outer context')
except ValueError:
    outer()
