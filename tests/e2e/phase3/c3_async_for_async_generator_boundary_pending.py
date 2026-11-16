# ryupy: pending
# 独立支持域负例：CPython 能消费异步生成器，本切候选须明确给出 SupportBoundary 诊断。
# 实施时接入拒绝门，不能并入已完成的 async-for 成功集合。
async def values():
    yield 1


async def consume():
    async for value in values():
        print(value)


coro = consume()
try:
    coro.send(None)
except StopIteration:
    pass
