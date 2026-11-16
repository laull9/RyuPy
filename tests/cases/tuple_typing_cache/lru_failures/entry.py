# 128项真实LRU在触碰后淘汰；重复失败不占位、不驱逐，重建不复活旧对象。
import typing as t
keys=[t.TypeVar('T') for i in range(128)]
values=[t.Tuple[k] for k in keys]
assert t.Tuple[keys[0]] is values[0]
for i in range(160):
    try:
        t.Tuple[(int,),(t.TypeVar('Bad'),)]
    except TypeError:
        pass
    else:
        assert False
assert t.Tuple[keys[1]] is values[1]
# 第二项刚被触碰，第三项才是下一次驱逐对象。
new=t.Tuple[t.TypeVar('New')]
assert t.Tuple[keys[0]] is values[0]
assert t.Tuple[keys[1]] is values[1]
rebuilt=t.Tuple[keys[2]]
assert rebuilt is not values[2] and rebuilt == values[2]
print('raw128-touch-failure-no-eviction-no-resurrection',True)
