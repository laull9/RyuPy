# 基准：递归 + 迭代斐波那契（输入固定，见 run_benchmarks.py）。
def fib_rec(n: int) -> int:
    if n < 2:
        return n
    return fib_rec(n - 1) + fib_rec(n - 2)


def fib_iter(n: int) -> int:
    a = 0
    b = 1
    for _ in range(n):
        t = a + b
        a = b
        b = t
    return a


print(fib_rec(30))
value = 0
for _ in range(200000):
    value = fib_iter(90)
print(value)
