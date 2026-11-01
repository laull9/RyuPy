# M3.6表示取舍基准：编译与进程启动由外部脚本计时，这里只测预热后的函数体。
from dataclasses import dataclass
import time

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

def field_reads(n: int, value: Record) -> int:
    total: int = 0
    for i in range(n):
        total += value.x + value.y
    return total

def equality(n: int, left: Record, right: Record) -> int:
    total: int = 0
    for i in range(n):
        if left == right:
            total += 1
    return total

first = Record(3, 4)
second = Record(3, 4)
# 旧静态表示缺eq；输出先保存语义证据，不能把错误结果的速度当作等价实现加速。
print("semantics", first == second, repr(first))
for warm in range(3):
    construction(10000)
    field_reads(10000, first)
    equality(10000, first, second)
for repeat in range(7):
    start = time.perf_counter()
    result = construction(50000)
    print("construction", time.perf_counter() - start, result)
    start = time.perf_counter()
    result = field_reads(200000, first)
    print("field_reads", time.perf_counter() - start, result)
    start = time.perf_counter()
    result = equality(50000, first, second)
    print("equality", time.perf_counter() - start, result)
