# ryupy: pending
# BaseException取消注入、退出抑制、退出再次取消与原异常链跨暂停保留。
events = []
class Cancelled(BaseException):
    pass
class Pause:
    def __init__(self, name):
        self.name = name
    def __await__(self):
        return (yield self.name)
class Manager:
    async def __aenter__(self):
        return "resource"
    async def __aexit__(self, kind, value, tb):
        events.append((kind.__name__, value is cancellation, tb is value.__traceback__))
        self.traceback = tb
        await Pause("cleanup")
        events.append(self.traceback is value.__traceback__)
        return suppress
async def operation(manager):
    async with manager:
        await Pause("body")
    events.append("after")
suppress = True
cancellation = Cancelled("cancel")
manager = Manager()
c = operation(manager)
print(c.send(None))
print(c.throw(cancellation))
try:
    c.send(None)
except StopIteration:
    pass
print(events)
suppress = False
cancellation = Cancelled("second")
manager = Manager()
c = operation(manager)
print(c.send(None))
print(c.throw(cancellation))
try:
    c.send(None)
except Cancelled as error:
    print(error is cancellation)
print(events)
cancellation = Cancelled("original")
manager = Manager()
c = operation(manager)
print(c.send(None))
print(c.throw(cancellation))
replacement = Cancelled("cleanup-cancel")
try:
    c.throw(replacement)
except Cancelled as error:
    print(error is replacement, error.__context__ is cancellation)
print(events)
