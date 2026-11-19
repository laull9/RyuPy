# ModuleSpec 完整公开源差分已通过。
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

s = ModuleSpec('p.m', None)
check('repr_defaults', repr(s) == "ModuleSpec(name='p.m', loader=None)")
s.origin = 'source.py'
s.submodule_search_locations = []
check('repr_source_package', repr(s) == "ModuleSpec(name='p.m', loader=None, origin='source.py', submodule_search_locations=[])")
class Locations:
    def __str__(self):
        return 'locations-str'
    def __repr__(self):
        return 'locations-repr'
s.submodule_search_locations = Locations()
check('repr_location_str', repr(s).endswith('submodule_search_locations=locations-str)'))
s.submodule_search_locations = []
t = ModuleSpec('p.m', None, origin='source.py', is_package=True)
t.loader_state = ['ignored']
check('equivalent', s == t and not (s != t))
check('not_same', s is not t)
t.name = 'other'
check('name_difference', s != t)
t.name = 'p.m'
t.loader = object()
check('loader_difference', s != t)
t.loader = None
t.origin = 'other.py'
check('origin_difference', s != t)
t.origin = 'source.py'
t.submodule_search_locations = ['other']
check('locations_difference', s != t)
t.submodule_search_locations = []
t.cached = 'other.pyc'
check('cached_difference', s != t)
t.cached = None
t.has_location = True
check('location_difference', s != t)
check('foreign_notimplemented', s.__eq__(object()) is NotImplemented)
check('foreign_operator', (s == object()) is False)
error('unhashable', lambda: hash(s), TypeError)
trace = []
class Other:
    @property
    def name(self):
        trace.append('name')
        return 'different'
    @property
    def loader(self):
        raise ValueError('must not read')
check('short_circuit', s.__eq__(Other()) is False and trace == ['name'])
class Missing:
    @property
    def name(self):
        raise AttributeError('from getter')
check('attribute_error_to_notimplemented', s.__eq__(Missing()) is NotImplemented)
u = ModuleSpec('p.m', None, origin='source.py', is_package=True)
del u.name
check('self_missing_inside_try', u.__eq__(s) is NotImplemented)
del u.submodule_search_locations
error('self_missing_outside_try', lambda: u.__eq__(s), AttributeError)
s.extra = []
check('extra_identity', s.extra is s.extra)
del s.extra
error('extra_deleted', lambda: s.extra, AttributeError)
error('parent_readonly', lambda: setattr(s, 'parent', 'bad'), AttributeError)
error('cached_no_delete', lambda: delattr(s, 'cached'), AttributeError)
print('complete', 'spec_object_protocol')
