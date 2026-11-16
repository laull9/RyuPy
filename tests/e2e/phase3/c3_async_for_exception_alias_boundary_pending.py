# ryupy: pending
# 独立支持域负例：变量别名动态继承内建异常仍由C1明确拒绝，保留原pending交叉合同。
BuiltinStop = StopAsyncIteration


class End(BuiltinStop):
    pass


class Source:
    def __aiter__(self):
        return self

    async def __anext__(self):
        raise End("end")


async def consume():
    async for value in Source():
        print(value)
    else:
        print("alias-subclass-end")


coro = consume()
try:
    coro.send(None)
except StopIteration:
    pass
