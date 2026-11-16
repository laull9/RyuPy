# ryupy: pending
# str子类取原始载体，不调用覆写；二进制与其它对象不得隐式转str。
import textwrap

calls = []
class Raw(str):
    def __str__(self):
        calls.append("str")
        raise AssertionError("unexpected str")
    def strip(self, *args):
        calls.append("strip")
        raise AssertionError("unexpected strip")
    def splitlines(self, *args):
        calls.append("splitlines")
        raise AssertionError("unexpected splitlines")
    def expandtabs(self, *args):
        calls.append("expandtabs")
        raise AssertionError("unexpected expandtabs")

for text, expected in [("", ""), ("plain", "plain"), ("  x\n    y", "x\n  y"), (" \t\n", "\n")]:
    value = Raw(text)
    result = textwrap.dedent(value)
    assert type(result) is str
    assert result is not value
    assert result == expected
    print("raw", repr(result))
class Stringable:
    def __str__(self):
        calls.append("object")
        raise AssertionError("unexpected object conversion")

for value in [Stringable(), None, 3, b"x", bytearray(b"x"), memoryview(b"x")]:
    try:
        textwrap.dedent(value)
        raise AssertionError("missing TypeError")
    except TypeError:
        print("type-error", type(value).__name__)
assert calls == []
print("RAW_OK", len(calls))
