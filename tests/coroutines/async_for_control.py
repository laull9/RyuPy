# async-for 控制流合同：真实取值、目标副作用、嵌套、闭包与清理后的跳转。
events = []


class Pause:
    def __init__(self, label):
        self.label = label

    def __await__(self):
        return (yield self.label)


class Values:
    def __init__(self, values):
        self.values = values
        self.index = 0

    def __aiter__(self):
        events.append("aiter")
        return self

    async def __anext__(self):
        if self.index == len(self.values):
            events.append("exhausted")
            raise StopAsyncIteration
        value = self.values[self.index]
        self.index += 1
        events.append(("next", value))
        return value

    def aclose(self):
        raise AssertionError("async for must not call aclose")


def finish(coro):
    try:
        while True:
            print("yield", coro.send(None))
    except StopIteration as error:
        print("result", error.value)


async def false_values():
    result = []
    async for value in Values([None, False, 0, ""]):
        result.append(value)
    else:
        result.append("else")
    return result


finish(false_values())


async def jumps(stop):
    async for value in Values([1, 2, 3]):
        try:
            if value == 1:
                continue
            if stop:
                break
            events.append(("body", value))
        finally:
            events.append(("finally", value))
            await Pause("jump-finally")
    else:
        events.append("loop-else")
    events.append("after")


for stop in [False, True]:
    events.clear()
    finish(jumps(stop))
    print("jumps", stop, events)


async def returning():
    try:
        async for value in Values([5, 6]):
            return value
        else:
            raise AssertionError("return must skip else")
    finally:
        events.append("return-finally")


events.clear()
finish(returning())
print(events)


async def nested():
    result = []
    async for outer in Values([1, 2]):
        async for inner in Values([3, 4]):
            if inner == 3:
                continue
            result.append((outer, inner))
            break
        else:
            raise AssertionError("inner break must skip else")
    else:
        result.append("outer-else")
    return result


finish(nested())


async def closures():
    readers = []
    async for value in Values([8, 9]):
        def read():
            return value
        readers.append(read)
        await Pause("closure-wait")
        events.append(("closure-now", read()))
    return [readers[0](), readers[1]()]


finish(closures())


class Target:
    def __setitem__(self, key, value):
        events.append(("store", key, value))


target = Target()


def target_key():
    events.append("target-key")
    return "slot"


async def assignment():
    async for target[target_key()], right in Values([(10, 11)]):
        events.append(("right", right))


events.clear()
finish(assignment())
print("target-order", events)


async def empty():
    value = "unchanged"
    async for value in Values([]):
        raise AssertionError("empty body")
    else:
        return value


finish(empty())


# else也可能在目标从未绑定时读取它；非空路径仍须返回真实最后一项。
async def else_target(values):
    async for value in Values(values):
        pass
    else:
        return value


for values in [[], [22], [None]]:
    try:
        finish(else_target(values))
    except UnboundLocalError as error:
        print("else-target", type(error).__name__)


async def deleted_target():
    value = 5
    async for value in Values([1]):
        del value
        await Pause("deleted-after")
    else:
        return value


async def handler_target():
    value = 5
    async for item in Values([1]):
        try:
            raise ValueError("clear-local")
        except ValueError as value:
            pass
    else:
        return value


for operation in [deleted_target, handler_target]:
    try:
        finish(operation())
    except UnboundLocalError as error:
        print("cleared-target", type(error).__name__)
