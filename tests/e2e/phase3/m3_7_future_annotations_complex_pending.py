# ryupy: pending
# 复杂注解表达式的完整PEP563字符串化仍待后续，不执行调用来伪装支持。
from __future__ import annotations

def function(value: unknown()) -> [item for item in Unknown]:
    return value
print(function.__annotations__)
