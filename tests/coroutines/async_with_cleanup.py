# enter取消不调用exit，内层enter失败由外层清理；控制转移和目标赋值均在正确保护区内。
events = []
class Cancelled(BaseException):
    pass
class Pause:
    def __init__(self, name):
        self.name = name
    def __await__(self):
        return (yield self.name)
class Entering:
    async def __aenter__(self):
        events.append("enter-start")
        await Pause("enter")
        return self
    async def __aexit__(self, kind, value, tb):
        events.append("unreachable-exit")
async def entering():
    async with Entering():
        events.append("unreachable-body")
c = entering()
print(c.send(None))
cancel = Cancelled("enter-cancel")
try:
    c.throw(cancel)
except Cancelled as error:
    print(error is cancel)
print(events)
enter_error = ValueError("inner-enter")
class Outer:
    async def __aenter__(self):
        return self
    async def __aexit__(self, kind, value, tb):
        events.append((kind.__name__, value is enter_error, tb is value.__traceback__))
        await Pause("outer-exit")
        return True
class Inner:
    async def __aenter__(self):
        await Pause("inner-enter")
        raise enter_error
    async def __aexit__(self, kind, value, tb):
        events.append("unreachable-inner-exit")
async def nested():
    async with Outer(), Inner():
        events.append("unreachable-nested-body")
    events.append("after-inner-failure")
c = nested()
print(c.send(None))
print(c.send(None))
try:
    c.send(None)
except StopIteration:
    pass
print(events)
class LoopManager:
    def __init__(self, label):
        self.label = label
    async def __aenter__(self):
        events.append("loop-enter" + self.label)
    async def __aexit__(self, kind, value, tb):
        events.append(("loop-exit" + self.label, kind is None))
        await Pause("loop-cleanup")
async def loops():
    for index in range(3):
        async with LoopManager(str(index)):
            if index == 0:
                continue
            break
    return "loop-done"
c = loops()
print(c.send(None))
print(c.send(None))
try:
    c.send(None)
except StopIteration as error:
    print(error.value)
print(events)
assignment_error = ValueError("assignment")
class Holder:
    @property
    def value(self):
        return None
    @value.setter
    def value(self, value):
        events.append(("setter", value))
        raise assignment_error
class Assignment:
    async def __aenter__(self):
        return [1, 2]
    async def __aexit__(self, kind, value, tb):
        events.append(("assignment-exit", value is assignment_error, tb is value.__traceback__))
        await Pause("assignment-cleanup")
        return True
async def assignment():
    holder = Holder()
    async with Assignment() as (prefix, holder.value):
        events.append("unreachable-assignment-body")
    print(prefix)
c = assignment()
print(c.send(None))
try:
    c.send(None)
except StopIteration:
    pass
print(events)
body_error = ValueError("body")
truth_error = ValueError("truth")
class Explosive:
    def __bool__(self):
        raise truth_error
class ExplosiveExit:
    async def __aenter__(self):
        return self
    async def __aexit__(self, kind, value, tb):
        await Pause("truth-cleanup")
        return Explosive()
async def explosive():
    async with ExplosiveExit():
        raise body_error
c = explosive()
print(c.send(None))
try:
    c.send(None)
except ValueError as error:
    print(error is truth_error, error.__context__ is body_error)
