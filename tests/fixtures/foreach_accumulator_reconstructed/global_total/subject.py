# 本轮全新重建语义样例；不是历史丢失源码恢复。
total = 0
def fold(seed, values):
    global total
    total = seed
    for value in values:
        total += value
    return total
