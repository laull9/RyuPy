# 首切完整二进制生命周期：仅用已有组值/跨度/源身份接口，不假设额外 Match 元数据支持。
import re

positive = re.compile(br'(?<=a)(b)')
negative = re.compile(br'(?<!a)(b)')
source = bytearray(b'ab zb')
alias = source
old = positive.search(source)
assert old.span() == (1, 2) and old.span(1) == (1, 2)
assert old.string is alias and old.re is positive
assert negative.search(source).span() == (4, 5)
# 只改后顾读取的前文：旧匹配仍存在且值不变，新匹配正负断言互换。
source[0] = ord('z')
assert old.group() == b'b' and old.groups() == (b'b',)
assert positive.search(source) is None
assert negative.match(source, 1, 2).span() == (1, 2)
print('context-changed', old.span(), old.groups(), [m.span() for m in negative.finditer(source)])
# 再改旧匹配本体：组结果读取当前源值，保存的跨度不变，也不重新验证正则。
source[1] = ord('c')
assert old.group(0, 1) == (b'c', b'c') and old.groups() == (b'c',)
assert old.start(1) == 1 and old.end(1) == 2 and old.string is alias
assert negative.match(source, 1, 2) is None
print('old-match-live', old.group(0, 1), old.start(), old.end(), old.string is alias)
# 反向改前文使第二位置满足正后顾，负后顾不再接受；pos不能截断原前文。
source[3] = ord('a')
assert positive.fullmatch(source, 4, 5).span() == (4, 5)
assert negative.match(source, 4, 5) is None
print('context-restored', positive.search(source).span(), list(negative.finditer(source)))

# 每个迭代器先消费一次，再修改尚未扫描部分的左侧上下文；分别制造消失与新增。
for label, compiled, initial, replacement, expected in [
    ('positive-remove', positive, b'ab ab', ord('z'), []),
    ('positive-create', positive, b'ab zb', ord('a'), [(4, 5)]),
    ('negative-remove', negative, b'zb zb', ord('a'), []),
    ('negative-create', negative, b'zb ab', ord('z'), [(4, 5)]),
]:
    source = bytearray(initial)
    iterator = compiled.finditer(source)
    first = next(iterator)
    assert first.span() == (1, 2) and first.string is source
    source[3] = replacement
    later = list(iterator)
    assert [m.span() for m in later] == expected
    for match in later:
        assert match.string is source and match.re is compiled and match.groups() == (b'b',)
    assert list(iterator) == []
    print(label, first.span(), [m.span() for m in later], [m.groups() for m in later])

# immutable bytes 对照和 bytearray 都走同一替换/分隔合同；组在后顾外部。
for source in (b'ab ab', bytearray(b'ab ab')):
    events = []
    def replacement(match):
        assert match.string is source and match.re is positive
        events.append((match.span(), match.group(0, 1), match.start(1), match.end(1)))
        return b'B'
    result = positive.subn(replacement, source)
    assert result == (b'aB aB', 2)
    assert events == [((1, 2), (b'b', b'b'), 1, 2), ((4, 5), (b'b', b'b'), 4, 5)]
    assert positive.subn(br'<\1>', source, count=1) == (b'a<b> ab', 1)
    assert positive.split(source) == [b'a', b'b', b' a', b'b', b'']
    assert positive.split(source, maxsplit=1) == [b'a', b'b', b' ab']
    assert positive.findall(source) == [b'b', b'b']
    print(type(source).__name__, result, events, positive.split(source),
          positive.subn(br'<\1>', source, count=1))

# 不匹配负后顾时不可调用回调；空输入和输入不足仍保持原起点语义。
events = []
def never(match):
    events.append(match.span())
    return b'X'
assert negative.subn(never, bytearray(b'ab')) == (b'ab', 0)
assert events == []
assert positive.search(bytearray()) is None
assert negative.fullmatch(bytearray(b'b')).span() == (0, 1)
print('no-callback-and-start', events, negative.fullmatch(bytearray(b'b')).groups())
