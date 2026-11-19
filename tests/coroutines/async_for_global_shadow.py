# 全局同名绑定只影响源码读取，不能改变async-for的内建耗尽匹配。
BuiltinStop = StopAsyncIteration


class Source:
    def __aiter__(self):
        return self

    async def __anext__(self):
        raise BuiltinStop("end")


async def consume():
    async for value in Source():
        raise AssertionError("exhausted body")
    else:
        print("global-shadow", StopAsyncIteration is ValueError)


StopAsyncIteration = ValueError
coro = consume()
try:
    coro.send(None)
except StopIteration:
    pass
