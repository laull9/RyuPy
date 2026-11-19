"""协程从非入口模块捕获真实定义点状态，暂停后继续写入同一实例。"""
journal = []


async def resolve(ready):
    journal.append("resolve:start")
    try:
        return await ready
    finally:
        journal.append("resolve:finally")


async def wait_for_cancel(never_ready):
    journal.append("cancel:start")
    try:
        await never_ready
    finally:
        journal.append("cancel:finally")
