# ryupy: pending
# 同一输入由固定CPython/JIT/AOT即时差分；包含真实range快路与不可合并回退。
print("simple", sum(x * 2 for x in range(100)))
print("negative", sum(x * -3 for x in range(-5, 8)))
print("empty", sum((x * 2 for x in range(0)), 12345))
start = 12345
print("mul-big", sum(x * 3 for x in range(4000000000000000000, 4000000000000000003)))
print("sum-big", sum((x * 2 for x in range(4)), 9223372036854775805))
print("negative-overflow", sum((x * -2 for x in range(4)), -9223372036854775807))
print("cancel", sum((x * 2 for x in range(-3, 4)), start))
g = (x * 2 for x in range(5))
print("partial", next(g), sum(g), sum(g))
a = (x * 2 for x in range(4))
b = (x * 2 for x in range(3))
print("independent", next(a), next(b), sum(a), sum(b))
g = (x * 2 for x in range(4))
try:
    g.send(1)
except TypeError:
    print("initial-send-error")
print("after-send-error", sum(g))
g = (x * 2 for x in range(4))
print("send-none", next(g), g.send(None), sum(g))
g = (x * 2 for x in range(4))
print("close-first", g.close(), sum(g))
g = (x * 2 for x in range(4))
print("close-partial", next(g), g.close(), sum(g))
g = (x * 2 for x in range(4))
print("throw-first", next(g))
try:
    g.throw(ValueError("stop"))
except ValueError:
    print("throw-error")
print("after-throw", sum(g))

# 非range游标必须零预消费回退，保持用户代码与每项加法的交错。
class Iterator:
    def __init__(self):
        self.i = 0
    def __iter__(self):
        return self
    def __next__(self):
        if self.i == 3:
            raise StopIteration
        self.i += 1
        print("next", self.i)
        return self.i
print("custom", sum(x * 2 for x in Iterator()))
# 参数先绑定后重绑定：独立于模块未绑定名称fallback能力，仍覆盖消费者/游标替换。
def binding_probe(sum, range):
    original_range = range
    range = lambda n: Iterator()
    print("rebound-range", sum(x * 2 for x in range(100)))
    range = original_range
    original_sum = sum
    sum = lambda iterable: list(iterable)
    print("rebound-sum", sum(x * 2 for x in range(3)))
    sum = original_sum
    print("normal", sum(x * 2 for x in range(3)))
binding_probe(sum, range)
