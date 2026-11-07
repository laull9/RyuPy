# 本轮全新重建语义样例；不是历史丢失源码恢复。
def fold(seed, arg):
    total = seed
    for value in range(arg(1), arg(5), arg(2)):
        total += value
    return total
