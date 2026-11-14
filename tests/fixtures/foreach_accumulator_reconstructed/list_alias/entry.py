# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
seed = [1]
result = subject.fold(seed, [[2], [], [3, 4]])
print(result is seed, seed)
