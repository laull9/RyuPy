# 活跃源应用：首次固定CP/JIT差分已通过；完整门禁仍由父任务验收。
# CP由driver以-S独立进程执行；先导入本地types，以哨兵确认未命中启动缓存。
import types
from types import MappingProxyType as saved
assert types.origin == 'mappingproxy-shadow-source'
assert types.__file__.endswith('types.py')
assert saved({'value': 1}) == ('local', 1)
assert types.calls == [1]
print('P4.origin', types.origin)
def replacement(mapping):
    return ('replacement', mapping['value'])
types.MappingProxyType = replacement
assert types.MappingProxyType({'value': 2}) == ('replacement', 2)
assert saved({'value': 3}) == ('local', 3)
assert types.calls == [1, 3]
print('P4.rebind', types.calls)
print('P4.complete', True)
