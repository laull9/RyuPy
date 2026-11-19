"""由真实 asyncio.Task 驱动异模块 CIL 协程，比较完成值和取消清理。"""
import asyncio
from context_tasks import accepts_await_slot, capture_context, marker
from jobs import journal, resolve, wait_for_cancel
from types import SimpleNamespace

loop = asyncio.new_event_loop()
try:
    value = object()
    ready = loop.create_future()
    result = loop.create_task(resolve(ready))
    assert not journal
    loop.call_soon(ready.set_result, value)
    assert loop.run_until_complete(result) is value

    never_ready = loop.create_future()
    cancelled = loop.create_task(wait_for_cancel(never_ready))
    loop.call_soon(cancelled.cancel)
    try:
        loop.run_until_complete(cancelled)
    except asyncio.CancelledError:
        pass
    else:
        raise AssertionError("任务没有收到取消")
    assert cancelled.cancelled() and never_ready.cancelled()
    assert journal == ["resolve:start", "resolve:finally", "cancel:start", "cancel:finally"]

    # wait_for(..., None) 在真正的 CPython 协程里直接 await 编译协程，不经第二个 Task。
    marker.set("task-context")
    gate = loop.create_future()
    task = loop.create_task(asyncio.wait_for(capture_context(gate), None))
    marker.set("outer-context")
    loop.call_soon(gate.set_result, None)
    task_before, task_after, context = loop.run_until_complete(task)
    assert task_before is task and task_after is task and context == "task-context"

    # 外部对象的实例属性不能冒充类型的 am_await 槽。
    counterfeit = SimpleNamespace(__await__=lambda: iter(()))
    assert not loop.run_until_complete(loop.create_task(accepts_await_slot(counterfeit)))
    assert loop.run_until_complete(loop.create_task(accepts_await_slot(gate)))
finally:
    loop.close()

print("任务完成、定义点状态、取消清理与类型槽通过")
