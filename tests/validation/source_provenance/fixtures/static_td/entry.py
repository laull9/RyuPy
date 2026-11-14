# 原版本Static TD可观察反例；本源不增加Python flush包装帧。
import sys
from functools import partial
from typing import TypedDict

events = []

class Sink:
    def write(self, text):
        events.append(('write', text))
        return len(text)

class ModuleMarker:
    def __bool__(self):
        events.append(('module-bool',))
        raise RuntimeError('module-bool-marker')

# print本身在类型层返回None；完整标注、常量实参，不读任何动态global。
def out() -> None:
    print('payload', flush=True)

sink = Sink()
sink.flush = partial(TypedDict, 'Record', {'value': int})
original_stdout = sys.stdout
marker = ModuleMarker()

for label, module_value in (('default', __name__), ('rebound', marker)):
    events.clear()
    out.__module__ = module_value
    outcome = 'not-called'
    sys.stdout = sink
    try:
        events.append(('before-out',))
        out()
        events.append(('after-out',))
        outcome = 'ok'
    except RuntimeError as error:
        events.append(('caught', str(error)))
        outcome = 'runtime-error'
    finally:
        sys.stdout = original_stdout
    print('S', label, outcome, out.__module__ is module_value, events)
print('S done')
