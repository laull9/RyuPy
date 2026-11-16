# bench: category=phase3
# bench: desc=原始generators第一臂：普通for完整百万项消费
# 手写源与原始工作负载由test_generator_consumers.py的AST合同绑定。
def count_up(n):
    i = 0
    while i < n:
        yield i
        i += 1


total = 0
for x in count_up(1000000):
    total += x
print(total)
