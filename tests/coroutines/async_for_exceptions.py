# async-for 异常区域合同：只截断下一项的真实耗尽，目标、正文及 else 错误原样传播。
events = []
BuiltinStop = StopAsyncIteration


class End(StopAsyncIteration):
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
    StopAsyncIteration = ValueError
    events.append(("local-shadow", StopAsyncIteration is ValueError))
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
# 局部同名变量不得改变内部耗尽匹配身份；全局遮蔽另用独立成功输入保护。
finish(exhaustion(SyncEnd()))
coro = exhaustion(AwaitEnd())
print(coro.send(None))
finish(coro)
print(events)


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


# CPython的async槽把描述符绑定失败改为AttributeError，不能误当作方法调用耗尽。
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


# 已观察的同一frame结束后仍冻结真实末行；合成的耗尽标记不能覆盖break源码行。
saved_frames = []
saved_traces = []


async def frozen_break_line():
    try:
        raise ValueError("observe-frame")
    except ValueError as error:
        saved_frames.append(error.__traceback__.tb_frame)
        saved_traces.append(error.__traceback__)
    async for value in One(1):
        break


async def frozen_finally_line():
    try:
        raise ValueError("observe-finally")
    except ValueError as error:
        saved_frames.append(error.__traceback__.tb_frame)
        saved_traces.append(error.__traceback__)
    async for value in One(2):
        try:
            break
        finally:
            pass


async def frozen_else_line():
    try:
        raise ValueError("observe-else")
    except ValueError as error:
        saved_frames.append(error.__traceback__.tb_frame)
        saved_traces.append(error.__traceback__)
    async for value in SyncEnd():
        raise AssertionError("unreachable")
    else:
        pass


for operation in [frozen_break_line, frozen_finally_line, frozen_else_line]:
    finish(operation())
for index in range(3):
    print("frozen-lines", saved_frames[index].f_lineno, saved_traces[index].tb_lineno,
          saved_traces[index].tb_frame is saved_frames[index])
