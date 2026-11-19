# namespace真实loader和包克隆资源保持CP期望。
import sys
import importlib.util as util
from importlib.machinery import ModuleSpec
from importlib.resources import files
spec = ModuleSpec('namespace_sample', None, is_package=True)
namespace = util.module_from_spec(spec)
assert spec.loader is namespace.__loader__ and spec.loader is not None
assert type(spec.loader).__name__ == 'NamespaceLoader'
assert namespace.__path__ is spec.submodule_search_locations
assert namespace.__file__ is None and 'namespace_sample' not in sys.modules
package_spec = util.find_spec('resourcepkg')
package = util.module_from_spec(package_spec)
assert files(package).joinpath('message.txt').read_text(encoding='utf-8') == '资源内容\n'
assert 'value' not in package.__dict__ and 'resourcepkg' not in sys.modules
print('namespace真实loader及未执行包克隆资源可用')
