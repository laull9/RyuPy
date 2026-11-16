import importlib.util as util
import pkg.subject
import other.a
import other.b
import other.plain
import cycle.subject
assert cycle.subject.subject is cycle.subject
assert cycle.subject.read() == 3
spec = util.find_spec('pkg.subject')
left = util.module_from_spec(spec)
right = util.module_from_spec(spec)
spec.loader.exec_module(left)
right.__package__ = 'other'
right.__spec__ = None
spec.loader.exec_module(right)
assert left.__dict__ is not right.__dict__
assert left.value is pkg.subject.value
assert right.value is other.plain.value
assert left.read()[0] == 'changed'
assert right.read()[0] is other.a
old = right.read
other.plain.value['x'] = 31
assert old()[1] == 31
right.__package__ = 'pkg'
spec.loader.exec_module(right)
assert right.read is not old
assert old()[0] == 'changed'
assert old()[1] == 7
print('metadata target and reexecution preserved')
print('cycle partial object preserved')
