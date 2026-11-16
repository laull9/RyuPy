# CP搜索域不含sources；产品-I可见源却不include，恢复封闭注册域旧合同。
from importlib import metadata
points = metadata.entry_points()
point = points.select(name='top')['top']
print('discovered', len(points), 'cold')
try:
    point.load()
except ModuleNotFoundError:
    print('closed ModuleNotFoundError')
else:
    raise RuntimeError('unexpected closed target load')
