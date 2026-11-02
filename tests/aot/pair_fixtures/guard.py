# 固定四guard场景；同一两Add候选，每次100000调用，不在计时内打印/校验。
import time


def positional(a, b, c):
    return a + b + c


class Custom:
    def __init__(self):
        self.reverse = 0
        self.forward = 0
        self.bad = False
    def __radd__(self, other):
        if other != 1 or self.reverse != self.forward:
            self.bad = True
        self.reverse += 1
        return self
    def __add__(self, other):
        if other != 2 or self.reverse != self.forward + 1:
            self.bad = True
        self.forward += 1
        return self


custom = Custom()
scenarios = [('float', 1.5, 2.5, 3.5), ('bool', True, 2, 3),
             ('custom', 1, custom, 2), ('overflow', 9223372036854775807, 1, -1)]


def kernel(a, b, c):
    result = None
    for index in range(100000):
        result = positional(a, b, c)
    return result


def reset():
    custom.reverse = 0
    custom.forward = 0
    custom.bad = False


def checksum(label, result):
    if label == 'custom':
        return str((result is custom, custom.reverse, custom.forward, custom.bad))
    return str(result)


for label, a, b, c in scenarios:
    for index in range(3):
        reset()
        result = kernel(a, b, c)
        print('warm', label, index, checksum(label, result))
    for index in range(7):
        reset()
        start = time.perf_counter()
        result = kernel(a, b, c)
        elapsed = time.perf_counter() - start
        print('sample', label, index, elapsed, checksum(label, result))
