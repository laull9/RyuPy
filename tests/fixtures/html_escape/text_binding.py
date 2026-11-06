# ryupy: pending
# 通用escape文本、身份、真实调用形状与固定CP原错误；不导入第三方项目。
import html
from html import escape

text = '&<>"\''
assert escape(text) == '&amp;&lt;&gt;&quot;&#x27;'
assert escape(text, False) == '&amp;&lt;&gt;"\''
assert escape('&amp;') == '&amp;amp;'
assert escape('😀\x00中文&\n') == '😀\x00中文&amp;\n'
print('H1.text', repr(escape(text)), repr(escape('😀\x00中文&\n')))

results = [html.escape(text), escape(s=text), escape(text, quote=True),
           escape(quote=True, s=text), escape(*[text]),
           escape(*[text, True]), escape(**{'s': text, 'quote': True})]
assert all(value == results[0] and type(value) is str for value in results)
assert escape(*[text], **{'quote': False}) == escape(text, False)
print('H1.binding', results)

for value in [False, None, 0, '', [], (), {}]:
    assert escape(text, value) == '&amp;&lt;&gt;"\''
for value in [True, 1, 'x', [1], (1,), {'x': 1}]:
    assert escape(text, value) == results[0]
print('H1.quote-values', True)

plain = ''.join(['ordinary', ' payload'])
quotes = ''.join(['quotes', '"\''])
assert escape(plain) is plain and escape(quotes, False) is quotes
empty = ''
assert escape(empty) is empty
print('H1.identity', escape(plain) is plain, escape(quotes, False) is quotes)


class Inherited(str):
    pass


class Override(str):
    def replace(self, old, new):
        events.append((old, new))
        return self


for value in [Inherited('plain'), Inherited(text), Inherited('')]:
    result = escape(value)
    assert type(result) is str and result is not value
    assert result == escape(str(value))
print('H1.subclass-root', True)
events = []
sub = Override(text)
assert escape(sub) is sub
assert events == [('&', '&amp;'), ('<', '&lt;'), ('>', '&gt;'),
                  ('"', '&quot;'), ("'", '&#x27;')]
print('H1.subclass-override', events)

saved = escape
container = [saved]


def supply():
    return saved


assert container[0](text) == results[0] and supply()(text) == results[0]
try:
    html.escape = lambda value: value
    assert html.escape(text) is text and saved(text) == results[0]
    assert escape is saved
    print('H1.module-rebind', html.escape(text), saved(text), escape is saved)
finally:
    html.escape = saved


class Subject:
    def replace(self, old, new):
        events.append('unexpected-replace')
        raise AssertionError('binder must not enter body')


subject = Subject()
missing = "escape() missing 1 required positional argument: 's'"
unknown = "escape() got an unexpected keyword argument 'bad'"
duplicate_s = "escape() got multiple values for argument 's'"
duplicate_quote = "escape() got multiple values for argument 'quote'"
too_many = 'escape() takes from 1 to 2 positional arguments but 3 were given'
for label, args, keywords, message in [
    ('missing', (), {}, missing),
    ('quote-only', (), {'quote': False}, missing),
    ('unknown', (subject,), {'bad': 1}, unknown),
    ('unknown-missing', (), {'bad': 1}, unknown),
    ('too-many', (subject, True, 3), {}, too_many),
    ('duplicate-s', (subject,), {'s': subject}, duplicate_s),
    ('duplicate-quote', (subject, True), {'quote': False}, duplicate_quote),
    ('many-unknown', (subject, True, 3), {'bad': 1}, unknown),
    ('many-duplicate-s', (subject, True, 3), {'s': subject}, duplicate_s),
    ('many-duplicate-quote', (subject, True, 3), {'quote': False}, duplicate_quote),
    ('duplicate-then-unknown', (subject,), {'s': subject, 'bad': 1}, duplicate_s),
    ('unknown-then-duplicate', (subject,), {'bad': 1, 's': subject}, unknown),
]:
    events = []
    try:
        escape(*args, **keywords)
    except TypeError as error:
        assert str(error) == message and events == []
        print('H1.' + label, type(error).__name__, str(error), events)
    else:
        raise AssertionError(label + ' must raise')


def argument(name, value):
    events.append(name)
    return value


events = []
try:
    escape(argument('s', subject), argument('quote', True),
           argument('extra', 3), bad=argument('bad', 1))
except TypeError as error:
    assert str(error) == unknown
    assert events == ['s', 'quote', 'extra', 'bad']
    print('H1.argument-order', type(error).__name__, str(error), events)
else:
    raise AssertionError('binding failure expected')
