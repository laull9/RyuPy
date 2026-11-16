# GET_ANEXT与普通await的协程所有权差异：每轮真实使用返回对象，不额外加普通await的占用检查。
class Pause:
    def __await__(self):
        return (yield "shared-wait")


async def value():
    return await Pause()


class Source:
    def __aiter__(self):
        return self

    def __anext__(self):
        return shared


async def once():
    async for item in Source():
        return item


async def ordinary():
    return await shared


def finish(coro):
    try:
        print("yield", coro.send(None))
    except StopIteration as error:
        print("done", error.value)
    except BaseException as error:
        print(type(error).__name__, str(error))


shared = value()
first = once()
finish(first)
finish(ordinary())
finish(once())
finish(first)
finish(once())
shared.close()
