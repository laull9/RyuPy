"""独立协程消费者验证同一原生任务和 contextvars 跨两次恢复保持可见。"""
import asyncio
from contextvars import ContextVar

marker = ContextVar("marker")


async def capture_context(gate):
    before = asyncio.current_task()
    await gate
    return before, asyncio.current_task(), marker.get()


async def accepts_await_slot(source):
    try:
        await source
    except TypeError:
        return False
    return True
