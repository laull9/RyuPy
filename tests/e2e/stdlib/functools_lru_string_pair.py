# ryupy: pending
# 两精确字符串的缓存键与真实同hash用户键比较，不能按wcwidth函数名特化。
from functools import lru_cache

events = []


class Key:
    def __init__(self, value):
        self.value = value

    def __hash__(self):
        return hash(self.value)

    def __eq__(self, other):
        events.append((self.value, other))
        return self.value == other


calls = []


@lru_cache(maxsize=2)
def f(a, b):
    calls.append(1)
    return [len(calls)]


left = Key("left")
right = Key("right")
assert hash((left, right)) == hash(("left", "right"))
first = f(left, right)
assert f("left", "right") is first
assert events == [("left", "left"), ("right", "right")]
assert len(calls) == 1
f.cache_clear()
second = f("left", "right")
assert f("".join(["le", "ft"]), "".join(["ri", "ght"])) is second
f("right", "left")
assert f("left", "right") is second
f("third", "key")
f("right", "left")
assert len(calls) == 5
print("lru-string-pair-ok")
