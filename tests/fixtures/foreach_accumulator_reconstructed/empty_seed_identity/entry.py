# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
for seed in [int("1000000"), 10 ** 40, True, 1.25, []]:
    result = subject.fold(seed, [])
    print(result is seed, result)
