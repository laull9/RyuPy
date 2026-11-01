# bytes/bytearray前后缀按原始buffer比较；不解码字符串，不预校验tuple未消费项。
for data in [b'abcd', bytearray(b'abcd')]:
    print(data.startswith(b'ab'), data.endswith(b'cd'))
    print(data.startswith((b'xx', b'ab', 42)), data.endswith((b'xx', b'cd', 42)))
    print(data.startswith(()), data.endswith(()))
    print(data.startswith(bytearray(b'ab')), data.endswith(memoryview(b'cd').cast('c')))
    for bounds in [(None, None), (-3, -1), (4, None), (5, None), (3, 2), (0, -9), (2**100, None), (-2**100, 2**100)]:
        print(data.startswith(b'', *bounds), data.endswith(b'', *bounds))
    for bad in [42, (b'zz', 42), 'ab']:
        try:
            data.startswith(bad)
        except TypeError as error:
            print(type(error).__name__)

class Bound:
    def __init__(self, label, value):
        self.label = label
        self.value = value
    def __index__(self):
        print(self.label)
        return self.value
for prefix in [b'a', 42, ()]:
    try:
        print(b'ab'.startswith(prefix, Bound('start', 0), Bound('end', 2)))
    except TypeError:
        print('转换边界后才校验prefix')

released = memoryview(b'a')
released.release()
print(b'ab'.startswith((b'a', released)))
try:
    b'ab'.startswith((b'x', released))
except ValueError:
    print('访问released候选才失败')

mutable = bytearray(b'ab')
class SameLengthWrite:
    def __index__(self):
        mutable[0] = 120
        return 0
print(mutable.startswith(b'x', SameLengthWrite()), mutable)

first_class = b'abcd'.endswith
print(first_class((b'bc', b'cd'), 1, 4))
for fail_args in [(), (b'a', 0, 2, 3)]:
    try:
        first_class(*fail_args)
    except TypeError:
        print('参数数目错误')
try:
    first_class(b'cd', start=0)
except TypeError:
    print('拒绝关键字')
