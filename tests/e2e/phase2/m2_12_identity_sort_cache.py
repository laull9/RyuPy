# M2.12：身份、排序与缓存相关的语义。
#
# - `is`：True / False 单例、[-5, 256] 小整数与 CPython 一致（不依赖装箱来源）；
# - 排序只调用 `<`（不调用 __eq__），稳定性、reverse、key、元组 / 混合数值 / 不可比较报错；
# - 属性缓存：property 读写、类属性被实例遮蔽 / 删除遮蔽 / 修改类属性后立即可见，
#   替换与删除 property；多态调用点轮换多个子类（超过 PIC 单条目）结果不变。

a = 3
b = 1 + 2
print(a is b, (a < b) is False, (a == b) is True)
flags = [True, False]
x = flags[0]
print(x is True, (not x) is False)
c = 1000
d = 999 + 1
print(c == d)
def f(v):
    return v > 2
print(f(5) is True, f(1) is False)

class K:
    def __init__(self, v):
        self.v = v

    def __lt__(self, other):
        return self.v < other.v

    def __eq__(self, other):
        raise RuntimeError("sort must not call __eq__")

    def __repr__(self):
        return f"K({self.v})"


print(sorted([K(3), K(1), K(2)]))
data = []
x = 1
for i in range(1000):
    x = (x * 7919 + 13) % 1009
    data.append((x % 10, i))
s1 = sorted(data)
s2 = sorted(data)
print(s1[:5], s1[-3:])
def first(p):
    return p[0]
s3 = sorted(data, key=first)
s4 = sorted(data, key=first, reverse=True)
print(s3[:5], s4[:5], s4[-3:])
words = ["b", "A", "a", "B", "ab", "", "zz", "é"]
print(sorted(words), sorted(words, reverse=True))
print(sorted([3.5, 1, 2.25, -1, 10**20, 0.0]))
mixed = [1, 1.0, True, 0, False]
print(sorted(mixed), sorted(mixed, reverse=True))
nan = float("nan")
lst = [5, 3, 9, 1]
lst.sort()
print(lst)
try:
    sorted([1, "a"])
except TypeError as e:
    print(e)
try:
    sorted([(1, None), (1, 2)])
except TypeError as e:
    print(e)
print(sorted([(None, 1), (None, 0)]))
big = []
for i in range(300):
    big.append((i * 37) % 101)
print(sorted(big) == sorted(sorted(big, reverse=True)), sorted(big)[:10])

def getv(self):
    return self._v * self.scale


def setv(self, value):
    self._v = value


class A:
    scale = 2
    tags = []

    def __init__(self):
        self._v = 1
        self.w = 0

    v = property(getv, setv)


class B(A):
    pass


a = A()
b = B()
out = []
for i in range(6):
    a.w = i
    a.v = i
    if i == 2:
        a.scale = 100
    if i == 3:
        del a.scale
    if i == 4:
        A.scale = 7
    for o in (a, b):
        out.append(o.v + o.w + o.scale)
print(out)
def newv(self):
    return -1
A.v = property(newv)
print(a.v, b.v)
del A.v
a.v = 9
print(a.v, b.scale, len(a.tags), a.tags is A.tags)


class Shape:
    def __init__(self, size):
        self.size = size

    def area(self):
        return 0


class Square(Shape):
    def area(self):
        return self.size * self.size


class Tri(Shape):
    def area(self):
        return self.size * self.size // 2


class Line(Shape):
    pass


class Big(Square):
    def area(self):
        return super().area() * 10


def doubled(self):
    return self.size * 100


shapes = [Square(3), Tri(4), Line(5), Big(2), Square(1)]
totals = []
for round_index in range(3):
    total = 0
    for shape in shapes:
        total += shape.area()
    totals.append(total)
    if round_index == 1:
        Tri.area = doubled
print(totals)
