# 包克隆保留原始路径对象，并服从传入spec的位置元数据。
import sys
import importlib.util as util
import fixturepkg
spec = util.find_spec('fixturepkg.child')
assert not hasattr(fixturepkg, 'child')
assert 'fixturepkg.child' not in sys.modules
paths = spec.submodule_search_locations
spec.has_location = False
first = util.module_from_spec(spec)
assert first.__spec__ is spec and first.__loader__ is spec.loader
assert first.__path__ is paths and first.__package__ == 'fixturepkg.child'
assert '__file__' not in first.__dict__ and '__cached__' not in first.__dict__
assert '__builtins__' not in first.__dict__ and 'runs' not in first.__dict__
spec.has_location = True
spec.origin = '公开位置.py'
spec.cached = '公开缓存.pyc'
second = util.module_from_spec(spec)
assert second.__path__ is paths and second.__spec__ is spec
assert second.__file__ == '公开位置.py' and second.__cached__ == '公开缓存.pyc'
assert '__file__' not in first.__dict__
spec.loader.exec_module(first)
assert first.read() == 41 and first.read.__module__ == 'fixturepkg.child'
assert '__file__' not in first.__dict__ and first.__path__ is paths
assert not hasattr(fixturepkg, 'child') and 'fixturepkg.child' not in sys.modules
assert fixturepkg.marker == 9
print('包路径身份、位置开关、缓存及父属性保持正确')
