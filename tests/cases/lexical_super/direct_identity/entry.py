# 实际构造并消费代理，不能用字符串标签替代类型身份。
import subject
obj = subject.Leaf()
assert obj.identify() is obj
assert subject.saved(obj) is obj
assert obj.label() == 'child:base'
proxy = obj.proxy()
assert type(subject.canonical) is type
assert type(proxy) is subject.canonical
assert isinstance(proxy, subject.canonical)
assert isinstance(proxy, object)
assert not isinstance(obj, subject.canonical)
assert subject.canonical is super
assert subject.canonical.__name__ == 'super'
assert subject.canonical.__module__ == 'builtins'
assert proxy.identify() is obj
print('direct, unbound, inherited and canonical identity')
