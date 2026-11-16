# 普通实例方法密集负载；同进程预热两轮后采四轮，冷启动墙钟由外部驱动另计。
import sys
import time

class Counter:
    def __init__(self):
        self.value = 3
    def zero(self):
        return self.value
    def one(self, a):
        return self.value + a
    def two(self, a, b):
        return self.value + a + b
    def three(self, a, b, c):
        return self.value + a + b + c

counter = Counter()
size = int(sys.argv[1])
for batch in range(6):
    total = 0
    start = time.perf_counter()
    for i in range(size):
        total += counter.zero()
        total += counter.one(1)
        total += counter.two(1, 2)
        total += counter.three(1, 2, 3)
    elapsed = time.perf_counter() - start
    print('CHECK', batch, total)
    if batch >= 2:
        print('WARM', batch, elapsed)
