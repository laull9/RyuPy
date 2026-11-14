# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
class Items:
    def __init__(self):
        self.n = 0
    def __iter__(self):
        return self
    def __next__(self):
        self.n += 1
        print("next", self.n)
        if self.n == 3:
            raise ValueError("stop")
        return [self.n]
seed = []
try:
    subject.fold(seed, Items())
except ValueError as exc:
    print(type(exc).__name__, str(exc), seed)
