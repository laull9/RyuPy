# 原typed range计数核心与通用伴生体分别保留；通用体使用既有range对象迭代协议。
def fold(seed: int, n: int) -> int:
    total = seed
    for value in range(n):
        total += value
    return total


def walk(seed: int, start: int, stop: int, step: int) -> int:
    total = seed
    for value in range(start, stop, step):
        if value == 2:
            continue
        if value == -2:
            break
        total += value
    else:
        total += 100
    return total
