# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
saved = int("1000000")
class Right:
    def __radd__(self, lhs):
        return saved
result = subject.fold(0, [Right()])
print(result is saved, result)
print(subject.fold(0, [Right(), 3]))
