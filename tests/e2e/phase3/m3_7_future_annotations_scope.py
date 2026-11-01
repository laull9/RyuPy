# 模块注解字典预创建、活动重绑定及非简单目标只执行赋值/目标组成部分。
from __future__ import annotations

print(__annotations__)
if False:
    hidden: Missing
log = []
class Mapping:
    def __setitem__(self, key, value):
        log.append((key, value))
__annotations__ = Mapping()
first: list[Unknown] = 1
second: Unknown
print(log)
class Target:
    pass
obj = Target()
def target():
    log.append("target")
    return obj
def value():
    log.append("value")
    return 7
target().field: Unknown = value()
target().unused: unknown()
items = [0]
def base():
    log.append("base")
    return items
def index():
    log.append("index")
    return 0
base()[index()]: Unknown = value()
base()[index()]: unknown()
print(obj.field, items, log)
def identity(value: int) -> str:
    return value
print(identity([1, 2]))
print(identity.__annotations__)
