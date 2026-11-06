# ryupy: pending
# CP合法的Static原生转发与RyuPy有限来源拒绝分开验收，不计普通差分成功数。
# 显式非None和真实Dynamic回调仍必须成功；Static分类由同源结构门强制检查。
from collections import namedtuple
from functools import partial
import sys


def static_output(value: int) -> None:
    print('x')


class Sink:
    pass


class Poison:
    def __bool__(self):
        raise AssertionError('explicit module must not request truth')

    def __str__(self):
        raise AssertionError('explicit module must not request string')


original_stdout = sys.stdout
sink = Sink()
for label, writer in [
    ('omitted', partial(namedtuple, 'Printed')),
    ('none', partial(namedtuple, 'Printed', module=None)),
]:
    sink.write = writer
    outcome = 'accepted'
    try:
        sys.stdout = sink
        static_output(1)
    except NotImplementedError as error:
        outcome = type(error).__name__ + ': ' + str(error)
    finally:
        sys.stdout = original_stdout
    print('C1.' + label, outcome)

for index, module in enumerate(['', 0, False, [], Poison()]):
    sink.write = partial(namedtuple, 'Printed', module=module)
    try:
        sys.stdout = sink
        static_output(1)
    finally:
        sys.stdout = original_stdout
    print('C1.explicit-' + str(index), 'accepted')

seen = []


def dynamic_write(text):
    record = namedtuple('CallbackRecord', text)
    seen.append(record.__module__)
    return len(text)


dynamic_write.__module__ = 'dynamic.callback'
sink.write = dynamic_write
try:
    sys.stdout = sink
    static_output(1)
finally:
    sys.stdout = original_stdout
assert seen == ['dynamic.callback', 'dynamic.callback']
print('C1.dynamic-callback', seen)
