# 原生候选尚未实现 **mapping 的跨暂停快照，必须在 HIR 明确拒绝。
async def unsupported(target, pause, mapping):
    return target(await pause(), **mapping)

async def unsupported_keyword(target, pause, mapping):
    return target(**mapping, value=await pause())
