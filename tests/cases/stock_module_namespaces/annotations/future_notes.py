# ryupy: pending
# 延迟注解沿既有规范字符串化路径；未知名字不求值，函数与模块元数据保持分域。
from __future__ import annotations

events = []
scalar: MissingName = 4
items: list[MissingName] = []

def identity(value: MissingName) -> list[MissingName]:
    return [value]

result = identity(5)
