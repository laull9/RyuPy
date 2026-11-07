# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
class Box:
    def __init__(self):
        self.value = 0
    def __iadd__(self, rhs):
        print("iadd", rhs)
        self.value += rhs
        return self
seed = Box()
result = subject.fold(seed, [2, 4])
print(result is seed, seed.value)
