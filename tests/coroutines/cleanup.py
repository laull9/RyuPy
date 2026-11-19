# 在多个暂停、处理器与嵌套 finally 之间保留局部、异常身份及清理次序。
events = []
class Pause:
    def __await__(self):
        return (yield "pause")
async def nested(value):
    try:
        try:
            first = await Pause()
            events.append(first)
            return await Pause()
        finally:
            events.append(value)
    finally:
        events.append("outer-finally")
c = nested("inner-finally")
print(c.send(None))
print(c.send(5))
error = ValueError("same")
try:
    c.throw(error)
except ValueError as actual:
    print(actual is error)
print(events)
async def recover():
    try:
        await Pause()
    except ValueError as error:
        events.append(str(error))
        value = await Pause()
        return value
    finally:
        events.append("recover-finally")
c = recover()
print(c.send(None))
print(c.throw(ValueError("recover")))
try:
    c.send(99)
except StopIteration as e:
    print(e.value)
print(events)
def factory(value):
    async def closure():
        first = await Pause()
        return value + first
    return closure
c = factory(20)()
print(c.send(None))
try:
    c.send(22)
except StopIteration as e:
    print(e.value)
class Worker:
    async def process(self, value):
        value = value + await Pause()
        return value
c = Worker().process(7)
print(c.send(None))
try:
    c.send(8)
except StopIteration as e:
    print(e.value)
async def return_in_finally():
    try:
        await Pause()
    finally:
        return 16
c = return_in_finally()
print(c.send(None))
try:
    c.throw(ValueError("overridden"))
except StopIteration as e:
    print(e.value)
