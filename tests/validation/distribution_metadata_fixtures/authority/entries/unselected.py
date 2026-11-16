# CP成功参考与自动未选NI分账；不把NI当CP相等。
import sys
from importlib import metadata
points = metadata.entry_points()
selected = points.select(name='selected')
if 'neutral_pkg' in sys.modules or 'neutral_pkg.target' in sys.modules:
    raise RuntimeError('discovery initialized target')
print('discovered', len(points), len(selected), 'cold')
print('value', selected['selected'].load()())
