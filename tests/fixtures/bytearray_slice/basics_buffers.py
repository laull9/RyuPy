# bytearray切片基本结果、宽边界、别名、导出锁与异常优先级的CP-first合同。
a = bytearray(b'abcdef')
a[1:4] = b'XY'
assert a == b'aXYef'
a[4:1] = b'12'
assert a == b'aXYe12f'
a[:] = bytearray(b'abcdef')
a[::2] = b'XYZ'
assert a == b'XbYdZf'
a[::-2] = b'123'
assert a == b'X3Y2Z1'
a[::-2] = b''
assert a == b'XYZ'
print('slice.basics', a)
huge = 10 ** 100
a = bytearray(b'abcdef')
a[-huge:huge:huge] = b'X'
a[huge:-huge:-huge] = b'Y'
a[False:True:True] = b'Z'
assert a == b'ZbcdeY'
a[-huge:huge] = b'Q'
assert a == b'Q'
print('slice.wide', a)
a = bytearray(b'abcd')
a[1:3] = a
assert a == b'aabcdd'
print('slice.self', a)
a = bytearray(b'abcdef')
root = memoryview(a)
part = root[:4]
chars = part.cast('c')
a[2:6] = chars
assert a == b'ababcd'
a[::-1] = root
assert a == b'dcbaba'
chars.release()
part.release()
root.release()
a.append(33)
print('slice.alias', a)
a = bytearray(b'abcdef')
view = memoryview(a)
a[:2] = b'XY'
a[99:99] = b''
a[::2] = b'123'
assert a == b'1Y2d3f'
try:
    a[::2] = b'Z'
except ValueError as error:
    assert str(error) == 'attempt to assign bytes of size 1 to extended slice of size 3'
    print('slice.mismatch', type(error).__name__, str(error))
else:
    raise AssertionError('extended mismatch accepted')
for extended in (True, False):
    try:
        if extended:
            a[::2] = b''
        else:
            a[0:1] = b''
    except BufferError as error:
        print('slice.lock', type(error).__name__, str(error))
    else:
        raise AssertionError('export resize accepted')
assert a == b'1Y2d3f'
view.release()
a[::-2] = b''
assert a == b'123'
print('slice.release', a)
