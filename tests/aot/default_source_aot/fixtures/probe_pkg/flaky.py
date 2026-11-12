from . import state
from . import dependency
state.attempts += 1
state.events.append("flaky")
if state.attempts == 1:
    raise ValueError("first")
value = 9
