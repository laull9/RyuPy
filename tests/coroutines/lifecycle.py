# 原生协程显式状态机：惰性创建、挂起输入、异常注入和finally。
events = []
class Pause:
    def __await__(self):
        events.append("pause")
        value = yield "waiting"
        return value
async def child():
    events.append("child")
    try:
        value = await Pause()
        return value + 1
    finally:
        events.append("child-finally")
async def parent():
    events.append("parent")
    try:
        return await child()
    finally:
        events.append("parent-finally")
c = parent()
print(events)
print(c.send(None))
print(events)
try:
    c.send(41)
except StopIteration as e:
    print(e.value)
print(events)
try:
    c.send(None)
except RuntimeError as e:
    print(str(e))
c = parent()
print(c.send(None))
try:
    c.throw(ValueError("injected"))
except ValueError as e:
    print(str(e))
print(events)
c = parent()
print(c.send(None))
c.close()
print(events)
c = parent()
c.close()
print(events)
try:
    iter(c)
except TypeError:
    print("coroutine-not-iterable")
