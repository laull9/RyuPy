# 类型化推导式区：整数边界、余数符号、已有盒身份和协议顺序。
values = [-1000000, -3, -1, 0, 2, 1000000, 2 ** 62, True, 2.5]
print([x * x for x in values])
print([x for x in values if x % 3 == 1])
print(sorted({x % -3 for x in values}))
print([(x + 2) * (x - 1) for x in values])
large = int("1000000")
items = [large, large]
kept = [x for x in items if x % 2 == 0]
print(kept[0] is large, kept[0] is kept[1])
print([(i, j) for i in range(4) for j in range(4) if i != j])
print({x: i for i, x in enumerate([7, 8, 7])})
events = []
class Value:
    def __init__(self, value):
        self.value = value
    def __mod__(self, other):
        events.append(self.value)
        return self.value % other
print([x.value for x in [Value(1), Value(2)] if x % 2 == 0])
print(events)
try:
    print([x % 0 for x in [1]])
except ZeroDivisionError as error:
    print(str(error))
# cell观察的推导式保留原物化；嵌套闭包得到最后一次绑定。
functions = [lambda: x for x in range(3)]
print([f() for f in functions])
# enumerate临时tuple只在立即解包时消除；分量的原盒身份与原迭代器进度保持。
source = [large, large]
rows = [(i, x, x) for i, x in enumerate(source, 7)]
print(rows[0][0], rows[1][0], rows[0][1] is large, rows[0][1] is rows[0][2])
kept_pairs = [pair for pair in enumerate(source)]
print(kept_pairs[0][1] is large)
shared = enumerate([10, 11, 12], 4)
print(next(shared), [(i, x) for i, x in shared])
def alternate(values):
    return iter([(3, 4), (5, 6)])
def shadowed(enumerate):
    return [(i, x) for i, x in enumerate([1])]
print(shadowed(alternate))

# 私有容量预留不消耗range；已推进和负步游标按真实剩余输入继续。
remaining = iter(range(8))
print(next(remaining), [x for x in remaining])
print([x for x in range(10, -1, -2)])
class NoHint:
    def __iter__(self):
        return iter([8, 9])
    def __length_hint__(self):
        raise ValueError("must not call hint")
print([x for x in NoHint()])
