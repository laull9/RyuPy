# 执行来源属于loader，而不属于目标spec或创建时的代码。
import sys
import importlib.util as util
from importlib.machinery import ModuleSpec
import source_a
import source_b
loader = source_a.__loader__
empty_spec = ModuleSpec('source_a', None)
empty = util.module_from_spec(empty_spec)
assert '__builtins__' not in empty.__dict__
assert loader.exec_module(empty) is None
assert empty.read() == 41 and empty.read.__module__ == 'source_a'
assert empty.__loader__ is None and empty.__spec__ is empty_spec
other_spec = ModuleSpec('source_a', source_b.__loader__)
other = util.module_from_spec(other_spec)
loader.exec_module(other)
assert other.read() == 41 and 'from_b' not in other.__dict__
assert other.__loader__ is source_b.__loader__ and other.__spec__ is other_spec
empty.value = 91
assert empty.read() == 91 and other.read() == 41 and source_a.read() == 41
assert sys.modules['source_a'] is source_a and sys.modules['source_b'] is source_b
assert source_b.from_b is True
print('空loader目标及异源目标均执行来源代码，函数和缓存独立')
