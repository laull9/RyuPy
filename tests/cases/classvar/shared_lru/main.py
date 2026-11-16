# ryupy: pending
# 只用公开 API 与新 TypeVar 排空历史项，验证真实共享 128 项原始键 LRU。
import typing as t
CV = t.ClassVar

def fresh():
    return t.TypeVar('K')

def fill(count):
    for i in range(count):
        t.Union[fresh(), int]

fill(128)
hot_key = fresh()
victim_key = fresh()
hot = CV[hot_key]
victim = CV[victim_key]
fill(126)
assert CV[hot_key] is hot
t.Union[fresh(), int]
assert CV[hot_key] is hot
assert CV[victim_key] is not victim
print('classvar-union-lru', True)

fill(128)
key = fresh()
old_union = t.Union[key, int]
for i in range(128):
    CV[fresh()]
assert t.Union[key, int] is not old_union
print('classvar-evicts-union', True)

fill(128)
key = fresh()
old = CV[key]
fill(126)
t.Optional[fresh()]
assert CV[key] is not old
print('optional-two-entries-evict-classvar', True)

fill(128)
key = fresh()
old = t.Optional[key]
for i in range(126):
    CV[fresh()]
assert t.Optional[key] is old
CV[fresh()]
assert t.Optional[key] is old
assert t.Union[key, type(None)] is not old
print('optional-hot-does-not-touch-inner', True)

fill(128)
old_none = CV[None]
old_type = CV[type(None)]
fill(126)
assert CV[type(None)] is old_type
t.Union[fresh(), int]
assert CV[None] is not old_none
assert CV[type(None)] is old_type
print('none-raw-keys-two-entries', True)

fill(128)
key = fresh()
old = CV[key]
for i in range(150):
    t.Literal[i]
assert CV[key] is old
print('literal-separate-pool', True)
