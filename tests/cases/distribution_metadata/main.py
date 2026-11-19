# ryupy: pending
# 真实快照/动态导入同一生产链；运行需request.json与include probe_pkg.target。
from importlib import metadata
points = metadata.entry_points(group='demo')
print(len(points))
for point in points:
    print(point.name, point.group, point.value)
print(points['same'].load())
print(points.select(name='other')['other'].load().value)
