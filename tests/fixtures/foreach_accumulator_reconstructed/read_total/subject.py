# 本轮全新重建语义样例；不是历史丢失源码恢复。
def fold(seed, values):
    total = seed
    for value in values:
        print(total)
        total += value
    return total
