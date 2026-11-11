# 必须先构造points，再写hooks；NI自然逃逸，不能吞掉错误类别。
import sys
from importlib import metadata
points = metadata.entry_points()
point = points.select(name='top')['top']
print('discovered', len(points), 'cold')
sys.path_hooks = []
print('hooks-written')
try:
    point.load()
except ModuleNotFoundError:
    print('closed ModuleNotFoundError')
else:
    raise RuntimeError('unexpected closed target load')
