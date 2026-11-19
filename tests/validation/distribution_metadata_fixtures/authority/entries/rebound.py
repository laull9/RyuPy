# 已保存bound load必须读取metadata当时的import_module，并逐次取属性。
import sys
from importlib import metadata
points = metadata.entry_points()
load = points['top'].load
print('discovered', len(points), 'cold')
class Replacement:
    token = 'first'
calls = []
def replacement(name):
    calls.append(name)
    return Replacement
sys.path_hooks = []
metadata.import_module = replacement
print('loaded', load())
Replacement.token = 'second'
print('loaded', load())
print('calls', calls)
