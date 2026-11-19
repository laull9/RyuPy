# ryupy: pending
"""原版 Task 保存缺槽异常又传播给调用者；完整 carrier 关闭计数待验。"""
import asyncio
from types import SimpleNamespace


async def resolve(source):
    return await source


loop = asyncio.new_event_loop()
try:
    try:
        loop.run_until_complete(loop.create_task(resolve(SimpleNamespace())))
    except TypeError:
        print("任务原样传播缺失 await 槽异常")
    else:
        raise AssertionError("缺槽未被拒绝")
finally:
    loop.close()
