# ryupy: pending
"""mutable finditer的创建/最后yield/真耗尽、公开路由与exact/text控制；新组合首次CP守门。"""
import re


LOCKED = 'Existing exports of data: object cannot be re-sized'


def locked(data):
    before = bytes(data)
    try:
        data.append(33)
    except BufferError as error:
        assert str(error) == LOCKED and bytes(data) == before
        return str(error)
    raise AssertionError('active iterator did not lock resize')


def drain(iterator):
    result = []
    for unused in range(18):
        try:
            result.append(next(iterator))
        except StopIteration:
            return result
    raise AssertionError('bounded iterator failed to exhaust')


# 六载体四种调用路由共用小pattern；view自身释放在所有路由真正耗尽之后。
data = bytearray(b'!aXa?')
root = memoryview(data)
chars = root.cast('c')
child = root[1:4]
readonly = memoryview(b'!aXa?')
p = re.compile(br'(?P<x>a)(z)?')
module_saved = re.finditer
pattern_saved = p.finditer
for name, source in [('bytes', b'!aXa?'), ('bytearray', data), ('view_B', root),
                     ('view_c', chars), ('view_child', child), ('view_readonly', readonly)]:
    wanted = [(0, 1), (2, 3)] if name == 'view_child' else [(1, 2), (3, 4)]
    for route in ('module', 'module_saved', 'pattern', 'pattern_saved'):
        if route == 'module':
            iterator = re.finditer(p, source)
        elif route == 'module_saved':
            iterator = module_saved(pattern=p, string=source)
        elif route == 'pattern':
            iterator = p.finditer(string=source)
        else:
            iterator = pattern_saved(source)
        assert iter(iterator) is iterator
        found = drain(iterator)
        assert [match.span() for match in found] == wanted
        for match in found:
            assert type(match) is re.Match and match.re is p and match.string is source
            assert match.group() == b'a' and match.groups() == (b'a', None)
            assert match.groupdict() == {'x': b'a'}
            assert match.pos == 0 and match.endpos == len(source)
        sentinel = []
        assert next(iterator, sentinel) is sentinel
        print('I1.routes-' + name + '-' + route, wanted, len(source))
child.release()
chars.release()
root.release()
readonly.release()
data.append(33)
print('I1.routes-clean', data)

# 原C：创建立即锁；首次next前写可见；最后一次yield不是耗尽。
data = bytearray(b'aXa')
iterator = re.finditer(b'a', data)
assert locked(data) == LOCKED
data[0] = ord('b')
match = next(iterator)
assert match.string is data and match.group() == b'a' and match.span() == (2, 3)
assert match.pos == 0 and match.endpos == 3
assert locked(data) == LOCKED
data[2] = ord('c')
assert match.group() == b'c'
print('I1.creation-last-yield', match.group(), match.span(), match.endpos)
sentinel = []
assert next(iterator, sentinel) is sentinel
data.append(ord('a'))
assert next(iterator, sentinel) is sentinel and next(iterator, sentinel) is sentinel
assert match.group() == b'c' and match.span() == (2, 3) and match.endpos == 3
print('I1.exhausted-stays-done', data, match.group())

# 原C三种零产出情形均在创建时持lease，不以范围或数据为空提前释放。
for name, pattern, value, pos, end in [('nomatch', b'z', b'aaa', 0, 99),
                                      ('pos_gt_end', b'a', b'aaa', 2, 1),
                                      ('empty_subject', b'z', b'', 0, 99)]:
    data = bytearray(value)
    iterator = re.compile(pattern).finditer(data, pos, end)
    assert locked(data) == LOCKED
    assert next(iterator, sentinel) is sentinel
    data.append(33)
    assert next(iterator, sentinel) is sentinel
    print('I1.empty-window-' + name, data)

# 原exact范围控制扩成mutable组合，首次CP核定；巨大但合法ssize不得走int32截断。
bounds = [(0, 0), (-2, 1), (1, 2), (9, 9), (2, 1), (0, 1 << 62)]
wanted = [[(0, 0)], [(0, 1), (1, 1)], [(1, 2), (2, 2)], [(2, 2)], [], [(0, 2), (2, 2)]]
for index, pair in enumerate(bounds):
    data = bytearray(b'aa')
    iterator = re.compile(b'a*').finditer(data, pair[0], pair[1])
    assert locked(data) == LOCKED
    matches = drain(iterator)
    assert [match.span() for match in matches] == wanted[index]
    first = min(max(pair[0], 0), 2)
    last = min(max(pair[1], 0), 2)
    assert [(match.pos, match.endpos) for match in matches] == [(first, last)] * len(matches)
    data.append(33)
    print('I1.bounds-' + str(index), wanted[index], first, last)

# 尾空Match也要再next；长度8同位空/非空共17项，第18次才证明耗尽。
data = bytearray(b'a')
iterator = re.finditer(b'', data)
assert next(iterator).span() == (0, 0) and next(iterator).span() == (1, 1)
assert locked(data) == LOCKED
assert next(iterator, sentinel) is sentinel
data.append(33)
print('I1.empty-tail', data)
data = bytearray(b'aaaaaaaa')
iterator = re.finditer(b'|a', data)
found = drain(iterator)
assert len(found) == 17 and found[0].span() == (0, 0) and found[-1].span() == (8, 8)
assert [match.group() for match in found] == [b'', b'a'] * 8 + [b'']
data.append(33)
print('I1.drain-bound', len(found), found[-1].span(), len(data))

# 直接for/list消费者也必须实际走到终止并释放view自身export。
for route in ('for', 'list'):
    data = bytearray(b'aXa')
    view = memoryview(data)
    iterator = re.finditer(b'a', view)
    if route == 'for':
        found = []
        for match in iterator:
            found.append(match)
    else:
        found = list(iterator)
    assert [match.span() for match in found] == [(0, 1), (2, 3)]
    assert next(iterator, sentinel) is sentinel
    view.release()
    data.append(33)
    print('I1.consumer-' + route, [match.span() for match in found], data)

# exactbytes仍保完整捕获原身份，字节偏移和旧空匹配进度不受mutable接线影响。
exact = bytes([0, 65, 128, 255])
found = drain(re.compile(br'(?P<body>.+)', re.S).finditer(exact))
assert len(found) == 1 and found[0].string is exact
assert found[0].group() is exact and found[0].group(1) is exact
assert found[0].span() == (0, 4) and found[0].endpos == 4
assert [(match.group(), match.span()) for match in drain(re.finditer(b'.', exact, re.S))] == [
    (b'\x00', (0, 1)), (b'A', (1, 2)), (b'\x80', (2, 3)), (b'\xff', (3, 4))]
print('I1.exact-identity', found[0].group(), found[0].span())
for index, pair in enumerate([(b'.*?', [b'', b'a', b'', b'a', b'']),
                              (b'|a', [b'', b'a', b'', b'a', b'']),
                              (b'a*', [b'aa', b'']), (b'(?=a)', [b'', b''])]):
    actual = [match.group() for match in drain(re.compile(pair[0]).finditer(b'aa'))]
    assert actual == pair[1]
    print('I1.exact-empty-' + str(index), actual)
text = '😀猫'
found = drain(re.finditer('.', text))
assert [(match.group(), match.span()) for match in found] == [('😀', (0, 1)), ('猫', (1, 2))]
assert found[0].string is text and found[1].string is text
print('I1.text-control', [(match.group(), match.span()) for match in found])
print('I1.complete')
