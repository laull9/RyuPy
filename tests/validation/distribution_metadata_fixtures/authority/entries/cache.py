# points建立后预热真实目标，再改hooks；Cache不得被新增fallback guard吞掉。
import sys
from importlib import metadata
points = metadata.entry_points()
point = points['selected']
print('discovered', len(points), 'cold')
import neutral_pkg.target
saved = neutral_pkg.target.value
sys.path_hooks = []
if point.load() is not saved:
    raise RuntimeError('cached target changed')
print('cached', point.load()(), True)
