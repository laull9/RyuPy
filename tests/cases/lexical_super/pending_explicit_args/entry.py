# ryupy: pending
# 完整保留 CP 一参和二参语义，不将 NI 当作 CP 预期。
import subject
obj = subject.Child()
assert obj.two() is obj
assert obj.one().__thisclass__ is subject.Child
assert obj.one().__self__ is None
print('pending explicit super construction')
