# bench: category=dynamic
# bench: desc=300 万次动态函数调用加法 + 200 万次方法调用（PIC 主要目标）
# bench: variant=PIC 关闭:RYUPY_PIC=0
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
