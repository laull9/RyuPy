# ryupy: pending
# async-for 异常区域合同：只截断下一项的真实耗尽，目标、正文及 else 错误原样传播。
events = []
BuiltinStop = StopAsyncIteration


class End(BuiltinStop):
    pass


class Pause:
    def __await__(self):
        return (yield "next")


class SyncEnd:
    def __aiter__(self):
        return self

    def __anext__(self):
        raise End("sync-end")


class AwaitEnd(SyncEnd):
    async def __anext__(self):
        await Pause()
        raise End("await-end")


async def exhaustion(source):
    async for value in source:
        raise AssertionError("exhausted body")
    else:
        events.append("natural-else")


def finish(coro):
    try:
        coro.send(None)
    except StopIteration as error:
        print("done", error.value)
    except BaseException as error:
        print("raised", type(error).__name__, str(error), error is sentinel)


sentinel = End("sentinel")
# 改写全局同名变量不得改变内部耗尽匹配身份。
StopAsyncIteration = ValueError
finish(exhaustion(SyncEnd()))
coro = exhaustion(AwaitEnd())
print(coro.send(None))
finish(coro)
print(events)
StopAsyncIteration = BuiltinStop


class AiterError(SyncEnd):
    def __aiter__(self):
        raise sentinel


finish(exhaustion(AiterError()))


class One:
    def __init__(self, value):
        self.value = value
        self.used = False

    def __aiter__(self):
        return self

    async def __anext__(self):
        if self.used:
            raise BuiltinStop
        self.used = True
        return self.value

    def aclose(self):
        raise AssertionError("unexpected aclose")


class Target:
    @property
    def value(self):
        raise AssertionError("target getter")

    @value.setter
    def value(self, value):
        events.append(("setter", value))
        raise sentinel


async def target_error():
    target = Target()
    async for target.value in One(1):
        events.append("unreachable-target-body")
    else:
        events.append("unreachable-target-else")


async def unpack_error():
    async for left, right in One([1]):
        events.append("unreachable-unpack-body")
    else:
        events.append("unreachable-unpack-else")


async def body_error():
    async for value in One(2):
        raise sentinel
    else:
        events.append("unreachable-body-else")


async def body_finally_error():
    async for value in One(3):
        try:
            continue
        finally:
            raise sentinel
    else:
        events.append("unreachable-finally-else")


async def else_error():
    async for value in SyncEnd():
        events.append("unreachable-else-body")
    else:
        raise sentinel


for operation in [target_error, unpack_error, body_error, body_finally_error, else_error]:
    finish(operation())
print("boundaries", events)


# 描述符在每轮调用区域绑定，其直接耗尽与 anext 同步抛出相同。
class EndingSlot:
    def __get__(self, instance, owner):
        events.append("ending-bind")
        raise End("slot-end")


class BoundEnd(SyncEnd):
    __anext__ = EndingSlot()


finish(exhaustion(BoundEnd()))
print("descriptor-end", events)


# 取得 await 迭代器前抛出会被专用 TypeError 包装，不能当作自然耗尽。
class AcquisitionEnd:
    def __await__(self):
        raise sentinel


class AcquisitionIterator(SyncEnd):
    def __anext__(self):
        return AcquisitionEnd()


async def acquisition_error():
    try:
        await exhaustion(AcquisitionIterator())
    except TypeError as error:
        print("acquisition", str(error), error.__cause__ is sentinel)


finish(acquisition_error())
