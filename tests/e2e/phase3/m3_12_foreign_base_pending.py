# ryupy: pending
# 路线B：编译类继承外部类型（Python类与C扩展类），构造、super、属性、回调与协议桥接。
# 纯native模式不加载CPython宿主，本用例保留pending标记供CPython provider模式验收；基线由CPython执行验证。
import collections


class TrackedDict(collections.UserDict):
    tag = "tracked"

    def __init__(self, initial=None, prefix="k:"):
        self.prefix = prefix
        self.events = []
        super().__init__(initial)

    def __setitem__(self, key, value):
        self.events.append(("set", key, value))
        super().__setitem__(self.prefix + str(key), value)

    def dump_via(self, callback):
        return callback(self, len(self), self.tag)


d = TrackedDict({"a": 1}, prefix="p:")
d["b"] = 2
assert isinstance(d, TrackedDict)
assert isinstance(d, collections.UserDict)
assert issubclass(TrackedDict, collections.UserDict)
assert type(d) is TrackedDict
assert d["p:a"] == 1 and d["p:b"] == 2
assert len(d) == 2
assert d.dump_via(lambda obj, count, tag: (obj.prefix, count, tag, list(obj))) == (
    "p:",
    2,
    "tracked",
    ["p:a", "p:b"],
)


class TaggedDeque(collections.deque):
    def __init__(self, items, label):
        super().__init__(items)
        self.label = label

    def push(self, x):
        super().append(x)
        return len(self)


q = TaggedDeque([10, 20], "nums")
assert isinstance(q, collections.deque)
assert q.push(30) == 3
assert list(q) == [10, 20, 30] and q.label == "nums"
print("foreign base inheritance ok")
