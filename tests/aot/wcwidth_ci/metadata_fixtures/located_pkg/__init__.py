# 同进程真实kind2包：来源规格及资源资格不能因kind0修复而回退。
import located_pkg
from importlib import resources
assert __spec__ is not None
assert __spec__.has_location is True
assert __spec__.origin is __file__
assert __spec__.loader is __loader__
assert __loader__ is not None
assert __spec__.cached is __cached__
assert __cached__.endswith(".pyc")
assert __spec__.submodule_search_locations is __path__
assert len(__path__) == 1
resource_path = resources.files(located_pkg)
