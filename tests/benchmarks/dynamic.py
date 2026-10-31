# 基准：动态函数调用 + 动态二元运算 + 方法调用（PIC 的主要目标）。
def add(a, b):
    return a + b


class Acc:
    def __init__(self):
        self.v = 0

    def step(self, x):
        self.v += x


total = 0
for i in range(3000000):
    total = add(total, 1)
acc = Acc()
for i in range(2000000):
    acc.step(1)
print(total, acc.v)
