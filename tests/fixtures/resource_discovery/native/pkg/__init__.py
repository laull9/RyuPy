# 包与data同basename，Static经原生partial回调TypedDict，不增加Python包装帧。
import sys
from functools import partial
from typing import TypedDict

events = []


class Sink:
    def write(self, text):
        events.append(text)
        return len(text)


def out() -> None:
    print('payload', flush=True)


sink = Sink()
sink.flush = partial(TypedDict, 'Record', {'value': int})
original = sys.stdout
sys.stdout = sink
try:
    out()
finally:
    sys.stdout = original
print('static', events)
