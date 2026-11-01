# 延迟注解只记录规范字符串，不解析未定义名字或捕获外层变量。
from __future__ import annotations

value: Missing = 3
print(__annotations__)
def function(a: list[Unknown], b: A | (B | C), c: "Forward") -> Callable[[str], None]:
    return a
print(function.__annotations__)
print(function(7, 8, 9))
class Box:
    item: dict[str, Unknown] = 4
    missing: Unknown
    def method(self, item: Box) -> Box:
        return item
print(Box.__annotations__)
print(Box.method.__annotations__)
def outer():
    Unbound: int
    def inner(x: Unbound) -> "Unbound":
        return x
    local: not_defined()
    return inner
inner = outer()
print(inner.__annotations__, inner(12))
