# ryupy: pending
# CP完整正例：wrapper元数据为构造时复制，unwrap则取实际__func__；未知属性与字典反射不伪装。
from typing import overload, get_overloads, clear_overloads

clear_overloads()
def original(value: int = 4):
    """原说明。"""
    return value

cm = classmethod(original)
sm = staticmethod(original)
print(cm.__func__ is original, sm.__func__ is original)
print(cm.__name__, cm.__qualname__, cm.__module__, cm.__doc__)
print(sm.__annotations__ is original.__annotations__, cm.__annotations__ is original.__annotations__)
original.__qualname__ = "changed"
original.__module__ = "changed.module"
original.__name__ = "renamed"
print(cm.__qualname__, sm.__qualname__, original.__qualname__)
print(overload(cm) is overload(sm))
print(len(get_overloads(original)), get_overloads(original)[0] is sm)
cm.__dict__["__func__"] = "spoof"
print(cm.__func__ is original, sm(8))
print(clear_overloads())
