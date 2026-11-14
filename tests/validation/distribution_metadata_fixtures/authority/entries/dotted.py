# 父在points后真实初始化；dotted末端不冒称新finder支持。
from importlib import metadata
points = metadata.entry_points()
point = points.select(name='dotted')['dotted']
print('discovered', len(points), 'cold')
import neutral_pkg
print('parent-ready')
try:
    point.load()
except ModuleNotFoundError:
    print('dotted ModuleNotFoundError')
else:
    raise RuntimeError('unexpected dotted target load')
