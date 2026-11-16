# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
class Item:
    def __radd__(self, lhs):
        inner = subject.fold(5, [2, 3])
        print("inner", inner)
        return lhs + inner
print(subject.fold(1, [Item(), 4]))
