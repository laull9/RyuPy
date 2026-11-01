# bench: category=static
# bench: desc=递归 fib_rec(30) + 20 万次迭代 fib_iter(90)，全标注的静态 i64 路径
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
