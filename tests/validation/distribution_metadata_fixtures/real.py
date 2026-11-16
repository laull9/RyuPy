# ryupy: pending
# 完整真实分发发现，与Markdown执行入口分开；19条不等于18插件组。
import sys
from importlib import metadata
points = metadata.entry_points()
print('before', 'markdown' in sys.modules)
print('total', len(points))
for point in points:
    print(point.group, point.name, point.value)
print('extensions', len(points.select(group='markdown.extensions')))
print('console', len(points.select(group='console_scripts')))
print('after', 'markdown' in sys.modules)
