# 本轮全新重建语义样例；不是历史丢失源码恢复。
value = 0
def fold(seed, values):
    global value
    total = seed
    for value in values:
        total += value
    return total
