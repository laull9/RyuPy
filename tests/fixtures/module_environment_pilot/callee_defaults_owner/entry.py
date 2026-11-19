# 完整普通import入口；不靠显式实例flag表达语义。
import subject
assert subject.events == ['decorate']
assert subject.invoke() == 'payload'
assert subject.events == ['decorate', 'argument', 'original']
assert subject.invoke() == 'payload'
assert subject.events == ['decorate', 'argument', 'original', 'argument', 'replacement']
print('callee captured before argument exactly once')
saved = subject.defaulted
seed = subject.seed
assert saved.__defaults__[0] is seed and saved() is seed
seed.append('in-place')
subject.seed = ['new binding']
assert saved() is seed and saved() == ['in-place']
replacement_default = ['new default']
saved.__defaults__ = (replacement_default,)
assert saved() is replacement_default
assert subject.defaulted is saved
print('default object identity and reassignment preserved')
owner = subject.read
owner.__module__ = 'not_the_owner'
assert owner.__globals__ is subject.__dict__
subject.value = None
assert owner() is None
subject.value = ['heterogeneous']
assert owner() is subject.value
subject.write('written inside')
assert owner() == 'written inside' and subject.__dict__['value'] == 'written inside'
subject.__dict__['value'] = {'alias': True}
assert owner() is subject.value and owner() == {'alias': True}
assert owner.__module__ == 'not_the_owner'
print('None heterogeneous global writes and owner preserved')
