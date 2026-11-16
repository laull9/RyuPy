# enter/body/exit 都真实挂起；return、解包失败与多上下文保持清理顺序。
events = []
class Pause:
    def __init__(self, name):
        self.name = name
    def __await__(self):
        return (yield self.name)
class Manager:
    def __init__(self, name, suppress=False):
        self.name = name
        self.suppress = suppress
    async def __aenter__(self):
        events.append(self.name + ":enter")
        return await Pause(self.name + ":enter-wait")
    async def __aexit__(self, kind, value, tb):
        events.append((self.name, kind.__name__ if kind else None, tb is None))
        await Pause(self.name + ":exit-wait")
        events.append(self.name + ":closed")
        return self.suppress
async def operation():
    async with Manager("one") as value:
        events.append(value)
        return await Pause("body")
c = operation()
print(c.send(None))
print(c.send("resource"))
print(c.send(42))
try:
    c.send(None)
except StopIteration as error:
    print(error.value)
print(events)
async def unpacking():
    async with Manager("unpack", True) as (left, right):
        events.append("unreachable")
    events.append("after-unpack")
c = unpacking()
print(c.send(None))
print(c.send([1]))
try:
    c.send(None)
except StopIteration:
    pass
print(events)
async def nested():
    async with Manager("outer"), Manager("inner"):
        events.append("nested-body")
c = nested()
for value in [None, None, None, None]:
    print(c.send(value))
try:
    c.send(None)
except StopIteration:
    pass
print(events)
