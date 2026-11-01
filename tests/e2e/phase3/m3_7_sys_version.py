# 语言兼容目标为固定3.12.13；实现身份不与CPython伪作差分。
import sys
v = sys.version_info
print(v.major, v.minor, v.micro, v.releaselevel, v.serial)
print(len(v), tuple(v), v[0], v[-1], v[:3], v[::-1])
print(v == (3, 12, 13, 'final', 0), (3, 12, 13, 'final', 0) == v)
print(v > (3, 12), (3, 12) < v, v >= (3,), v < (4,), v != (3,))
print(hash(v) == hash(tuple(v)))
print(isinstance(v, tuple), issubclass(type(v), tuple), type(v).__name__)
print(str(v), repr(v), v[:] is v)
for action in range(3):
    try:
        if action == 0:
            v.major = 4
        elif action == 1:
            del v.micro
        else:
            v[0] = 4
    except (AttributeError, TypeError):
        print('readonly')
try:
    print(v[99])
except IndexError:
    print('index error')
try:
    print(v < [3, 12])
except TypeError:
    print('comparison error')
try:
    print([3, 12] < v)
except TypeError:
    print('reverse comparison error')
class Reflected:
    def __gt__(self, other):
        print('reflected', other is v)
        return True
print(v < Reflected())
