# find_spec 完整源已通过冻结 runtime 的真实 PE 差分。
# find_spec 合同：断言保留 CPython 行为，不用显式边界改写 CP 结果。
import sys
import importlib.util as util

def check(label, value):
    assert value, label
    print(label, value)

def error(label, action, expected, message=None):
    try:
        action()
    except Exception as exc:
        assert type(exc) is expected, (label, type(exc))
        if message is not None:
            assert str(exc) == message, (label, str(exc))
        print(label, type(exc).__name__, str(exc))
        return exc
    raise AssertionError(label)

# 本例只读取 sys.modules；保留原完整 cache_contract 另测缓存写入和删除。
import fs_cached
original = fs_cached.__spec__
original_name = original.name
check('cached_original', util.find_spec('fs_cached') is original)
check('module_cache_readonly', sys.modules['fs_cached'] is fs_cached)
original.name = 'mutable.name'
check('cached_no_revalidation', util.find_spec('fs_cached') is original)
marker = []
fs_cached.__spec__ = marker
check('arbitrary_spec_identity', util.find_spec(name='fs_cached') is marker)
marker.append('visible')
check('arbitrary_spec_alias', util.find_spec('fs_cached') == ['visible'])
for value in (False, 0, '', (), {}):
    fs_cached.__spec__ = value
    check('false_spec_is_not_none', util.find_spec('fs_cached') is value)
fs_cached.__spec__ = None
error('none_spec', lambda: util.find_spec('fs_cached'), ValueError, 'fs_cached.__spec__ is None')
fs_cached.__spec__ = original
original.name = original_name
check('restored_original', util.find_spec('fs_cached') is original and original.name == 'fs_cached')
check('cache_object_unchanged', sys.modules['fs_cached'] is fs_cached)
