# 固定 AOT/JIT 配对内核；原函数体不改，输入准备和校验均在计时外。
import sys
import time
from dataclasses import dataclass

@dataclass
class Record:
    x: int
    y: int


def construction(n: int) -> int:
    total: int = 0
    for i in range(n):
        value = Record(i, i + 1)
        total += value.x
    return total




def checksum(value):
    return value

if len(sys.argv) > 1 and sys.argv[1] == "hot":
    for index in range(3):
        value = construction(50000)
        print("warm", index, checksum(value))
    for index in range(7):
        start = time.perf_counter()
        value = construction(50000)
        elapsed = time.perf_counter() - start
        print("sample", index, elapsed, checksum(value))
else:
    value = construction(50000)
    print("result", checksum(value))
