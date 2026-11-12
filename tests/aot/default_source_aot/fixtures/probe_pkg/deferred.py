from . import state
state.events.append("deferred")
value = 11

async def unsupported(manager):
    async with manager:
        pass
