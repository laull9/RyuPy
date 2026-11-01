# bench: category=phase3
# bench: pending=M3.3
# bench: desc=装饰器包装函数的调用开销：计数包装与带参数装饰器
def counted(fn):
    def wrapper(*args):
        wrapper.calls += 1
        return fn(*args)
    wrapper.calls = 0
    return wrapper


def scaled(k):
    def deco(fn):
        def inner(x):
            return fn(x) * k
        return inner
    return deco


@counted
def add(a, b):
    return a + b


@scaled(3)
def ident(x):
    return x


total = 0
for i in range(500000):
    total = add(total, 1)
for i in range(500000):
    total += ident(1)
print(total, add.calls)
