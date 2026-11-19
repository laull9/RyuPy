# ryupy: pending
# 十一个根各自真实冷origin比较：暖键保留、新raw拒绝、恢复后首次成功。
import typing as t
CV=t.ClassVar
roots=((t.List,1),(t.Dict,2),(t.Set,1),(t.FrozenSet,1),
       (t.Iterable,1),(t.Iterator,1),(t.Sequence,1),(t.Mapping,2),
       (t.MutableMapping,2),(t.Collection,1),(t.Container,1))
for root,arity in roots:
    token=t.TypeVar('Fresh')
    key=t.List[token]
    warm_raw=key if arity==1 else (None,key)
    cold_raw=(key,) if arity==1 else (type(None),key)
    warm=root[warm_raw]
    try:
        t.ClassVar=list
        assert root[warm_raw] is warm
        for repeat in range(2):
            try: root[cold_raw]
            except TypeError as error:
                assert str(error)==repr(key)+' is not valid as type argument'
            else: assert False
    finally: t.ClassVar=CV
    recovered=root[cold_raw]
    assert recovered==warm and recovered is not warm
    assert root[cold_raw] is recovered
    for replacement in (list,'changed',None):
        # 新alias确保没有另一轮成功缓存，旧ClassVar origin不等于当前replacement。
        old_cv=CV[t.TypeVar('Saved')]
        raw=(old_cv,) if arity==1 else (int,old_cv)
        try:
            t.ClassVar=replacement
            alias=root[raw]
            assert alias.__args__==raw
        finally: t.ClassVar=CV
    print(root.__name__,'warm/new-raw/error/recovery/current-origin',True)
print('all-eleven-rebound-origin',len(roots))
