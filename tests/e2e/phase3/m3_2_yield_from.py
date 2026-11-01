# 委托在同一次父恢复中消费StopIteration.value，转发send/throw/close。
def child():
    try:
        value = yield "child"
        yield value
    except ValueError as error:
        yield str(error)
    finally:
        print("child final")
    return 42


def parent():
    result = yield from child()
    yield result


it = parent()
print(next(it), it.send(7))
print(it.throw(ValueError("forwarded")))
print(next(it), next(it, "done"))
it = parent()
print(next(it), it.close(), next(it, "closed"))


def plain():
    value = yield from [1, 2]
    print("plain return", value)
    return 9


it = plain()
print(next(it), it.send(None))
try:
    next(it)
except StopIteration as error:
    print(error.value)

it = plain()
print(next(it))
try:
    it.send(1)
except AttributeError:
    print("没有send")

# 真实自定义iterator的返回值不能被普通MoveNext吞掉。
class Custom:
    def __init__(self):
        self.count = 0

    def __iter__(self):
        print("iter once")
        return self

    def __next__(self):
        self.count += 1
        if self.count == 1:
            return "custom"
        raise StopIteration("custom result")

    def send(self, value):
        print("custom send", value)
        raise StopIteration("sent result")

    def throw(self, error):
        print("custom throw", type(error).__name__)
        return "caught"

    def close(self):
        print("custom close")


def custom_parent():
    result = yield from Custom()
    yield result


it = custom_parent()
print(next(it), next(it), next(it, "done"))
it = custom_parent()
print(next(it), it.send(5))
it.close()
it = custom_parent()
print(next(it), it.throw(TypeError("x")), it.close())
