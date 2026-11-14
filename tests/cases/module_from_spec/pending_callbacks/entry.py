# 固定CP参考：公开协议的真实属性顺序，非生产支持声明。
from importlib.util import module_from_spec
from importlib.machinery import ModuleSpec
log = []
class Loader:
    def __getattribute__(self, name):
        log.append('loader.' + name)
        return object.__getattribute__(self, name)
    def create_module(self, spec):
        log.append('create')
        return None
    def exec_module(self, module):
        log.append('exec')
class Spec:
    loader = Loader()
    name = 'trace'
    parent = ''
    submodule_search_locations = None
    has_location = True
    origin = 'trace.py'
    cached = 'trace.pyc'
    def __getattribute__(self, name):
        log.append('spec.' + name)
        return object.__getattribute__(self, name)
m = module_from_spec(Spec())
print('默认创建读取序', log)
assert log == ['spec.loader', 'loader.create_module', 'spec.loader', 'loader.create_module', 'create', 'spec.name', 'spec.loader', 'spec.parent', 'spec.submodule_search_locations', 'spec.has_location', 'spec.origin', 'spec.cached', 'spec.cached']
assert '__builtins__' not in m.__dict__
class Result:
    __name__ = 'keep'
    __loader__ = 'keep'
    __package__ = 'keep'
    __path__ = []
    __file__ = 'keep'
    __cached__ = 'keep'
r = Result()
class Custom:
    def create_module(self, spec):
        return r
s = ModuleSpec('other', Custom(), origin='new')
s.has_location = True
assert module_from_spec(s) is r and r.__spec__ is s
assert r.__name__ == r.__loader__ == r.__package__ == r.__file__ == r.__cached__ == 'keep'
print('自定义对象非空字段保留且spec总覆盖', True)
class ReturnsInt:
    def create_module(self, spec):
        return 7
assert module_from_spec(ModuleSpec('n', ReturnsInt())) == 7
print('create返回不要求ModuleType', True)
