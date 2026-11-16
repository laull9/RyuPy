# 本轮全新重建语义样例；不是历史丢失源码恢复。
def fold(seed: int, n: int) -> int:
    total = seed
    for value in range(n):
        total += value
    return total
