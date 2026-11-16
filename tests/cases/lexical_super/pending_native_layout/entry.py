# ryupy: pending
# 保留完整原生布局方法调用与结果。
import subject
obj = subject.Child()
obj.append('value')
assert list(obj) == ['value']
print('pending native list receiver layout')
