# ryupy: pending
# 新 exec namespace 与已有模块 frame 不同，保留真实 globals 名对象。
import typing as t
for value in (None, 0, False, [], {}, 'actual_module'):
    namespace = {'__name__': value, 't': t}
    exec("V = t.TypeVar('V')", namespace)
    assert namespace['V'].__module__ is value
namespace = {'t': t}
exec("V = t.TypeVar('V')", namespace)
assert namespace['V'].__module__ is None
print('new-namespace', True)
