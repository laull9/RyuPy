# ryupy: pending
# 原设计完整 failure 程序，保留所有身份/全局/异常/解析断言。
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

spec = util.find_spec('fixture_pkg.broken')
m = util.module_from_spec(spec)
error('首次直接exec', lambda: spec.loader.exec_module(m))
assert m.started and spec.name not in sys.modules
sys.modules[spec.name] = m
error('缓存后直接exec', lambda: spec.loader.exec_module(m))
assert sys.modules[spec.name] is m
del sys.modules[spec.name]
error('普通import失败', lambda: __import__(spec.name))
assert spec.name not in sys.modules and 'fixture_pkg' in sys.modules
print('直接exec不管理缓存；普通import清失败缓存', True)
