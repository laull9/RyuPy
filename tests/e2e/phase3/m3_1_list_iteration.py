# 最左 iterable 的获取只发生一次；隐式函数不能重新调用用户 __iter__。
log = []


class Source:
    def __iter__(self):
        log.append("iter")
        return Counter()


class Counter:
    def __init__(self):
        self.value = 0

    def __iter__(self):
        log.append("iterator-iter")
        return self

    def __next__(self):
        if self.value == 3:
            raise StopIteration
        self.value += 1
        return self.value


print([x for x in Source() if x != 2])
print(log)


def scope():
    x = [7, 8]
    print([x for x in x], x)
    a = [lambda: x for x in [1, 2]]
    b = [lambda: x for x in [3, 4]]
    print([f() for f in a], [f() for f in b])
    # 内部目标不是外层使用，允许之后才声明 global。
    values = [declared for declared in [1]]
    global declared
    declared = values
    return x


print(scope(), declared)


class Invalid:
    def __iter__(self):
        return []


try:
    print([x for x in Invalid()])
except TypeError as error:
    print(str(error))

# 同层过滤逐个短路，先失败时后续条件和元素都不能产生副作用。
log = []


def trace(label, value):
    log.append(label)
    return value


print([trace("元素", x) for x in [1, 2]
       if trace("条件一", x == 2) if trace("条件二", True)])
print(log)
print([(first, rest) for first, *rest in [(1, 2, 3), (4,)]])


class MissingNext:
    def __iter__(self):
        return self


try:
    print([x for x in MissingNext()])
except TypeError as error:
    print(str(error))
