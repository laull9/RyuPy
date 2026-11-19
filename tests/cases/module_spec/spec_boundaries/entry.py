# ryupy: pending
import sys
import __main__
import importlib.machinery
import ms_plain
from importlib.machinery import ModuleSpec

def check(label, value):
    assert value, label
    print(label, value)

def error(label, action, expected):
    try:
        action()
    except Exception as exc:
        check(label, type(exc) is expected)
    else:
        raise AssertionError(label)

s = ModuleSpec('p.m', None, origin='source.py', is_package=True)
print('repr', repr(s))
t = ModuleSpec('p.m', None, origin='source.py', is_package=True, loader_state=[])
check('equal_ignores_state', s == t)
t.has_location = True
check('equal_location', s != t)
check('equal_foreign', (s == object()) is False)
error('unhashable', lambda: hash(s), TypeError)
s.extra = []
check('extra_attribute', type(s.extra) is list)
del s.extra
error('deleted_extra', lambda: s.extra, AttributeError)
error('parent_readonly', lambda: setattr(s, 'parent', 'x'), AttributeError)
error('parent_no_delete', lambda: delattr(s, 'parent'), AttributeError)
error('cached_no_delete', lambda: delattr(s, 'cached'), AttributeError)
error('location_no_delete', lambda: delattr(s, 'has_location'), AttributeError)
del s.name
error('deleted_name', lambda: s.name, AttributeError)
error('deleted_name_parent', lambda: s.parent, AttributeError)
s.name = 'restored'
check('restored_name', s.parent == 'restored')
# 源loader反射只记录真实CP身份；已编译部署不得伪造该类型。
loader = ms_plain.__loader__
print('source_loader_type', type(loader).__module__, type(loader).__name__)
check('source_loader_identity', type(loader) is importlib.machinery.SourceFileLoader)
print('source_loader_methods', hasattr(loader, 'create_module'), hasattr(loader, 'exec_module'), hasattr(loader, 'get_source'))
# 内建和脚本入口是单列边界，不能套普通源元数据合同。
print('builtin_spec', type(sys.__spec__).__name__, sys.__spec__.origin, sys.__spec__.has_location, sys.__spec__.cached)
check('builtin_loader', sys.__loader__ is sys.__spec__.loader)
print('builtin_file', hasattr(sys, '__file__'), hasattr(sys, '__cached__'))
print('main_metadata', __main__.__spec__, __main__.__package__, __main__.__cached__)
print('complete', 'spec_boundaries')
