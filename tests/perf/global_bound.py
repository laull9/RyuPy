# 静态模块字段基准：禁止加入import/time/顶层动态调用，计时与调用由外部诊断器完成。
scale: int = 7


def global_sum(n: int) -> int:
    total: int = 0
    for i in range(n):
        total += scale
    return total


def global_store_read(n: int) -> int:
    global scale
    total: int = 0
    for i in range(n):
        scale = i
        total += scale
    return total
