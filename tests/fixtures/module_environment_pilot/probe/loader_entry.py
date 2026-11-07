# 同一subject原源的公开loader合同：双实例、旧函数、重执行与目标owner。
import sys
import importlib.util as util
import subject
spec = util.find_spec('subject')
left = util.module_from_spec(spec)
right = util.module_from_spec(spec)
assert left is not right and left is not subject
assert left.__dict__ is not right.__dict__
assert 'value' not in left.__dict__ and 'value' not in right.__dict__
assert sys.modules['subject'] is subject
assert spec.loader.exec_module(left) is None
assert spec.loader.exec_module(right) is None
assert left.read.__globals__ is left.__dict__
assert right.read.__globals__ is right.__dict__
assert subject.read.__globals__ is subject.__dict__
left.value = 31
right.value = 72
old = left.read
old.__module__ = 'unrelated'
assert old() == 31 and right.read() == 72 and subject.read() == 10
left.extra = ['keep']
extra = left.extra
alias = left.__dict__
generator = left.stream()
assert next(generator) == 31
assert spec.loader.exec_module(left) is None
assert left.__dict__ is alias and left.extra is extra
assert left.read is not old and old.__globals__ is alias
assert old() == 10 and next(generator) == 10
assert right.read() == 72 and subject.read() == 10
# 将另一目标函数挂到左实例不会改变其定义环境。
left.foreign = right.read
assert left.foreign.__globals__ is right.__dict__
assert left.foreign() == 72
left.value = None
assert old() is None and left.read() is None and right.read() == 72
assert sys.modules['subject'] is subject
assert left.__spec__ is spec and right.__spec__ is spec
print('loader same source distinct targets preserved')
print('old function reexecution retained dictionary')
print('uncached clones and foreign owner preserved')
