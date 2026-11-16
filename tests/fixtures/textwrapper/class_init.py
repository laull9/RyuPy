"""T1：真实TextWrapper类、完整初始化、普通继承与setter顺序；不声称vars支持。"""
import textwrap
from textwrap import TextWrapper as TW


FIELDS = ('width', 'initial_indent', 'subsequent_indent', 'expand_tabs',
          'replace_whitespace', 'fix_sentence_endings', 'break_long_words',
          'drop_whitespace', 'break_on_hyphens', 'tabsize', 'max_lines', 'placeholder')
DEFAULTS = (70, '', '', True, True, False, True, True, True, 8, None, ' [...]')


def fields(instance):
    return tuple(getattr(instance, name) for name in FIELDS)


def expect_error(label, operation, expected_type, message):
    try:
        operation()
    except Exception as error:
        assert type(error) is expected_type
        assert str(error) == message and error.args == (message,)
        print(label, type(error).__name__, str(error), error.args)
    else:
        raise AssertionError('expected constructor error')


wrapper = TW()
assert TW is textwrap.TextWrapper and type(wrapper) is TW and isinstance(wrapper, TW)
assert TW.__name__ == 'TextWrapper' and TW.__module__ == 'textwrap'
assert fields(wrapper) == DEFAULTS
print('T1.defaults', fields(wrapper), TW.__name__, TW.__module__)
ten = TW(4, '>', '-', False, False, False, False, False, False, 2)
assert fields(ten) == (4, '>', '-', False, False, False, False, False, False, 2, None, ' [...]')
print('T1.ten-positional', fields(ten))
expect_error('T1.kwonly', lambda: TW(*range(11)), TypeError,
             'TextWrapper.__init__() takes from 1 to 11 positional arguments but 12 were given')
expect_error('T1.duplicate', lambda: TW(4, width=5), TypeError,
             "TextWrapper.__init__() got multiple values for argument 'width'")
expect_error('T1.unknown', lambda: TW(no_such_option=True), TypeError,
             "TextWrapper.__init__() got an unexpected keyword argument 'no_such_option'")

token = object()
raw = TW(width=token, expand_tabs=token, tabsize=token, max_lines=token, placeholder=token)
identity = [getattr(raw, name) is token for name in
            ('width', 'expand_tabs', 'tabsize', 'max_lines', 'placeholder')]
assert identity == [True] * 5
print('T1.raw-identity', identity)
extra = ['kept']
wrapper = TW(4, initial_indent='>')
wrapper.extra = extra
assert TW.__init__(wrapper, 9, subsequent_indent='-') is None
assert fields(wrapper) == (9, '', '-', True, True, False, True, True, True, 8, None, ' [...]')
assert wrapper.extra is extra
print('T1.repeat', fields(wrapper), wrapper.extra)
a, b = TW(3), TW(8)
a.width = 1
a.extra = []
assert a.width == 1 and b.width == 8 and not hasattr(b, 'extra')
assert a.wrap('ab cd') == ['a', 'b', 'c', 'd'] and b.wrap('ab cd') == ['ab cd']
print('T1.independent', a.width, b.width, a.wrap('ab cd'), b.wrap('ab cd'))

events = []
marker = ValueError('stop setter')


class Setter(TW):
    @property
    def width(self):
        return self.saved_width

    @width.setter
    def width(self, item):
        events.append(['width', item])
        self.saved_width = item

    @property
    def initial_indent(self):
        return self.saved_indent

    @initial_indent.setter
    def initial_indent(self, item):
        events.append(['initial_indent', item])
        self.saved_indent = item

    @property
    def subsequent_indent(self):
        return None

    @subsequent_indent.setter
    def subsequent_indent(self, item):
        events.append(['subsequent_indent', item])
        raise marker


partial = object.__new__(Setter)
try:
    TW.__init__(partial, 7, '>', '-')
except ValueError as error:
    assert error is marker
else:
    raise AssertionError('setter exception swallowed')
assert events == [['width', 7], ['initial_indent', '>'], ['subsequent_indent', '-']]
assert partial.saved_width == 7 and partial.saved_indent == '>'
assert not hasattr(partial, 'expand_tabs') and not hasattr(partial, 'placeholder')
print('T1.setter-partial', events, partial.saved_width, partial.saved_indent, str(marker))


class Duck:
    pass


duck = Duck()
assert TW.__init__(duck, 6) is None and not isinstance(duck, TW)
assert fields(duck) == (6, '', '', True, True, False, True, True, True, 8, None, ' [...]')
print('T1.duck-init', fields(duck))

base_events = []


def get_base():
    base_events.append('base')
    return textwrap.TextWrapper


BaseAlias = get_base()


class Child(BaseAlias):
    def __init__(self, width):
        super().__init__(width, initial_indent='>')

    def wrap(self, text):
        return super().wrap(text)


child = Child(4)
assert BaseAlias is TW and base_events == ['base'] and Child.__bases__[0] is TW
assert type(child) is Child and isinstance(child, Child) and isinstance(child, TW)
assert issubclass(Child, TW) and child.wrap('ab cd') == ['>ab', 'cd']
assert TW.__init__(child, 5) is None and child.wrap('ab cd') == ['ab cd']
print('T1.real-inheritance', base_events, child.width, child.wrap('ab cd'))
