# 生成器表达式：最左 iterable 立即求值，元素/条件/内层 iterable 严格惰性。
log = []


def source():
    log.append("源")
    return [1, 2, 3]


def keep(value):
    log.append(("条件", value))
    return value != 2


def element(value):
    log.append(("元素", value))
    return value * 10


generator = (element(x) for x in source() if keep(x))
print(log)
print(next(generator))
print(log)
print(list(generator))
print(log)
print(next(generator, "耗尽"))


class Source:
    def __iter__(self):
        log.append("取迭代器")
        return iter([4, 5])


log.clear()
generator = (x for x in Source())
print(log)
print(list(generator), log)
try:
    generator = (x for x in 1)
except TypeError:
    print("立即拒绝非迭代对象")


# 同名目标不能覆盖外层；之后改变cell对尚未恢复的生成器可见。
def outer():
    x = [1, 2]
    factor = 2
    values = (x * factor for x in x)
    print(x)
    factor = 3
    return values


print(list(outer()))
print([list(x + y for y in [1, 2]) for x in [10, 20]])
functions = list(lambda: x for x in range(3))
print([function() for function in functions])
functions = list(lambda x=x: x for x in range(3))
print([function() for function in functions])


def inside(value):
    log.append(("内层", value))
    return [value, value + 10]


log.clear()
generator = ((x, y) for x in [1, 2] for y in inside(x))
print(log)
print(next(generator), log)
print(list(generator), log)
print(list(a + sum(b) for a, *b in [(1, 2, 3), (4,)]))


# 错误应发生在恢复时，且之后永久耗尽。
generator = (10 // x for x in [2, 0, 1])
print(next(generator))
try:
    next(generator)
except ZeroDivisionError:
    print("惰性异常")
print(next(generator, "错误后耗尽"))
print(list(x for x in []))
