# M1.3 里程碑：递归与迭代斐波那契。
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


print(fib_rec(25))
print(fib_iter(90))
