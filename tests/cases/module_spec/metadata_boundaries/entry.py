# ryupy: pending
import sys
import builtins
import mb_state
import mb_delete

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

try:
    import mb_retry
except Exception as exc:
    check('first_failure', type(exc) is ValueError and str(exc) == 'retained first module')
else:
    raise AssertionError('first import must fail')
old_module = mb_state.old_modules[0]
old_spec = mb_state.old_specs[0]
old_loader = mb_state.old_loaders[0]
check('failed_cache_removed', 'mb_retry' not in sys.modules)
check('old_metadata_after_failure', old_module.__spec__ is old_spec and old_module.__loader__ is old_loader)
check('old_body_partial', not hasattr(old_module, 'ready'))
import mb_retry
check('retry_executed', mb_state.attempts == 2 and mb_retry.ready)
check('new_module_identity', mb_retry is not old_module and sys.modules['mb_retry'] is mb_retry)
check('new_spec_identity', mb_retry.__spec__ is not old_spec)
check('new_loader_binding', mb_retry.__loader__ is mb_retry.__spec__.loader)
check('old_metadata_after_retry', old_module.__spec__ is old_spec and old_module.__loader__ is old_loader)
check('old_body_still_partial', not hasattr(old_module, 'ready'))
# 外部delattr只影响模块字典；裸读取会查内建名字，模块属性读取不会。
spec = mb_delete.__spec__
loader = mb_delete.__loader__
check('before_delete', mb_delete.read_spec() is spec and mb_delete.read_loader() is loader)
delattr(mb_delete, '__spec__')
check('external_delete_spec_absent', not hasattr(mb_delete, '__spec__'))
error('external_read_spec_error', lambda: mb_delete.__spec__, AttributeError)
check('external_delete_spec_fallback', mb_delete.read_spec() is builtins.__spec__)
error('external_delete_spec_twice', lambda: delattr(mb_delete, '__spec__'), AttributeError)
mb_delete.__spec__ = spec
check('restore_spec_visible', mb_delete.read_spec() is spec)
mb_delete.delete_spec_bare()
check('bare_delete_spec_absent', not hasattr(mb_delete, '__spec__'))
check('bare_delete_spec_fallback', mb_delete.read_spec() is builtins.__spec__)
error('bare_delete_spec_twice', mb_delete.delete_spec_bare, NameError)
mb_delete.__spec__ = None
check('none_spec_shadows_builtins', mb_delete.read_spec() is None)
mb_delete.__spec__ = spec
delattr(mb_delete, '__loader__')
check('external_delete_loader_absent', not hasattr(mb_delete, '__loader__'))
error('external_read_loader_error', lambda: mb_delete.__loader__, AttributeError)
check('external_delete_loader_fallback', mb_delete.read_loader() is builtins.__loader__)
error('external_delete_loader_twice', lambda: delattr(mb_delete, '__loader__'), AttributeError)
mb_delete.__loader__ = loader
check('restore_loader_visible', mb_delete.read_loader() is loader)
mb_delete.delete_loader_bare()
check('bare_delete_loader_absent', not hasattr(mb_delete, '__loader__'))
check('bare_delete_loader_fallback', mb_delete.read_loader() is builtins.__loader__)
error('bare_delete_loader_twice', mb_delete.delete_loader_bare, NameError)
mb_delete.__loader__ = None
check('none_loader_shadows_builtins', mb_delete.read_loader() is None)
mb_delete.__loader__ = loader
check('final_restoration', mb_delete.read_spec() is spec and mb_delete.read_loader() is loader)
print('complete', 'metadata_boundaries')
