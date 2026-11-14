# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
print(subject.fold(9223372036854775806, [1, 1, -2, -9223372036854775806, 3]))
print(subject.fold(10 ** 40, [-(10 ** 40), 7, -2]))
print(subject.fold(True, [True, 3, -1]))
print(subject.fold(2, [0.5, 1, -0.5]))
