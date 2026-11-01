# 非简单注解仍建立空命名空间字典，但不把名字写入注解映射。
from __future__ import annotations
print(__annotations__)
missing = None
if False:
    missing.attribute: Missing
(number): Unknown = 1
print(__annotations__, number)
class Parenthesized:
    print(__annotations__)
    (value): Unknown = 3
print(Parenthesized.__annotations__, Parenthesized.value)
def function(value: (1).real):
    pass
print(function.__annotations__)
