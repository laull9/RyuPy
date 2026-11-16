# 普通dataclass使用同一运行时类身份；函数构造值不得被错误准入typed伴生体。
from dataclasses import dataclass


@dataclass
class Box:
    x: int


def make_box(x: int) -> Box:
    return Box(x)
