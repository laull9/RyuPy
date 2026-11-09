import warnings
import importlib.util as util
from importlib.machinery import ModuleSpec
import source_a
import source_b

loader = source_a.__loader__
empty_spec = ModuleSpec('source_a', None)
empty = util.module_from_spec(empty_spec)
other_spec = ModuleSpec('source_a', source_b.__loader__)
other = util.module_from_spec(other_spec)
loader.exec_module(empty)
loader.exec_module(other)
assert empty.__dict__ is not other.__dict__
assert empty.source_marker == other.source_marker == 'A'
assert not hasattr(other, 'from_b')
assert other.__loader__ is source_b.__loader__ and other.__spec__ is other_spec
print('module-top', empty.Top.__module__, other.Top.__module__)

# 每action独立filter版本：同行重复、异行、同globals改名、同action重设。
for action in ('default', 'module', 'once'):
    empty.__name__ = 'source_a'
    other.__name__ = 'source_a'
    with warnings.catch_warnings(record=True) as records:
        warnings.simplefilter(action)
        print(action, 'first', empty.emit(1), empty.emit(1), other.emit(1), other.emit(1))
        print(action, 'second-line', empty.emit(2), other.emit(2))
        print(action, 'before-rename-count', len(records))
        empty.__name__ = 'renamed-target'
        empty.emit.__module__ = 'reflection-instance'
        print(action, 'renamed', empty.emit(1), empty.emit(2))
        print(action, 'after-rename-count', len(records))
        warnings.simplefilter(action)
        print(action, 'after-filter', empty.emit(1), other.emit(1))
        print(action, 'after-filter-count', len(records))
        for record in records:
            print(action, 'record', str(record.message), record.filename, record.lineno)

# 新旧函数和暂停生成器都保持实际A代码及同一目标字典；重执行不选B。
old = other.emit
other.stream.__module__ = 'reflection-generator'
with warnings.catch_warnings(record=True) as records:
    warnings.simplefilter('always')
    generator = other.stream()
    print('generator-first', next(generator))
    other.__name__ = 'generator-renamed'
    print('generator-second', next(generator))
    generator.close()
    other.__name__ = 'source_a'
    loader.exec_module(other)
    assert other.emit is not old
    other.__name__ = 'after-reexecute'
    print('old-and-new', old(1), other.emit(1))
    print('retained-globals', old.__globals__ is other.__dict__, other.emit.__globals__ is other.__dict__)
    for record in records:
        print('lifecycle-record', str(record.message), record.filename, record.lineno)
print('done')
