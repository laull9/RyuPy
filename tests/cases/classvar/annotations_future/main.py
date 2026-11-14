# ryupy: pending
# future 注解保留实际字符串；Markdown 原形中的 Element 没有运行时定义，不应被读取。
from __future__ import annotations
from typing import ClassVar, Callable
events = []

def mark():
    events.append('annotation')
    return int

def initial():
    events.append('rhs')
    return 7

class FutureRecord:
    value: ClassVar[mark()] = initial()
    output_formats: ClassVar[dict[str, Callable[[Element], str]]] = {}

assert events == ['rhs']
assert FutureRecord.__annotations__ == {
    'value': 'ClassVar[mark()]',
    'output_formats': 'ClassVar[dict[str, Callable[[Element], str]]]'}
record = FutureRecord()
record.value = 8
assert FutureRecord.value == 7 and record.value == 8
print('future', events, FutureRecord.__annotations__)
