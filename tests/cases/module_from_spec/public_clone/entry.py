# 公开loader双实例与重执行完整正例候选；固定CP与真实PE差分回归。
import sys
import importlib.util as util
import subject as original
spec = util.find_spec('subject')
make = util.module_from_spec
assert make is util.module_from_spec
clone = make(spec=spec)
other = [make][0](spec)
assert clone is not original and clone is not other
assert clone.__dict__ is not other.__dict__
assert clone.__spec__ is spec and clone.__loader__ is spec.loader
assert '__builtins__' not in clone.__dict__ and 'value' not in clone.__dict__
assert sys.modules['subject'] is original
assert spec.loader.create_module(spec) is None
assert spec.loader.exec_module(clone) is None
spec.loader.exec_module(other)
assert clone.read.__globals__ is clone.__dict__
assert clone.Reader.read.__globals__ is clone.__dict__
clone.write(31)
f, g = clone.closure(), clone.stream()
assert next(g) == 31
clone.value = 32
assert next(g) == f() == clone.Reader().read() == 32
assert original.read() == other.read() == 10
clone.items.append('clone')
assert original.items == other.items == []
old = clone.read
clone.extra = 'keep'
old_dict = clone.__dict__
spec.loader.exec_module(clone)
assert clone.extra == 'keep' and old() == 10
assert clone.read is not old and clone.__dict__ is old_dict
assert sys.modules['subject'] is original
print('公开创建、双实例、定义归属、重执行与缓存隔离', True)
