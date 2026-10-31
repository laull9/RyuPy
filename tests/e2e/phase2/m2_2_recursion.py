# M2.2：递归深度守卫抛可捕获的 RecursionError，而不是让 .NET 栈溢出终止进程。
def rec(n):
    return rec(n + 1)


try:
    rec(0)
except RecursionError:
    print("recursion caught")


def depth(n):
    if n == 0:
        return 0
    return depth(n - 1) + 1


print(depth(100))


def boom():
    try:
        raise ValueError("inner")
    finally:
        print("cleanup before propagation")


try:
    boom()
except ValueError as error:
    print(str(error), type(error).__name__)
