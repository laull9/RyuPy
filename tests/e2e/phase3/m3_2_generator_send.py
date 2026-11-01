# send恢复yield表达式，None启动/next等价；完成值仍只属于触发结束的操作。
def echo():
    value = yield "就绪"
    yield value
    total = (yield 1) + (yield 2)
    return total


iterator = echo()
try:
    iterator.send(9)
except TypeError:
    print("新生成器拒绝非None且不启动")
print(iterator.send(None))
marker = []
print(iterator.send(marker) is marker)
print(next(iterator))
print(iterator.send(3))
try:
    iterator.send(4)
except StopIteration as error:
    print(error.value)
try:
    iterator.send(99)
except StopIteration as error:
    print(error.value)


# yield结果用于容器/函数调用，早先求值的局部必须跨第二次暂停保存。
def collected():
    values = [(yield "左"), (yield "右")]
    yield values
    yield "忽略接收"
    yield None


iterator = collected()
print(next(iterator), iterator.send(10), iterator.send(20))
print(iterator.send(30), iterator.send(40), next(iterator, "结束"))


# lambda自身也是生成器，默认参数表达式则在外层生成器恢复中执行。
iterator = (lambda: (yield "lambda"))()
print(next(iterator))
try:
    iterator.send(5)
except StopIteration as error:
    print(error.value)


def defaults():
    def value(x=(yield "默认值")):
        return x
    yield value()


iterator = defaults()
print(next(iterator), iterator.send(7), next(iterator, "结束"))


def loop():
    value = yield 0
    while value is not None:
        value = yield value * 2


iterator = loop()
print(next(iterator), iterator.send(2), iterator.send(3))
print(next(iterator, "None结束"))


# 错误参数不能改变状态；生成器表达式的语句yield忽略发送值。
iterator = echo()
try:
    iterator.send()
except TypeError:
    print("send缺参数")
try:
    iterator.send(1, 2)
except TypeError:
    print("send多参数")
print(next(iterator))
expression = (x for x in [1, 2])
print(expression.send(None), expression.send("忽略"), next(expression, "结束"))


# 推导式首iterable属于外层，允许在外层yield后取得send传入的可迭代对象。
def lazy_source():
    return (x * 2 for x in (yield "源"))


iterator = lazy_source()
print(next(iterator))
try:
    iterator.send([2, 3])
except StopIteration as error:
    print(list(error.value))


# 短路与条件表达式只暂停实际执行的分支。
def short_circuit():
    yield False and (yield "不应出现")
    value = (yield "选择") if True else (yield "不应出现")
    yield value


iterator = short_circuit()
print(next(iterator), next(iterator), iterator.send(8))


# 普通tuple/set保持最终类型；列表星号在下次暂停前已经展开。
def container_types():
    pair = ((yield "tuple"), 2)
    group = {(yield "set"), 3}
    return pair, group


iterator = container_types()
print(next(iterator), iterator.send(1))
try:
    iterator.send(3)
except StopIteration as error:
    pair, group = error.value
    print(pair, type(pair).__name__, len(group), type(group).__name__)


def starred_list(items):
    return [*items, (yield "list")]


items = [1]
iterator = starred_list(items)
print(next(iterator))
items.append(2)
try:
    iterator.send(9)
except StopIteration as error:
    print(error.value)
