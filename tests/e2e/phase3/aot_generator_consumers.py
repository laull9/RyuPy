# ryupy: pending
# 消费融合的语义准入；尚未完成产品差分前保留pending，不代表融合已经启用。
# 普通for、sum和yield from分别观察消费、身份及提前结束，不能相互替代。


def count(n):
    i = 0
    while i < n:
        yield i
        i += 1


def plain():
    total = 0
    for value in count(7):
        total += value
    return total


def chained():
    yield from count(4)
    yield from count(3)


print("arms", plain(), sum(x * 2 for x in range(7)), list(chained()))

# 最左iter必须立即求值，元素/过滤必须惰性且按原顺序执行。
events = []


class Source:
    def __iter__(self):
        events.append("iter")
        return iter([1, 2, 3])


def element(value):
    events.append(value)
    return value * 2


g = (element(x) for x in Source() if x != 2)
print("created", events)
print("first", next(g), events)
print("rest", sum(g), events)

# 已创建生成器保留原函数捕获；不能在每次恢复时重新解析全局函数。
original = count
g = count(3)


def replacement(n):
    yield 99


count = replacement
print("binding", list(g), list(count(3)))
count = original

# 共享cell在逐项消费期间更新；不能在融合区入口快照。
def captured():
    factor = 2
    g = (factor * x for x in range(4))
    first = next(g)
    factor = 7
    return first, list(g)


print("cell", captured())

# for提前break不会隐式close；保留别名可以继续恢复，finally只在显式close时运行。
events = []


def cleanup():
    try:
        yield 10
        events.append("continued")
        yield 20
    finally:
        events.append("closed")


g = cleanup()
alias = g
for value in g:
    print("break", value)
    break
print("paused", events)
print("resume", next(alias), events)
g.close()
print("close", events, next(alias, "done"))

# 消费者异常也不能多取下一项或强制关闭仍被引用的生成器。
events = []
g = cleanup()
try:
    for value in g:
        raise ValueError("consumer")
except ValueError as error:
    print("consumer", str(error), events)
print("after error", next(g), events)
g.close()

# sum的用户加法回调与生产者交错，失败必须停在当前元素之后。
events = []


class Accumulator:
    def __add__(self, value):
        events.append(value)
        if value == 2:
            raise ValueError("add")
        return self


g = count(4)
try:
    sum(g, Accumulator())
except ValueError as error:
    print("sum error", str(error), events, next(g))

# 同一已有对象产出多次不能重新装箱或复制，任一消费者都观察真实别名。
def objects(value):
    yield value
    yield value


value = [1]
g = objects(value)
first = next(g)
first.append(2)
print("identity", first is value, next(g) is first, value)

# yield from的返回值与send/close转发必须保留，不能当作普通for改写。
def child():
    received = yield 3
    return received


def parent():
    result = yield from child()
    yield result


g = parent()
print("delegate", next(g), g.send(9), next(g, "done"))
events = []


def delegate_cleanup():
    yield from cleanup()


g = delegate_cleanup()
print("delegate first", next(g))
g.close()
print("delegate closed", events)

# PEP479错误属于生产者，不得被消费融合误认为正常耗尽。
def invalid():
    yield 1
    raise StopIteration("body")


try:
    sum(invalid())
except RuntimeError as error:
    print("pep479", str(error))

# throw必须传给当前子生成器；子生成器可捕获、继续yield并把返回值交给父生成器。
events = []


def throw_child():
    try:
        try:
            yield "ready"
        except ValueError as error:
            events.append(str(error))
            yield "handled"
        return "child-return"
    finally:
        events.append("child-finally")


def throw_parent():
    result = yield from throw_child()
    yield result


g = throw_parent()
print("throw ready", next(g))
print("throw handled", g.throw(ValueError("forwarded")), events)
print("throw result", next(g), events)
print("throw done", next(g, "done"))

# 未捕获异常穿过委托时，子/父finally各执行一次且顺序不变。
events = []


def uncaught_child():
    try:
        yield 1
    finally:
        events.append("child")


def uncaught_parent():
    try:
        yield from uncaught_child()
    finally:
        events.append("parent")


g = uncaught_parent()
next(g)
try:
    g.throw(ValueError("escape"))
except ValueError as error:
    print("throw escape", str(error), events, next(g, "done"))
