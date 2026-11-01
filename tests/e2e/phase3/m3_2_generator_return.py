# return value 只属于触发首次完成的 next；消费协议不能把它留下再次领取。
def values():
    yield 1
    return ("完成", 7)


iterator = values()
print(next(iterator))
try:
    next(iterator)
except StopIteration as error:
    print(error.value, error.args)
try:
    next(iterator)
except StopIteration as error:
    print(error.value, error.args)


def immediate():
    if False:
        yield 0
    return [1, 2]


try:
    immediate().__next__()
except StopIteration as error:
    print(error.value, error.args)
iterator = immediate()
print(next(iterator, "默认"))
try:
    next(iterator)
except StopIteration as error:
    print(error.value, error.args)
iterator = values()
print(list(iterator))
try:
    next(iterator)
except StopIteration as error:
    print(error.value, error.args)
iterator = values()
for value in iterator:
    print(value)
try:
    next(iterator)
except StopIteration as error:
    print(error.value, error.args)


# 显式None、裸return、自然落尾都给None，返回值的副作用依然惰性且只执行一次。
log = []


def finish():
    log.append("结束")
    return 9


def returning():
    yield None
    return finish()


iterator = returning()
print(log, next(iterator), log)
try:
    next(iterator)
except StopIteration as error:
    print(error.value, log)
print(next(iterator, "已结束"), log)



# 返回对象保持身份，不能展开tuple或把args转换成字符串。
marker = []


def identity():
    yield 0
    return marker


iterator = identity()
print(next(iterator))
try:
    next(iterator)
except StopIteration as error:
    print(error.value is marker, error.args[0] is marker)


# 提前break未消费完成值；正常循环耗尽才丢弃它。
iterator = values()
for value in iterator:
    print(value)
    break
try:
    next(iterator)
except StopIteration as error:
    print(error.value, error.args)


def explicit_none():
    yield 0
    return None


iterator = explicit_none()
print(next(iterator))
try:
    next(iterator)
except StopIteration as error:
    print(error.value, error.args)


def bare():
    yield 0
    return


def fallthrough():
    yield 0


for iterator in [bare(), fallthrough()]:
    print(next(iterator))
    try:
        next(iterator)
    except StopIteration as error:
        print(error.value, error.args)


# 构造完成异常不能提前调用终值__str__；真正格式化时每次读取最新对象内容。
log = []


class Result:
    def __init__(self):
        self.text = "初值"

    def __str__(self):
        log.append(self.text)
        return self.text


def result_object(value):
    if False:
        yield 0
    return value


result = Result()
try:
    next(result_object(result))
except StopIteration as error:
    print(error.value is result, log)
    print(str(error), log)
    result.text = "修改"
    print(str(error), log)


class BadString:
    def __str__(self):
        raise ValueError("格式化才失败")


result = BadString()
try:
    next(result_object(result))
except StopIteration as error:
    print(error.value is result)
    try:
        print(str(error))
    except ValueError as failure:
        print(str(failure))
