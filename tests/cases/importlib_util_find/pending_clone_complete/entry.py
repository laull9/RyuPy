# ryupy: pending
# 原设计完整 clone 程序，保留所有身份/全局/异常/解析断言。
# 固定 CPython 的公开语义探针；每个模式可单独运行，无 RyuPy 调用。
import sys
import builtins
import importlib.util as util
from importlib.machinery import ModuleSpec
from types import ModuleType
builtins.importlib_design_events = []
def error(label, action):
    try:
        action()
    except Exception as exc:
        cause = exc.__cause__
        print(label, type(exc).__name__, str(exc), 'cause', None if cause is None else type(cause).__name__)
    else:
        raise AssertionError(label + ': 未抛预期异常')

import fixture_pkg.subject as original
spec = util.find_spec('fixture_pkg.subject')
clone = util.module_from_spec(spec=spec)
assert clone is not original and clone.__spec__ is spec
assert clone.__loader__ is spec.loader
assert clone.__name__ == original.__name__
assert not hasattr(clone, 'value')
assert sys.modules[spec.name] is original
result = spec.loader.exec_module(clone)
assert result is None and sys.modules[spec.name] is original
assert clone.read is not original.read and clone.Reader is not original.Reader
assert clone.read.__globals__ is clone.__dict__
assert clone.Reader.read.__globals__ is clone.__dict__
clone.write(71)
c, g = clone.closure(), clone.stream()
assert next(g) == 71
clone.value = 72
assert next(g) == 72 and c() == 72
assert clone.Reader().read() == 72 and original.read() == 10
clone.items.append('copy')
assert original.items == []
sys.modules['independent_alias'] = clone
assert __import__('independent_alias') is clone
print('身份/函数全局/类方法/闭包/生成器/容器/别名', True)
old_read, old_items = clone.read, clone.items
clone.extra = 'preserved'
spec.loader.exec_module(clone)
assert clone.extra == 'preserved' and clone.value == 10
assert clone.read is not old_read and clone.items is not old_items
assert old_read.__globals__ is clone.__dict__ and old_read() == 10
print('重复执行不清空字典', builtins.importlib_design_events)
