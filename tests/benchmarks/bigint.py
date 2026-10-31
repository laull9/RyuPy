# bench: category=numeric
# bench: desc=大整数：3000! 的数位和、2^10000、大 Fibonacci 与 i64 溢出升级
def factorial(n):
    r = 1
    for i in range(2, n + 1):
        r *= i
    return r


def digit_sum(x):
    s = 0
    while x > 0:
        s += x % 10
        x //= 10
    return s


def big_fib(n):
    a = 0
    b = 1
    for _ in range(n):
        a, b = b, a + b
    return a


print(digit_sum(factorial(3000)))
print(len(str(2 ** 10000)))
print(big_fib(20000) % 1000000007)
acc = 1
for i in range(200000):
    acc = (acc * 31 + i) % 4611686018427387903 * 3
print(acc)
