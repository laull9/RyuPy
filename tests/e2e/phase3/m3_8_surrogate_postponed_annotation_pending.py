# ryupy: pending
# 延迟注解字符串化尚未恢复surrogate原token；现有规范渲染器给出明确E0100。
from __future__ import annotations

def annotated(argument: '\ud800') -> '\udc00':
    return argument
print(annotated.__annotations__)
