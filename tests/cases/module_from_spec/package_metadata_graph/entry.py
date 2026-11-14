# 独立窄例：位置元数据和包路径身份，不测试异常计数器。
import sys
import importlib.util as util
import fixturepkg
spec = util.find_spec('fixturepkg.child')
paths = spec.submodule_search_locations
loader = spec.loader
assert not hasattr(fixturepkg, 'child') and 'fixturepkg.child' not in sys.modules
spec.has_location = False
unlocated = util.module_from_spec(spec)
assert unlocated.__spec__ is spec and unlocated.__loader__ is loader
assert unlocated.__path__ is paths and unlocated.__package__ == 'fixturepkg.child'
assert '__file__' not in unlocated.__dict__ and '__cached__' not in unlocated.__dict__
assert '__builtins__' not in unlocated.__dict__ and 'source_marker' not in unlocated.__dict__
spec.has_location = True
spec.origin = '公开位置.py'
spec.cached = '公开缓存.pyc'
located = util.module_from_spec(spec)
assert located.__spec__ is spec and located.__loader__ is loader
assert located.__path__ is paths and located.__package__ == 'fixturepkg.child'
assert located.__file__ == '公开位置.py' and located.__cached__ == '公开缓存.pyc'
assert located.__dict__ is not unlocated.__dict__
assert '__file__' not in unlocated.__dict__ and '__cached__' not in unlocated.__dict__
assert loader.exec_module(unlocated) is None
assert unlocated.source_marker == 'A' and unlocated.read() == 17
assert unlocated.read.__module__ == 'fixturepkg.child'
assert unlocated.__path__ is paths and unlocated.__spec__ is spec
assert '__file__' not in unlocated.__dict__ and '__cached__' not in unlocated.__dict__
assert 'source_marker' not in located.__dict__ and '__builtins__' not in located.__dict__
assert not hasattr(fixturepkg, 'child') and 'fixturepkg.child' not in sys.modules
assert fixturepkg.marker == 9
print('窄例：包路径身份及公开位置元数据正确')

# 真正的后续导入让子包自然进入编译图，并独立验证普通发布路径。
import fixturepkg.child
assert fixturepkg.child is sys.modules['fixturepkg.child']
assert fixturepkg.child is not unlocated and fixturepkg.child is not located
assert fixturepkg.child.source_marker == 'A' and fixturepkg.child.read() == 17
assert fixturepkg.child.read.__module__ == 'fixturepkg.child'
assert fixturepkg.child.__name__ == 'fixturepkg.child'
assert fixturepkg.child.__package__ == 'fixturepkg.child'
assert fixturepkg.child.__spec__ is not spec
assert fixturepkg.child.__dict__ is not unlocated.__dict__
assert fixturepkg.child.__dict__ is not located.__dict__
assert fixturepkg.marker == 9
# 普通导入不得反向初始化或重写先前创建的克隆。
assert 'source_marker' not in located.__dict__ and '__builtins__' not in located.__dict__
assert located.__file__ == '公开位置.py' and located.__cached__ == '公开缓存.pyc'
assert unlocated.__spec__ is spec and unlocated.__path__ is paths
assert '__file__' not in unlocated.__dict__ and '__cached__' not in unlocated.__dict__
