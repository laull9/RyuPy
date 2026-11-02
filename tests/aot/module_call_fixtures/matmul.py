# 固定 AOT/JIT 配对内核；原函数体不改，输入准备和校验均在计时外。
import sys
import time
def make_matrix(n, seed):
    m = []
    x = seed
    for i in range(n):
        row = []
        for j in range(n):
            x = (x * 1103515245 + 12345) % 2147483648
            row.append((x % 1000) / 1000.0)
        m.append(row)
    return m


def matmul(a, b, n):
    result = []
    for i in range(n):
        row_a = a[i]
        row = []
        for j in range(n):
            s = 0.0
            for k in range(n):
                s += row_a[k] * b[k][j]
            row.append(s)
        result.append(row)
    return result


n = 64
a = make_matrix(n, 1)
b = make_matrix(n, 2)

def checksum(value):
    total = 0.0
    for row in value:
        for element in row:
            total += element
    return round(total, 3)

if len(sys.argv) > 1 and sys.argv[1] == "hot":
    for index in range(3):
        value = matmul(a, b, n)
        print("warm", index, checksum(value))
    for index in range(7):
        start = time.perf_counter()
        value = matmul(a, b, n)
        elapsed = time.perf_counter() - start
        print("sample", index, elapsed, checksum(value))
else:
    value = matmul(a, b, n)
    print("result", checksum(value))
