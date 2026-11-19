# 本轮全新重建语义样例；不是历史丢失源码恢复。
def fold(seed, values, rhs):
    total = seed
    del total
    for value in values:
        total += rhs(value)
    return total
