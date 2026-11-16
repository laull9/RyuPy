"""CP 跨域类继承：编译类直接继承 CPython 提供的 Python 与 C 扩展基类。"""
import io
from collections import UserDict, deque


class TrackedDict(UserDict):
    def __init__(self, initial=None):
        self.writes = []
        super().__init__(initial)

    def __setitem__(self, key, value):
        self.writes.append(key)
        super().__setitem__(key, value)


class RingDeque(deque):
    def push_pair(self, value):
        super().append(value)
        super().append(value)


class LabelMixin:
    def label(self):
        return f"label:{self.tag}"


class MixedDict(LabelMixin, UserDict):
    def __init__(self, tag, **kwargs):
        self.tag = tag
        super().__init__(**kwargs)

    @classmethod
    def make(cls, tag):
        return cls(tag, a=1)

    @staticmethod
    def helper(x):
        return x * 2

    @property
    def summary(self):
        return f"{self.tag}:{len(self)}"


class LeafMixedDict(MixedDict):
    def label(self):
        return super().label() + "!"


class GuardedIO(io.StringIO):
    def write_line(self, line):
        return super().write(line + "\n")


d = TrackedDict({"x": 1})
d["y"] = 2
assert isinstance(d, TrackedDict)
assert isinstance(d, UserDict)
assert issubclass(TrackedDict, UserDict)
assert type(d) is TrackedDict
assert d["x"] == 1 and d["y"] == 2
assert "y" in d
assert d.writes == ["x", "y"]
d.custom_attr = 99
assert d.custom_attr == 99
del d.custom_attr
assert "summary" not in dir(d)

q = RingDeque([1])
q.push_pair(5)
assert list(q) == [1, 5, 5]
assert isinstance(q, deque)

m = LeafMixedDict.make("san")
m["b"] = 2
assert m.label() == "label:san!"
assert m.summary == "san:2"
assert MixedDict.helper(7) == 14
assert isinstance(m, LabelMixin) and isinstance(m, UserDict)

buf = GuardedIO()
buf.write_line("hello")
assert buf.getvalue() == "hello\n"

print("CP 跨域类继承通过")
