# ryupy: pending
# 独立支持域：真实 Markdown 形状的延迟注解，不含旧前端尚未支持的注解调用表达式。
from __future__ import annotations
from typing import ClassVar, Callable
events = []

def initial():
    events.append('rhs')
    return {}

class MarkdownShape:
    output_formats: ClassVar[dict[str, Callable[[Element], str]]] = initial()
    value: ClassVar[int] = 7

assert events == ['rhs']
assert MarkdownShape.__annotations__ == {
    'output_formats': 'ClassVar[dict[str, Callable[[Element], str]]]',
    'value': 'ClassVar[int]'}
assert MarkdownShape.output_formats == {}
record = MarkdownShape()
record.value = 8
assert MarkdownShape.value == 7 and record.value == 8
assert events == ['rhs']
print('future-markdown', events, MarkdownShape.__annotations__, MarkdownShape.value, record.value)
