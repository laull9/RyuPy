# ModuleSpec 完整公开源差分已通过。
import sys
from importlib.machinery import ModuleSpec
import ms_plain
import ms_pkg
import ms_pkg.left
import ms_state

def check(label, value):
    assert value, label
    print(label, value)

def metadata(label, module, package):
    spec = module.__spec__
    check(label + '_type', type(spec) is ModuleSpec)
    check(label + '_name', spec.name == module.__name__)
    check(label + '_loader', module.__loader__ is spec.loader and spec.loader is not None)
    check(label + '_package', module.__package__ == spec.parent)
    check(label + '_origin', module.__file__ is spec.origin and spec.has_location is True)
    check(label + '_cached', module.__cached__ is spec.cached and spec.cached.endswith('.pyc'))
    if package:
        check(label + '_path', module.__path__ is spec.submodule_search_locations and type(module.__path__) is list)
    else:
        check(label + '_no_path', spec.submodule_search_locations is None and not hasattr(module, '__path__'))

metadata('plain', ms_plain, False)
metadata('package', ms_pkg, True)
metadata('child', ms_pkg.left, False)
check('initialization_visible', ms_plain.ready and ms_pkg.ready)
check('parent_attachment', ms_pkg.left is sys.modules['ms_pkg.left'])
check('cycle_identity', ms_pkg.left.right.left is ms_pkg.left and ms_pkg.left.right.left_spec is ms_pkg.left.__spec__ and ms_pkg.left.right.left_loader is ms_pkg.left.__loader__)
check('cycle_partial', ms_pkg.left.right.saw_incomplete)
import ms_plain as repeated
check('repeat_import', repeated is ms_plain and repeated.__spec__ is ms_plain.__spec__)
spec = ms_plain.__spec__
old_name, old_file, old_cached, old_loader, old_package = ms_plain.__name__, ms_plain.__file__, ms_plain.__cached__, ms_plain.__loader__, ms_plain.__package__
spec.name = 'changed.name'
spec.origin = '/changed.py'
spec.cached = 'custom.pyc'
spec.loader = None
check('spec_not_sync_module', ms_plain.__name__ == old_name and ms_plain.__file__ == old_file and ms_plain.__cached__ == old_cached and ms_plain.__loader__ is old_loader and ms_plain.__package__ == old_package)
ms_plain.__file__ = '/module-only.py'
ms_plain.__cached__ = 'module-only.pyc'
ms_plain.__package__ = 'module-only'
ms_plain.__loader__ = []
check('module_not_sync_spec', spec.origin == '/changed.py' and spec.cached == 'custom.pyc' and spec.parent == 'changed' and spec.loader is None)
old_path = ms_pkg.__path__
old_path.append('metadata-only-path')
check('path_mutation_alias', ms_pkg.__spec__.submodule_search_locations[-1] == 'metadata-only-path')
ms_pkg.__path__ = []
check('path_rebind_independent', ms_pkg.__spec__.submodule_search_locations is old_path and ms_pkg.__path__ is not old_path)
try:
    import ms_retry
except Exception as exc:
    check('retry_first_exception', type(exc) is ValueError and str(exc) == 'first import failure')
else:
    raise AssertionError('first import must fail')
check('failed_removed', 'ms_retry' not in sys.modules and ms_state.attempts == 1)
import ms_retry
check('retry_success', ms_retry.ready and ms_state.attempts == 2)
check('retry_fresh_spec', ms_retry.__spec__ is not ms_state.failed_specs[0])
metadata('retry', ms_retry, False)
print('complete', 'module_metadata')
