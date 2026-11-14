# 首切独立验收：全局标志与外围捕获的既有公开接口；不依赖 groupindex/regs/lastindex/lastgroup。
import re

for binary in (False, True):
    def domain(text):
        return text.encode('ascii') if binary else text

    # 对每种标志同时核对模块和编译模式的迭代、搜索、查找结果，不只打印一列信息。
    for label, pattern, flags, source, expected in [
        ('ignorecase', r'(?<=a)b', re.I, 'ab AB xb', [(1, 2), (4, 5)]),
        ('ascii', r'(?<=\w)x', re.A, 'ax _x 1x -x', [(1, 2), (4, 5), (7, 8)]),
        ('dotall', r'(?<=.)x', re.S, 'ax\nx', [(1, 2), (3, 4)]),
        ('without-dotall', r'(?<=.)x', 0, 'ax\nx', [(1, 2)]),
        ('verbose', '(?<= [a-c] # 左侧字符\n ) x', re.X, 'ax bx dx', [(1, 2), (4, 5)]),
        ('classes', r'(?<=[a-c\t/])X', 0, 'aX\tX/XdX', [(1, 2), (3, 4), (5, 6)]),
        ('escape', r'(?<=\x41\x42)c', re.I, 'ABc abC', [(2, 3), (6, 7)]),
        ('negative', r'(?<![a-c])x', re.I, 'ax dx X', [(4, 5), (6, 7)]),
    ]:
        # VERBOSE 注释也经 ASCII 二进制域转换，采用同一逻辑的英文注释版本。
        if binary and label == 'verbose':
            pattern = '(?<= [a-c] # left character\n ) x'
        pattern = domain(pattern)
        subject = domain(source)
        compiled = re.compile(pattern, flags)
        spans = [m.span() for m in compiled.finditer(subject)]
        assert spans == expected
        assert [m.span() for m in re.finditer(pattern, subject, flags)] == expected
        assert compiled.findall(subject) == [subject[start:end] for start, end in expected]
        found = compiled.search(subject)
        assert found.span() == expected[0]
        start, end = expected[0]
        assert compiled.match(subject, start, end).span() == (start, end)
        assert compiled.fullmatch(subject, start, end).span() == (start, end)
        print(binary, label, spans, compiled.findall(subject))

    # 左右外围捕获有命名和匿名混合；可选尾组未参与时 defaults、span、expand 均须一致。
    pattern = domain(r'(?P<pre>a)(?<=a)(b)(?P<tail>c)?')
    compiled = re.compile(pattern)
    assert compiled.groups == 3
    template = domain(r'<\g<pre>:\2:\g<tail>>')
    subject = domain('ab abc')
    matches = list(compiled.finditer(subject))
    assert [m.span() for m in matches] == [(0, 2), (3, 6)]
    first, second = matches
    assert first.group(0, 1, 2, 3) == (domain('ab'), domain('a'), domain('b'), None)
    assert first.groups(domain('-')) == (domain('a'), domain('b'), domain('-'))
    assert first.groupdict(domain('-')) == {'pre': domain('a'), 'tail': domain('-')}
    assert first.span('pre') == (0, 1) and first.span('tail') == (-1, -1)
    assert second.groups() == (domain('a'), domain('b'), domain('c'))
    assert second.groupdict() == {'pre': domain('a'), 'tail': domain('c')}
    assert second.start(2) == 4 and second.end(2) == 5
    assert first.expand(template) == domain('<a:b:>')
    assert second.expand(template) == domain('<a:b:c>')
    for match in matches:
        assert match.re is compiled and match.string is subject
        print('capture', binary, match.group(0, 1, 2, 3), match.groups(domain('-')),
              match.groupdict(domain('-')), [match.span(i) for i in range(4)], match.expand(template))

    # 保存绑定方法并核对完整匹配与替换，替换回调验证实际捕获值和调用顺序。
    saved_fullmatch = compiled.fullmatch
    saved_sub = compiled.sub
    assert saved_fullmatch(domain('abc')).groups() == (domain('a'), domain('b'), domain('c'))
    assert saved_sub(template, subject) == domain('<a:b:> <a:b:c>')
    assert compiled.subn(template, subject, count=1) == (domain('<a:b:> abc'), 1)
    events = []
    def replace(match):
        events.append((match.span(), match.groups()))
        return match.expand(template)
    replaced = compiled.sub(replace, subject)
    assert replaced == domain('<a:b:> <a:b:c>')
    assert events == [((0, 2), (domain('a'), domain('b'), None)),
                      ((3, 6), (domain('a'), domain('b'), domain('c')))]
    print('replacement', binary, replaced, events, compiled.subn(template, subject, count=1))

# 文本 Unicode/ASCII 类的差异独立核对，不把 bytes 域误当 Unicode。
subject = 'Ωx 1x _x'
assert [m.span() for m in re.finditer(r'(?<=\w)x', subject)] == [(1, 2), (4, 5), (7, 8)]
assert [m.span() for m in re.finditer(r'(?<=\w)x', subject, re.A)] == [(4, 5), (7, 8)]
print('unicode-ascii', re.findall(r'(?<=\w)x', subject), re.findall(r'(?<=\w)x', subject, re.A))
