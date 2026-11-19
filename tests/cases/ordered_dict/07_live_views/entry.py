# 正反向活视图保owner、内容/值更新与有限视图比较，不测试集合算术。
from collections import OrderedDict as OD

d = OD(a=1, b=2, c=3)
keys, values, items = d.keys(), d.values(), d.items()
print('types', type(keys).__name__, type(values).__name__, type(items).__name__)
print('repr', repr(keys), repr(values), repr(items))
print('lengths', len(keys), len(values), len(items))
print('membership', 'b' in keys, 2 in values, ('b', 2) in items, ('b', 9) in items)
d.move_to_end('a')
d['b'] = 20
print('live', list(keys), list(values), list(items))
print('reverse', list(reversed(d)), list(reversed(keys)), list(reversed(values)), list(reversed(items)))
other = OD(a=1, c=3, b=20)
print('view_eq', keys == other.keys(), items == other.items(), values == other.values(), values == values, values != values)
assert keys == other.keys() and items == other.items() and values == values
d.clear()
print('cleared', list(keys), list(values), list(items))
d['z'] = 9
print('refill', list(keys), list(values), list(items))
print('DONE live_views')
