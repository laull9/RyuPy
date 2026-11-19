# 复用已验Static print/flush/partial/TypedDict路径；不增加Python flush包装帧。
import sys
import importlib.resources
from functools import partial
from typing import TypedDict

events = []


def unused():
    print('UNUSED called')
    return importlib.resources.files('pkg.data')


class Sink:
    def write(self, text):
        events.append(('write', text))
        return len(text)


class ModuleMarker:
    def __bool__(self):
        events.append(('module-bool',))
        raise RuntimeError('module-bool-marker')


# 此body与现有static_td fixture保持相同。不能把它改为Dynamic。
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
    except NotImplementedError as error:
        events.append(('caught-ni', str(error)))
        outcome = 'not-implemented'
    except RuntimeError as error:
        events.append(('caught', str(error)))
        outcome = 'runtime-error'
    finally:
        sys.stdout = original_stdout
    print('S', label, outcome, out.__module__ is module_value, events)
print('S data-cached', 'pkg.data' in sys.modules)
print('S done')
