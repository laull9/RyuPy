# 本轮全新边界探针，不计常用CP等价通过。
import subject
import asyncio
def run():
    async def task():
        return subject.fold(0, [1, 2])
    return asyncio.run(task())
print(run())
