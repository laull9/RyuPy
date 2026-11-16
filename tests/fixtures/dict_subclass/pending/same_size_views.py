# ryupy: pending
# 同尺寸结构修改与view集合代数保留真实CP源，不用RuntimeError或NI当成功。
class D(dict):
    pass
d = D(a=1, b=2, c=3)
i = iter(d)
assert next(i) == 'a'
del d['b']
d['d'] = 4
assert list(i) == ['c', 'd']
assert d.keys() & {'a', 'd'} == {'a', 'd'}
assert d.items() | {('extra', 5)} == {('a', 1), ('c', 3), ('d', 4), ('extra', 5)}
proxy = d.keys().mapping
assert proxy['d'] == 4
d['d'] = 40
assert proxy['d'] == 40
print('pending.same-size-views', list(d), proxy['d'])
