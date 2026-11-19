# CP合法真实祖先关系，G1须明确NI，不能伪造False；保留完整后代检查。
import typing as t
for cls in (t.IO, t.BinaryIO, t.TextIO):
    assert issubclass(cls, t.Generic)
    assert isinstance(cls(), t.Generic)
class Child(t.IO):
    pass
assert issubclass(Child, t.Generic) and isinstance(Child(), t.Generic)
print('io-relation', True)
