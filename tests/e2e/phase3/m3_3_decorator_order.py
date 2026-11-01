# 定义位置源码顺序求值、逆序应用；默认值在decorator表达式之后。
log = []


def default():
    log.append("default")
    return 7


def decorator(label):
    log.append("evaluate " + label)

    def apply(function):
        log.append("apply " + label)
        return function

    return apply


@decorator("outer")
@decorator("inner")
def function(value=default()):
    return value


print(log, function())

# 装饰失败前不能覆盖旧绑定。
old = "previous"


def fails(function):
    raise ValueError("failed")


try:
    @fails
    def old():
        return "new"
except ValueError:
    print(old)

# 返回非callable也可定义；错误留到调用时。
def replace(function):
    return 19


@replace
def replaced():
    return 0


print(replaced)
try:
    replaced()
except TypeError:
    print("not callable")

# 闭包decorator使用定义处外层cell。
def outer(prefix):
    def wrap(function):
        def result(value):
            return prefix + function(value)
        return result

    @wrap
    def inner(value):
        return value

    prefix = "changed:"
    return inner


print(outer("old:")("ok"))

# 类装饰器先求值，再执行类体，最后逆序应用；失败时保留原公开绑定。
class_events = []


def class_decorator(label):
    class_events.append("evaluate " + label)
    def apply(cls):
        class_events.append("apply " + label)
        return cls
    return apply


@class_decorator("outer")
@class_decorator("inner")
class Ordered:
    class_events.append("body")


print(class_events)


def fail_class(cls):
    raise ValueError("failed")


old = Ordered
try:
    @fail_class
    class Ordered:
        pass
except ValueError:
    print(Ordered is old)
