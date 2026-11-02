# 固定 AOT/JIT 配对内核；原函数体不改，输入准备和校验均在计时外。
import sys
import time
def positional(a, b, c):
    return a + b + c


def kernel():
    total = 0
    for i in range(200000):
        total += positional(i, 1, 2)
    return total




def checksum(value):
    return value

if len(sys.argv) > 1 and sys.argv[1] == "hot":
    for index in range(3):
        value = kernel()
        print("warm", index, checksum(value))
    for index in range(7):
        start = time.perf_counter()
        value = kernel()
        elapsed = time.perf_counter() - start
        print("sample", index, elapsed, checksum(value))
else:
    value = kernel()
    print("result", checksum(value))
