# 本轮全新重建语义样例；不是历史丢失源码恢复。
import subject
class Items:
    def __init__(self):
        self.n = 0
    def __iter__(self):
        print("iter")
        return self
    def __next__(self):
        self.n += 1
        print("next", self.n)
        if self.n == 4:
            raise StopIteration
        return self.n
print(subject.fold(10, Items()))
