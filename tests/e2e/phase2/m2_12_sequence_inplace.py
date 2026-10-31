# M2.12：序列拼接 / 重复、增量赋值的原地语义与「目标只求值一次」。
#
# 覆盖：list / tuple / str / bytes / bytearray 的 + 与 *（含 n * seq、负数与 bool 次数）、
# 同类拼接的 TypeError 消息；list / set / bytearray / 实例 __iadd__ 的原地修改与别名可见，
# 不可变值 += 产生新对象；a[f()] += v 与 f().attr += v 中 f 只调用一次。

a = [1, 2]
b = [3]
print(a + b)
t = (1, 2)
print(t + (3,))
x = "ab"
n = 3
print(x * n)
print(n * x)
print([0] * n)
print(n * [1, 2])
print((1,) * 2)
a += b
print(a)
c = a
c += [9]
print(a is c, a)
c *= 2
print(c)
print(True + 1, b"ab" + b"c", b"x" * 3)
print("x" * -1, [1] * 0)

class V:
    def __init__(self, x):
        self.x = x

    def __iadd__(self, other):
        self.x += other
        return self

    def __add__(self, other):
        return V(self.x + other)


v = V(1)
w = v
v += 5
print(v is w, v.x)
s = {1, 2}
t = s
s |= {3}
s &= {1, 3, 4}
s -= {9}
print(t is s, sorted(s))
b = bytearray(b"ab")
c = b
b += b"cd"
b *= 2
print(c is b, b)
x = (1, 2)
y = x
x += (3,)
print(x, y, x is y)
z = "a"
z *= 3
print(z)
lst = [1]
lst += (2, 3)
lst += "ab"
lst += range(2)
print(lst)
try:
    q = [1] + (2,)
except TypeError as e:
    print(e)
try:
    q = "a" + 1
except TypeError as e:
    print(e)
try:
    q = [1] * 1.5
except TypeError as e:
    print(e)
try:
    q = b"a" + "b"
except TypeError as e:
    print(e)
try:
    n = 1
    n += "a"
except TypeError as e:
    print(e)
try:
    m = [1]
    m += 1
except TypeError as e:
    print(e)
print(3 * [0], [1, 2] * -1, True * "ab", "ab" * False)
print(7 // 2, -7 // 2, 7 % 3, -7 % 3, 7 / 2, 2 ** 53 / 3, (2 ** 60) / 7, 6 & 3, 6 | 3, 6 ^ 3)
print(1.5 + 2, 3 - 0.5, 2 * 0.25, 1 / 4, 0.5 / 2)

calls = []
def idx(i):
    calls.append(i)
    return i


class Box:
    def __init__(self):
        self.n = 0


boxes = [Box(), Box()]
def pick(i):
    calls.append("pick")
    return boxes[i]


data = [1, 2, 3]
data[idx(0)] += 10
pick(1).n += 5
grid = {"a": [0, 0]}
grid["a"][idx(1)] += 7
s = "x"
s += "y"
data[1:2] += [9]
print(data, calls, boxes[1].n, grid, s)
def inner():
    local = [1]
    local[idx(0)] *= 3
    return local
print(inner(), calls)
