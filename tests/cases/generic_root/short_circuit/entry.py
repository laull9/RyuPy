# 前项成功必须短路，后项未知关系或非法alias不得抢先。
import typing as t
T = t.TypeVar('T')
A = t.Generic[T]
for cls in (t.IO, t.BinaryIO, t.TextIO):
    value = cls()
    assert isinstance(value, (cls, t.Generic))
    assert issubclass(cls, (cls, t.Generic))
    assert isinstance(value, (cls, A))
    assert issubclass(cls, (cls, A))
assert isinstance(1, (int, A)) and issubclass(int, (int, A))
assert not isinstance(1, t.Generic) and not issubclass(int, t.Generic)
print('short-circuit', True)
