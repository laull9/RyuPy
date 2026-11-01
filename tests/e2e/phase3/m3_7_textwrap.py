import textwrap as t

for text, width in (('hello world', 5), ('a\tb', 4), ('a\nb\vc\fd\re', 3), ('a\u00a0b c', 3), ('你好世界abc', 3), ('😀😀abcd', 3), ('ab-cd-ef', 4), ('goof-ball', 5), ('foo--bar', 4), ('a-b-c', 3), ('  a b  ', 3), ('', 2), ('   ', 2)):
    print(t.wrap(text, width))
print(t.fill('one two three four', 7))
print(t.wrap('a\tb', 4, expand_tabs=False))
print(t.wrap('a\tb', 4, tabsize=2))
print(t.wrap('a\tb', 4, tabsize=0))
print(t.wrap('a\nb c', 3, replace_whitespace=False))
print(t.wrap('abcdef', 2, break_long_words=False))
print(t.wrap('goof-ball a-b-c', 5, break_on_hyphens=False))
print(t.wrap('  a b  ', 3, drop_whitespace=False))
print(t.wrap('abc', 1, initial_indent='>>', subsequent_indent='--'))
print(t.wrap('😀abcdef', 4, initial_indent='😀', subsequent_indent='>'))
for text in ('', '   ', 'abc'):
    try:
        t.wrap(text, 0)
    except ValueError:
        print('invalid width')
for text in (None, 1):
    try:
        t.wrap(text, 0)
    except AttributeError:
        print('text error before width')
try:
    t.wrap('abc', unknown_option=True)
except TypeError:
    print('unknown option')
