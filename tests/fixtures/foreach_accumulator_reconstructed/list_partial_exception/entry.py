# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
class Broken:
    def __init__(self):
        self.n = 0
    def __iter__(self):
        return self
    def __next__(self):
        self.n += 1
        print("rhs-next", self.n)
        if self.n == 2:
            raise ValueError("partial")
        return 9
seed = [1]
try:
    subject.fold(seed, [Broken()])
except ValueError as exc:
    print(type(exc).__name__, str(exc), seed)
