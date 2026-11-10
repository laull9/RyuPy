# ryupy: pending
# 同源CP/JIT/Native完整闭环；静态include不等于提前执行插件。
import importlib
from importlib import metadata
points = metadata.entry_points()
print('discovered', len(points), bool(points))
rows = []
index = 0
for point in points:
    if index < 3:
        print('first-root-order', point.name)
    rows.append((point.group, point.name, point.value))
    index += 1
for group, name, value in sorted(rows):
    print(group, name, value)
print('selected', len(points.select(group='demo', name='same')))
point = points.select(group='demo')['same']
load = point.load
factory = load()
print('called', factory(**{'value': 9}))
module = importlib.import_module('probe_pkg.target')
print('cached', module is importlib.import_module('.target', 'probe_pkg'))
print('same-callable', load() is factory)
print('attribute', points['other'].load())
module.Box.value = 'rebound'
print('attribute', points['other'].load())
for name in ['X', 'missing_attr', 'path_key']:
    try:
        points[name].load()
    except ModuleNotFoundError:
        print(name, 'ModuleNotFoundError')
    except AttributeError:
        print(name, 'AttributeError')
try:
    points['shadowed']
except KeyError:
    print('shadowed', 'KeyError')
