# ryupy: pending
# 独立支持域负例：自定义__getattribute__仍属普通类未开放域，不能混入async-for成功集合。
class Source:
    def __getattribute__(self, name):
        if name == "__aiter__" or name == "__anext__":
            raise AssertionError("special lookup must bypass instance override")
        return object.__getattribute__(self, name)

    def __aiter__(self):
        return self

    async def __anext__(self):
        raise StopAsyncIteration


async def consume():
    async for value in Source():
        print(value)
    else:
        print("special-slot-bypass")


coro = consume()
try:
    coro.send(None)
except StopIteration:
    pass
