# M2.4：惰性迭代、提前退出、空迭代器、对象内建与格式化。

# 迭代器只能消费一次；iter(iterator) 返回自身
it = iter([1, 2])
print(iter(it) is it, next(it), next(it, "end"), next(it, "end"))
try:
    next(it)
except StopIteration:
    print("stop")

# 空迭代器与惰性短路
print(any([]), all([]), any([0, "", 2]), all([1, "x"]))
print(list(enumerate([])), list(zip([], [1])), list(reversed(range(3))))

# sum / min / max / round
print(sum([1, 2, 3]), sum([1, 2], 10), min(3, 1, 2), max([], default=9))
print(round(2.5), round(3.5), round(2.675, 2), round(2.5, 0))
print(round(123.456, -1), round(135.0, -1), round(1250, -2))

# 静态函数里的 while / for else（break 跳过 else）
def loop_else(limit: int) -> None:
    i = 0
    while i < limit:
        i += 1
        if i == 2:
            break
    else:
        print("while else")
    for _ in range(limit):
        break
    else:
        print("never")
    for _ in range(limit):
        pass
    else:
        print("for else")

loop_else(0)
loop_else(5)

# 对象内建
def probe(value) -> None:
    print(isinstance(value, int), isinstance(value, (str, float)), type(value).__name__)
    print(getattr(value, "missing", "fallback"), hasattr(value, "missing"))

probe(True)
probe("text")
probe(1.5)

# repr / str / bool / hash
print(repr("a"), str(1), bool(""), bool([0]), hash(12) == hash(12))

# 格式化：宽度、补零、进制、对齐、百分号
print(f"[{42:5d}] [{42:05d}] [{255:x}] [{255:#X}] [{3.14159:.3f}]")
print(f"[{'ab':>4}] [{'ab':<4}] [{'ab':^5}] [{0.25:%}]")
print("{}-{}".format if False else "skip")

# 元组循环目标：先写隐藏局部，再在循环体开头解包
for index, ch in enumerate("ab"):
    print(index, ch)
for left, right in zip([1, 2], [3, 4]):
    print(left + right)
