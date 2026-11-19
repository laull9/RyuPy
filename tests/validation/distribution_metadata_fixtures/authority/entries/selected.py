# 显式/自动共用同字节入口；无include的literal-group冷候选。
import sys
from importlib import metadata
points = metadata.entry_points(group='chosen')
selected = points.select(name='selected')
if 'neutral_pkg' in sys.modules or 'neutral_pkg.target' in sys.modules:
    raise RuntimeError('discovery initialized target')
print('discovered', len(points), len(selected), 'cold')
load = selected['selected'].load
value = load()
print('value', value())
if load() is not value:
    raise RuntimeError('load identity changed')
print('same', True)
