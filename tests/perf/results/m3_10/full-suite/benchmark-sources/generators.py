# bench: category=phase3
# bench: desc=生成器：yield 迭代、yield from 委托、生成器表达式求和
def count_up(n):
    i = 0
    while i < n:
        yield i
        i += 1


def chained(n):
    yield from count_up(n)
    yield from count_up(n)


total = 0
for x in count_up(1000000):
    total += x
for x in chained(500000):
    total += x
total += sum(x * 2 for x in range(1000000))
print(total)
