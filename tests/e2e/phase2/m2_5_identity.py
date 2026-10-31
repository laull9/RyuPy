# ryupy: pending
# M2.5：普通对象和 frozen dataclass 跨动态调用仍共享身份。
from dataclasses import dataclass


@dataclass(frozen=True)
class Point:
    x: int


def identity(value):
    return value


point = Point(3)
alias = point
print(point is alias, identity(point) is point, [point][0] is point)
print(id(point) == id(identity(point)))
other = Point(3)
print(point is other)
items = []
alias = identity(items)
alias.append(1)
print(items, alias is items)
