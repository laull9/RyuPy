# 调用目标必须在参数中的yield之前绑定，恢复不能重新查属性。
class Target:
    def call(self, value):
        return ("旧", value)


target = Target()


def method():
    yield target.call((yield "参数"))


iterator = method()
print(next(iterator))
Target.call = lambda self, value: ("新", value)
print(iterator.send(7))


log = []


def original(value):
    return ("函数旧", value)


function = original


def function_call():
    yield function((yield "函数参数"))


iterator = function_call()
print(next(iterator))
function = lambda value: ("函数新", value)
print(iterator.send(8))


class Property:
    @property
    def method(self):
        log.append("读取属性")
        return original


def property_call():
    yield Property().method((yield "属性参数"))


iterator = property_call()
print(next(iterator), log)
print(iterator.send(9), log)


def missing():
    yield target.missing((yield "不应暂停"))


try:
    next(missing())
except AttributeError:
    print("先报告缺属性")


# 内建方法也必须先验证存在；绑定对象跨暂停保留。
values = []


def append():
    yield values.append((yield "append参数"))


iterator = append()
print(next(iterator))
old = values
values = []
print(iterator.send(10), old, values)


def builtin_missing():
    yield values.missing((yield "不应暂停"))


try:
    next(builtin_missing())
except AttributeError:
    print("先报告内建缺属性")


# 非callable属性要先求参数，再报TypeError，不可提前验证可调用性。
class NonCallable:
    value = 1


def non_callable():
    yield NonCallable().value((yield "仍须暂停"))


iterator = non_callable()
print(next(iterator))
try:
    iterator.send(None)
except TypeError:
    print("参数之后才检查callable")


# 关键字参数同样先绑定callee；普通内建必须求完实参，不能跳过后续暂停。
def keyword():
    yield original(value=(yield "关键字"))


iterator = keyword()
print(next(iterator), iterator.send(11))


def builtin():
    print((yield "打印左"), (yield "打印右"))
    yield abs((yield "abs"))


iterator = builtin()
print(next(iterator), iterator.send("左"))
print(iterator.send("右"))
print(iterator.send(-4))


def invalid_abs():
    abs((yield "abs左"), (yield "abs右"))


iterator = invalid_abs()
print(next(iterator), iterator.send(1))
try:
    iterator.send(2)
except TypeError:
    print("求完全部参数才报错")


def invoke(function):
    return function()


def default_argument():
    yield invoke(lambda x=(yield "lambda默认"): x)


iterator = default_argument()
print(next(iterator), iterator.send(12))


# 所有参数先求完，arity错误在执行内建副作用之前报告。
values = [1, 2]


def bad_clear():
    yield values.clear((yield "clear参数"))


iterator = bad_clear()
print(next(iterator))
try:
    iterator.send(9)
except TypeError:
    print("clear拒绝参数", values)


def bad_upper():
    yield "x".upper((yield "upper参数"))


iterator = bad_upper()
print(next(iterator))
try:
    iterator.send(9)
except TypeError:
    print("upper拒绝参数")


def bad_get():
    yield {}.get((yield "get参数"), 2, 3)


iterator = bad_get()
print(next(iterator))
try:
    iterator.send("key")
except TypeError:
    print("get拒绝多参数")


def bad_pop():
    yield values.pop((yield "pop左"), (yield "pop右"))


iterator = bad_pop()
print(next(iterator), iterator.send(0))
try:
    iterator.send(1)
except TypeError:
    print("pop拒绝多参数", values)


class Box:
    def __init__(self, value):
        self.value = value


def construct():
    return Box((yield "构造"))


iterator = construct()
print(next(iterator))
try:
    iterator.send(13)
except StopIteration as error:
    print(error.value.value)


items = [1, 3, 2]


def sorting():
    yield items.sort(reverse=(yield "sort关键字"))


iterator = sorting()
print(next(iterator), iterator.send(True), items)
