# ryupy: pending
# 保留两种 CP 查找结果；仅 __class__ 特别优先读取代理自身。
import subject
obj = subject.Child()
proxy = obj.proxy()
assert proxy.__thisclass__ is subject.Child
assert proxy.__self__ is obj
assert proxy.__self_class__ is subject.Child
assert proxy.__class__ is super
shadow = subject.ShadowChild()
shadow_proxy = shadow.proxy()
assert shadow_proxy.__thisclass__ == 'base start'
assert shadow_proxy.__self__ == 'base receiver'
assert shadow_proxy.__self_class__ == 'base receiver class'
assert shadow_proxy.__class__ is super
print('pending super fields and base attribute precedence')
