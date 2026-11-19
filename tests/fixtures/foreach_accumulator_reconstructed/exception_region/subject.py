# 本轮全新重建语义样例；不是历史丢失源码恢复。
def fold(seed, values):
    total = seed
    try:
        for value in values:
            total += value
    finally:
        print("finally", total)
    return total
