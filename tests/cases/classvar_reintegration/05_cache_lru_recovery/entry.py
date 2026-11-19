# ryupy: pending
# 公开API证明四池容量/touch/失败不占位、逐池隔离与恢复后首次成功。
import typing as t
CV=t.ClassVar

def fresh(): return t.TypeVar('K')

def apply(pool,key):
    if pool=='ordinary': return t.List[key]
    if pool=='tuple': return t.Tuple[key]
    if pool=='callable': return t.Callable[[],key]
    return t.Union[key,str]

def fail(pool,key,token):
    if pool=='ordinary': return t.List[(key,token)]
    if pool=='tuple': return t.Tuple[key,token]
    if pool=='callable': return t.Callable[[token],key]
    return t.Union[key,token]

def fill(pool,count):
    for i in range(count): apply(pool,fresh())

for pool in ('ordinary','tuple','callable','special'):
    key=t.List[int]
    # 排空历史状态；每个apply恰好只占目标池一项。
    fill(pool,128)
    hot_key=fresh(); victim_key=fresh()
    hot=apply(pool,hot_key); victim=apply(pool,victim_key)
    fill(pool,126)
    assert apply(pool,hot_key) is hot
    apply(pool,fresh())
    assert apply(pool,hot_key) is hot
    assert apply(pool,victim_key) is not victim
    print(pool,'capacity-128-real-touch')
    fill(pool,128)
    survivor_key=fresh(); survivor=apply(pool,survivor_key)
    fill(pool,127)
    fail_token=fresh()
    try:
        t.ClassVar=list
        for i in range(140):
            token=fail_token if i==0 else fresh()
            try: fail(pool,key,token)
            except TypeError as error:
                assert str(error)=='typing.List[int] is not valid as type argument'
            else: assert False,pool
        assert apply(pool,survivor_key) is survivor
    finally: t.ClassVar=CV
    # ordinary失败键有两个有效参数，恢复后仍为arity错误；其余三池有首次成功。
    if pool=='ordinary':
        try: fail(pool,key,fail_token)
        except TypeError as error:
            assert str(error)=='Too many arguments for typing.List; actual 2, expected 1'
        else: assert False
        recovered=t.List[(key,)]
        assert t.List[(key,)] is recovered
    else:
        recovered=fail(pool,key,fail_token)
        assert fail(pool,key,fail_token) is recovered
    print(pool,'failed-140-distinct-do-not-evict-recovery')
    # 已暖alias参数经真实淘汰后必须重进冷检查，不是永久豁免。
    fill(pool,128)
    old=apply(pool,key)
    fill(pool,128)
    try:
        t.ClassVar=list
        try: apply(pool,key)
        except TypeError as error: assert str(error)=='typing.List[int] is not valid as type argument'
        else: assert False,pool
    finally: t.ClassVar=CV
    newer=apply(pool,key)
    assert newer==old and newer is not old
    assert apply(pool,key) is newer
    print(pool,'evicted-warm-becomes-cold')
# 四个池独立：仅令其余池产生512次成功不能淘汰本池热条目。
for pool in ('ordinary','tuple','callable','special'):
    fill(pool,128)
    key=fresh(); saved=apply(pool,key)
    for other in ('ordinary','tuple','callable','special'):
        if other!=pool: fill(other,128)
    assert apply(pool,key) is saved
    print(pool,'isolated')
