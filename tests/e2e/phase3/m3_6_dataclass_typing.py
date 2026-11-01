# M3.6：dataclass 默认工厂、生成方法、冻结与注解元数据。
from dataclasses import FrozenInstanceError, dataclass, field
from typing import Optional


@dataclass
class Record:
    name: str
    values: list = field(default_factory=list)


@dataclass(frozen=True)
class Frozen:
    value: int


def annotated(value: Optional[int]) -> Optional[int]:
    return value


first = Record("a")
second = Record("a")
print(first == second, first)
first.values.append(1)
print(first.values, second.values, first == second)
value = Frozen(3)
try:
    value.value = 4
except FrozenInstanceError:
    print("frozen")
print(annotated.__annotations__["value"] == Optional[int])
