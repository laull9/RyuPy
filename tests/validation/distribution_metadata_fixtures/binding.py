# ryupy: pending
# 首次metadata初始化捕获父函数，之后metadata和target重绑仍是动态普通调用。
import importlib
original = importlib.import_module
calls = []
def before(name, package=None):
    calls.append('before')
    return original(name, package)
importlib.import_module = before
from importlib import metadata
print('captured', metadata.import_module is before)
importlib.import_module = original
point = metadata.entry_points(group='demo', name='same')['same']
print('called', point.load()(3))
print('calls', calls)
def after(name, package=None):
    calls.append('after')
    return original(name, package)
metadata.import_module = after
print('called', point.load()(5))
print('calls', calls)
importlib.import_module = before
print('called', point.load()(7))
print('calls', calls)
