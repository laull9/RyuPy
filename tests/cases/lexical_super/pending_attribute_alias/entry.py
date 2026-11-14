# ryupy: pending
# 保留实际属性调用，不能先取临时局部改成支持形状。
import subject
obj = subject.Child()
assert obj.identify() is obj
print('pending attribute alias call')
