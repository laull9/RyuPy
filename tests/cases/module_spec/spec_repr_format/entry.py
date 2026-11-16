# ModuleSpec 完整公开源差分已通过。
from importlib.machinery import ModuleSpec

trace = []

class Locations:
    def __str__(self):
        trace.append('locations-str')
        return 'wrong-str'

    def __format__(self, spec):
        trace.append('locations-format:' + spec)
        return 'formatted-locations'

s = ModuleSpec('example', None)
s.submodule_search_locations = Locations()
result = repr(s)
assert result == "ModuleSpec(name='example', loader=None, submodule_search_locations=formatted-locations)"
assert trace == ['locations-format:']
print('locations_result', result)
print('locations_trace', trace)

class DisplayName:
    def __str__(self):
        trace.append('name-str')
        return 'WrongName'

    def __format__(self, spec):
        trace.append('name-format:' + spec)
        return 'FormattedName'

class ClassView:
    __name__ = DisplayName()

# 普通对象显式调用公开方法，避免把格式协议核查混入ModuleSpec子类基类支持。
class SpecView:
    name = 'view'
    loader = None
    origin = None
    submodule_search_locations = None

    @property
    def __class__(self):
        return ClassView()

trace.clear()
result = ModuleSpec.__repr__(SpecView())
assert result == "FormattedName(name='view', loader=None)"
assert trace == ['name-format:']
print('name_result', result)
print('name_trace', trace)
print('complete', 'spec_repr_format')
