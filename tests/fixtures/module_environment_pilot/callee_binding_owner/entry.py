# 既有能力域完整合同；公开__defaults__反射另保原完整失败源，不在此扩张。
import subject
assert subject.events == ['decorate']
assert subject.invoke() == 'payload'
assert subject.events == ['decorate', 'argument', 'original']
assert subject.invoke() == 'payload'
assert subject.events == ['decorate', 'argument', 'original', 'argument', 'replacement']
print('callee captured before argument exactly once')
saved = subject.defaulted
seed = subject.seed
assert saved() is seed
seed.append('in-place')
subject.seed = ['new binding']
assert saved() is seed and saved() == ['in-place']
assert subject.defaulted is saved
print('default call retains original object')
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
