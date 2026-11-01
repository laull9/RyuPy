# 普通生成器首切：惰性执行、控制流恢复、局部/cell、错误终止与独立实例。
log = []


def sequence(limit):
    log.append("开始")
    yield None
    for value in range(limit):
        log.append(value)
        if value % 2 == 0:
            yield value
    index = 0
    while index < 2:
        yield index + 10
        index += 1
    log.append("结束")


left = sequence(3)
right = sequence(1)
print(log)
print(iter(left) is left)
print(next(left), next(right))
print(next(left), next(right))
print(list(left), list(right))
print(log)
print(next(left, "耗尽"), next(left, "仍耗尽"))


def empty():
    if False:
        yield 1
    return


print(list(empty()))


def outer():
    value = 1

    def generate():
        yield value
        yield value

    def update():
        nonlocal value
        value = 8

    return generate(), update


iterator, update = outer()
print(next(iterator))
update()
print(next(iterator))


def broken():
    yield 1
    raise ValueError("出错")


iterator = broken()
print(next(iterator))
try:
    next(iterator)
except ValueError as error:
    print(str(error))
print(next(iterator, "错误后终止"))


def escaped_stop():
    raise StopIteration("不能当正常结束")
    yield 1


try:
    next(escaped_stop())
except RuntimeError as error:
    print(str(error))


# 默认参数在定义时捕获，参数错误在调用工厂时而不是第一次 next 时报告。
def arguments(first: int, second: int = 7) -> int:
    yield first
    yield second


print(list(arguments(second=9, first=3)))
try:
    arguments()
except TypeError:
    print("参数立即检查")


# 局部 cell 与未赋值变量必须跨暂停保存；循环跳转不能重新执行函数序言。
def local_cells():
    value = 2

    def read():
        return value

    yield read
    value = 5
    yield read
    for index in range(5):
        if index == 1:
            continue
        if index == 3:
            break
        yield index


iterator = local_cells()
read = next(iterator)
print(read())
print(next(iterator) is read, read())
print(list(iterator), read())


def unbound():
    yield 0
    print(value)
    value = 1


iterator = unbound()
print(next(iterator))
try:
    next(iterator)
except UnboundLocalError:
    print("未绑定保持")
print(next(iterator, "终止"))


# 重入错误也让外层这次恢复失败并永久终止，不能留下 running 标志或调用栈。
def recursive():
    yield 0
    next(reentrant)


reentrant = recursive()
print(next(reentrant))
try:
    next(reentrant)
except ValueError as error:
    print(str(error))
print(next(reentrant, "重入后终止"))
