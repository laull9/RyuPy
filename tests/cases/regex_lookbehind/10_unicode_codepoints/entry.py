# 独立 Unicode 码点契约：不调用 ascii，不输出代理字符，保留完整位置和宽度行为。
import re

astral = chr(0x1f600)
high = chr(0xd800)
low = chr(0xdc00)
combining = chr(0x0301)
assert [ord(astral), ord(high), ord(low)] == [128512, 55296, 56320]
# 此处仅分别测试不相邻的孤立代理；相邻高低代理在独立长尾用例中保留。
for label, pattern, subject, expected in [
    ('astral-dot', r'(?<=.)x', astral + 'x', [(1, 2)]),
    ('astral-literal', '(?<=' + astral + ')x', astral + 'x', [(1, 2)]),
    ('astral-negative', '(?<!' + astral + ')x', astral + 'x ax', [(4, 5)]),
    ('two-codepoints', r'(?<=.{2})x', astral + 'x a' + astral + 'x', [(5, 6)]),
    ('combining-two', '(?<=e' + combining + ')x', 'e' + combining + 'x', [(2, 3)]),
    ('combining-one', r'(?<=.)x', 'e' + combining + 'x', [(2, 3)]),
    ('high-literal', '(?<=' + high + ')x', high + 'x', [(1, 2)]),
    ('high-dot', r'(?<=.)x', high + 'x', [(1, 2)]),
    ('low-literal', '(?<=' + low + ')x', low + 'x', [(1, 2)]),
    ('low-dot', r'(?<=.)x', low + 'x', [(1, 2)]),
    ('isolated-separated', r'(?<=.)x', high + 'x ' + low + 'x', [(1, 2), (4, 5)]),
    ('equal-one-branch', '(?<=a|' + astral + ')x', 'ax ' + astral + 'x', [(1, 2), (4, 5)]),
    ('equal-two-branch', '(?<=a' + astral + '|' + astral + 'a)x',
     'a' + astral + 'x ' + astral + 'ax', [(2, 3), (6, 7)]),
]:
    compiled = re.compile(pattern)
    matches = list(compiled.finditer(subject))
    spans = [m.span() for m in matches]
    assert spans == expected
    print(label, [ord(c) for c in pattern], [ord(c) for c in subject], spans)
    print('groups', [[ord(c) for c in m.group()] for m in matches])

subject = 'a' + astral + 'x ' + high + 'x ' + low + 'x'
compiled = re.compile(r'(?<=.)x')
for pos, endpos, expected in [
    (0, 9, [(2, 3), (5, 6), (8, 9)]),
    (2, 3, [(2, 3)]), (5, 6, [(5, 6)]), (8, 9, [(8, 9)]),
    (2, 2, []), (3, 5, []), (3, 6, [(5, 6)]), (6, 9, [(8, 9)]),
    (-2, 99, [(2, 3), (5, 6), (8, 9)]), (10, 99, []), (6, 3, []),
]:
    spans = [m.span() for m in compiled.finditer(subject, pos, endpos)]
    assert spans == expected
    print('window', pos, endpos, spans)
    for label, method in [('match', compiled.match), ('search', compiled.search),
                          ('fullmatch', compiled.fullmatch)]:
        match = method(subject, pos, endpos)
        print(label, None if match is None else (match.span(), match.pos, match.endpos))
for pos in (2, 5, 8):
    assert compiled.fullmatch(subject, pos, pos + 1).span() == (pos, pos + 1)
print('fullmatch-left-context', True)
