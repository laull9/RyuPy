# bench: category=phase3
# bench: desc=原始generators第二臂：yield from两段各五十万项
# 手写源与原始工作负载由test_generator_consumers.py的AST合同绑定。
def count_up(n):
    i = 0
    while i < n:
        yield i
        i += 1


def chained(n):
    yield from count_up(n)
    yield from count_up(n)


total = 0
for x in chained(500000):
    total += x
print(total)
