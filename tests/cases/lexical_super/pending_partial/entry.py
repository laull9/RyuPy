# ryupy: pending
# 该边界是 RyuPy 能力不足，不是 CP 无 frame 错误。
import subject
obj = subject.Child()
assert obj.identify() is obj
print('pending native partial forwarding')
