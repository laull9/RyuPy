# 空 cell 下 shadow 成功，类完成后同一函数可构造真实代理。
import subject
import helpers
assert helpers.observations == ['empty', 'no arguments']
obj = subject.Child()
proxy = obj.choose(super)
assert type(proxy) is super
assert proxy.identify() is obj
assert obj.choose(lambda: 'after') == 'after'
print('empty cell is consumed lazily after canonical identity check')
