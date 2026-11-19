# ryupy: pending
# 真实视图与set比较暂缓；原主views失败留证后独立保CP合同。
class Table(dict):
    pass
t = Table(a=1, b=2, c=3)
assert t.keys() == {'a', 'b', 'c'}
assert t.items() == {('a', 1), ('b', 2), ('c', 3)}
print('pending.view-set-comparison', list(t))
