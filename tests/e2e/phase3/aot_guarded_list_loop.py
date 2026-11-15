# 受守卫的布尔写入循环：终值、身份、别名与通用回退同次对照 CPython。
def mark(flags, j, bound, step):
    original = j
    alias = flags
    while j <= bound:
        flags[j] = False
        j += step
    return j, original, alias is flags, flags

print(mark([True] * 10, 1, 8, 3))
print(mark([1, "x", None, True], 0, 3, 1))
print(mark([True, True], 1, 1, 9223372036854775807))
print(mark([True] * 3, 4, 2, 1))
print(mark([True] * 3, True, 2, True))
print(mark({0: True, 1: True}, 0, 1, 1))
print(mark([True] * 3, -1, 1, 1))

class Sink:
    def __setitem__(self, key, value):
        print("set", key, value)

sink = Sink()
result = mark(sink, 0, 2, 1)
print(result[0], result[1], result[2], result[3] is sink)

try:
    mark([True], 0, 1, 1)
except IndexError:
    print("bounds")

# 零轮保留传入整数身份；协议对象逐次比较/写入/加法顺序可观察。
def zero(flags, j, bound, step):
    original = j
    while j <= bound:
        flags[j] = False
        j += step
    return j is original

print(zero([], 10000, 0, 1))

class Counter:
    def __init__(self):
        self.value = 0
    def __le__(self, bound):
        print("compare", self.value)
        return self.value <= bound
    def __iadd__(self, step):
        print("add", step)
        self.value += step
        return self
    def __repr__(self):
        return str(self.value)

counter = Counter()
result = mark(sink, counter, 1, 1)
print(result[0] is counter, counter.value)
