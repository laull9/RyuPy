# 不依赖私有清空钩子：新TypeVar填满缓存后精确观察边界。
import typing as t
def fresh():
    return t.TypeVar('K')
def fill(count):
    for i in range(count):
        t.Union[fresh(), int]
fill(128)
key = fresh()
a = t.Union[key, int]
first_key = fresh()
first = t.Union[first_key, int]
fill(126)
assert t.Union[key, int] is a
last = fresh()
t.Union[last, int]
assert t.Union[key, int] is a
assert t.Union[first_key, int] is not first
print('lru-not-fifo', True)
# 外Optional命中不能触碰内Union；再插一项只淘汰内Union。
fill(128)
key = fresh()
a = t.Optional[key]
fill(126)
assert t.Optional[key] is a
t.Union[fresh(), int]
assert t.Optional[key] is a
assert t.Union[key, type(None)] is not a
print('optional-two-entries', True)
# Optional新调用插入两项，足以淘汰第127个旧项。
fill(128)
key = fresh()
a = t.Union[key, int]
fill(126)
t.Optional[fresh()]
assert t.Union[key, int] is not a
print('shared-pool', True)
fill(128)
key = fresh()
a = t.Union[key, int]
for i in range(150):
    t.Literal[i]
assert t.Union[key, int] is a
print('literal-independent', True)
