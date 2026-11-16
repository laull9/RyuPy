# 类型槽绑定、错误awaitable、退出结果真值与关闭协议保持真实求值顺序。
events = []
class Pause:
    def __await__(self):
        return (yield "waiting")
class Truth:
    def __bool__(self):
        events.append("truth")
        return True
class Manager:
    async def __aenter__(self):
        events.append("enter")
        await Pause()
        return self
    async def __aexit__(self, kind, value, tb):
        events.append("original-exit")
        return Truth()
original_exit = Manager.__aexit__
async def replacement(self, kind, value, tb):
    events.append("replacement-exit")
    return False
async def operation(manager, error):
    async with manager:
        if error:
            raise ValueError("body")
manager = Manager()
manager.__aenter__ = None
manager.__aexit__ = None
c = operation(manager, True)
print(c.send(None))
Manager.__aexit__ = replacement
try:
    c.send(None)
except StopIteration:
    pass
print(events)
# 正常退出不测试返回值真值。
Manager.__aexit__ = original_exit
c = operation(Manager(), False)
print(c.send(None))
try:
    c.send(None)
except StopIteration:
    pass
print(events)
class InvalidEnter:
    def __aenter__(self):
        return 1
    async def __aexit__(self, kind, value, tb):
        events.append("unreachable-exit")
class InvalidExit:
    async def __aenter__(self):
        return self
    def __aexit__(self, kind, value, tb):
        return 1
async def invalid(manager):
    async with manager:
        pass
for manager in [InvalidEnter(), InvalidExit()]:
    c = invalid(manager)
    try:
        c.send(None)
    except TypeError as error:
        print(str(error))
# close在await清理期间真实产生值时，报告忽略GeneratorExit并保留暂停状态。
class Closing:
    async def __aenter__(self):
        return self
    async def __aexit__(self, kind, value, tb):
        events.append(kind.__name__)
        await Pause()
        events.append("closed")
async def closing():
    async with Closing():
        await Pause()
c = closing()
print(c.send(None))
try:
    c.close()
except RuntimeError as error:
    print(str(error))
try:
    c.send(None)
except GeneratorExit:
    print("generator-exit")
print(events)
