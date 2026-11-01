# 每个失败阶段都不能提前覆盖公开名字；静态调用者也必须读取装饰后的结果。
def identity(function):
    return function


def fail():
    raise ValueError("definition")


def existing():
    return "old"


old = existing
try:
    @fail()
    def existing():
        return "new"
except ValueError:
    print(existing is old)

try:
    @identity
    def existing(value=fail()):
        return "new"
except ValueError:
    print(existing is old)

try:
    @fail()
    def never_bound():
        return 1
except ValueError:
    try:
        print(never_bound)
    except NameError:
        print("unbound")


def replace(function):
    def wrapped(value):
        return value + 10
    return wrapped


@replace
def annotated(value: int) -> int:
    return value - 1


def caller(value: int) -> int:
    return annotated(value)


print(caller(3))
