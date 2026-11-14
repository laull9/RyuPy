# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
print(subject.fold(0, 5, -2, -2))
print(subject.fold(7, 1, 1, 1))
try:
    subject.fold(7, 1, 4, 0)
except ValueError as exc:
    print(type(exc).__name__, str(exc))
