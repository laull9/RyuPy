# async-for 生命周期合同：委托 send/throw/close、取消展开和跨暂停上下文清理。
events = []


class Cancel(BaseException):
    pass


class Pause:
    def __init__(self, label):
        self.label = label

    def __await__(self):
        try:
            return (yield self.label)
        finally:
            events.append(("pause-finally", self.label))


class Source:
    def __aiter__(self):
        events.append("aiter")
        return self

    async def __anext__(self):
        try:
            return await Pause("next-wait")
        finally:
            events.append("next-finally")

    def aclose(self):
        raise AssertionError("async for must not call aclose")


class Manager:
    async def __aenter__(self):
        events.append("enter")
        return self

    async def __aexit__(self, kind, error, tb):
        events.append(("exit", kind.__name__ if kind else None,
                       error is injected, tb is error.__traceback__ if error else tb is None))
        await Pause("exit-wait")
        events.append("exit-finished")
        return False


async def operation():
    try:
        async with Manager():
            async for value in Source():
                events.append(("body", value))
                await Pause("body-wait")
                break
            else:
                events.append("else")
    finally:
        events.append("outer-finally")


def resume(coro, value=None):
    try:
        print("yield", coro.send(value))
    except StopIteration as error:
        print("done", error.value)
    except BaseException as error:
        print("raised", type(error).__name__, error is injected)


injected = Cancel("cancel")
coro = operation()
print("lazy", events)
resume(coro)
resume(coro, 41)
resume(coro)
resume(coro)
print("normal", events)


# 下一项与正文的暂停点都须传递相同异常对象，再 await 外层退出清理。
for body in [False, True]:
    events.clear()
    coro = operation()
    resume(coro)
    if body:
        resume(coro, 42)
    print("throw-yield", coro.throw(injected))
    resume(coro)
    print("cancel", body, events)


# close 清理仍产出时保留暂停；随后恢复完成清理并传播原 GeneratorExit。
events.clear()
coro = operation()
resume(coro)
try:
    coro.close()
except RuntimeError as error:
    print("close", str(error))
resume(coro)
print("closed", events)


# 委托可以处理注入异常并给出一项；不能在恢复时重新调用 anext。
class Recovering(Source):
    async def __anext__(self):
        events.append("recovering-next")
        try:
            return await Pause("recovering-wait")
        except Cancel as error:
            events.append(error is injected)
            return 73


async def recover():
    async for value in Recovering():
        events.append(("recovered", value))
        return value


events.clear()
coro = recover()
resume(coro)
try:
    coro.throw(injected)
except StopIteration as error:
    print("recovered-result", error.value)
print(events)


# 同一个 StopAsyncIteration 在 next await 中耗尽，在 body await 中则正常外传。
injected = StopAsyncIteration("injected-end")
for body in [False, True]:
    events.clear()
    coro = operation()
    resume(coro)
    if body:
        resume(coro, 44)
    print("stop-yield", coro.throw(injected))
    resume(coro)
    print("stop-location", body, events)


# return 的值先冻结，finally 可以继续挂起而不丢失控制流。
async def returning():
    try:
        async for value in Source():
            return value
    finally:
        await Pause("return-cleanup")


coro = returning()
resume(coro)
resume(coro, 81)
resume(coro)
