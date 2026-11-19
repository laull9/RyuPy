# ryupy: pending
# 实际传入不相关对象，不能用 Child 实例代替探针。
import subject
obj = subject.Proxy()
assert subject.Child.identify(obj) is obj
print('pending proxy class fallback')
