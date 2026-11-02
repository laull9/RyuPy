# 固定模块属性调用成本；不预存函数，校验在每轮计时外。
import math
import time

def run(n):
    total = 0.0
    for i in range(n):
        total += math.sqrt(4.0)
    return total

n = 100000
for index in range(3):
    value = run(n)
    print("warm", index, int(value))
for index in range(7):
    start = time.perf_counter()
    value = run(n)
    elapsed = time.perf_counter() - start
    print("sample", index, elapsed, int(value))
