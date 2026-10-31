# bench: category=dynamic
# bench: desc=动态函数调用形态：位置、关键字、默认值、*args / **kwargs
def positional(a, b, c):
    return a + b + c


def defaults(a, b=1, c=2):
    return a + b + c


def keywords(a, b, c):
    return a - b + c


def star_args(*args):
    return len(args)


def star_kwargs(**kwargs):
    return len(kwargs)


total = 0
for i in range(1000000):
    total += positional(i, 1, 2)
for i in range(1000000):
    total += defaults(i)
for i in range(500000):
    total += keywords(i, c=3, b=2)
for i in range(300000):
    total += star_args(i, i, i)
for i in range(200000):
    total += star_kwargs(x=i, y=i)
print(total)
