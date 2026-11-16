# 独立窄例：固定正文区分执行来源与目标元数据。
import sys
import importlib.util as util
from importlib.machinery import ModuleSpec
import source_a
import source_b
loader = source_a.__loader__
empty_spec = ModuleSpec('source_a', None)
empty = util.module_from_spec(empty_spec)
assert '__builtins__' not in empty.__dict__ and 'source_marker' not in empty.__dict__
assert loader.exec_module(empty) is None
assert empty.source_marker == 'A' and empty.read() == 17
assert empty.read.__module__ == 'source_a'
assert empty.__spec__ is empty_spec and empty.__loader__ is None
other_spec = ModuleSpec('source_a', source_b.__loader__)
other = util.module_from_spec(other_spec)
assert '__builtins__' not in other.__dict__ and 'source_marker' not in other.__dict__
assert loader.exec_module(other) is None
assert other.source_marker == 'A' and 'from_b' not in other.__dict__
assert other.read() == 17 and other.read.__module__ == 'source_a'
assert other.__spec__ is other_spec and other.__loader__ is source_b.__loader__
assert other.__dict__ is not empty.__dict__
empty.value = 91
assert empty.read() == 91 and other.read() == 17 and source_a.read() == 17
assert sys.modules['source_a'] is source_a and sys.modules['source_b'] is source_b
assert source_a.source_marker == 'A' and source_b.source_marker == 'B' and source_b.from_b
print('窄例：空loader和异源目标都执行A代码且保留目标绑定')
