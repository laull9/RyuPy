# async-for 协议合同：惰性、类型槽绑定、暂停期间重绑与各类入口错误。
# 预期由固定 CPython 同次生成；此文件不包含循环的替代实现。
events = []
call_error = TypeError("next-body-type-error")


class Pause:
    def __await__(self):
        return (yield "next-wait")


def trace_functions(error):
    result = []
    tb = error.__traceback__
    while tb is not None:
        result.append(tb.tb_frame.f_code.co_name)
        tb = tb.tb_next
    return result


def finish(coro):
    try:
        coro.send(None)
    except StopIteration as error:
        print("done", error.value)
    except BaseException as error:
        print("error", type(error).__name__, str(error), error is call_error)
        cause = error.__cause__
        print("cause", type(cause).__name__ if cause else None,
              str(cause) if cause else None)
        if cause is not None:
            print("cause-frames", trace_functions(cause))


async def consume(source):
    result = []
    async for value in source:
        result.append(value)
    return result


class NextSlot:
    def __get__(self, instance, owner):
        events.append("bind-next")

        async def bound():
            events.append("call-next")
            if instance.used:
                raise StopAsyncIteration
            instance.used = True
            return 7
        return bound


class Described:
    __anext__ = NextSlot()

    def __init__(self):
        self.used = False

    def __aiter__(self):
        events.append("aiter")
        return self

    def __getattr__(self, name):
        raise AssertionError("special lookup used instance fallback")


def source():
    events.append("source")
    value = Described()
    value.__aiter__ = None
    value.__anext__ = None
    return value


async def lazy():
    async for value in source():
        events.append(value)


coro = lazy()
print("created", events)
finish(coro)
print("slots", events)
events.clear()


class Rebound:
    def __aiter__(self):
        return self

    async def __anext__(self):
        events.append("original-next")
        return await Pause()


async def replacement(self):
    events.append("replacement-next")
    raise StopAsyncIteration


coro = consume(Rebound())
print(coro.send(None))
Rebound.__anext__ = replacement
try:
    coro.send(19)
except StopIteration as error:
    print("rebound", error.value, events)


class Missing:
    pass


class InvalidIterator:
    def __aiter__(self):
        return 1


class InvalidNext:
    def __aiter__(self):
        return self

    def __anext__(self):
        return 1


class InvalidAwait:
    def __await__(self):
        return []


class InvalidAwaitNext(InvalidNext):
    def __anext__(self):
        return InvalidAwait()


acquisition_error = ValueError("await-acquisition")


class RaisingAwait:
    def __await__(self):
        raise acquisition_error


class RaisingAwaitNext(InvalidNext):
    def __anext__(self):
        return RaisingAwait()


class RaisingNext(InvalidNext):
    def __anext__(self):
        raise call_error


for value in [Missing(), InvalidIterator(), InvalidNext(),
              InvalidAwaitNext(), RaisingAwaitNext(), RaisingNext()]:
    finish(consume(value))


# coroutine 类型的 aiter 返回值直接拒绝；显式关闭它，防止警告污染 stderr。
async def unused():
    return Described()


raw = unused()


class AwaitableIterator:
    def __aiter__(self):
        return raw


finish(consume(AwaitableIterator()))
raw.close()


class Deleted:
    def __aiter__(self):
        return self

    async def __anext__(self):
        return 1


async def delete_between_rounds():
    async for value in Deleted():
        print("before-delete", value)
        del Deleted.__anext__


finish(delete_between_rounds())


# 初始槽检查只检存在性；None 仍使槽存在，调用阶段才报不可调用。
class NoneNext:
    __anext__ = None

    def __aiter__(self):
        return self


finish(consume(NoneNext()))


# 错误文案的精度按UTF-8字节计算；中文类型名在边界处保留CPython替换字符。
class 甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲:
    pass


finish(consume(甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲甲()))


class 乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙:
    def __aiter__(self):
        return self

    @property
    def __anext__(self):
        raise ValueError("binding")


finish(consume(乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙乙()))
