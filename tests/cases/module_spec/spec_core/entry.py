# ModuleSpec 完整公开源差分已通过。
import importlib.machinery
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

check('export_identity', ModuleSpec is importlib.machinery.ModuleSpec)
s = ModuleSpec('one.two', None)
check('real_type', type(s) is ModuleSpec and isinstance(s, ModuleSpec))
print('type_metadata', ModuleSpec.__name__, ModuleSpec.__module__)
check('fresh', s is not ModuleSpec('one.two', None))
check('defaults', s.loader is None and s.origin is None and s.loader_state is None and s.submodule_search_locations is None and s.cached is None and s.has_location is False)
name, loader, origin, state = [], [], [], []
r = ModuleSpec(name=name, loader=loader, origin=origin, loader_state=state)
check('references', r.name is name and r.loader is loader and r.origin is origin and r.loader_state is state)
check('keyword_bind', ModuleSpec(loader=None, name='a').name == 'a')
error('missing_both', lambda: ModuleSpec(), TypeError)
error('missing_loader', lambda: ModuleSpec('a'), TypeError)
error('extra_position', lambda: ModuleSpec('a', None, None), TypeError)
error('duplicate_name', lambda: ModuleSpec('a', None, name='b'), TypeError)
error('duplicate_loader', lambda: ModuleSpec('a', None, loader=None), TypeError)
error('unknown_keyword', lambda: ModuleSpec('a', None, surprise=True), TypeError)
trace = []
class Truth:
    def __bool__(self):
        trace.append('bool')
        return True
class BadTruth:
    def __bool__(self):
        trace.append('raise')
        raise ValueError('truth failure')
error('binding_before_truth', lambda: ModuleSpec('a', None, None, is_package=Truth()), TypeError)
check('binding_no_hook', trace == [])
p = ModuleSpec('one.two', None, is_package=Truth())
check('truth_once', trace == ['bool'])
error('constructor_truth_error', lambda: ModuleSpec('a', None, is_package=BadTruth()), ValueError)
check('constructor_truth_phase', trace == ['bool', 'raise'])
q = ModuleSpec('one.two', None, is_package=True)
check('package_lists', type(p.submodule_search_locations) is list and p.submodule_search_locations == [] and p.submodule_search_locations is not q.submodule_search_locations)
check('falsy_package', ModuleSpec('a', None, is_package=[]).submodule_search_locations is None)
check('parent_module', s.parent == 'one')
check('parent_package', p.parent == 'one.two')
s.name = 'new.child'
check('parent_current_name', s.parent == 'new')
locations = []
s.submodule_search_locations = locations
check('locations_alias', s.submodule_search_locations is locations and s.parent == 'new.child')
s.submodule_search_locations = False
check('parent_not_truth', s.parent == 'new.child')
s.submodule_search_locations = None
check('parent_none', s.parent == 'new')
error('name_lazy_parent', lambda: r.parent, AttributeError)
s.has_location = Truth()
check('location_bool', s.has_location is True and trace == ['bool', 'raise', 'bool'])
error('location_truth_error', lambda: setattr(s, 'has_location', BadTruth()), ValueError)
check('location_unchanged_on_error', s.has_location is True)
s.has_location = []
check('location_false', s.has_location is False)
c = ModuleSpec('cache', None, origin='/not-created/source.py')
check('cache_disabled', c.cached is None)
c.has_location = True
print('cache_first', c.cached)
check('cache_path', c.cached == '/not-created/__pycache__/source.cpython-312.pyc')
c.origin = '/not-created/changed.py'
check('cache_sticky', c.cached == '/not-created/__pycache__/source.cpython-312.pyc')
c.cached = None
check('cache_clear_recompute', c.cached == '/not-created/__pycache__/changed.cpython-312.pyc')
marker = []
c.cached = marker
check('cache_write_identity', c.cached is marker)
c.has_location = False
check('cache_explicit_without_location', c.cached is marker)
c.cached = None
check('cache_clear_disabled', c.cached is None)
c.origin = '/not-created/ready.pyc'
c.has_location = True
check('cache_bytecode_identity', c.cached is c.origin)
c.cached = None
c.origin = '/not-created/no-suffix'
check('cache_unknown_suffix', c.cached is None)
c.origin = '/not-created/later.py'
check('cache_none_not_memoized', c.cached == '/not-created/__pycache__/later.cpython-312.pyc')
c.cached = None
c.origin = []
error('cache_origin_lazy_error', lambda: c.cached, AttributeError)
print('complete', 'spec_core')
