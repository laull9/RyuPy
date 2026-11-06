# ryupy: pending
# 字段驱动报告：真实模板、语法错误与嵌套规格；CP先行，不依赖旧f-string。
events = []
class Value:
    def __format__(self, spec):
        events.append(('format', spec))
        return 'F<' + spec + '>'
v = Value()
assert ''.format(v, unused=v) == ''
assert 'abc'.format(v, unused=v) == 'abc'
assert events == []
assert '{self}:{format_string}:{}:{other}'.format('x', self='a', format_string='b', other='o') == 'a:b:x:o'
assert '{{{0}}} {0}'.format('x') == '{x} x'
assert '{name} {} {other}'.format('x', name='n', other='o') == 'n x o'
assert '{0:04x}|{1:>4s}|{2:.3g}'.format(42, 'x', 12.5) == '002a|   x|12.5'
assert '😀 {0:🦊>4} \ud800'.format('é') == '😀 🦊🦊🦊é \ud800'
for number in ('00', '٠', '０', '𝟎', '٠0', '0' * 80):
    assert ('{' + number + '}').format('x') == 'x'
for key in ('²', '-1', '+1', ' ', 'x y'):
    assert ('{' + key + '}').format(**{key: 'named'}) == 'named'
assert '{[x]} {}'.format({'x': 'item'}, 'next') == 'item next'
assert '{value:{width}.{precision}}'.format(value=v, width='4', precision='2') == 'F<4.2>'
assert '{:{}}'.format(v, '4') == 'F<4>'
assert '{0:{1:}}'.format(v, '4') == 'F<4>'
assert '{0:{{}}}'.format(v) == 'F<{}>'
assert '{0:}}}'.format(v) == 'F<>}'
assert '{0:{1}}'.format(v, '{2}') == 'F<{2}>'

def failure(text, args, kind, message, expected):
    events.clear()
    try:
        text.format(*args)
    except Exception as error:
        assert type(error).__name__ == kind
        assert str(error) == message
        assert events == expected
        print('error', kind, str(error), events)
    else:
        raise AssertionError('expected template error')

for text, message in (
    ('{', "Single '{' encountered in format string"),
    ('}', "Single '}' encountered in format string"),
    ('{0', "expected '}' before end of string"),
    ('{0!}', "unmatched '{' in format spec"),
    ('{0!ss}', "expected ':' after conversion specifier"),
    ('{0:{1}', "unmatched '{' in format spec"),
):
    failure(text, (v,), 'ValueError', message, [])
failure('{0} {', (v,), 'ValueError', "Single '{' encountered in format string", [('format', '')])
failure('{0} }', (v,), 'ValueError', "Single '}' encountered in format string", [('format', '')])
failure('{} {0}', (v,), 'ValueError', 'cannot switch from automatic field numbering to manual field specification', [('format', '')])
failure('{0} {}', (v,), 'ValueError', 'cannot switch from manual field specification to automatic field numbering', [('format', '')])
failure('{0:{}}', (v, '4'), 'ValueError', 'cannot switch from manual field specification to automatic field numbering', [])
failure('{:{0}}', (v, '4'), 'ValueError', 'cannot switch from automatic field numbering to manual field specification', [])
failure('{0:{1:{2}}}', (v, v, v), 'ValueError', 'Max string recursion exceeded', [])
failure('{0:{1:{missing}}}', (v, v), 'ValueError', 'Max string recursion exceeded', [])
failure('{0:{missing:{2}}}', (v, v, v), 'KeyError', "'missing'", [])
failure('{1.a!q}', (v,), 'IndexError', 'Replacement index 1 out of range for positional args tuple', [])
failure('{missing!q}', (v,), 'KeyError', "'missing'", [])
failure('{9223372036854775807}', (), 'IndexError', 'Replacement index 9223372036854775807 out of range for positional args tuple', [])
# 尾字母前数字溢出来自固定官方源码推导；首次本源CP验证其断言。
for field in ('9223372036854775808', '9' * 100, '9223372036854775808x'):
    failure('{' + field + '}', (), 'ValueError', 'Too many decimal digits in format string', [])
assert '{}'.format('reset') == 'reset'
print('string-format-report-ok')
